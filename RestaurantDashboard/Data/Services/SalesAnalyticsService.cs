using Dapper;
using Microsoft.Data.SqlClient;
using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public interface ISalesAnalyticsService
{
    Task<OverviewMetrics> GetOverviewMetricsAsync(DateTime startDate, DateTime endDate, Guid? locationId = null);
    Task<List<SalesDataPoint>> GetSalesOverTimeAsync(DateTime startDate, DateTime endDate, Guid? locationId = null, string interval = "day");
    Task<RevenueCostAnalysis> GetRevenueCostAnalysisAsync(DateTime startDate, DateTime endDate, Guid? locationId = null);
}

public class SalesAnalyticsService : ISalesAnalyticsService
{
    private readonly string _connectionString;

    public SalesAnalyticsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<OverviewMetrics> GetOverviewMetricsAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var locationFilter = locationId.HasValue ? "AND s.LocationId = @LocationId" : "";

        var sql = $@"
            SELECT 
                COUNT(DISTINCT o.Id) as TotalOrders,
                COALESCE(SUM(o.TotalAmount), 0) as TotalRevenue,
                COALESCE(SUM(o.TotalAmount) / NULLIF(COUNT(DISTINCT o.Id), 0), 0) as AverageOrderValue,
                (SELECT COUNT(DISTINCT s2.LocationId) FROM Orders o2 
                 LEFT JOIN Shifts s2 ON o2.ShiftId = s2.Id 
                 WHERE o2.CreatedAt BETWEEN @StartDate AND @EndDate {locationFilter.Replace("s.LocationId", "s2.LocationId")}) as ActiveLocations
            FROM Orders o
            LEFT JOIN Shifts s ON o.ShiftId = s.Id
            WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate {locationFilter}";

        var result = await connection.QuerySingleOrDefaultAsync<OverviewMetrics>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });
        return result ?? new OverviewMetrics();
    }

    public async Task<List<SalesDataPoint>> GetSalesOverTimeAsync(DateTime startDate, DateTime endDate, Guid? locationId = null, string interval = "day")
    {
        using var connection = new SqlConnection(_connectionString);

        var locationFilter = locationId.HasValue ? "AND s.LocationId = @LocationId" : "";

        var dateFormat = interval.ToLower() switch
        {
            "hour" => "CONVERT(VARCHAR, o.CreatedAt, 120)",
            "day" => "CONVERT(DATE, o.CreatedAt)",
            "week" => "DATEADD(week, DATEDIFF(week, 0, o.CreatedAt), 0)",
            "month" => "DATEFROMPARTS(YEAR(o.CreatedAt), MONTH(o.CreatedAt), 1)",
            _ => "CONVERT(DATE, o.CreatedAt)"
        };

        var sql = $@"
            WITH OrdersInPeriod AS (
                SELECT
                    o.Id,
                    o.CreatedAt,
                    o.TotalAmount,
                    s.LocationId
                FROM Orders o
                JOIN Shifts s ON o.ShiftId = s.Id
                WHERE o.CreatedAt >= @StartDate
                  AND o.CreatedAt < @EndDate
                  AND (@LocationId IS NULL OR s.LocationId = @LocationId)
            ),
            OrderComponentCosts AS (
                SELECT
                    o.Id AS OrderId,
                    o.CreatedAt,
                    mic.InventoryItemId,
                    mic.Quantity AS ComponentQuantity,
                    (
                        SELECT TOP 1
                            icr.TotalCost / NULLIF(icr.QuantityReceived, 0)
                        FROM InventoryCostRecords icr
                        WHERE icr.InventoryItemId = mic.InventoryItemId
                          AND icr.RecordedAt <= o.CreatedAt
                        ORDER BY icr.RecordedAt DESC
                    ) AS UnitCost
                FROM OrdersInPeriod o
                JOIN OrderLines ol ON ol.OrderId = o.Id
                JOIN MenuItemComponents mic ON mic.MenuItemId = ol.MenuItemId
            ),
            OrderCosts AS (
                SELECT
                    OrderId,
                    SUM(ComponentQuantity * COALESCE(UnitCost, 0)) AS OrderCost
                FROM OrderComponentCosts
                GROUP BY OrderId
            )
            SELECT
                {dateFormat} AS Period,
                COUNT(DISTINCT o.Id) AS OrderCount,
                SUM(o.TotalAmount) AS TotalRevenue,
                SUM(oc.OrderCost) AS TotalCost,
                SUM(o.TotalAmount) - SUM(oc.OrderCost) AS Profit,
                CASE
                    WHEN SUM(o.TotalAmount) > 0 THEN
                        ((SUM(o.TotalAmount) - SUM(oc.OrderCost)) / SUM(o.TotalAmount)) * 100
                    ELSE 0
                END AS ProfitMargin
            FROM OrdersInPeriod o
            LEFT JOIN OrderCosts oc ON oc.OrderId = o.Id
            GROUP BY {dateFormat}
            ORDER BY Period;";

        var results = await connection.QueryAsync<dynamic>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });

        return results.Select(r => new SalesDataPoint
        {
            Period = r.Period.ToString(),
            OrderCount = (int)r.OrderCount,
            TotalRevenue = (decimal?)r.TotalRevenue ?? 0,
            AverageOrderValue = (decimal?)r.AverageOrderValue ?? 0,
            TotalCost = (decimal?)r.TotalCost ?? 0,
            TotalProfit = (decimal?)r.Profit ?? 0
        }).ToList();
    }

    public async Task<RevenueCostAnalysis> GetRevenueCostAnalysisAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var locationFilterRevenue = locationId.HasValue ? "AND s.LocationId = @LocationId" : "";
        var locationFilterCost = locationId.HasValue ? "AND LocationId = @LocationId" : "";

        // Get revenue
        var revenueSql = $@"
            SELECT COALESCE(SUM(o.TotalAmount), 0) as TotalRevenue
            FROM Orders o
            LEFT JOIN Shifts s ON o.ShiftId = s.Id
            WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate {locationFilterRevenue}";

        var revenue = await connection.QuerySingleOrDefaultAsync<decimal>(revenueSql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });

        // Get costs
        var costSql = $@"
            SELECT COALESCE(SUM(TotalCost), 0) as TotalCost
            FROM InventoryCostRecords
            WHERE RecordedAt BETWEEN @StartDate AND @EndDate {locationFilterCost}";

        var cost = await connection.QuerySingleOrDefaultAsync<decimal>(costSql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });

        return new RevenueCostAnalysis
        {
            TotalRevenue = revenue,
            TotalCost = cost,
            GrossProfit = revenue - cost,
            ProfitMargin = revenue > 0 ? ((revenue - cost) / revenue) * 100 : 0
        };
    }
}

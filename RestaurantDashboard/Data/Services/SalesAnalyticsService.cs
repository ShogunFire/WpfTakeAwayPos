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

        var locationFilter = locationId.HasValue ? "AND o.LocationId = @LocationId" : "";
        var subqueryLocationFilter = locationId.HasValue ? "AND LocationId = @LocationId" : "";

        var sql = $@"
            SELECT 
                COUNT(DISTINCT o.Id) as TotalOrders,
                COALESCE(SUM(o.TotalAmount), 0) as TotalRevenue,
                COALESCE(SUM(o.TotalAmount) / NULLIF(COUNT(DISTINCT o.Id), 0), 0) as AverageOrderValue,
                (SELECT COUNT(DISTINCT LocationId) FROM Orders 
                 WHERE CreatedAt BETWEEN @StartDate AND @EndDate {subqueryLocationFilter}) as ActiveLocations
            FROM Orders o
            WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate {locationFilter}";

        var result = await connection.QuerySingleOrDefaultAsync<OverviewMetrics>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });
        return result ?? new OverviewMetrics();
    }

    public async Task<List<SalesDataPoint>> GetSalesOverTimeAsync(DateTime startDate, DateTime endDate, Guid? locationId = null, string interval = "day")
    {
        using var connection = new SqlConnection(_connectionString);

        var locationFilter = locationId.HasValue ? "AND o.LocationId = @LocationId" : "";

        var dateFormat = interval.ToLower() switch
        {
            "hour" => "CONVERT(VARCHAR, o.CreatedAt, 120)",
            "day" => "CONVERT(DATE, o.CreatedAt)",
            "week" => "DATEADD(week, DATEDIFF(week, 0, o.CreatedAt), 0)",
            "month" => "DATEFROMPARTS(YEAR(o.CreatedAt), MONTH(o.CreatedAt), 1)",
            _ => "CONVERT(DATE, o.CreatedAt)"
        };

        var sql = $@"
            SELECT
                {dateFormat} AS Period,
                COUNT(DISTINCT o.Id) AS OrderCount,
                SUM(o.TotalAmount) AS TotalRevenue,
                SUM(o.TotalCOGS) AS TotalCost,
                SUM(o.GrossProfit) AS Profit
            FROM Orders o
            WHERE o.CreatedAt >= @StartDate
              AND o.CreatedAt < @EndDate
              AND (@LocationId IS NULL OR o.LocationId = @LocationId)
            GROUP BY {dateFormat}
            ORDER BY Period;";

        var results = await connection.QueryAsync<dynamic>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });

        return results.Select(r => new SalesDataPoint
        {
            Period = r.Period.ToString(),
            OrderCount = (int)r.OrderCount,
            TotalRevenue = (decimal?)r.TotalRevenue ?? 0,
            AverageOrderValue = 0, // Can be calculated from results if needed
            TotalCost = (decimal?)r.TotalCost ?? 0,
            TotalProfit = (decimal?)r.Profit ?? 0
        }).ToList();
    }

    public async Task<RevenueCostAnalysis> GetRevenueCostAnalysisAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var locationFilter = locationId.HasValue ? "AND o.LocationId = @LocationId" : "";

        var sql = $@"
            SELECT 
                COALESCE(SUM(o.TotalAmount), 0) as TotalRevenue,
                COALESCE(SUM(o.TotalCOGS), 0) as TotalCost,
                COALESCE(SUM(o.GrossProfit), 0) as GrossProfit
            FROM Orders o
            WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate {locationFilter}";

        var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });

        decimal revenue = result?.TotalRevenue ?? 0;
        decimal cost = result?.TotalCost ?? 0;
        decimal grossProfit = result?.GrossProfit ?? 0;

        return new RevenueCostAnalysis
        {
            TotalRevenue = revenue,
            TotalCost = cost,
            GrossProfit = grossProfit,
            ProfitMargin = revenue > 0 ? ((grossProfit / revenue) * 100) : 0
        };
    }
}

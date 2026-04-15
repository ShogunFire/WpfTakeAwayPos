using Dapper;
using Microsoft.Data.SqlClient;
using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public interface IMenuItemAnalyticsService
{
    Task<List<MenuItemPerformance>> GetMenuItemPerformanceAsync(DateTime startDate, DateTime endDate, Guid? locationId = null);
    Task<List<MenuItemOption>> GetMenuItemsForPeriodAsync(DateTime startDate, DateTime endDate, Guid? locationId = null);
    Task<List<MenuItemTrendPoint>> GetMenuItemTrendAsync(DateTime startDate, DateTime endDate, Guid menuItemId, Guid? locationId = null, string interval = "day");
}

public class MenuItemAnalyticsService : IMenuItemAnalyticsService
{
    private readonly string _connectionString;

    public MenuItemAnalyticsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<List<MenuItemPerformance>> GetMenuItemPerformanceAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = $@"
            SELECT
                ol.MenuItemId,
                ol.MenuItemName,
                SUM(ol.Quantity) AS TotalQuantity,
                SUM(ol.LineTotal) AS TotalRevenue,
                AVG(CAST(ol.UnitPrice AS DECIMAL(18,2))) AS AveragePrice,
                COALESCE(SUM(hc.UnitCost * ol.Quantity), 0) AS TotalCost,
                COALESCE(AVG(hc.UnitCost), 0) AS AverageCost,
                CASE
                    WHEN SUM(ol.LineTotal) > 0 THEN
                        ((SUM(ol.LineTotal) - COALESCE(SUM(hc.UnitCost * ol.Quantity), 0)) / SUM(ol.LineTotal)) * 100
                    ELSE 0
                END AS ProfitMargin
            FROM Orders o
            JOIN OrderLines ol ON o.Id = ol.OrderId
            OUTER APPLY (
                SELECT TOP 1 h.UnitCost
                FROM MenuItemGrossProfitHistory h
                WHERE h.MenuItemId = ol.MenuItemId
                  AND h.SnapshotDate <= o.CreatedAt
                ORDER BY h.SnapshotDate DESC
            ) hc
            WHERE o.CreatedAt >= @StartDate
              AND o.CreatedAt < @EndDate
              AND (@LocationId IS NULL OR o.LocationId = @LocationId)
            GROUP BY ol.MenuItemId, ol.MenuItemName
            ORDER BY TotalRevenue DESC;";

        var results = await connection.QueryAsync<MenuItemPerformance>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });
        return results.ToList();
    }

    public async Task<List<MenuItemOption>> GetMenuItemsForPeriodAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT DISTINCT
                ol.MenuItemId,
                ol.MenuItemName
            FROM Orders o
            JOIN OrderLines ol ON o.Id = ol.OrderId
            LEFT JOIN Shifts s ON o.ShiftId = s.Id
            WHERE o.CreatedAt >= @StartDate
              AND o.CreatedAt < @EndDate
              AND (@LocationId IS NULL OR s.LocationId = @LocationId)
            ORDER BY ol.MenuItemName";

        var results = await connection.QueryAsync<MenuItemOption>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });
        return results.ToList();
    }

    public async Task<List<MenuItemTrendPoint>> GetMenuItemTrendAsync(DateTime startDate, DateTime endDate, Guid menuItemId, Guid? locationId = null, string interval = "day")
    {
        using var connection = new SqlConnection(_connectionString);

        var dateFormat = interval.ToLower() switch
        {
            "hour" => "CONVERT(VARCHAR, h.SnapshotDate, 120)",
            "day" => "CONVERT(DATE, h.SnapshotDate)",
            "week" => "DATEADD(week, DATEDIFF(week, 0, h.SnapshotDate), 0)",
            "month" => "DATEFROMPARTS(YEAR(h.SnapshotDate), MONTH(h.SnapshotDate), 1)",
            _ => "CONVERT(DATE, h.SnapshotDate)"
        };

        var sql = $@"
            SELECT
                {dateFormat} AS Period,
                AVG(h.Price) AS Price,
                AVG(h.UnitCost) AS Cost,
                AVG(h.GrossProfit) AS Profit,
                AVG(h.GrossMargin) AS Margin
            FROM MenuItemGrossProfitHistory h
            WHERE h.SnapshotDate >= @StartDate
              AND h.SnapshotDate < @EndDate
              AND h.MenuItemId = @MenuItemId
            GROUP BY {dateFormat}
            ORDER BY Period;";

        var results = await connection.QueryAsync<MenuItemTrendPoint>(sql, new { StartDate = startDate, EndDate = endDate, MenuItemId = menuItemId });
        return results.ToList();
    }
}

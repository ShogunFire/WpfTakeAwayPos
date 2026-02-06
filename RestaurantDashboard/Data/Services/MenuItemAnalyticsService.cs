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

        var locationFilter = locationId.HasValue ? "AND s.LocationId = @LocationId" : "";

        var sql = $@"
            WITH MenuItemComponentCosts AS (
                SELECT
                    ol.OrderId,
                    ol.MenuItemId,
                    ol.MenuItemName,
                    ol.Quantity as OrderLineQuantity,
                    ol.UnitPrice,
                    ol.LineTotal,
                    mic.InventoryItemId,
                    mic.Quantity as ComponentQuantity,
                    -- Get the most recent delivery cost BEFORE this order
                    COALESCE(
                        (SELECT TOP 1 icr.TotalCost / NULLIF(icr.QuantityReceived, 0)
                         FROM InventoryCostRecords icr
                         WHERE icr.InventoryItemId = mic.InventoryItemId
                             AND icr.RecordedAt <= (SELECT o2.CreatedAt FROM Orders o2 WHERE o2.Id = ol.OrderId)
                         ORDER BY icr.RecordedAt DESC),
                        0
                    ) as ComponentUnitCost
                FROM OrderLines ol
                JOIN Orders o ON ol.OrderId = o.Id
                JOIN Shifts s ON o.ShiftId = s.Id
                LEFT JOIN MenuItemComponents mic ON ol.MenuItemId = mic.MenuItemId
                WHERE o.CreatedAt >= @StartDate
                  AND o.CreatedAt < @EndDate
                  AND (@LocationId IS NULL OR s.LocationId = @LocationId)
            ),
            MenuItemCostsPerOrder AS (
                SELECT
                    OrderId,
                    MenuItemId,
                    MenuItemName,
                    OrderLineQuantity,
                    UnitPrice,
                    LineTotal,
                    SUM(ComponentQuantity * ComponentUnitCost) as ItemCostPerUnit
                FROM MenuItemComponentCosts
                GROUP BY OrderId, MenuItemId, MenuItemName, OrderLineQuantity, UnitPrice, LineTotal
            )
            SELECT
                MenuItemId,
                MenuItemName,
                SUM(OrderLineQuantity) AS TotalQuantity,
                SUM(LineTotal) AS TotalRevenue,
                SUM(UnitPrice * OrderLineQuantity) / NULLIF(SUM(OrderLineQuantity), 0) AS AveragePrice,
                COALESCE(SUM(ItemCostPerUnit * OrderLineQuantity), 0) AS TotalCost,
                COALESCE(SUM(ItemCostPerUnit * OrderLineQuantity) / NULLIF(SUM(OrderLineQuantity), 0), 0) AS AverageCost,
                CASE
                    WHEN SUM(LineTotal) > 0 THEN
                        ((SUM(LineTotal) - COALESCE(SUM(ItemCostPerUnit * OrderLineQuantity), 0)) / SUM(LineTotal)) * 100
                    ELSE 0
                END AS ProfitMargin
            FROM MenuItemCostsPerOrder
            GROUP BY MenuItemId, MenuItemName
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
            "hour" => "CONVERT(VARCHAR, olip.CreatedAt, 120)",
            "day" => "CONVERT(DATE, olip.CreatedAt)",
            "week" => "DATEADD(week, DATEDIFF(week, 0, olip.CreatedAt), 0)",
            "month" => "DATEFROMPARTS(YEAR(olip.CreatedAt), MONTH(olip.CreatedAt), 1)",
            _ => "CONVERT(DATE, olip.CreatedAt)"
        };

        var sql = $@"
            WITH OrdersInPeriod AS (
                SELECT
                    o.Id,
                    o.CreatedAt,
                    s.LocationId
                FROM Orders o
                JOIN Shifts s ON o.ShiftId = s.Id
                WHERE o.CreatedAt >= @StartDate
                  AND o.CreatedAt < @EndDate
                  AND (@LocationId IS NULL OR s.LocationId = @LocationId)
            ),
            OrderLinesInPeriod AS (
                SELECT
                    ol.OrderId,
                    ol.MenuItemId,
                    ol.Quantity,
                    ol.UnitPrice,
                    ol.LineTotal,
                    o.CreatedAt
                FROM OrdersInPeriod o
                JOIN OrderLines ol ON ol.OrderId = o.Id
                WHERE ol.MenuItemId = @MenuItemId
            ),
            LineComponentCosts AS (
                SELECT
                    olip.OrderId,
                    olip.CreatedAt,
                    olip.LineTotal,
                    olip.Quantity,
                    mic.Quantity AS ComponentQuantity,
                    (SELECT TOP 1 icr.TotalCost / NULLIF(icr.QuantityReceived, 0)
                     FROM InventoryCostRecords icr
                     WHERE icr.InventoryItemId = mic.InventoryItemId
                       AND icr.RecordedAt <= olip.CreatedAt
                     ORDER BY icr.RecordedAt DESC) AS UnitCost
                FROM OrderLinesInPeriod olip
                JOIN MenuItemComponents mic ON mic.MenuItemId = olip.MenuItemId
            ),
            LineCosts AS (
                SELECT
                    OrderId,
                    CreatedAt,
                    LineTotal,
                    Quantity,
                    SUM(ComponentQuantity * COALESCE(UnitCost, 0)) AS ItemCostPerUnit
                FROM LineComponentCosts
                GROUP BY OrderId, CreatedAt, LineTotal, Quantity
            )
            SELECT
                {dateFormat} AS Period,
                SUM(LineTotal) AS TotalRevenue,
                SUM(ItemCostPerUnit * Quantity) AS TotalCost,
                SUM(LineTotal) - SUM(ItemCostPerUnit * Quantity) AS TotalProfit
            FROM LineCosts olip
            GROUP BY {dateFormat}
            ORDER BY Period;";

        var results = await connection.QueryAsync<MenuItemTrendPoint>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId, MenuItemId = menuItemId });
        return results.ToList();
    }
}

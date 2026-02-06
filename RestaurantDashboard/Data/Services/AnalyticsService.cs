using Dapper;
using Microsoft.Data.SqlClient;

namespace RestaurantDashboard.Data.Services;

public class AnalyticsService
{
    private readonly string _connectionString;

    public AnalyticsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found");
    }

    // Overview Metrics
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

    // Sales Data Over Time
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
            SELECT 
                {dateFormat} as Period,
                COUNT(*) as OrderCount,
                SUM(o.TotalAmount) as TotalSales,
                AVG(o.TotalAmount) as AverageOrderValue
            FROM Orders o
            LEFT JOIN Shifts s ON o.ShiftId = s.Id
            WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate {locationFilter}
            GROUP BY {dateFormat}
            ORDER BY Period";

        var results = await connection.QueryAsync<dynamic>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });
        
        return results.Select(r => new SalesDataPoint
        {
            Period = r.Period.ToString(),
            OrderCount = (int)r.OrderCount,
            TotalSales = (decimal)r.TotalSales,
            AverageOrderValue = (decimal)r.AverageOrderValue
        }).ToList();
    }

    // Menu Item Performance
    public async Task<List<MenuItemPerformance>> GetMenuItemPerformanceAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var locationFilter = locationId.HasValue ? "AND s.LocationId = @LocationId" : "";
        
        var sql = $@"
            WITH InventoryAverageCost AS (
    SELECT
        InventoryItemId,
        SUM(TotalCost) / NULLIF(SUM(QuantityReceived), 0) AS AverageCost
    FROM InventoryCostRecords
    WHERE ReceivedAt <= @EndDate
    GROUP BY InventoryItemId
),
MenuItemCosts AS (
    SELECT
        mic.MenuItemId,
        SUM(mic.Quantity * COALESCE(iac.AverageCost, 0)) AS AverageCost
    FROM MenuItemComponents mic
    LEFT JOIN InventoryAverageCost iac
        ON mic.InventoryItemId = iac.InventoryItemId
    GROUP BY mic.MenuItemId
)
SELECT
    ol.MenuItemId,
    ol.MenuItemName,

    SUM(ol.Quantity) AS TotalQuantity,
    SUM(ol.LineTotal) AS TotalRevenue,

    -- weighted avg selling price
    SUM(ol.UnitPrice * ol.Quantity)
        / NULLIF(SUM(ol.Quantity), 0) AS AveragePrice,

    COALESCE(mc.AverageCost, 0) AS AverageCost,

    CASE
        WHEN SUM(ol.UnitPrice * ol.Quantity) > 0 THEN
            (
                (
                    (SUM(ol.UnitPrice * ol.Quantity) / SUM(ol.Quantity))
                    - COALESCE(mc.AverageCost, 0)
                )
                /
                (SUM(ol.UnitPrice * ol.Quantity) / SUM(ol.Quantity))
            ) * 100
        ELSE 0
    END AS ProfitMargin

FROM OrderLines ol
JOIN Orders o ON ol.OrderId = o.Id
LEFT JOIN MenuItemCosts mc ON ol.MenuItemId = mc.MenuItemId
WHERE o.CreatedAt >= @StartDate
  AND o.CreatedAt < @EndDate
  {locationFilter}
GROUP BY
    ol.MenuItemId,
    ol.MenuItemName,
    mc.AverageCost
ORDER BY TotalRevenue DESC;";

        var results = await connection.QueryAsync<MenuItemPerformance>(sql, new { StartDate = startDate, EndDate = endDate, LocationId = locationId });
        return results.ToList();
    }

    // Inventory Status
    public async Task<List<InventoryStatus>> GetInventoryStatusAsync(Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var locationFilter = locationId.HasValue ? "AND li.LocationId = @LocationId" : "";
        
        var sql = $@"
            SELECT 
                ii.Id as InventoryItemId,
                ii.Name,
                ii.Unit,
                COALESCE(SUM(li.Quantity), 0) as CurrentQuantity,
                @LocationId as LocationId
            FROM InventoryItems ii
            LEFT JOIN LocationInventory li ON ii.Id = li.InventoryItemId {locationFilter}
            GROUP BY ii.Id, ii.Name, ii.Unit
            ORDER BY ii.Name";

        var results = await connection.QueryAsync<InventoryStatus>(sql, new { LocationId = locationId });
        return results.ToList();
    }

    // Location Performance Comparison
    public async Task<List<LocationPerformance>> GetLocationPerformanceAsync(DateTime startDate, DateTime endDate)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT 
                l.Id as LocationId,
                l.Name as LocationName,
                COUNT(DISTINCT o.Id) as TotalOrders,
                COALESCE(SUM(o.TotalAmount), 0) as TotalRevenue,
                COALESCE(AVG(o.TotalAmount), 0) as AverageOrderValue
            FROM Locations l
            LEFT JOIN Shifts s ON l.Id = s.LocationId
            LEFT JOIN Orders o ON s.Id = o.ShiftId AND o.CreatedAt BETWEEN @StartDate AND @EndDate
            WHERE l.IsActive = 1
            GROUP BY l.Id, l.Name
            ORDER BY TotalRevenue DESC";

        var results = await connection.QueryAsync<LocationPerformance>(sql, new { StartDate = startDate, EndDate = endDate });
        return results.ToList();
    }

    // Revenue vs Cost Analysis
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

    // Get all locations
    public async Task<List<Location>> GetLocationsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT Id, Name, Code, IsActive FROM Locations WHERE IsActive = 1 ORDER BY Name";
        var results = await connection.QueryAsync<Location>(sql);
        return results.ToList();
    }
}

// Data models
public class OverviewMetrics
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int ActiveLocations { get; set; }
}

public class SalesDataPoint
{
    public string Period { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class MenuItemPerformance
{
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal AverageCost { get; set; }
    public decimal ProfitMargin { get; set; }
}

public class InventoryStatus
{
    public Guid InventoryItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public Guid? LocationId { get; set; }
}

public class LocationPerformance
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class RevenueCostAnalysis
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
}

public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

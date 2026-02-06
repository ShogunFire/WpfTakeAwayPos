using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

/// <summary>
/// Main analytics service facade that aggregates specialized analytics services
/// </summary>
public class AnalyticsService
{
    private readonly ISalesAnalyticsService _salesAnalytics;
    private readonly IMenuItemAnalyticsService _menuItemAnalytics;
    private readonly IInventoryAnalyticsService _inventoryAnalytics;
    private readonly ILocationAnalyticsService _locationAnalytics;

    public AnalyticsService(
        ISalesAnalyticsService salesAnalytics,
        IMenuItemAnalyticsService menuItemAnalytics,
        IInventoryAnalyticsService inventoryAnalytics,
        ILocationAnalyticsService locationAnalytics)
    {
        _salesAnalytics = salesAnalytics;
        _menuItemAnalytics = menuItemAnalytics;
        _inventoryAnalytics = inventoryAnalytics;
        _locationAnalytics = locationAnalytics;
    }

    // Sales Analytics
    public Task<OverviewMetrics> GetOverviewMetricsAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
        => _salesAnalytics.GetOverviewMetricsAsync(startDate, endDate, locationId);

    public Task<List<SalesDataPoint>> GetSalesOverTimeAsync(DateTime startDate, DateTime endDate, Guid? locationId = null, string interval = "day")
        => _salesAnalytics.GetSalesOverTimeAsync(startDate, endDate, locationId, interval);

    public Task<RevenueCostAnalysis> GetRevenueCostAnalysisAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
        => _salesAnalytics.GetRevenueCostAnalysisAsync(startDate, endDate, locationId);

    // Menu Item Analytics
    public Task<List<MenuItemPerformance>> GetMenuItemPerformanceAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
        => _menuItemAnalytics.GetMenuItemPerformanceAsync(startDate, endDate, locationId);

    public Task<List<MenuItemOption>> GetMenuItemsForPeriodAsync(DateTime startDate, DateTime endDate, Guid? locationId = null)
        => _menuItemAnalytics.GetMenuItemsForPeriodAsync(startDate, endDate, locationId);

    public Task<List<MenuItemTrendPoint>> GetMenuItemTrendAsync(DateTime startDate, DateTime endDate, Guid menuItemId, Guid? locationId = null, string interval = "day")
        => _menuItemAnalytics.GetMenuItemTrendAsync(startDate, endDate, menuItemId, locationId, interval);

    // Inventory Analytics
    public Task<List<InventoryStatus>> GetInventoryStatusAsync(Guid? locationId = null)
        => _inventoryAnalytics.GetInventoryStatusAsync(locationId);

    // Location Analytics
    public Task<List<LocationPerformance>> GetLocationPerformanceAsync(DateTime startDate, DateTime endDate)
        => _locationAnalytics.GetLocationPerformanceAsync(startDate, endDate);

    public Task<List<Location>> GetLocationsAsync()
        => _locationAnalytics.GetLocationsAsync();
}

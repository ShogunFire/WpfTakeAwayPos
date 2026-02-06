namespace RestaurantDashboard.Data.Models;

public class OverviewMetrics
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int ActiveLocations { get; set; }
}

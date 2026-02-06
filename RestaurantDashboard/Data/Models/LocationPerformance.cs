namespace RestaurantDashboard.Data.Models;

public class LocationPerformance
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

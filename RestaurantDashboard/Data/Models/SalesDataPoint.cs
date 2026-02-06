namespace RestaurantDashboard.Data.Models;

public class SalesDataPoint
{
    public string Period { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
}

namespace RestaurantDashboard.Data.Models;

public class MenuItemTrendPoint
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
}

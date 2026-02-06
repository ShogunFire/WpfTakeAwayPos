namespace RestaurantDashboard.Data.Models;

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

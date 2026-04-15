namespace RestaurantDashboard.Data.Models;

public class MenuItemTrendPoint
{
    public string Period { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal Margin { get; set; }
}

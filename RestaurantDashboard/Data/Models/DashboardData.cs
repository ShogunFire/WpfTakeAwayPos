namespace RestaurantDashboard.Data.Models;

public class DashboardMetrics
{
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageCheck { get; set; }
}

public class TopItem
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Percentage { get; set; }
}

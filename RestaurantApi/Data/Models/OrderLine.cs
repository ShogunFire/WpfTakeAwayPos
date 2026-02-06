namespace RestaurantApi.Data.Models;

public class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public Guid MenuItemId { get; set; }
}

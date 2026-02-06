namespace RestaurantDashboard.Data.Models;

public class InventoryStatus
{
    public Guid InventoryItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public Guid? LocationId { get; set; }
}

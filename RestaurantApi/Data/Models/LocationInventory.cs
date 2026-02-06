namespace RestaurantApi.Data.Models;

public class LocationInventory
{
    public Guid LocationId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

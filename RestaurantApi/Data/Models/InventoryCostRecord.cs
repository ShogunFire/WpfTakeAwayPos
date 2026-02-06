namespace RestaurantApi.Data.Models;

public class InventoryCostRecord
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid LocationId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime RecordedAt { get; set; }
}

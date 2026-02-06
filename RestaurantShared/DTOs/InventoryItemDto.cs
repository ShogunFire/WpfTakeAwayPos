using System;

namespace RestaurantShared.DTOs;

public class InventoryItemDto
{
    public Guid InventoryItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

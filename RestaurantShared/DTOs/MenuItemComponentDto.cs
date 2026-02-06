using System;

namespace RestaurantShared.DTOs;

public class MenuItemComponentDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
}

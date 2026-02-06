using System;

namespace RestaurantApi.Data.Models;

public class MenuItemComponent
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
}

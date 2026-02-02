using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RestaurantPOS.Models
{
    public partial class MenuItemComponent : ObservableObject
    {
        [ObservableProperty] private Guid inventoryItemId;
        [ObservableProperty] private decimal quantityUsed;

        public MenuItemComponent() { }

        public MenuItemComponent(Guid inventoryItemId, decimal quantityUsed)
        {
            InventoryItemId = inventoryItemId;
            QuantityUsed = quantityUsed;
        }
    }
}

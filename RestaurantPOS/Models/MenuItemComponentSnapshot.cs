using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RestaurantPOS.Models
{
    public partial class MenuItemComponentSnapshot : ObservableObject
    {
        [ObservableProperty] private Guid inventoryItemId;
        [ObservableProperty] private decimal quantityUsed;

        public MenuItemComponentSnapshot() { }

        public MenuItemComponentSnapshot(Guid inventoryItemId, decimal quantityUsed)
        {
            InventoryItemId = inventoryItemId;
            QuantityUsed = quantityUsed;
        }
    }
}

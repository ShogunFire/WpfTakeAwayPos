using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RestaurantPOS.Models
{
    public partial class InventoryItem : ObservableObject
    {
        [ObservableProperty] private long id;
        [ObservableProperty] private Guid inventoryItemId;
        [ObservableProperty] private string name;
        [ObservableProperty] private decimal quantity;
        [ObservableProperty] private string unit;

        public InventoryItem() { }

        public InventoryItem(long id, string name, decimal quantity, string unit, Guid? inventoryItemId = null)
        {
            Id = id;
            InventoryItemId = inventoryItemId ?? Guid.NewGuid();
            Name = name;
            Quantity = quantity;
            Unit = unit;
        }

        public override string ToString() => $"{Name} ({Quantity} {Unit})";
    }
}

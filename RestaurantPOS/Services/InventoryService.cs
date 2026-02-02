using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.Services
{
    public class InventoryService : IInventoryService
    {
        public ObservableCollection<InventoryItem> InventoryItems { get; }

        public InventoryService()
        {
            InventoryItems = new ObservableCollection<InventoryItem>
            {
                new InventoryItem(1, "Chicken", 50m, "unit", Guid.NewGuid()),
                new InventoryItem(2, "Fries", 20m, "kg", Guid.NewGuid()),
                new InventoryItem(3, "Coca-Cola", 100m, "bottle", Guid.NewGuid())
            };
        }

        public InventoryItem FindByName(string name)
        {
            return InventoryItems.FirstOrDefault(i => i.Name == name);
        }

        public InventoryItem FindByInventoryItemId(Guid inventoryItemId)
        {
            return InventoryItems.FirstOrDefault(i => i.InventoryItemId == inventoryItemId);
        }

        public bool TryConsume(Guid inventoryItemId, decimal quantity)
        {
            if (quantity <= 0)
                return true;

            var item = FindByInventoryItemId(inventoryItemId);
            if (item == null)
                return false;

            if (item.Quantity < quantity)
            {
                item.Quantity = 0;
                return false;
            }

            item.Quantity -= quantity;
            return true;
        }

        public void AddStock(Guid inventoryItemId, decimal quantity)
        {
            if (quantity <= 0)
                return;

            var item = FindByInventoryItemId(inventoryItemId);
            if (item == null)
                return;

            item.Quantity += quantity;
        }
    }
}

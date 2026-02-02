using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace RestaurantPOS.Services
{
    public class NoopInventoryService : IInventoryService
    {
        public ObservableCollection<InventoryItem> InventoryItems { get; } = new ObservableCollection<InventoryItem>();

        public InventoryItem FindByName(string name)
        {
            return null;
        }

        public InventoryItem FindByInventoryItemId(Guid inventoryItemId)
        {
            return null;
        }

        public bool TryConsume(Guid inventoryItemId, decimal quantity)
        {
            return true;
        }

        public void AddStock(Guid inventoryItemId, decimal quantity)
        {
            // no-op
        }
    }
}

using RestaurantPOS.Models;
using System;
using System.Collections.ObjectModel;

namespace RestaurantPOS.Services.Interfaces
{
    public interface IInventoryService
    {
        ObservableCollection<InventoryItem> InventoryItems { get; }
        InventoryItem FindByName(string name);
        InventoryItem FindByInventoryItemId(Guid inventoryItemId);
        InventoryItem InsertInventoryItem(string name, decimal quantity, string unit);
        bool TryConsume(Guid inventoryItemId, decimal quantity, string reason = null);
        void AddStock(Guid inventoryItemId, decimal quantity, string reason = null, decimal? totalCost = null, bool paidWithCash = false);
    }
}

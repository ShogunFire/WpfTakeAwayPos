using RestaurantPOS.Models;
using System;
using System.Collections.Generic;

namespace RestaurantPOS.Services.Interfaces
{
    public interface IInventoryCostService
    {
        IEnumerable<InventoryCostRecord> CostRecords { get; }
        void RecordPurchase(Guid inventoryItemId, string itemName, decimal quantity, decimal totalCost, string notes = null);
        IEnumerable<InventoryCostRecord> GetRecordsForItem(Guid inventoryItemId);
        decimal GetAverageUnitCost(Guid inventoryItemId);
        decimal GetLastUnitCost(Guid inventoryItemId);
        decimal GetTotalSpent(Guid inventoryItemId);
        IEnumerable<InventoryCostRecord> GetRecordsByDateRange(DateTime startDate, DateTime endDate);
        bool IsCostMandatory();
    }
}

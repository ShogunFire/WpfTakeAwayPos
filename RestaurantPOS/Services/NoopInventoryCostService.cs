using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RestaurantPOS.Services
{
    public class NoopInventoryCostService : IInventoryCostService
    {
        private readonly ObservableCollection<InventoryCostRecord> _records = new ObservableCollection<InventoryCostRecord>();

        public IEnumerable<InventoryCostRecord> CostRecords => _records;

        public Guid RecordPurchase(Guid inventoryItemId, string itemName, decimal quantity, decimal totalCost, string notes = null)
        {
            // no-op
            return Guid.Empty;
        }

        public IEnumerable<InventoryCostRecord> GetRecordsForItem(Guid inventoryItemId)
        {
            return _records;
        }

        public decimal GetAverageUnitCost(Guid inventoryItemId)
        {
            return 0;
        }

        public decimal GetLastUnitCost(Guid inventoryItemId)
        {
            return 0;
        }

        public decimal GetTotalSpent(Guid inventoryItemId)
        {
            return 0;
        }

        public IEnumerable<InventoryCostRecord> GetRecordsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _records;
        }

        public bool IsCostMandatory()
        {
            return false;
        }
    }
}

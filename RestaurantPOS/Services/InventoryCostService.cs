using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.Services
{
    /// <summary>
    /// Service to track inventory purchase costs and calculate cost metrics
    /// </summary>
    public class InventoryCostService : IInventoryCostService
    {
        private readonly ObservableCollection<InventoryCostRecord> _costRecords;

        public IEnumerable<InventoryCostRecord> CostRecords => _costRecords;

        public InventoryCostService()
        {
            _costRecords = new ObservableCollection<InventoryCostRecord>();
        }

        /// <summary>
        /// Records a new inventory purchase/delivery
        /// </summary>
        public void RecordPurchase(Guid inventoryItemId, string itemName, decimal quantity, decimal totalCost, string notes = null)
        {
            var record = new InventoryCostRecord
            {
                InventoryItemId = inventoryItemId,
                ItemName = itemName,
                QuantityReceived = quantity,
                TotalCost = totalCost,
                Notes = notes
            };

            _costRecords.Add(record);
        }

        /// <summary>
        /// Gets all cost records for a specific inventory item
        /// </summary>
        public IEnumerable<InventoryCostRecord> GetRecordsForItem(Guid inventoryItemId)
        {
            return _costRecords.Where(r => r.InventoryItemId == inventoryItemId).OrderByDescending(r => r.RecordedDate);
        }

        /// <summary>
        /// Calculates the average unit cost for an inventory item based on all recorded purchases
        /// </summary>
        public decimal GetAverageUnitCost(Guid inventoryItemId)
        {
            var records = GetRecordsForItem(inventoryItemId).ToList();
            if (!records.Any())
                return 0;

            var totalQuantity = records.Sum(r => r.QuantityReceived);
            var totalCost = records.Sum(r => r.TotalCost);

            return totalQuantity > 0 ? totalCost / totalQuantity : 0;
        }

        /// <summary>
        /// Gets the most recent unit cost for an inventory item
        /// </summary>
        public decimal GetLastUnitCost(Guid inventoryItemId)
        {
            var lastRecord = GetRecordsForItem(inventoryItemId).FirstOrDefault();
            return lastRecord?.UnitCost ?? 0;
        }

        /// <summary>
        /// Gets total amount spent on a specific inventory item
        /// </summary>
        public decimal GetTotalSpent(Guid inventoryItemId)
        {
            return GetRecordsForItem(inventoryItemId).Sum(r => r.TotalCost);
        }

        /// <summary>
        /// Gets records within a date range
        /// </summary>
        public IEnumerable<InventoryCostRecord> GetRecordsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _costRecords.Where(r => r.RecordedDate >= startDate && r.RecordedDate <= endDate);
        }

        public bool IsCostMandatory()
        {
            return true;
        }
    }
}

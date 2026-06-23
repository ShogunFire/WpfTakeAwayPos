using Microsoft.Data.Sqlite;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RestaurantPOS.Services
{
    /// <summary>
    /// Service to track inventory purchase costs and calculate cost metrics
    /// </summary>
    public class InventoryCostService : IInventoryCostService
    {
        private readonly IShiftService _shiftService;
        public IEnumerable<InventoryCostRecord> CostRecords => GetAllRecords();

        public InventoryCostService(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        /// <summary>
        /// Records a new inventory purchase/delivery
        /// </summary>
        public Guid RecordPurchase(Guid inventoryItemId, string itemName, decimal quantity, decimal totalCost, string notes = null)
        {
            var record = new InventoryCostRecord
            {
                ShiftId = _shiftService.GetActiveShiftId(),
                InventoryItemId = inventoryItemId,
                ItemName = itemName,
                QuantityReceived = quantity,
                TotalCost = totalCost,
                Notes = notes
            };

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO InventoryCostRecords (Id, ShiftId, InventoryItemId, ItemName, QuantityReceived, TotalCost, RecordedDate, Notes)
                                VALUES (@Id, @ShiftId, @InventoryItemId, @ItemName, @QuantityReceived, @TotalCost, @RecordedDate, @Notes);";
            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@ShiftId", record.ShiftId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@InventoryItemId", record.InventoryItemId.ToString());
            cmd.Parameters.AddWithValue("@ItemName", record.ItemName);
            cmd.Parameters.AddWithValue("@QuantityReceived", record.QuantityReceived);
            cmd.Parameters.AddWithValue("@TotalCost", record.TotalCost);
            cmd.Parameters.AddWithValue("@RecordedDate", record.RecordedDate.ToString("O"));
            cmd.Parameters.AddWithValue("@Notes", record.Notes ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();

            return record.Id;
        }

        /// <summary>
        /// Gets all cost records for a specific inventory item
        /// </summary>
        public IEnumerable<InventoryCostRecord> GetRecordsForItem(Guid inventoryItemId)
        {
            return GetRecords("WHERE InventoryItemId = @InventoryItemId", cmd =>
            {
                cmd.Parameters.AddWithValue("@InventoryItemId", inventoryItemId.ToString());
            }).OrderByDescending(r => r.RecordedDate);
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
            return GetRecords("WHERE RecordedDate >= @StartDate AND RecordedDate <= @EndDate", cmd =>
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("O"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("O"));
            });
        }

        public bool IsCostMandatory()
        {
            return true;
        }

        private IEnumerable<InventoryCostRecord> GetAllRecords()
        {
            return GetRecords(null, null);
        }

        private IEnumerable<InventoryCostRecord> GetRecords(string whereClause, Action<SqliteCommand> bind)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, ShiftId, InventoryItemId, ItemName, QuantityReceived, TotalCost, RecordedDate, Notes FROM InventoryCostRecords " + (whereClause ?? string.Empty);
            bind?.Invoke(cmd);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new InventoryCostRecord
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ShiftId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    InventoryItemId = Guid.Parse(reader.GetString(2)),
                    ItemName = reader.GetString(3),
                    QuantityReceived = Convert.ToDecimal(reader.GetValue(4)),
                    TotalCost = Convert.ToDecimal(reader.GetValue(5)),
                    RecordedDate = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    Notes = reader.IsDBNull(7) ? null : reader.GetString(7)
                };
            }
        }
    }
}

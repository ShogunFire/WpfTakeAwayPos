using Microsoft.Data.Sqlite;
using RestaurantPOS.Configuration;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using RestaurantShared.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.Services
{
    public class InventoryService : IInventoryService
    {
        public ObservableCollection<InventoryItem> InventoryItems { get; }
        private readonly ISyncEventService _syncEventService;
        private readonly PosSettings _settings;

        public InventoryService(ISyncEventService syncEventService, PosSettings settings)
        {
            _syncEventService = syncEventService;
            _settings = settings;
            InventoryItems = new ObservableCollection<InventoryItem>(LoadInventoryItems());
        }

        public InventoryItem InsertInventoryItem(string name, decimal quantity, string unit)
        {
            var item = new InventoryItem(name, quantity, unit, Guid.NewGuid());

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO InventoryItems (Id, Name, Quantity, Unit)
                                VALUES (@Id, @Name, @Quantity, @Unit);";
            cmd.Parameters.AddWithValue("@Id", item.InventoryItemId.ToString());
            cmd.Parameters.AddWithValue("@Name", item.Name);
            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@Unit", item.Unit);
            cmd.ExecuteNonQuery();

            InventoryItems.Add(item);
            _syncEventService.CreateEvent(EventTypes.InventoryItemAdded, new InventoryItemPayload
            {
                InventoryItemId = item.InventoryItemId,
                Name = item.Name,
                Quantity = quantity,
                Unit = item.Unit
            });
            return item;
        }

        public InventoryItem FindByName(string name)
        {
            return InventoryItems.FirstOrDefault(i => i.Name == name);
        }

        public InventoryItem FindByInventoryItemId(Guid inventoryItemId)
        {
            return InventoryItems.FirstOrDefault(i => i.InventoryItemId == inventoryItemId);
        }

        public bool TryConsume(Guid inventoryItemId, decimal quantity, string reason = null)
        {
            if (quantity <= 0)
                return true;

            var item = FindByInventoryItemId(inventoryItemId);
            if (item == null)
                return false;

            if (item.Quantity < quantity)
            {
                item.Quantity = 0;
                UpdateQuantity(item);
                _syncEventService.CreateEvent(EventTypes.InventoryItemRemoved, new InventoryItemPayload
                {
                    InventoryItemId = item.InventoryItemId,
                    Quantity = quantity,
                    Reason = reason
                });
                return false;
            }

            item.Quantity -= quantity;
            UpdateQuantity(item);
            _syncEventService.CreateEvent(EventTypes.InventoryItemRemoved, new InventoryItemPayload
            {
                InventoryItemId = item.InventoryItemId,
                Quantity = quantity,
                Reason = reason
            });
            return true;
        }

        public void AddStock(Guid inventoryItemId, decimal quantity, string reason = null, decimal? totalCost = null, bool paidWithCash = false)
        {
            if (quantity <= 0)
                return;

            var item = FindByInventoryItemId(inventoryItemId);
            if (item == null)
                return;

            item.Quantity += quantity;
            UpdateQuantity(item);
            _syncEventService.CreateEvent(EventTypes.InventoryItemAdded, new InventoryItemPayload
            {
                ShiftId = null,
                InventoryItemId = item.InventoryItemId,
                Name = item.Name,
                Quantity = quantity,
                Unit = item.Unit,
                TotalCost = totalCost,
                Reason = reason,
                PaidWithCash = paidWithCash
            });
        }

        private static IEnumerable<InventoryItem> LoadInventoryItems()
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Quantity, Unit FROM InventoryItems ORDER BY Name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new InventoryItem
                {
                    InventoryItemId = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(1),
                    Quantity = Convert.ToDecimal(reader.GetValue(2)),
                    Unit = reader.GetString(3)
                };
            }
        }

        private static void UpdateQuantity(InventoryItem item)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE InventoryItems SET Quantity = @Quantity WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@Id", item.InventoryItemId.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}

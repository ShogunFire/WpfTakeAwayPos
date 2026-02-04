using Microsoft.Data.Sqlite;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.Services
{
    public class InventoryService : IInventoryService
    {
        public ObservableCollection<InventoryItem> InventoryItems { get; }

        public InventoryService()
        {
            InventoryItems = new ObservableCollection<InventoryItem>(LoadInventoryItems());
        }

        public InventoryItem InsertInventoryItem(string name, decimal quantity, string unit)
        {
            var item = new InventoryItem(0, name, quantity, unit, Guid.NewGuid());

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO InventoryItems (InventoryItemId, Name, Quantity, Unit)
                                VALUES (@InventoryItemId, @Name, @Quantity, @Unit);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@InventoryItemId", item.InventoryItemId.ToString());
            cmd.Parameters.AddWithValue("@Name", item.Name);
            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@Unit", item.Unit);
            item.Id = Convert.ToInt64(cmd.ExecuteScalar());

            InventoryItems.Add(item);
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
                UpdateQuantity(item);
                return false;
            }

            item.Quantity -= quantity;
            UpdateQuantity(item);
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
            UpdateQuantity(item);
        }

        private static IEnumerable<InventoryItem> LoadInventoryItems()
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, InventoryItemId, Name, Quantity, Unit FROM InventoryItems ORDER BY Name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new InventoryItem
                {
                    Id = reader.GetInt64(0),
                    InventoryItemId = Guid.Parse(reader.GetString(1)),
                    Name = reader.GetString(2),
                    Quantity = Convert.ToDecimal(reader.GetValue(3)),
                    Unit = reader.GetString(4)
                };
            }
        }

        private static void UpdateQuantity(InventoryItem item)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE InventoryItems SET Quantity = @Quantity WHERE InventoryItemId = @InventoryItemId";
            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@InventoryItemId", item.InventoryItemId.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}

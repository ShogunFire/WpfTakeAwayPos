using Microsoft.Data.Sqlite;
using RestaurantPOS.Configuration;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using RestaurantShared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RestaurantPOS.Services
{
    public class OrderService
    {
        private readonly IShiftService _shiftService;
        private readonly ISyncEventService _syncEventService;
        private readonly PosSettings _settings;

        public OrderService(IShiftService shiftService, ISyncEventService syncEventService, PosSettings settings)
        {
            _shiftService = shiftService;
            _syncEventService = syncEventService;
            _settings = settings;
        }

        public void AddOrder(Order order)
        {
            if (order == null)
                return;

            order.ShiftId = _shiftService.GetActiveShiftId();

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO Orders (ShiftId, Subtotal, Tax, TotalAmount, TotalPaid, Remaining, TotalChange)
                                VALUES (@ShiftId, @Subtotal, @Tax, @TotalAmount, @TotalPaid, @Remaining, @TotalChange);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@ShiftId", order.ShiftId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Subtotal", order.Subtotal);
            cmd.Parameters.AddWithValue("@Tax", order.Tax);
            cmd.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
            cmd.Parameters.AddWithValue("@TotalPaid", order.TotalPaid);
            cmd.Parameters.AddWithValue("@Remaining", order.Remaining);
            cmd.Parameters.AddWithValue("@TotalChange", order.TotalChange);
            order.OrderId = (int)(long)cmd.ExecuteScalar();

            foreach (var line in order.OrderLines)
            {
                var itemJson = JsonSerializer.Serialize(line.Item);
                using var lineCmd = connection.CreateCommand();
                lineCmd.CommandText = @"INSERT INTO OrderLines (OrderId, ItemJson, Quantity)
                                       VALUES (@OrderId, @ItemJson, @Quantity);";
                lineCmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                lineCmd.Parameters.AddWithValue("@ItemJson", itemJson);
                lineCmd.Parameters.AddWithValue("@Quantity", line.Quantity);
                lineCmd.ExecuteNonQuery();
            }

            var orderLines = order.OrderLines
                .Where(line => line.Item != null)
                .Select(line => new OrderLinePayload
                {
                    MenuItemId = line.Item.OriginalMenuItemId,
                    MenuItemName = line.Item.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.Item.Price,
                    LineTotal = line.Item.Price * line.Quantity
                })
                .ToList();

            var activeShift = _shiftService.GetActiveShift();

            _syncEventService.CreateEvent(EventTypes.OrderCompleted, new OrderPayload
            {
                OrderId = order.OrderGuid,
                ShiftId = activeShift?.ShiftGuid,
                Subtotal = order.Subtotal,
                Tax = order.Tax,
                TotalAmount = order.TotalAmount,
                TotalPaid = order.TotalPaid,
                Remaining = order.Remaining,
                TotalChange = order.TotalChange,
                OrderLines = orderLines
            });
        }

        public List<Order> GetAllOrders()
        {
            var result = new List<Order>();
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT OrderId, ShiftId, Subtotal, Tax, TotalAmount, TotalPaid, Remaining, TotalChange FROM Orders";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var orderId = reader.GetInt32(0);
                var order = new Order
                {
                    OrderId = orderId,
                    ShiftId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    Subtotal = Convert.ToDecimal(reader.GetValue(2)),
                    Tax = Convert.ToDecimal(reader.GetValue(3)),
                    TotalAmount = Convert.ToDecimal(reader.GetValue(4)),
                    TotalPaid = Convert.ToDecimal(reader.GetValue(5)),
                    Remaining = Convert.ToDecimal(reader.GetValue(6)),
                    TotalChange = Convert.ToDecimal(reader.GetValue(7))
                };

                order.OrderLines = LoadOrderLines(connection, orderId);
                order.UpdatePaymentCalculations();
                result.Add(order);
            }

            return result;
        }

        public Order GetOrderById(int orderId)
        {
            return GetAllOrders().FirstOrDefault(o => o.OrderId == orderId);
        }

        public void RemoveOrder(int orderId)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var deleteLines = connection.CreateCommand();
            deleteLines.CommandText = "DELETE FROM OrderLines WHERE OrderId = @OrderId";
            deleteLines.Parameters.AddWithValue("@OrderId", orderId);
            deleteLines.ExecuteNonQuery();

            using var deleteOrder = connection.CreateCommand();
            deleteOrder.CommandText = "DELETE FROM Orders WHERE OrderId = @OrderId";
            deleteOrder.Parameters.AddWithValue("@OrderId", orderId);
            deleteOrder.ExecuteNonQuery();
        }

        public void UpdateOrder(Order updatedOrder)
        {
            if (updatedOrder == null)
                return;

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"UPDATE Orders
                                SET Subtotal = @Subtotal,
                                    Tax = @Tax,
                                    TotalAmount = @TotalAmount,
                                    TotalPaid = @TotalPaid,
                                    Remaining = @Remaining,
                                    TotalChange = @TotalChange
                                WHERE OrderId = @OrderId;";
            cmd.Parameters.AddWithValue("@Subtotal", updatedOrder.Subtotal);
            cmd.Parameters.AddWithValue("@Tax", updatedOrder.Tax);
            cmd.Parameters.AddWithValue("@TotalAmount", updatedOrder.TotalAmount);
            cmd.Parameters.AddWithValue("@TotalPaid", updatedOrder.TotalPaid);
            cmd.Parameters.AddWithValue("@Remaining", updatedOrder.Remaining);
            cmd.Parameters.AddWithValue("@TotalChange", updatedOrder.TotalChange);
            cmd.Parameters.AddWithValue("@OrderId", updatedOrder.OrderId);
            cmd.ExecuteNonQuery();

            using var deleteLines = connection.CreateCommand();
            deleteLines.CommandText = "DELETE FROM OrderLines WHERE OrderId = @OrderId";
            deleteLines.Parameters.AddWithValue("@OrderId", updatedOrder.OrderId);
            deleteLines.ExecuteNonQuery();

            foreach (var line in updatedOrder.OrderLines)
            {
                var itemJson = JsonSerializer.Serialize(line.Item);
                using var lineCmd = connection.CreateCommand();
                lineCmd.CommandText = @"INSERT INTO OrderLines (OrderId, ItemJson, Quantity)
                                       VALUES (@OrderId, @ItemJson, @Quantity);";
                lineCmd.Parameters.AddWithValue("@OrderId", updatedOrder.OrderId);
                lineCmd.Parameters.AddWithValue("@ItemJson", itemJson);
                lineCmd.Parameters.AddWithValue("@Quantity", line.Quantity);
                lineCmd.ExecuteNonQuery();
            }
        }

        private static System.Collections.ObjectModel.ObservableCollection<OrderLine> LoadOrderLines(SqliteConnection connection, int orderId)
        {
            var lines = new System.Collections.ObjectModel.ObservableCollection<OrderLine>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ItemJson, Quantity FROM OrderLines WHERE OrderId = @OrderId";
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var itemJson = reader.GetString(0);
                var quantity = reader.GetInt32(1);
                var item = JsonSerializer.Deserialize<MenuItemSnapshot>(itemJson);
                lines.Add(new OrderLine(item, quantity));
            }

            return lines;
        }
    }
}
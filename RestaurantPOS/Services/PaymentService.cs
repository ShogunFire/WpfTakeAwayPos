using Microsoft.Data.Sqlite;
using RestaurantPOS.Data;
using RestaurantPOS.Services.Interfaces;
using RestaurantShared.DTOs;
using System;
using System.Collections.Generic;

namespace RestaurantPOS.Services
{
    public class PaymentService
    {
        private readonly ISyncEventService _syncEventService;

        public PaymentService(ISyncEventService syncEventService)
        {
            _syncEventService = syncEventService;
        }

        public bool ProcessPayment(Payment payment)
        {
            // Logic to process the payment
            // This could involve interacting with a payment gateway or processing system
            if (payment == null || payment.Amount <= 0)
            {
                throw new ArgumentException("Invalid payment details.");
            }

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO Payments (OrderId, Amount, PaymentMethod)
                                VALUES (@OrderId, @Amount, @PaymentMethod);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@OrderId", payment.OrderId);
            cmd.Parameters.AddWithValue("@Amount", payment.Amount);
            cmd.Parameters.AddWithValue("@PaymentMethod", payment.PaymentMethod ?? string.Empty);
            payment.PaymentId = (int)(long)cmd.ExecuteScalar();

            _syncEventService.CreateEvent(EventTypes.PaymentProcessed, new PaymentPayload
            {
                PaymentId = payment.PaymentGuid,
                OrderId = payment.OrderGuid,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod
            });

            return true; // Assume payment is successful
        }

        public List<Payment> GetPaymentsForOrder(int orderId)
        {
            var result = new List<Payment>();
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT PaymentId, OrderId, Amount, PaymentMethod FROM Payments WHERE OrderId = @OrderId";
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Payment
                {
                    PaymentId = reader.GetInt32(0),
                    OrderId = reader.GetInt32(1),
                    Amount = Convert.ToDecimal(reader.GetValue(2)),
                    PaymentMethod = reader.GetString(3)
                });
            }

            return result;
        }
    }
}
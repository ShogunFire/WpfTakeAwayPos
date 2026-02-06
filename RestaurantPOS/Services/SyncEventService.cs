using Microsoft.Data.Sqlite;
using RestaurantPOS.Data;
using RestaurantPOS.Services.Interfaces;
using RestaurantShared.DTOs;
using System;
using System.Text.Json;

namespace RestaurantPOS.Services
{
    public class SyncEventService : ISyncEventService
    {
        public void CreateEvent(string type, object payload)
        {
            var @event = new EventDto
            {
                Id = Guid.NewGuid(),
                Type = type ?? string.Empty,
                Payload = payload,
                CreatedAt = DateTime.UtcNow,
                DeviceId = DeviceIdProvider.GetDeviceId()
            };

            CreateEvent(@event);
        }

        public void CreateEvent(EventDto @event)
        {
            if (@event == null)
            {
                return;
            }

            var payloadJson = @event.Payload == null
                ? string.Empty
                : JsonSerializer.Serialize(@event.Payload);

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO SyncEvents (Id, Type, Payload, CreatedAt, SyncedAt, DeviceId)
                                VALUES (@Id, @Type, @Payload, @CreatedAt, @SyncedAt, @DeviceId);";
            cmd.Parameters.AddWithValue("@Id", @event.Id.ToString());
            cmd.Parameters.AddWithValue("@Type", @event.Type ?? string.Empty);
            cmd.Parameters.AddWithValue("@Payload", payloadJson ?? string.Empty);
            cmd.Parameters.AddWithValue("@CreatedAt", @event.CreatedAt.ToString("O"));
            cmd.Parameters.AddWithValue("@SyncedAt", DBNull.Value);
            cmd.Parameters.AddWithValue("@DeviceId", @event.DeviceId ?? string.Empty);
            cmd.ExecuteNonQuery();
        }
    }
}

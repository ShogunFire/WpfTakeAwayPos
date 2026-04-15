using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using RestaurantShared.DTOs;

namespace RestaurantSynchronizationLib.Persistence;

/// <summary>
/// Interface for accessing SyncEvent records from database
/// </summary>
public interface ISyncEventRepository
{
    Task<List<EventDto>> GetUnsyncedEventsAsync();
    Task<List<EventDto>> GetUnsyncedEventsByTypeAsync(string eventType);
    Task MarkAsSyncedAsync(Guid eventId);
    Task MarkAsSyncedAsync(IEnumerable<Guid> eventIds);
    Task DeleteEventAsync(Guid eventId);
    Task<int> GetUnsyncedEventCountAsync();
}

/// <summary>
/// Accesses SyncEvent records from SQLite database
/// </summary>
public class SyncEventRepository : ISyncEventRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SyncEventRepository> _logger;
    private readonly string _deviceId;

    public SyncEventRepository(string connectionString, ILogger<SyncEventRepository> logger, string deviceId = "")
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceId = deviceId ?? string.Empty;
    }

    /// <summary>
    /// Get all unsynced events as EventDtos
    /// </summary>
    public async Task<List<EventDto>> GetUnsyncedEventsAsync()
    {
        var events = new List<EventDto>();

        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Type, Payload, CreatedAt, DeviceId, LocationId
                    FROM SyncEvents
                    WHERE SyncedAt IS NULL
                    ORDER BY CreatedAt ASC";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var payload = ParsePayload(reader.IsDBNull(2) ? null : reader.GetString(2));
                        
                        events.Add(new EventDto
                        {
                            Id = reader.GetGuid(0),
                            Type = reader.GetString(1),
                            Payload = payload,
                            CreatedAt = reader.GetDateTime(3),
                            DeviceId = reader.IsDBNull(4) ? _deviceId : reader.GetString(4),
                            LocationId = reader.IsDBNull(5) ? (Guid?)null : reader.GetGuid(5)
                        });
                    }
                }
            }

            _logger.LogInformation("Retrieved {Count} unsynced events from database", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unsynced events from database");
            throw;
        }

        return events;
    }

    /// <summary>
    /// Get unsynced events by type as EventDtos
    /// </summary>
    public async Task<List<EventDto>> GetUnsyncedEventsByTypeAsync(string eventType)
    {
        var events = new List<EventDto>();

        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Type, Payload, CreatedAt, DeviceId, LocationId
                    FROM SyncEvents
                    WHERE SyncedAt IS NULL AND Type = @Type
                    ORDER BY CreatedAt ASC";
                cmd.Parameters.AddWithValue("@Type", eventType);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var payload = ParsePayload(reader.IsDBNull(2) ? null : reader.GetString(2));
                        
                        events.Add(new EventDto
                        {
                            Id = reader.GetGuid(0),
                            Type = reader.GetString(1),
                            Payload = payload,
                            CreatedAt = reader.GetDateTime(3),
                            DeviceId = reader.IsDBNull(4) ? _deviceId : reader.GetString(4),
                            LocationId = reader.IsDBNull(5) ? (Guid?)null : reader.GetGuid(5)
                        });
                    }
                }
            }

            _logger.LogInformation("Retrieved {Count} unsynced events of type {Type} from database", events.Count, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unsynced events of type {Type}", eventType);
            throw;
        }

        return events;
    }

    /// <summary>
    /// Parse JSON payload to object
    /// </summary>
    private object? ParsePayload(string? payloadJson)
    {
        if (string.IsNullOrEmpty(payloadJson))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(payloadJson);
        }
        catch
        {
            // If it fails to parse as JSON, keep it as string
            return payloadJson;
        }
    }

    /// <summary>
    /// Mark an event as synced
    /// </summary>
    public async Task MarkAsSyncedAsync(Guid eventId)
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE SyncEvents
                    SET SyncedAt = @SyncedAt
                    WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@Id", eventId.ToString()); // Convert Guid to string (TEXT)
                cmd.Parameters.AddWithValue("@SyncedAt", DateTime.UtcNow); // DateTime as DATETIME

                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Marked event {EventId} as synced", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking event {EventId} as synced", eventId);
            throw;
        }
    }

    /// <summary>
    /// Mark multiple events as synced
    /// </summary>
    public async Task MarkAsSyncedAsync(IEnumerable<Guid> eventIds)
    {
        try
        {
            var idList = eventIds.ToList();
            _logger.LogInformation("Starting to mark {Count} events as synced", idList.Count);
            
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                foreach (var eventId in idList)
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE SyncEvents
                        SET SyncedAt = @SyncedAt
                        WHERE Id = @Id";
                    cmd.Parameters.Clear(); // Clear parameters before adding new ones
                    cmd.Parameters.AddWithValue("@Id", eventId.ToString()); // Convert Guid to string (TEXT)
                    cmd.Parameters.AddWithValue("@SyncedAt", DateTime.UtcNow); // DateTime as DATETIME

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("No rows updated for event {EventId} - event may not exist in database", eventId);
                    }
                    else
                    {
                        _logger.LogDebug("Marked event {EventId} as synced", eventId);
                    }
                }
            }

            _logger.LogInformation("Successfully marked {Count} events as synced", idList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking events as synced");
            throw;
        }
    }

    /// <summary>
    /// Delete a synced event
    /// </summary>
    public async Task DeleteEventAsync(Guid eventId)
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM SyncEvents WHERE Id = @Id AND SyncedAt IS NOT NULL";
                cmd.Parameters.AddWithValue("@Id", eventId.ToString()); // Convert Guid to string (TEXT)

                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Deleted event {EventId}", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", eventId);
            throw;
        }
    }

    /// <summary>
    /// Get the total count of unsynced events
    /// </summary>
    public async Task<int> GetUnsyncedEventCountAsync()
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM SyncEvents WHERE SyncedAt IS NULL";

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unsynced event count");
            throw;
        }
    }
}

/// <summary>
/// Represents a SyncEvent record from the database
/// </summary>


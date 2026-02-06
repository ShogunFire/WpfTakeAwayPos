using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantSynchronizationLib.Persistence;

/// <summary>
/// Accesses SyncEvent records from SQLite database
/// </summary>
public class SyncEventRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SyncEventRepository> _logger;

    public SyncEventRepository(string connectionString, ILogger<SyncEventRepository> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all unsynced events
    /// </summary>
    public async Task<List<SyncEventRecord>> GetUnsyncedEventsAsync()
    {
        var events = new List<SyncEventRecord>();

        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Type, Payload, CreatedAt, SyncedAt, DeviceId
                    FROM SyncEvents
                    WHERE SyncedAt IS NULL
                    ORDER BY CreatedAt ASC";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        events.Add(new SyncEventRecord
                        {
                            Id = reader.GetGuid(0),
                            Type = reader.GetString(1),
                            Payload = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3),
                            SyncedAt = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4),
                            DeviceId = reader.IsDBNull(5) ? null : reader.GetString(5)
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
    /// Get unsynced events by type
    /// </summary>
    public async Task<List<SyncEventRecord>> GetUnsyncedEventsByTypeAsync(string eventType)
    {
        var events = new List<SyncEventRecord>();

        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Type, Payload, CreatedAt, SyncedAt, DeviceId
                    FROM SyncEvents
                    WHERE SyncedAt IS NULL AND Type = @Type
                    ORDER BY CreatedAt ASC";
                cmd.Parameters.AddWithValue("@Type", eventType);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        events.Add(new SyncEventRecord
                        {
                            Id = reader.GetGuid(0),
                            Type = reader.GetString(1),
                            Payload = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3),
                            SyncedAt = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4),
                            DeviceId = reader.IsDBNull(5) ? null : reader.GetString(5)
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
public class SyncEventRecord
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    public string? DeviceId { get; set; }
}

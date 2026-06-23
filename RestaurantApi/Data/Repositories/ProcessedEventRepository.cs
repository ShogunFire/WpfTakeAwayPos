using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IProcessedEventRepository
{
    Task<ProcessedEvent?> GetByIdAsync(Guid id);
    Task AddOrUpdateAsync(ProcessedEvent processedEvent);
    Task UpdateStatusAsync(Guid id, string status, string? errorMessage, DateTime? processedAt, int? attemptCount = null);
    Task<List<ProcessedEvent>> GetPendingEventsAsync();
}

public class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly string _connectionString;

    public ProcessedEventRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ProcessedEvent?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM ProcessedEvents WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<ProcessedEvent>(sql, new { Id = id });
    }

    public async Task AddOrUpdateAsync(ProcessedEvent processedEvent)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            IF EXISTS (SELECT 1 FROM ProcessedEvents WHERE Id = @Id)
            BEGIN
                UPDATE ProcessedEvents
                SET EventType = @EventType,
                    Payload = @Payload,
                    DeviceId = @DeviceId,
                    LocationId = @LocationId,
                    EventCreatedAt = COALESCE(EventCreatedAt, @EventCreatedAt),
                    ReceivedAt = COALESCE(ReceivedAt, @ReceivedAt)
                WHERE Id = @Id;
            END
            ELSE
            BEGIN
                INSERT INTO ProcessedEvents (Id, EventType, Payload, Status, ErrorMessage, EventCreatedAt, ReceivedAt, LastAttemptAt, AttemptCount, ProcessedAt, DeviceId, LocationId)
                VALUES (@Id, @EventType, @Payload, @Status, @ErrorMessage, @EventCreatedAt, @ReceivedAt, @LastAttemptAt, @AttemptCount, @ProcessedAt, @DeviceId, @LocationId);
            END;";

        await connection.ExecuteAsync(sql, processedEvent);
    }

    public async Task UpdateStatusAsync(Guid id, string status, string? errorMessage, DateTime? processedAt, int? attemptCount = null)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE ProcessedEvents
            SET Status = @Status,
                ErrorMessage = @ErrorMessage,
                LastAttemptAt = SYSUTCDATETIME(),
                AttemptCount = CASE WHEN @AttemptCount IS NOT NULL THEN @AttemptCount ELSE AttemptCount + 1 END,
                ProcessedAt = @ProcessedAt
            WHERE Id = @Id;";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            ErrorMessage = errorMessage,
            ProcessedAt = processedAt,
            AttemptCount = attemptCount
        });
    }

    public async Task<List<ProcessedEvent>> GetPendingEventsAsync()
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM ProcessedEvents
            WHERE Status = 'Queued'
            ORDER BY COALESCE(EventCreatedAt, ReceivedAt) ASC, ReceivedAt ASC, Id ASC;";
        var results = await connection.QueryAsync<ProcessedEvent>(sql);
        return results.ToList();
    }
}

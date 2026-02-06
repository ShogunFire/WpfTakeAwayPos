using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IProcessedEventRepository
{
    Task<ProcessedEvent?> GetByIdAsync(Guid id);
    Task AddOrUpdateAsync(ProcessedEvent processedEvent);
    Task UpdateStatusAsync(Guid id, string status, string? errorMessage, DateTime? processedAt);
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
                    ReceivedAt = COALESCE(ReceivedAt, @ReceivedAt)
                WHERE Id = @Id;
            END
            ELSE
            BEGIN
                INSERT INTO ProcessedEvents (Id, EventType, Payload, Status, ErrorMessage, ReceivedAt, LastAttemptAt, AttemptCount, ProcessedAt, DeviceId)
                VALUES (@Id, @EventType, @Payload, @Status, @ErrorMessage, @ReceivedAt, @LastAttemptAt, @AttemptCount, @ProcessedAt, @DeviceId);
            END;";

        await connection.ExecuteAsync(sql, processedEvent);
    }

    public async Task UpdateStatusAsync(Guid id, string status, string? errorMessage, DateTime? processedAt)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE ProcessedEvents
            SET Status = @Status,
                ErrorMessage = @ErrorMessage,
                LastAttemptAt = SYSUTCDATETIME(),
                AttemptCount = AttemptCount + 1,
                ProcessedAt = @ProcessedAt
            WHERE Id = @Id;";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            ErrorMessage = errorMessage,
            ProcessedAt = processedAt
        });
    }
}

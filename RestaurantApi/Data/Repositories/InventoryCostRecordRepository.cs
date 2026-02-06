using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IInventoryCostRecordRepository
{
    Task AddAsync(InventoryCostRecord record);
}

public class InventoryCostRecordRepository : IInventoryCostRecordRepository
{
    private readonly string _connectionString;

    public InventoryCostRecordRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AddAsync(InventoryCostRecord record)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM InventoryCostRecords WHERE EventId = @EventId)
            BEGIN
                INSERT INTO InventoryCostRecords (Id, EventId, LocationId, InventoryItemId, QuantityReceived, TotalCost, RecordedAt)
                VALUES (@Id, @EventId, @LocationId, @InventoryItemId, @QuantityReceived, @TotalCost, @RecordedAt);
            END";

        await connection.ExecuteAsync(sql, record);
    }
}

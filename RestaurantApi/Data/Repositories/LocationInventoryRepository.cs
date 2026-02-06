using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface ILocationInventoryRepository
{
    Task<LocationInventory?> GetAsync(Guid locationId, Guid inventoryItemId);
    Task<decimal> GetQuantityAsync(Guid locationId, Guid inventoryItemId);
    Task UpsertQuantityAsync(Guid locationId, Guid inventoryItemId, decimal quantity);
}

public class LocationInventoryRepository : ILocationInventoryRepository
{
    private readonly string _connectionString;

    public LocationInventoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<LocationInventory?> GetAsync(Guid locationId, Guid inventoryItemId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"SELECT LocationId, InventoryItemId, Quantity, UpdatedAt
                    FROM LocationInventory
                    WHERE LocationId = @LocationId AND InventoryItemId = @InventoryItemId";
        return await connection.QueryFirstOrDefaultAsync<LocationInventory>(sql, new
        {
            LocationId = locationId,
            InventoryItemId = inventoryItemId
        });
    }

    public async Task<decimal> GetQuantityAsync(Guid locationId, Guid inventoryItemId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"SELECT Quantity FROM LocationInventory
                    WHERE LocationId = @LocationId AND InventoryItemId = @InventoryItemId";
        var quantity = await connection.QueryFirstOrDefaultAsync<decimal?>(sql, new
        {
            LocationId = locationId,
            InventoryItemId = inventoryItemId
        });

        return quantity ?? 0m;
    }

    public async Task UpsertQuantityAsync(Guid locationId, Guid inventoryItemId, decimal quantity)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            MERGE LocationInventory AS target
            USING (SELECT @LocationId AS LocationId, @InventoryItemId AS InventoryItemId) AS source
            ON target.LocationId = source.LocationId AND target.InventoryItemId = source.InventoryItemId
            WHEN MATCHED THEN
                UPDATE SET Quantity = @Quantity, UpdatedAt = @UpdatedAt
            WHEN NOT MATCHED THEN
                INSERT (LocationId, InventoryItemId, Quantity, UpdatedAt)
                VALUES (@LocationId, @InventoryItemId, @Quantity, @UpdatedAt);";

        await connection.ExecuteAsync(sql, new
        {
            LocationId = locationId,
            InventoryItemId = inventoryItemId,
            Quantity = quantity,
            UpdatedAt = DateTime.UtcNow
        });
    }
}

using Dapper;
using RestaurantShared.DTOs;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IInventoryQueryRepository
{
    Task<IEnumerable<InventoryItemDto>> GetInventoryForLocationAsync(Guid locationId);
}

public class InventoryQueryRepository : IInventoryQueryRepository
{
    private readonly string _connectionString;

    public InventoryQueryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetInventoryForLocationAsync(Guid locationId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT i.Id AS InventoryItemId, i.Name, i.Unit,
                   COALESCE(li.Quantity, 0) AS Quantity
            FROM InventoryItems i
            LEFT JOIN LocationInventory li
                ON li.InventoryItemId = i.Id AND li.LocationId = @LocationId
            ORDER BY i.Name";

        return await connection.QueryAsync<InventoryItemDto>(sql, new { LocationId = locationId });
    }
}

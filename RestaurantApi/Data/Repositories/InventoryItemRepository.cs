using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IInventoryItemRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id);
    Task<IEnumerable<InventoryItem>> GetAllAsync();
    Task AddAsync(InventoryItem inventoryItem);
    Task<bool> UpdateAsync(InventoryItem inventoryItem);
    Task<bool> DeleteAsync(Guid id);
}

public class InventoryItemRepository : IInventoryItemRepository
{
    private readonly string _connectionString;

    public InventoryItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM InventoryItems WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<InventoryItem>(sql, new { Id = id });
    }

    public async Task<IEnumerable<InventoryItem>> GetAllAsync()
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM InventoryItems";
        return await connection.QueryAsync<InventoryItem>(sql);
    }

    public async Task AddAsync(InventoryItem inventoryItem)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM InventoryItems WHERE Id = @Id)
            BEGIN
                INSERT INTO InventoryItems (Id, Name, Unit, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @Unit, @CreatedAt, @UpdatedAt);
            END";
        await connection.ExecuteAsync(sql, inventoryItem);
    }

    public async Task<bool> UpdateAsync(InventoryItem inventoryItem)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE InventoryItems
            SET Name = @Name, Unit = @Unit, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        var result = await connection.ExecuteAsync(sql, inventoryItem);
        return result > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM InventoryItems WHERE Id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }
}

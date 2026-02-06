using Dapper;
using RestaurantApi.Data.Models;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IMenuItemComponentRepository
{
    Task<IEnumerable<MenuItemComponent>> GetByMenuItemIdAsync(Guid menuItemId);
    Task<IEnumerable<MenuItemComponent>> GetByInventoryItemIdAsync(Guid inventoryItemId);
    Task<IEnumerable<MenuItemComponent>> GetAllAsync();
}

public class MenuItemComponentRepository : IMenuItemComponentRepository
{
    private readonly string _connectionString;

    public MenuItemComponentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<MenuItemComponent>> GetByMenuItemIdAsync(Guid menuItemId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, MenuItemId, InventoryItemId, Quantity
            FROM MenuItemComponents
            WHERE MenuItemId = @MenuItemId";
        
        return await connection.QueryAsync<MenuItemComponent>(sql, new { MenuItemId = menuItemId });
    }

    public async Task<IEnumerable<MenuItemComponent>> GetByInventoryItemIdAsync(Guid inventoryItemId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, MenuItemId, InventoryItemId, Quantity
            FROM MenuItemComponents
            WHERE InventoryItemId = @InventoryItemId";
        
        return await connection.QueryAsync<MenuItemComponent>(sql, new { InventoryItemId = inventoryItemId });
    }

    public async Task<IEnumerable<MenuItemComponent>> GetAllAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, MenuItemId, InventoryItemId, Quantity
            FROM MenuItemComponents
            ORDER BY MenuItemId";
        
        return await connection.QueryAsync<MenuItemComponent>(sql);
    }
}

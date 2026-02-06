using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IMenuItemGrossProfitHistoryRepository
{
    Task<MenuItemGrossProfitHistory?> GetByIdAsync(Guid id);
    Task<IEnumerable<MenuItemGrossProfitHistory>> GetByMenuItemIdAsync(Guid menuItemId);
    Task<IEnumerable<MenuItemGrossProfitHistory>> GetRecentByMenuItemIdAsync(Guid menuItemId, int limit = 10);
    Task AddAsync(MenuItemGrossProfitHistory history);
}

public class MenuItemGrossProfitHistoryRepository : IMenuItemGrossProfitHistoryRepository
{
    private readonly string _connectionString;

    public MenuItemGrossProfitHistoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<MenuItemGrossProfitHistory?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM MenuItemGrossProfitHistory WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<MenuItemGrossProfitHistory>(sql, new { Id = id });
    }

    public async Task<IEnumerable<MenuItemGrossProfitHistory>> GetByMenuItemIdAsync(Guid menuItemId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM MenuItemGrossProfitHistory WHERE MenuItemId = @MenuItemId ORDER BY SnapshotDate DESC";
        return await connection.QueryAsync<MenuItemGrossProfitHistory>(sql, new { MenuItemId = menuItemId });
    }

    public async Task<IEnumerable<MenuItemGrossProfitHistory>> GetRecentByMenuItemIdAsync(Guid menuItemId, int limit = 10)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT TOP (@Limit) * FROM MenuItemGrossProfitHistory 
            WHERE MenuItemId = @MenuItemId 
            ORDER BY SnapshotDate DESC";
        return await connection.QueryAsync<MenuItemGrossProfitHistory>(sql, new { MenuItemId = menuItemId, Limit = limit });
    }

    public async Task AddAsync(MenuItemGrossProfitHistory history)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            INSERT INTO MenuItemGrossProfitHistory (Id, MenuItemId, Price, UnitCost, GrossProfit, GrossMargin, SnapshotDate, CreatedAt)
            VALUES (@Id, @MenuItemId, @Price, @UnitCost, @GrossProfit, @GrossMargin, @SnapshotDate, @CreatedAt)";
        await connection.ExecuteAsync(sql, history);
    }
}

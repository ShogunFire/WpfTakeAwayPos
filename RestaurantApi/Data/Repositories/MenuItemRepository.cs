using Dapper;

using RestaurantApi.Data.Models;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IMenuItemRepository
{
    Task<IEnumerable<MenuItem>> GetAllAsync();
    Task<MenuItem?> GetByIdAsync(Guid id);
}

public class MenuItemRepository : IMenuItemRepository
{
    private readonly string _connectionString;

    public MenuItemRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<MenuItem>> GetAllAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Name, Description, Price, Category, IsActive, CreatedAt, UpdatedAt
            FROM MenuItems
            WHERE IsActive = 1
            ORDER BY Category, Name";
        
        return await connection.QueryAsync<MenuItem>(sql);
    }

    public async Task<MenuItem?> GetByIdAsync(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, Name, Description, Price, Category, IsActive, CreatedAt, UpdatedAt
            FROM MenuItems
            WHERE Id = @Id";
        
        return await connection.QuerySingleOrDefaultAsync<MenuItem>(sql, new { Id = id });
    }
}

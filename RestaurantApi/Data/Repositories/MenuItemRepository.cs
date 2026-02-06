using Dapper;

using RestaurantApi.Data.Models;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IMenuItemRepository
{
    Task<IEnumerable<MenuItem>> GetAllAsync();
    Task<MenuItem?> GetByIdAsync(Guid id);
    Task<bool> UpdateAsync(MenuItem menuItem);
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
            SELECT Id, IdCategory, Name, Description, Price, CurrentCOGS, LastCOGSUpdate, IsActive, CreatedAt, UpdatedAt
            FROM MenuItems
            WHERE IsActive = 1
            ORDER BY IdCategory, Name";
        
        return await connection.QueryAsync<MenuItem>(sql);
    }

    public async Task<MenuItem?> GetByIdAsync(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, IdCategory, Name, Description, Price, CurrentCOGS, LastCOGSUpdate, IsActive, CreatedAt, UpdatedAt
            FROM MenuItems
            WHERE Id = @Id";
        
        return await connection.QuerySingleOrDefaultAsync<MenuItem>(sql, new { Id = id });
    }

    public async Task<bool> UpdateAsync(MenuItem menuItem)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            UPDATE MenuItems
            SET IdCategory = @IdCategory,
                Name = @Name,
                Description = @Description,
                Price = @Price,
                CurrentCOGS = @CurrentCOGS,
                LastCOGSUpdate = @LastCOGSUpdate,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        var result = await connection.ExecuteAsync(sql, menuItem);
        return result > 0;
    }
}

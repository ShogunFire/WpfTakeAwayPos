using Dapper;
using System.Data.SqlClient;
using RestaurantApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantApi.Data.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category> GetByIdAsync(Guid id);
    Task<Category> CreateAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task<bool> DeleteAsync(Guid id);
}

public class CategoryRepository : ICategoryRepository
{
    private readonly string _connectionString;

    public CategoryRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<List<Category>> GetAllAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
                SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
                FROM Categories
                WHERE IsActive = 1
                ORDER BY Name";

            var categories = await connection.QueryAsync<Category>(query);
            return new List<Category>(categories);
        }
    }

    public async Task<Category> GetByIdAsync(Guid id)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
                SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
                FROM Categories
                WHERE Id = @Id";

            return await connection.QueryFirstOrDefaultAsync<Category>(query, new { Id = id });
        }
    }

    public async Task<Category> CreateAsync(Category category)
    {
        if (category == null)
            throw new ArgumentNullException(nameof(category));

        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.Now;
        category.UpdatedAt = DateTime.Now;

        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
                INSERT INTO Categories (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @Description, @IsActive, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(query, category);
        }

        return category;
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        if (category == null)
            throw new ArgumentNullException(nameof(category));

        category.UpdatedAt = DateTime.Now;

        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
                UPDATE Categories
                SET Name = @Name, Description = @Description, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            var rowsAffected = await connection.ExecuteAsync(query, category);
            return rowsAffected > 0;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = "UPDATE Categories SET IsActive = 0 WHERE Id = @Id";
            var rowsAffected = await connection.ExecuteAsync(query, new { Id = id });
            return rowsAffected > 0;
        }
    }
}

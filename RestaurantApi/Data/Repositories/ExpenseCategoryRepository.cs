using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IExpenseCategoryRepository
{
    Task<ExpenseCategory?> GetByIdAsync(Guid id);
    Task<ExpenseCategory?> GetByNameAsync(string name);
    Task<IEnumerable<ExpenseCategory>> GetAllActiveAsync();
    Task AddAsync(ExpenseCategory category);
    Task<bool> UpdateAsync(ExpenseCategory category);
}

public class ExpenseCategoryRepository : IExpenseCategoryRepository
{
    private readonly string _connectionString;

    public ExpenseCategoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ExpenseCategory?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM ExpenseCategories WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<ExpenseCategory>(sql, new { Id = id });
    }

    public async Task<ExpenseCategory?> GetByNameAsync(string name)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM ExpenseCategories WHERE Name = @Name AND IsActive = 1";
        return await connection.QueryFirstOrDefaultAsync<ExpenseCategory>(sql, new { Name = name });
    }

    public async Task<IEnumerable<ExpenseCategory>> GetAllActiveAsync()
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM ExpenseCategories WHERE IsActive = 1 ORDER BY Name";
        return await connection.QueryAsync<ExpenseCategory>(sql);
    }

    public async Task AddAsync(ExpenseCategory category)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            INSERT INTO ExpenseCategories (Id, Name, IsCOGS, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @IsCOGS, @IsActive, @CreatedAt, @UpdatedAt)";
        await connection.ExecuteAsync(sql, category);
    }

    public async Task<bool> UpdateAsync(ExpenseCategory category)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        category.UpdatedAt = DateTime.Now;
        var sql = @"
            UPDATE ExpenseCategories 
            SET Name = @Name,
                IsCOGS = @IsCOGS,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, category);
        return rowsAffected > 0;
    }
}

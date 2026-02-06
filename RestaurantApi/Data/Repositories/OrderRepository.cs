using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetAllAsync();
    Task AddAsync(Order order);
    Task<bool> UpdateAsync(Order order);
}

public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Orders WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Order>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Orders";
        return await connection.QueryAsync<Order>(sql);
    }

    public async Task AddAsync(Order order)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM Orders WHERE Id = @Id)
            BEGIN
                INSERT INTO Orders (Id, ShiftId, Subtotal, Tax, TotalAmount, TotalPaid, Remaining, TotalChange, CreatedAt, UpdatedAt)
                VALUES (@Id, @ShiftId, @Subtotal, @Tax, @TotalAmount, @TotalPaid, @Remaining, @TotalChange, @CreatedAt, @UpdatedAt);
            END";
        await connection.ExecuteAsync(sql, order);
    }

    public async Task<bool> UpdateAsync(Order order)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE Orders
            SET ShiftId = @ShiftId, Subtotal = @Subtotal, Tax = @Tax, TotalAmount = @TotalAmount, 
                TotalPaid = @TotalPaid, Remaining = @Remaining, TotalChange = @TotalChange, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        var result = await connection.ExecuteAsync(sql, order);
        return result > 0;
    }
}

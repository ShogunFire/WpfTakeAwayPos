using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(Payment payment);
    Task<bool> DeleteAsync(Guid id);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly string _connectionString;

    public PaymentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Payments WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Payments";
        return await connection.QueryAsync<Payment>(sql);
    }

    public async Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Payments WHERE OrderId = @OrderId";
        return await connection.QueryAsync<Payment>(sql, new { OrderId = orderId });
    }

    public async Task AddAsync(Payment payment)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM Payments WHERE Id = @Id)
            BEGIN
                INSERT INTO Payments (Id, LocationId, OrderId, Amount, PaymentMethod, CreatedAt, UpdatedAt)
                VALUES (@Id, @LocationId, @OrderId, @Amount, @PaymentMethod, @CreatedAt, @UpdatedAt);
            END";
        await connection.ExecuteAsync(sql, payment);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM Payments WHERE Id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }
}

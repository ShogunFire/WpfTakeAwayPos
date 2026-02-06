using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IOrderLineRepository
{
    Task<OrderLine?> GetByIdAsync(Guid id);
    Task<IEnumerable<OrderLine>> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(OrderLine orderLine);
    Task<bool> DeleteByOrderIdAsync(Guid orderId);
}

public class OrderLineRepository : IOrderLineRepository
{
    private readonly string _connectionString;

    public OrderLineRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<OrderLine?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM OrderLines WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<OrderLine>(sql, new { Id = id });
    }

    public async Task<IEnumerable<OrderLine>> GetByOrderIdAsync(Guid orderId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM OrderLines WHERE OrderId = @OrderId";
        return await connection.QueryAsync<OrderLine>(sql, new { OrderId = orderId });
    }

    public async Task AddAsync(OrderLine orderLine)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            INSERT INTO OrderLines (Id, OrderId, Quantity, UnitPrice, LineTotal, MenuItemName, MenuItemId)
            VALUES (@Id, @OrderId, @Quantity, @UnitPrice, @LineTotal, @MenuItemName, @MenuItemId);";
        await connection.ExecuteAsync(sql, orderLine);
    }

    public async Task<bool> DeleteByOrderIdAsync(Guid orderId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM OrderLines WHERE OrderId = @OrderId";
        var result = await connection.ExecuteAsync(sql, new { OrderId = orderId });
        return result > 0;
    }
}

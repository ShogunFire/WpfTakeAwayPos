using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface ICashTransactionRepository
{
    Task<CashTransaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<CashTransaction>> GetAllAsync();
    Task AddAsync(CashTransaction transaction);
    Task<bool> DeleteAsync(Guid id);
}

public class CashTransactionRepository : ICashTransactionRepository
{
    private readonly string _connectionString;

    public CashTransactionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CashTransaction?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM CashTransactions WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<CashTransaction>(sql, new { Id = id });
    }

    public async Task<IEnumerable<CashTransaction>> GetAllAsync()
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM CashTransactions";
        return await connection.QueryAsync<CashTransaction>(sql);
    }

    public async Task AddAsync(CashTransaction transaction)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM CashTransactions WHERE Id = @Id)
            BEGIN
                INSERT INTO CashTransactions (
                    Id,
                    ShiftId,
                    TransactionType,
                    Amount,
                    Reason,
                    Description,
                    OccurredAt,
                    CreatedAt,
                    UpdatedAt
                )
                VALUES (
                    @Id,
                    @ShiftId,
                    @TransactionType,
                    @Amount,
                    @Reason,
                    @Description,
                    @OccurredAt,
                    @CreatedAt,
                    @UpdatedAt
                );
            END";
        await connection.ExecuteAsync(sql, transaction);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM CashTransactions WHERE Id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }
}

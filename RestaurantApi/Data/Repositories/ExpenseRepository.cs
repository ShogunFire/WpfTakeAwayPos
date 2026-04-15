using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id);
    Task<IEnumerable<Expense>> GetByLocationAsync(Guid locationId, DateTime startDate, DateTime endDate);
    Task<Expense?> GetByInventoryCostRecordIdAsync(Guid inventoryCostRecordId);
    Task<Expense?> GetByCashTransactionIdAsync(Guid cashTransactionId);
    Task AddAsync(Expense expense);
    Task<bool> UpdateAsync(Expense expense);
    Task<bool> LinkCashTransactionAsync(Guid expenseId, Guid cashTransactionId);
}

public class ExpenseRepository : IExpenseRepository
{
    private readonly string _connectionString;

    public ExpenseRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Expense?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Expenses WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Expense>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Expense>> GetByLocationAsync(Guid locationId, DateTime startDate, DateTime endDate)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM Expenses 
            WHERE LocationId = @LocationId 
              AND ExpenseDate >= @StartDate 
              AND ExpenseDate < @EndDate
            ORDER BY ExpenseDate DESC";
        return await connection.QueryAsync<Expense>(sql, new { LocationId = locationId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<Expense?> GetByInventoryCostRecordIdAsync(Guid inventoryCostRecordId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Expenses WHERE InventoryCostRecordId = @InventoryCostRecordId";
        return await connection.QueryFirstOrDefaultAsync<Expense>(sql, new { InventoryCostRecordId = inventoryCostRecordId });
    }

    public async Task<Expense?> GetByCashTransactionIdAsync(Guid cashTransactionId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Expenses WHERE CashTransactionId = @CashTransactionId";
        return await connection.QueryFirstOrDefaultAsync<Expense>(sql, new { CashTransactionId = cashTransactionId });
    }

    public async Task AddAsync(Expense expense)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            INSERT INTO Expenses (Id, ExpenseCategoryId, Amount, Description, ExpenseDate, LocationId, ShiftId, 
                                  InventoryCostRecordId, CashTransactionId, CreatedAt, UpdatedAt)
            VALUES (@Id, @ExpenseCategoryId, @Amount, @Description, @ExpenseDate, @LocationId, @ShiftId, 
                    @InventoryCostRecordId, @CashTransactionId, @CreatedAt, @UpdatedAt)";
        await connection.ExecuteAsync(sql, expense);
    }

    public async Task<bool> UpdateAsync(Expense expense)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        expense.UpdatedAt = DateTime.Now;
        var sql = @"
            UPDATE Expenses 
            SET ExpenseCategoryId = @ExpenseCategoryId,
                Amount = @Amount,
                Description = @Description,
                ExpenseDate = @ExpenseDate,
                LocationId = @LocationId,
                ShiftId = @ShiftId,
                InventoryCostRecordId = @InventoryCostRecordId,
                CashTransactionId = @CashTransactionId,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, expense);
        return rowsAffected > 0;
    }

    public async Task<bool> LinkCashTransactionAsync(Guid expenseId, Guid cashTransactionId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE Expenses 
            SET CashTransactionId = @CashTransactionId,
                UpdatedAt = @UpdatedAt
            WHERE Id = @ExpenseId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { ExpenseId = expenseId, CashTransactionId = cashTransactionId, UpdatedAt = DateTime.Now });
        return rowsAffected > 0;
    }
}

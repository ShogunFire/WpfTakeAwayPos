using Dapper;
using RestaurantApi.Data.Models;
using System.Data;
using System.Data.SqlClient;

namespace RestaurantApi.Data.Repositories;

public interface IShiftRepository
{
    Task<Shift?> GetByIdAsync(Guid id);
    Task<IEnumerable<Shift>> GetAllAsync();
    Task<IEnumerable<Shift>> GetByLocationAsync(Guid locationId);
    Task AddAsync(Shift shift);
    Task<bool> UpdateAsync(Shift shift);
}

public class ShiftRepository : IShiftRepository
{
    private readonly string _connectionString;

    public ShiftRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Shift?> GetByIdAsync(Guid id)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Shifts WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Shift>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Shift>> GetAllAsync()
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Shifts";
        return await connection.QueryAsync<Shift>(sql);
    }

    public async Task<IEnumerable<Shift>> GetByLocationAsync(Guid locationId)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Shifts WHERE LocationId = @LocationId ORDER BY OpenedAt DESC";
        return await connection.QueryAsync<Shift>(sql, new { LocationId = locationId });
    }

    public async Task AddAsync(Shift shift)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM Shifts WHERE Id = @Id)
            BEGIN
                INSERT INTO Shifts (Id, LocationId, OpenedAt, ClosedAt, OpeningCash, ClosingCash, CreatedAt, UpdatedAt)
                VALUES (@Id, @LocationId, @OpenedAt, @ClosedAt, @OpeningCash, @ClosingCash, @CreatedAt, @UpdatedAt);
            END";
        await connection.ExecuteAsync(sql, shift);
    }

    public async Task<bool> UpdateAsync(Shift shift)
    {
        using IDbConnection connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE Shifts 
            SET LocationId = @LocationId,
                OpenedAt = @OpenedAt,
                ClosedAt = @ClosedAt,
                OpeningCash = @OpeningCash,
                ClosingCash = @ClosingCash,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        var affectedRows = await connection.ExecuteAsync(sql, shift);
        return affectedRows > 0;
    }
}

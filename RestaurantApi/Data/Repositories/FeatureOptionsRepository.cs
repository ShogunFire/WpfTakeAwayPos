using Dapper;
using System.Data.SqlClient;
using RestaurantShared.DTOs;

namespace RestaurantApi.Data.Repositories;

public interface IFeatureOptionsRepository
{
    Task<FeatureOptionsDto> GetAsync();
    Task UpdateAsync(FeatureOptionsDto options);
}

public class FeatureOptionsRepository : IFeatureOptionsRepository
{
    private readonly string _connectionString;

    public FeatureOptionsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<FeatureOptionsDto> GetAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await EnsureTableAndSeedAsync(connection);

        const string sql = @"
            SELECT InventoryModuleEnabled, InventoryCostModuleEnabled
            FROM AppFeatureOptions
            WHERE Id = 1";

        var result = await connection.QueryFirstOrDefaultAsync<FeatureOptionsDto>(sql);
        return result ?? new FeatureOptionsDto();
    }

    public async Task UpdateAsync(FeatureOptionsDto options)
    {
        using var connection = new SqlConnection(_connectionString);
        await EnsureTableAndSeedAsync(connection);

        const string sql = @"
            UPDATE AppFeatureOptions
            SET InventoryModuleEnabled = @InventoryModuleEnabled,
                InventoryCostModuleEnabled = @InventoryCostModuleEnabled,
                UpdatedAt = @Now
            WHERE Id = 1";

        await connection.ExecuteAsync(sql, new
        {
            options.InventoryModuleEnabled,
            options.InventoryCostModuleEnabled,
            Now = DateTime.UtcNow
        });
    }

    private static Task EnsureTableAndSeedAsync(SqlConnection connection)
    {
        const string sql = @"
            IF OBJECT_ID('AppFeatureOptions', 'U') IS NULL
            BEGIN
                CREATE TABLE AppFeatureOptions
                (
                    Id INT NOT NULL PRIMARY KEY,
                    InventoryModuleEnabled BIT NOT NULL,
                    InventoryCostModuleEnabled BIT NOT NULL,
                    UpdatedAt DATETIME2 NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM AppFeatureOptions WHERE Id = 1)
            BEGIN
                INSERT INTO AppFeatureOptions (Id, InventoryModuleEnabled, InventoryCostModuleEnabled, UpdatedAt)
                VALUES (1, 1, 1, SYSUTCDATETIME());
            END;";

        return connection.ExecuteAsync(sql);
    }
}

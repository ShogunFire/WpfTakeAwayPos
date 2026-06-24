using Dapper;
using Microsoft.Data.SqlClient;
using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public interface IFeatureOptionsService
{
    Task<FeatureOptions> GetAsync();
    Task UpdateAsync(FeatureOptions options);
}

public class SqlFeatureOptionsService : IFeatureOptionsService
{
    private readonly string _connectionString;

    public SqlFeatureOptionsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<FeatureOptions> GetAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await EnsureTableAndSeedAsync(connection);

        const string sql = @"
            SELECT InventoryModuleEnabled, InventoryCostModuleEnabled
            FROM AppFeatureOptions
            WHERE Id = 1";

        var result = await connection.QueryFirstOrDefaultAsync<FeatureOptions>(sql);
        return result ?? new FeatureOptions();
    }

    public async Task UpdateAsync(FeatureOptions options)
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

public class FeatureOptionsState
{
    private readonly IFeatureOptionsService _featureOptionsService;

    public FeatureOptionsState(IFeatureOptionsService featureOptionsService)
    {
        _featureOptionsService = featureOptionsService;
    }

    public FeatureOptions Current { get; private set; } = new();
    public bool IsLoaded { get; private set; }

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (IsLoaded)
        {
            return;
        }

        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            Current = await _featureOptionsService.GetAsync();
        }
        catch
        {
            Current = new FeatureOptions();
        }

        IsLoaded = true;
        Changed?.Invoke();
    }

    public async Task SaveAsync(FeatureOptions options)
    {
        await _featureOptionsService.UpdateAsync(options);
        Current = new FeatureOptions
        {
            InventoryModuleEnabled = options.InventoryModuleEnabled,
            InventoryCostModuleEnabled = options.InventoryCostModuleEnabled
        };
        IsLoaded = true;
        Changed?.Invoke();
    }
}

using Dapper;
using Microsoft.Data.SqlClient;
using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public interface IInventoryAnalyticsService
{
    Task<List<InventoryStatus>> GetInventoryStatusAsync(Guid? locationId = null);
}

public class InventoryAnalyticsService : IInventoryAnalyticsService
{
    private readonly string _connectionString;

    public InventoryAnalyticsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<List<InventoryStatus>> GetInventoryStatusAsync(Guid? locationId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var locationFilter = locationId.HasValue ? "AND li.LocationId = @LocationId" : "";

        var sql = $@"
            SELECT 
                ii.Id as InventoryItemId,
                ii.Name,
                ii.Unit,
                COALESCE(SUM(li.Quantity), 0) as CurrentQuantity,
                @LocationId as LocationId
            FROM InventoryItems ii
            LEFT JOIN LocationInventory li ON ii.Id = li.InventoryItemId {locationFilter}
            GROUP BY ii.Id, ii.Name, ii.Unit
            ORDER BY ii.Name";

        var results = await connection.QueryAsync<InventoryStatus>(sql, new { LocationId = locationId });
        return results.ToList();
    }
}

using Dapper;
using Microsoft.Data.SqlClient;
using RestaurantDashboard.Data.Models;

namespace RestaurantDashboard.Data.Services;

public interface ILocationAnalyticsService
{
    Task<List<LocationPerformance>> GetLocationPerformanceAsync(DateTime startDate, DateTime endDate);
    Task<List<Location>> GetLocationsAsync();
}

public class LocationAnalyticsService : ILocationAnalyticsService
{
    private readonly string _connectionString;

    public LocationAnalyticsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<List<LocationPerformance>> GetLocationPerformanceAsync(DateTime startDate, DateTime endDate)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT 
                o.LocationId,
                l.Name as LocationName,
                COUNT(DISTINCT o.Id) as TotalOrders,
                COALESCE(SUM(o.TotalAmount), 0) as TotalRevenue,
                COALESCE(AVG(o.TotalAmount), 0) as AverageOrderValue
            FROM Orders o
            JOIN Locations l ON o.LocationId = l.Id
            WHERE o.CreatedAt BETWEEN @StartDate AND @EndDate
              AND l.IsActive = 1
            GROUP BY o.LocationId, l.Name
            ORDER BY TotalRevenue DESC";

        var results = await connection.QueryAsync<LocationPerformance>(sql, new { StartDate = startDate, EndDate = endDate });
        return results.ToList();
    }

    public async Task<List<Location>> GetLocationsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT Id, Name, Code, IsActive FROM Locations WHERE IsActive = 1 ORDER BY Name";
        var results = await connection.QueryAsync<Location>(sql);
        return results.ToList();
    }
}

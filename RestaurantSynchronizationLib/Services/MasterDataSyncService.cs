using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using RestaurantShared.DTOs;
using RestaurantSynchronizationLib.Configuration;

namespace RestaurantSynchronizationLib.Services.Deprecated;

/// <summary>
/// DEPRECATED: Use RestaurantPOS.Services.MasterDataSyncService instead.
/// This class is kept for backwards compatibility only.
/// </summary>
[Obsolete("Use RestaurantPOS.Services.MasterDataSyncService instead")]
public class MasterDataSyncService : IMasterDataSyncService
{
    private readonly HttpClient _httpClient;
    private readonly string _connectionString;
    private readonly ILogger<MasterDataSyncService> _logger;

    public MasterDataSyncService(
        SyncConfiguration config,
        string connectionString,
        ILogger<MasterDataSyncService> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.ApiBaseAddress),
            Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds)
        };
    }

    /// <summary>
    /// Sync all master data (categories and menu items) from API to local database
    /// </summary>
    public async Task<MasterDataSyncResult> SyncMasterDataAsync()
    {
        var result = new MasterDataSyncResult();

        try
        {
            _logger.LogInformation("Starting master data sync");

            // Sync categories first (as MenuItems depend on them)
            result.CategoriesSync = await SyncCategoriesAsync();
            if (!result.CategoriesSync.Success)
            {
                _logger.LogWarning("Failed to sync categories: {Message}", result.CategoriesSync.Message);
                result.Success = false;
                result.Message = "Failed to sync categories";
                return result;
            }

            // Then sync menu items
            result.MenuItemsSync = await SyncMenuItemsAsync();
            if (!result.MenuItemsSync.Success)
            {
                _logger.LogWarning("Failed to sync menu items: {Message}", result.MenuItemsSync.Message);
                result.Success = false;
                result.Message = "Failed to sync menu items";
                return result;
            }

            result.Success = true;
            result.Message = "Master data synced successfully";
            _logger.LogInformation("Master data sync completed: {Categories} categories, {MenuItems} menu items",
                result.CategoriesSync.Count, result.MenuItemsSync.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing master data");
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Sync categories from API to local database
    /// </summary>
    private async Task<DataSyncResult> SyncCategoriesAsync()
    {
        var result = new DataSyncResult();

        try
        {
            _logger.LogInformation("Fetching categories from API");

            var response = await _httpClient.GetAsync("/api/categories");
            if (!response.IsSuccessStatusCode)
            {
                result.Message = $"API returned status {response.StatusCode}";
                return result;
            }

            var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
            if (categories == null || !categories.Any())
            {
                result.Message = "No categories returned from API";
                return result;
            }

            // Store in local database
            await StoreCategoriesToLocalDbAsync(categories);

            result.Success = true;
            result.Count = categories.Count;
            _logger.LogInformation("Synced {Count} categories", categories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing categories");
            result.Message = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Sync menu items from API to local database
    /// </summary>
    private async Task<DataSyncResult> SyncMenuItemsAsync()
    {
        var result = new DataSyncResult();

        try
        {
            _logger.LogInformation("Fetching menu items from API");

            var response = await _httpClient.GetAsync("/api/menuitems");
            if (!response.IsSuccessStatusCode)
            {
                result.Message = $"API returned status {response.StatusCode}";
                return result;
            }

            var menuItems = await response.Content.ReadFromJsonAsync<List<MenuItemDto>>();
            if (menuItems == null || !menuItems.Any())
            {
                result.Message = "No menu items returned from API";
                return result;
            }

            // Store in local database
            await StoreMenuItemsToLocalDbAsync(menuItems);

            result.Success = true;
            result.Count = menuItems.Count;
            _logger.LogInformation("Synced {Count} menu items", menuItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing menu items");
            result.Message = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Store categories in local SQLite database
    /// </summary>
    private async Task StoreCategoriesToLocalDbAsync(List<CategoryDto> categories)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();

            foreach (var category in categories)
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO Categories (Id, Name, Description, IsActive)
                    VALUES (@Id, @Name, @Description, @IsActive)";
                cmd.Parameters.AddWithValue("@Id", category.Id.ToString());
                cmd.Parameters.AddWithValue("@Name", category.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@Description", category.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@IsActive", category.IsActive ? 1 : 0);

                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogDebug("Stored {Count} categories in local database", categories.Count);
        }
    }

    /// <summary>
    /// Store menu items in local SQLite database
    /// </summary>
    private async Task StoreMenuItemsToLocalDbAsync(List<MenuItemDto> menuItems)
    {
        using (var connection = new SQLiteConnection(_connectionString))
        {
            await connection.OpenAsync();

            foreach (var item in menuItems)
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO MenuItems (Id, IdCategory, Name, Description, Price, IsActive)
                    VALUES (@Id, @IdCategory, @Name, @Description, @Price, @IsActive)";
                cmd.Parameters.AddWithValue("@Id", item.Id.ToString());
                cmd.Parameters.AddWithValue("@IdCategory", item.IdCategory.ToString());
                cmd.Parameters.AddWithValue("@Name", item.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@Description", item.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@Price", item.Price);
                cmd.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);

                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogDebug("Stored {Count} menu items in local database", menuItems.Count);
        }
    }
}

/// <summary>
/// Result of master data sync operation
/// </summary>
public class MasterDataSyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DataSyncResult CategoriesSync { get; set; } = new();
    public DataSyncResult MenuItemsSync { get; set; } = new();
}

/// <summary>
/// Result of syncing a single data type
/// </summary>
public class DataSyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}

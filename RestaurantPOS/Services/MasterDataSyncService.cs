using Microsoft.Data.Sqlite;
using RestaurantPOS.Configuration;
using RestaurantPOS.Data;
using RestaurantShared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RestaurantPOS.Services
{
    public interface IMasterDataSyncService
    {
        Task SyncMasterDataAsync();
    }

    public class MasterDataSyncService : IMasterDataSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly PosSettings _settings;

        public MasterDataSyncService(HttpClient httpClient, PosSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task SyncMasterDataAsync()
        {
            try
            {
                // Sync inventory items
                var inventoryItems = await FetchInventoryItemsAsync();
                if (inventoryItems != null && inventoryItems.Any())
                {
                    SaveInventoryItems(inventoryItems);
                }

                // Sync menu items
                var menuItems = await FetchMenuItemsAsync();
                if (menuItems != null && menuItems.Any())
                {
                    SaveMenuItems(menuItems);
                }

                // Sync menu item components
                var components = await FetchMenuItemComponentsAsync();
                if (components != null && components.Any())
                {
                    SaveMenuItemComponents(components);
                }

                // Sync location-specific inventory quantities if location is set
                if (_settings.LocationId != Guid.Empty)
                {
                    var locationInventory = await FetchLocationInventoryAsync();
                    if (locationInventory != null && locationInventory.Any())
                    {
                        UpdateInventoryQuantities(locationInventory);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                Console.WriteLine($"Error syncing master data: {ex.Message}");
            }
        }

        private async Task<IEnumerable<InventoryItemDto>?> FetchInventoryItemsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/inventoryitems");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<InventoryItemDto>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching inventory items: {ex.Message}");
                return null;
            }
        }

        private async Task<IEnumerable<MenuItemDto>?> FetchMenuItemsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/menuitems");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<MenuItemDto>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching menu items: {ex.Message}");
                return null;
            }
        }

        private async Task<IEnumerable<MenuItemComponentDto>?> FetchMenuItemComponentsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/menuitems/components");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<MenuItemComponentDto>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching menu item components: {ex.Message}");
                return null;
            }
        }

        private async Task<IEnumerable<InventoryItemDto>?> FetchLocationInventoryAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/locations/{_settings.LocationId}/inventory");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<InventoryItemDto>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching location inventory: {ex.Message}");
                return null;
            }
        }

        private void SaveInventoryItems(IEnumerable<InventoryItemDto> items)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Clear existing inventory items
                using (var deleteCmd = connection.CreateCommand())
                {
                    deleteCmd.CommandText = "DELETE FROM InventoryItems";
                    deleteCmd.Transaction = transaction;
                    deleteCmd.ExecuteNonQuery();
                }

                // Insert new inventory items
                foreach (var item in items)
                {
                    using var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = @"
                        INSERT INTO InventoryItems (Id, Name, Unit, Quantity)
                        VALUES (@Id, @Name, @Unit, 0)";
                    
                    insertCmd.Parameters.AddWithValue("@Id", item.InventoryItemId.ToString());
                    insertCmd.Parameters.AddWithValue("@Name", item.Name);
                    insertCmd.Parameters.AddWithValue("@Unit", item.Unit);
                    
                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine($"Synced {items.Count()} inventory items");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error saving inventory items: {ex.Message}");
                throw;
            }
        }

        private void SaveMenuItems(IEnumerable<MenuItemDto> items)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Clear existing menu items
                using (var deleteCmd = connection.CreateCommand())
                {
                    deleteCmd.CommandText = "DELETE FROM MenuItems";
                    deleteCmd.Transaction = transaction;
                    deleteCmd.ExecuteNonQuery();
                }

                // Insert new menu items
                foreach (var item in items)
                {
                    using var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = @"
                        INSERT INTO MenuItems (Id, Name, Description, Price, Category, IsActive)
                        VALUES (@Id, @Name, @Description, @Price, @Category, @IsActive)";
                    
                    insertCmd.Parameters.AddWithValue("@Id", item.Id.ToString());
                    insertCmd.Parameters.AddWithValue("@Name", item.Name);
                    insertCmd.Parameters.AddWithValue("@Description", item.Description ?? string.Empty);
                    insertCmd.Parameters.AddWithValue("@Price", item.Price);
                    insertCmd.Parameters.AddWithValue("@Category", item.Category);
                    insertCmd.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);
                    
                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine($"Synced {items.Count()} menu items");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error saving menu items: {ex.Message}");
                throw;
            }
        }

        private void SaveMenuItemComponents(IEnumerable<MenuItemComponentDto> components)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Clear existing components
                using (var deleteCmd = connection.CreateCommand())
                {
                    deleteCmd.CommandText = "DELETE FROM MenuItemComponents";
                    deleteCmd.Transaction = transaction;
                    deleteCmd.ExecuteNonQuery();
                }

                // Insert new components
                foreach (var component in components)
                {
                    using var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = @"
                        INSERT INTO MenuItemComponents (Id, MenuItemId, InventoryItemId, Quantity)
                        VALUES (@Id, @MenuItemId, @InventoryItemId, @Quantity)";
                    
                    insertCmd.Parameters.AddWithValue("@Id", component.Id.ToString());
                    insertCmd.Parameters.AddWithValue("@MenuItemId", component.MenuItemId.ToString());
                    insertCmd.Parameters.AddWithValue("@InventoryItemId", component.InventoryItemId.ToString());
                    insertCmd.Parameters.AddWithValue("@Quantity", component.Quantity);
                    
                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine($"Synced {components.Count()} menu item components");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error saving menu item components: {ex.Message}");
                throw;
            }
        }

        private void UpdateInventoryQuantities(IEnumerable<InventoryItemDto> items)
        {
            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var item in items)
                {
                    using var updateCmd = connection.CreateCommand();
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = @"
                        UPDATE InventoryItems 
                        SET Quantity = @Quantity
                        WHERE Id = @Id";
                    
                    updateCmd.Parameters.AddWithValue("@Id", item.InventoryItemId.ToString());
                    updateCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Console.WriteLine($"Updated quantities for {items.Count()} inventory items");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error updating inventory quantities: {ex.Message}");
                throw;
            }
        }
    }
}

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
                var categories = await FetchCategoriesAsync();
                var inventoryItems = await FetchInventoryItemsAsync();
                var menuItems = await FetchMenuItemsAsync();
                var components = await FetchMenuItemComponentsAsync();

                // Avoid partial updates that can violate FK constraints and leave stale state.
                if (categories == null || inventoryItems == null || menuItems == null || components == null)
                {
                    Console.WriteLine("Master data sync skipped because one or more endpoints failed.");
                    return;
                }

                SaveMasterData(categories, inventoryItems, menuItems, components);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                Console.WriteLine($"Error syncing master data: {ex.Message}");
            }
        }

        private async Task<IEnumerable<CategoryDto>?> FetchCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/categories");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<CategoryDto>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching categories: {ex.Message}");
                return null;
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



        private void SaveMasterData(
            IEnumerable<CategoryDto> categories,
            IEnumerable<InventoryItemDto> inventoryItems,
            IEnumerable<MenuItemDto> menuItems,
            IEnumerable<MenuItemComponentDto> components)
        {
            var categoriesList = categories.ToList();
            var inventoryList = inventoryItems.ToList();
            var menuItemsList = menuItems.ToList();
            var componentsList = components.ToList();

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                DeleteMasterDataTables(connection, transaction);

                InsertCategories(connection, transaction, categoriesList);
                InsertInventoryItems(connection, transaction, inventoryList);
                InsertMenuItems(connection, transaction, menuItemsList);
                InsertMenuItemComponents(connection, transaction, componentsList);

                transaction.Commit();
                Console.WriteLine($"Synced categories={categoriesList.Count}, inventoryItems={inventoryList.Count}, menuItems={menuItemsList.Count}, components={componentsList.Count}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error saving master data: {ex.Message}");
                throw;
            }
        }

        private static void DeleteMasterDataTables(SqliteConnection connection, SqliteTransaction transaction)
        {
            // Delete children first to satisfy FK constraints.
            using var deleteComponentsCmd = connection.CreateCommand();
            deleteComponentsCmd.Transaction = transaction;
            deleteComponentsCmd.CommandText = "DELETE FROM MenuItemComponents";
            deleteComponentsCmd.ExecuteNonQuery();

            using var deleteMenuItemsCmd = connection.CreateCommand();
            deleteMenuItemsCmd.Transaction = transaction;
            deleteMenuItemsCmd.CommandText = "DELETE FROM MenuItems";
            deleteMenuItemsCmd.ExecuteNonQuery();

            using var deleteCategoriesCmd = connection.CreateCommand();
            deleteCategoriesCmd.Transaction = transaction;
            deleteCategoriesCmd.CommandText = "DELETE FROM Categories";
            deleteCategoriesCmd.ExecuteNonQuery();

            using var deleteInventoryCmd = connection.CreateCommand();
            deleteInventoryCmd.Transaction = transaction;
            deleteInventoryCmd.CommandText = "DELETE FROM InventoryItems";
            deleteInventoryCmd.ExecuteNonQuery();
        }

        private static void InsertCategories(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<CategoryDto> categories)
        {
            foreach (var category in categories)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO Categories (Id, Name, Description, IsActive)
                    VALUES (@Id, @Name, @Description, @IsActive)";

                insertCmd.Parameters.AddWithValue("@Id", category.Id.ToString());
                insertCmd.Parameters.AddWithValue("@Name", category.Name ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@Description", category.Description ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@IsActive", category.IsActive ? 1 : 0);

                insertCmd.ExecuteNonQuery();
            }
        }

        private static void InsertInventoryItems(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<InventoryItemDto> items)
        {
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
        }

        private static void InsertMenuItems(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<MenuItemDto> items)
        {
            foreach (var item in items)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO MenuItems (Id, IdCategory, Name, Description, Price, IsActive)
                    VALUES (@Id, @IdCategory, @Name, @Description, @Price, @IsActive)";

                insertCmd.Parameters.AddWithValue("@Id", item.Id.ToString());
                insertCmd.Parameters.AddWithValue("@IdCategory", item.IdCategory.ToString());
                insertCmd.Parameters.AddWithValue("@Name", item.Name);
                insertCmd.Parameters.AddWithValue("@Description", item.Description ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@Price", item.Price);
                insertCmd.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);

                insertCmd.ExecuteNonQuery();
            }
        }

        private static void InsertMenuItemComponents(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<MenuItemComponentDto> components)
        {
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
        }

       
    }
}

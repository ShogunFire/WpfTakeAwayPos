using Microsoft.Data.Sqlite;
using RestaurantPOS.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;

namespace RestaurantPOS.Services
{
    public class MenuService
    {
        private List<MenuItem> _menuItems;
        private readonly IInventoryService _inventoryService;

        public MenuService(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
            _menuItems = LoadMenuItems();
        }

        private System.Guid GetInventoryId(string name)
        {
            var item = _inventoryService.FindByName(name);
            return item?.InventoryItemId ?? System.Guid.Empty;
        }

        public List<MenuItem> GetMenuItems()
        {
            _menuItems = LoadMenuItems();
            return _menuItems;
        }

        public List<Category> GetCategories()
        {
            return LoadCategories();
        }

        public MenuItem GetMenuItemById(int id)
        {
            return _menuItems.Find(item => item.MenuItemId == id);
        }

        public MenuItem InsertMenuItem(MenuItem menuItem)
        {
            if (menuItem == null)
                return null;

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO MenuItems (Id, IdCategory, Name, Description, Price, IsActive)
                                VALUES (@Id, @IdCategory, @Name, @Description, @Price, @IsActive);";
            cmd.Parameters.AddWithValue("@Id", menuItem.MenuItemGuid.ToString());
            cmd.Parameters.AddWithValue("@IdCategory", menuItem.CategoryId.ToString());
            cmd.Parameters.AddWithValue("@Name", menuItem.Name);
            cmd.Parameters.AddWithValue("@Description", "");
            cmd.Parameters.AddWithValue("@Price", menuItem.Price);
            cmd.Parameters.AddWithValue("@IsActive", 1);
            cmd.ExecuteNonQuery();

            foreach (var component in menuItem.Components ?? new List<MenuItemComponent>())
            {
                using var componentCmd = connection.CreateCommand();
                componentCmd.CommandText = @"INSERT INTO MenuItemComponents (Id, MenuItemId, InventoryItemId, Quantity)
                                             VALUES (@Id, @MenuItemId, @InventoryItemId, @Quantity);";
                componentCmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                componentCmd.Parameters.AddWithValue("@MenuItemId", menuItem.MenuItemGuid.ToString());
                componentCmd.Parameters.AddWithValue("@InventoryItemId", component.InventoryItemId.ToString());
                componentCmd.Parameters.AddWithValue("@Quantity", component.QuantityUsed);
                componentCmd.ExecuteNonQuery();
            }

            _menuItems.Add(menuItem);
            return menuItem;
        }

        private List<MenuItem> LoadMenuItems()
        {
            var componentsByMenu = LoadComponentsByMenu();
            var items = new List<MenuItem>();
            var menuItemCounter = 0;

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, IdCategory, Name, Price FROM MenuItems ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                menuItemCounter++;
                var menuGuid = Guid.Parse(reader.GetString(0));
                var categoryGuid = Guid.Parse(reader.GetString(1));
                var name = reader.GetString(2);
                var price = Convert.ToDecimal(reader.GetValue(3));

                var components = componentsByMenu.TryGetValue(menuGuid.GetHashCode(), out var comps)
                    ? comps
                    : new List<MenuItemComponent>();

                var menuItem = new MenuItem(menuItemCounter, categoryGuid, name, price, components)
                {
                    MenuItemGuid = menuGuid
                };

                items.Add(menuItem);
            }

            return items;
        }

        private List<Category> LoadCategories()
        {
            var categories = new List<Category>();

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, IsActive FROM Categories ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    CategoryId = Guid.Parse(reader.GetString(0)),
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    IsActive = !reader.IsDBNull(3) && Convert.ToInt32(reader.GetValue(3)) == 1
                });
            }

            return categories;
        }

        private Dictionary<int, List<MenuItemComponent>> LoadComponentsByMenu()
        {
            var result = new Dictionary<int, List<MenuItemComponent>>();

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT MenuItemId, InventoryItemId, Quantity FROM MenuItemComponents";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var menuItemId = Guid.Parse(reader.GetString(0));
                var inventoryId = Guid.Parse(reader.GetString(1));
                var quantity = Convert.ToDecimal(reader.GetValue(2));

                if (!result.TryGetValue(menuItemId.GetHashCode(), out var list))
                {
                    list = new List<MenuItemComponent>();
                    result[menuItemId.GetHashCode()] = list;
                }

                list.Add(new MenuItemComponent(inventoryId, quantity));
            }

            return result;
        }
    }
}
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
            return _menuItems;
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
            cmd.CommandText = @"INSERT INTO MenuItems (MenuItemId, MenuItemGuid, CategoryId, Name, Price)
                                VALUES (@MenuItemId, @MenuItemGuid, @CategoryId, @Name, @Price);";
            cmd.Parameters.AddWithValue("@MenuItemId", menuItem.MenuItemId);
            cmd.Parameters.AddWithValue("@MenuItemGuid", menuItem.MenuItemGuid.ToString());
            cmd.Parameters.AddWithValue("@CategoryId", menuItem.CategoryId);
            cmd.Parameters.AddWithValue("@Name", menuItem.Name);
            cmd.Parameters.AddWithValue("@Price", menuItem.Price);
            cmd.ExecuteNonQuery();

            foreach (var component in menuItem.Components ?? new List<MenuItemComponent>())
            {
                using var componentCmd = connection.CreateCommand();
                componentCmd.CommandText = @"INSERT INTO MenuItemComponents (MenuItemId, InventoryItemId, QuantityUsed)
                                             VALUES (@MenuItemId, @InventoryItemId, @QuantityUsed);";
                componentCmd.Parameters.AddWithValue("@MenuItemId", menuItem.MenuItemId);
                componentCmd.Parameters.AddWithValue("@InventoryItemId", component.InventoryItemId.ToString());
                componentCmd.Parameters.AddWithValue("@QuantityUsed", component.QuantityUsed);
                componentCmd.ExecuteNonQuery();
            }

            _menuItems.Add(menuItem);
            return menuItem;
        }

        private List<MenuItem> LoadMenuItems()
        {
            var componentsByMenu = LoadComponentsByMenu();
            var items = new List<MenuItem>();

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT MenuItemId, MenuItemGuid, CategoryId, Name, Price FROM MenuItems ORDER BY MenuItemId";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var menuItemId = reader.GetInt32(0);
                var menuGuid = Guid.Parse(reader.GetString(1));
                var categoryId = reader.GetInt32(2);
                var name = reader.GetString(3);
                var price = Convert.ToDecimal(reader.GetValue(4));

                var components = componentsByMenu.TryGetValue(menuItemId, out var comps)
                    ? comps
                    : new List<MenuItemComponent>();

                var menuItem = new MenuItem(menuItemId, categoryId, name, price, components)
                {
                    MenuItemGuid = menuGuid
                };

                items.Add(menuItem);
            }

            return items;
        }

        private Dictionary<int, List<MenuItemComponent>> LoadComponentsByMenu()
        {
            var result = new Dictionary<int, List<MenuItemComponent>>();

            using var connection = SqliteDb.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT MenuItemId, InventoryItemId, QuantityUsed FROM MenuItemComponents";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var menuItemId = reader.GetInt32(0);
                var inventoryId = Guid.Parse(reader.GetString(1));
                var quantityUsed = Convert.ToDecimal(reader.GetValue(2));

                if (!result.TryGetValue(menuItemId, out var list))
                {
                    list = new List<MenuItemComponent>();
                    result[menuItemId] = list;
                }

                list.Add(new MenuItemComponent(inventoryId, quantityUsed));
            }

            return result;
        }
    }
}
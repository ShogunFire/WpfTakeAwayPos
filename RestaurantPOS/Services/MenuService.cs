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
            cmd.CommandText = @"INSERT INTO MenuItems (Id, Name, Description, Price, Category, IsActive)
                                VALUES (@Id, @Name, @Description, @Price, @Category, @IsActive);";
            cmd.Parameters.AddWithValue("@Id", menuItem.MenuItemGuid.ToString());
            cmd.Parameters.AddWithValue("@Name", menuItem.Name);
            cmd.Parameters.AddWithValue("@Description", "");
            cmd.Parameters.AddWithValue("@Price", menuItem.Price);
            cmd.Parameters.AddWithValue("@Category", "General");
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
            cmd.CommandText = "SELECT Id, Name, Price FROM MenuItems ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                menuItemCounter++;
                var menuGuid = Guid.Parse(reader.GetString(0));
                var name = reader.GetString(1);
                var price = Convert.ToDecimal(reader.GetValue(2));

                var components = componentsByMenu.TryGetValue(menuGuid.GetHashCode(), out var comps)
                    ? comps
                    : new List<MenuItemComponent>();

                var menuItem = new MenuItem(menuItemCounter, 0, name, price, components)
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
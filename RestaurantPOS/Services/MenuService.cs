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

            var chickenId = GetInventoryId("Chicken");
            var friesId = GetInventoryId("Fries");
            var cocaColaId = GetInventoryId("Coca-Cola");

            _menuItems = new List<MenuItem>
            {
                // Appetizers (Category 2)
                new MenuItem(1, 2, "Bruschetta", 6.50m),
                new MenuItem(2, 2, "Calamari", 8.99m),
                new MenuItem(3, 2, "Chicken Wings", 9.50m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(chickenId, 0.25m)
                    }),
                new MenuItem(4, 2, "Nachos", 7.99m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(friesId, 0.15m)
                    }),

                // Burgers (Category 3)
                new MenuItem(5, 3, "Classic Cheeseburger", 12.50m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(chickenId, 0.25m),
                        new MenuItemComponent(friesId, 0.15m)
                    }),
                new MenuItem(6, 3, "Spicy Chicken Sandwich", 11.00m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(chickenId, 0.20m)
                    }),
                new MenuItem(7, 3, "Bacon Burger", 13.99m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(chickenId, 0.30m),
                        new MenuItemComponent(friesId, 0.10m)
                    }),
                new MenuItem(8, 3, "Double Burger", 15.50m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(chickenId, 0.40m),
                        new MenuItemComponent(friesId, 0.20m)
                    }),
                new MenuItem(9, 3, "Veggie Burger", 10.99m),

                // Pizzas (Category 4)
                new MenuItem(10, 4, "Margherita Pizza", 14.99m),
                new MenuItem(11, 4, "Pepperoni Pizza", 15.99m),
                new MenuItem(12, 4, "Veggie Pizza", 14.49m),
                new MenuItem(13, 4, "BBQ Pizza", 16.99m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(chickenId, 0.10m)
                    }),

                // Beverages (Category 5)
                new MenuItem(14, 5, "Coca-Cola", 2.75m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(cocaColaId, 1m)
                    }),
                new MenuItem(15, 5, "Sprite", 2.75m),
                new MenuItem(16, 5, "Orange Juice", 3.99m),
                new MenuItem(17, 5, "Iced Tea", 2.99m),
                new MenuItem(18, 5, "Lemonade", 3.49m),
                new MenuItem(19, 5, "Water", 1.99m),

                // Sides (Category 1 - All Items)
                new MenuItem(20, 1, "Large Fries", 4.50m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(friesId, 0.20m)
                    }),
                new MenuItem(21, 1, "Onion Rings", 4.99m,
                    components: new List<MenuItemComponent>
                    {
                        new MenuItemComponent(friesId, 0.15m)
                    }),
                new MenuItem(22, 1, "Mozzarella Sticks", 5.99m)
            };
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
    }
}
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace RestaurantPOS.Data
{
    public static class SqliteDb
    {
        public static string DbPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RestaurantPOS",
            "restaurantpos.db");

        public static string ConnectionString => $"Data Source={DbPath}";

        public static SqliteConnection CreateConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        public static void Initialize()
        {
            var directory = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = CreateConnection();
            connection.Open();

            CreateTables(connection);
            SeedIfEmpty(connection);
        }

        private static void CreateTables(SqliteConnection connection)
        {
            var commands = new List<string>
            {
                @"CREATE TABLE IF NOT EXISTS InventoryItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InventoryItemId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Quantity REAL NOT NULL,
                    Unit TEXT NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS MenuItems (
                    MenuItemId INTEGER PRIMARY KEY,
                    MenuItemGuid TEXT NOT NULL,
                    CategoryId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Price REAL NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS MenuItemComponents (
                    MenuItemId INTEGER NOT NULL,
                    InventoryItemId TEXT NOT NULL,
                    QuantityUsed REAL NOT NULL,
                    FOREIGN KEY(MenuItemId) REFERENCES MenuItems(MenuItemId)
                );",
                @"CREATE TABLE IF NOT EXISTS CashTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ShiftId INTEGER,
                    Timestamp TEXT NOT NULL,
                    Type INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    Reason TEXT,
                    Description TEXT,
                    FOREIGN KEY(ShiftId) REFERENCES Shifts(ShiftId)
                );",
                @"CREATE TABLE IF NOT EXISTS InventoryCostRecords (
                    Id TEXT PRIMARY KEY,
                    ShiftId INTEGER,
                    InventoryItemId TEXT NOT NULL,
                    ItemName TEXT NOT NULL,
                    QuantityReceived REAL NOT NULL,
                    TotalCost REAL NOT NULL,
                    RecordedDate TEXT NOT NULL,
                    Notes TEXT,
                    FOREIGN KEY(ShiftId) REFERENCES Shifts(ShiftId)
                );",
                @"CREATE TABLE IF NOT EXISTS Orders (
                    OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ShiftId INTEGER,
                    Subtotal REAL NOT NULL,
                    Tax REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    TotalPaid REAL NOT NULL,
                    Remaining REAL NOT NULL,
                    TotalChange REAL NOT NULL,
                    FOREIGN KEY(ShiftId) REFERENCES Shifts(ShiftId)
                );",
                @"CREATE TABLE IF NOT EXISTS OrderLines (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    ItemJson TEXT NOT NULL,
                    Quantity INTEGER NOT NULL,
                    FOREIGN KEY(OrderId) REFERENCES Orders(OrderId)
                );",
                @"CREATE TABLE IF NOT EXISTS Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    PaymentMethod TEXT NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS Shifts (
                    ShiftId INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartDateTime TEXT NOT NULL,
                    EndDateTime TEXT,
                    OpeningCash REAL NOT NULL,
                    DeclaredCash REAL,
                    ExpectedCash REAL,
                    Difference REAL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    UserId TEXT,
                    Notes TEXT
                );"
            };

            foreach (var sql in commands)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedIfEmpty(SqliteConnection connection)
        {
            using var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(1) FROM InventoryItems";
            var inventoryCount = Convert.ToInt32(countCmd.ExecuteScalar());

            if (inventoryCount == 0)
            {
                SeedInventory(connection);
            }

            using var menuCountCmd = connection.CreateCommand();
            menuCountCmd.CommandText = "SELECT COUNT(1) FROM MenuItems";
            var menuCount = Convert.ToInt32(menuCountCmd.ExecuteScalar());

            if (menuCount == 0)
            {
                SeedMenu(connection);
            }
        }

        private static void SeedInventory(SqliteConnection connection)
        {
            var items = new (string Name, decimal Quantity, string Unit, Guid InventoryItemId)[]
            {
                ("Chicken", 50m, "unit", Guid.Parse("11111111-1111-1111-1111-111111111111")),
                ("Fries", 20m, "kg", Guid.Parse("22222222-2222-2222-2222-222222222222")),
                ("Coca-Cola", 100m, "bottle", Guid.Parse("33333333-3333-3333-3333-333333333333"))
            };

            foreach (var item in items)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO InventoryItems (InventoryItemId, Name, Quantity, Unit)
                                    VALUES (@InventoryItemId, @Name, @Quantity, @Unit);";
                cmd.Parameters.AddWithValue("@InventoryItemId", item.InventoryItemId.ToString());
                cmd.Parameters.AddWithValue("@Name", item.Name);
                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@Unit", item.Unit);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedMenu(SqliteConnection connection)
        {
            var menuItems = new List<(int MenuItemId, int CategoryId, string Name, decimal Price, List<(string InventoryName, decimal Qty)> Components)>
            {
                (1, 2, "Bruschetta", 6.50m, new List<(string, decimal)>()),
                (2, 2, "Calamari", 8.99m, new List<(string, decimal)>()),
                (3, 2, "Chicken Wings", 9.50m, new List<(string, decimal)>{ ("Chicken", 0.25m) }),
                (4, 2, "Nachos", 7.99m, new List<(string, decimal)>{ ("Fries", 0.15m) }),
                (5, 3, "Classic Cheeseburger", 12.50m, new List<(string, decimal)>{ ("Chicken", 0.25m), ("Fries", 0.15m) }),
                (6, 3, "Spicy Chicken Sandwich", 11.00m, new List<(string, decimal)>{ ("Chicken", 0.20m) }),
                (7, 3, "Bacon Burger", 13.99m, new List<(string, decimal)>{ ("Chicken", 0.30m), ("Fries", 0.10m) }),
                (8, 3, "Double Burger", 15.50m, new List<(string, decimal)>{ ("Chicken", 0.40m), ("Fries", 0.20m) }),
                (9, 3, "Veggie Burger", 10.99m, new List<(string, decimal)>()),
                (10, 4, "Margherita Pizza", 14.99m, new List<(string, decimal)>()),
                (11, 4, "Pepperoni Pizza", 15.99m, new List<(string, decimal)>()),
                (12, 4, "Veggie Pizza", 14.49m, new List<(string, decimal)>()),
                (13, 4, "BBQ Pizza", 16.99m, new List<(string, decimal)>{ ("Chicken", 0.10m) }),
                (14, 5, "Coca-Cola", 2.75m, new List<(string, decimal)>{ ("Coca-Cola", 1m) }),
                (15, 5, "Sprite", 2.75m, new List<(string, decimal)>()),
                (16, 5, "Orange Juice", 3.99m, new List<(string, decimal)>()),
                (17, 5, "Iced Tea", 2.99m, new List<(string, decimal)>()),
                (18, 5, "Lemonade", 3.49m, new List<(string, decimal)>()),
                (19, 5, "Water", 1.99m, new List<(string, decimal)>()),
                (20, 1, "Large Fries", 4.50m, new List<(string, decimal)>{ ("Fries", 0.20m) }),
                (21, 1, "Onion Rings", 4.99m, new List<(string, decimal)>{ ("Fries", 0.15m) }),
                (22, 1, "Mozzarella Sticks", 5.99m, new List<(string, decimal)>()),
            };

            foreach (var menu in menuItems)
            {
                var menuGuid = Guid.NewGuid();
                using var insertMenu = connection.CreateCommand();
                insertMenu.CommandText = @"INSERT INTO MenuItems (MenuItemId, MenuItemGuid, CategoryId, Name, Price)
                                          VALUES (@MenuItemId, @MenuItemGuid, @CategoryId, @Name, @Price);";
                insertMenu.Parameters.AddWithValue("@MenuItemId", menu.MenuItemId);
                insertMenu.Parameters.AddWithValue("@MenuItemGuid", menuGuid.ToString());
                insertMenu.Parameters.AddWithValue("@CategoryId", menu.CategoryId);
                insertMenu.Parameters.AddWithValue("@Name", menu.Name);
                insertMenu.Parameters.AddWithValue("@Price", menu.Price);
                insertMenu.ExecuteNonQuery();

                foreach (var component in menu.Components)
                {
                    var inventoryId = GetInventoryIdByName(connection, component.InventoryName);
                    if (inventoryId == Guid.Empty)
                        continue;

                    using var insertComponent = connection.CreateCommand();
                    insertComponent.CommandText = @"INSERT INTO MenuItemComponents (MenuItemId, InventoryItemId, QuantityUsed)
                                                   VALUES (@MenuItemId, @InventoryItemId, @QuantityUsed);";
                    insertComponent.Parameters.AddWithValue("@MenuItemId", menu.MenuItemId);
                    insertComponent.Parameters.AddWithValue("@InventoryItemId", inventoryId.ToString());
                    insertComponent.Parameters.AddWithValue("@QuantityUsed", component.Qty);
                    insertComponent.ExecuteNonQuery();
                }
            }
        }

        private static Guid GetInventoryIdByName(SqliteConnection connection, string name)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT InventoryItemId FROM InventoryItems WHERE Name = @Name LIMIT 1";
            cmd.Parameters.AddWithValue("@Name", name);
            var result = cmd.ExecuteScalar() as string;
            return Guid.TryParse(result, out var id) ? id : Guid.Empty;
        }
    }
}

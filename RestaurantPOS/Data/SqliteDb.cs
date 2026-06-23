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
                @"CREATE TABLE IF NOT EXISTS Categories (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );",
                @"CREATE TABLE IF NOT EXISTS InventoryItems (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Unit TEXT NOT NULL,
                    Quantity REAL NOT NULL DEFAULT 0
                );",
                @"CREATE TABLE IF NOT EXISTS MenuItems (
                    Id TEXT PRIMARY KEY,
                    IdCategory TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Price REAL NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY(IdCategory) REFERENCES Categories(Id)
                );",
                @"CREATE TABLE IF NOT EXISTS MenuItemComponents (
                    Id TEXT PRIMARY KEY,
                    MenuItemId TEXT NOT NULL,
                    InventoryItemId TEXT NOT NULL,
                    Quantity REAL NOT NULL,
                    FOREIGN KEY(MenuItemId) REFERENCES MenuItems(Id),
                    FOREIGN KEY(InventoryItemId) REFERENCES InventoryItems(Id)
                );",
                @"CREATE TABLE IF NOT EXISTS CashTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionGuid TEXT,
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
                    ShiftGuid TEXT,
                    StartDateTime TEXT NOT NULL,
                    EndDateTime TEXT,
                    OpeningCash REAL NOT NULL,
                    DeclaredCash REAL,
                    ExpectedCash REAL,
                    Difference REAL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    UserId TEXT,
                    Notes TEXT
                );",
                @"CREATE TABLE IF NOT EXISTS SyncEvents (
                    Id TEXT PRIMARY KEY,
                    Type TEXT NOT NULL,
                    Payload TEXT NOT NULL,
                    CreatedAt DATETIME NOT NULL,
                    SyncedAt DATETIME NULL,
                    DeviceId TEXT NOT NULL,
                    LocationId TEXT
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
            // No longer seed inventory items or menu items
            // These will be synced from the server instead
        }

        private static Guid GetInventoryIdByName(SqliteConnection connection, string name)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id FROM InventoryItems WHERE Name = @Name LIMIT 1";
            cmd.Parameters.AddWithValue("@Name", name);
            var result = cmd.ExecuteScalar() as string;
            return Guid.TryParse(result, out var id) ? id : Guid.Empty;
        }
    }
}

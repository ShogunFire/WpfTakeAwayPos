-- Create database tables for RestaurantApi
-- All tables use UNIQUEIDENTIFIER (GUID) as primary key named 'Id'

-- ============================================================================
-- DROP TABLES IN CORRECT ORDER (respecting foreign key constraints)
-- ============================================================================

IF OBJECT_ID('Expenses', 'U') IS NOT NULL DROP TABLE Expenses;
IF OBJECT_ID('Payments', 'U') IS NOT NULL DROP TABLE Payments;
IF OBJECT_ID('OrderLines', 'U') IS NOT NULL DROP TABLE OrderLines;
IF OBJECT_ID('MenuItemGrossProfitHistory', 'U') IS NOT NULL DROP TABLE MenuItemGrossProfitHistory;
IF OBJECT_ID('Orders', 'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('CashTransactions', 'U') IS NOT NULL DROP TABLE CashTransactions;
IF OBJECT_ID('MenuItemComponents', 'U') IS NOT NULL DROP TABLE MenuItemComponents;
IF OBJECT_ID('InventoryCostRecords', 'U') IS NOT NULL DROP TABLE InventoryCostRecords;
IF OBJECT_ID('LocationInventory', 'U') IS NOT NULL DROP TABLE LocationInventory;
IF OBJECT_ID('MenuItems', 'U') IS NOT NULL DROP TABLE MenuItems;
IF OBJECT_ID('Shifts', 'U') IS NOT NULL DROP TABLE Shifts;
IF OBJECT_ID('ExpenseCategories', 'U') IS NOT NULL DROP TABLE ExpenseCategories;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('InventoryItems', 'U') IS NOT NULL DROP TABLE InventoryItems;
IF OBJECT_ID('ProcessedEvents', 'U') IS NOT NULL DROP TABLE ProcessedEvents;
IF OBJECT_ID('Locations', 'U') IS NOT NULL DROP TABLE Locations;

-- ============================================================================
-- CREATE TABLES (clean-slate, no backward-compat migrations)
-- ============================================================================

CREATE TABLE Locations (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Code NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
CREATE INDEX IX_Locations_Code ON Locations(Code);

CREATE TABLE Shifts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    OpenedAt DATETIME2 NOT NULL,
    ClosedAt DATETIME2 NULL,
    OpeningCash DECIMAL(18,2) NOT NULL DEFAULT(0),
    ClosingCash DECIMAL(18,2) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Shifts_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id)
);
CREATE INDEX IX_Shifts_LocationId ON Shifts(LocationId);
CREATE INDEX IX_Shifts_OpenedAt ON Shifts(OpenedAt);

CREATE TABLE ProcessedEvents (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EventType NVARCHAR(100) NOT NULL,
    Payload NVARCHAR(MAX) NULL,
    Status NVARCHAR(20) NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    EventCreatedAt DATETIME2 NULL,
    ReceivedAt DATETIME2 NOT NULL,
    LastAttemptAt DATETIME2 NULL,
    AttemptCount INT NOT NULL DEFAULT(0),
    ProcessedAt DATETIME2 NULL,
    DeviceId NVARCHAR(255) NULL,
    LocationId UNIQUEIDENTIFIER NULL
);
CREATE INDEX IX_ProcessedEvents_Status ON ProcessedEvents(Status);
CREATE INDEX IX_ProcessedEvents_EventCreatedAt ON ProcessedEvents(EventCreatedAt);
CREATE INDEX IX_ProcessedEvents_ReceivedAt ON ProcessedEvents(ReceivedAt);

CREATE TABLE InventoryItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Unit NVARCHAR(50) NOT NULL,
    CurrentUnitCost DECIMAL(18,4) NOT NULL DEFAULT(0),
    LastCostUpdate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
CREATE INDEX IX_InventoryItems_Name ON InventoryItems(Name);

CREATE TABLE Categories (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
CREATE INDEX IX_Categories_Name ON Categories(Name);

CREATE TABLE MenuItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    IdCategory UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    CurrentCOGS DECIMAL(18,4) NOT NULL DEFAULT(0),
    LastCOGSUpdate DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_MenuItems_Categories FOREIGN KEY (IdCategory) REFERENCES Categories(Id)
);
CREATE INDEX IX_MenuItems_Name ON MenuItems(Name);
CREATE INDEX IX_MenuItems_IdCategory ON MenuItems(IdCategory);

CREATE TABLE MenuItemComponents (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    MenuItemId UNIQUEIDENTIFIER NOT NULL,
    InventoryItemId UNIQUEIDENTIFIER NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    CONSTRAINT FK_MenuItemComponents_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MenuItemComponents_InventoryItems FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id)
);
CREATE INDEX IX_MenuItemComponents_MenuItemId ON MenuItemComponents(MenuItemId);
CREATE INDEX IX_MenuItemComponents_InventoryItemId ON MenuItemComponents(InventoryItemId);

CREATE TABLE MenuItemGrossProfitHistory (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    MenuItemId UNIQUEIDENTIFIER NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,
    GrossProfit DECIMAL(18,2) NOT NULL,
    GrossMargin DECIMAL(5,4) NOT NULL,
    SnapshotDate DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_MenuItemGrossProfitHistory_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
);
CREATE INDEX IX_MenuItemGrossProfitHistory_MenuItemId ON MenuItemGrossProfitHistory(MenuItemId);
CREATE INDEX IX_MenuItemGrossProfitHistory_SnapshotDate ON MenuItemGrossProfitHistory(SnapshotDate);

CREATE TABLE LocationInventory (
    LocationId UNIQUEIDENTIFIER NOT NULL,
    InventoryItemId UNIQUEIDENTIFIER NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT PK_LocationInventory PRIMARY KEY (LocationId, InventoryItemId),
    CONSTRAINT FK_LocationInventory_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id),
    CONSTRAINT FK_LocationInventory_InventoryItems FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id)
);
CREATE INDEX IX_LocationInventory_InventoryItemId ON LocationInventory(InventoryItemId);

CREATE TABLE InventoryCostRecords (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EventId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    ShiftId UNIQUEIDENTIFIER NULL,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    InventoryItemId UNIQUEIDENTIFIER NOT NULL,
    QuantityReceived DECIMAL(18,4) NOT NULL,
    TotalCost DECIMAL(18,2) NOT NULL,
    RecordedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_InventoryCostRecords_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id),
    CONSTRAINT FK_InventoryCostRecords_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id),
    CONSTRAINT FK_InventoryCostRecords_InventoryItems FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems(Id)
);
CREATE INDEX IX_InventoryCostRecords_InventoryItemId ON InventoryCostRecords(InventoryItemId);
CREATE INDEX IX_InventoryCostRecords_LocationId ON InventoryCostRecords(LocationId);
CREATE INDEX IX_InventoryCostRecords_ShiftId ON InventoryCostRecords(ShiftId);

CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ShiftId UNIQUEIDENTIFIER NULL,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    Tax DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    TotalPaid DECIMAL(18,2) NOT NULL,
    Remaining DECIMAL(18,2) NOT NULL,
    TotalChange DECIMAL(18,2) NOT NULL,
    TotalCOGS DECIMAL(18,2) NOT NULL DEFAULT(0),
    GrossProfit DECIMAL(18,2) NOT NULL DEFAULT(0),
    ProfitMargin DECIMAL(5,2) NOT NULL DEFAULT(0),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Orders_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id),
    CONSTRAINT FK_Orders_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id)
);
CREATE INDEX IX_Orders_ShiftId ON Orders(ShiftId);
CREATE INDEX IX_Orders_LocationId ON Orders(LocationId);
CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);

CREATE TABLE OrderLines (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    MenuItemName NVARCHAR(255) NOT NULL,
    MenuItemId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT FK_OrderLines_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderLines_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id)
);
CREATE INDEX IX_OrderLines_OrderId ON OrderLines(OrderId);
CREATE INDEX IX_OrderLines_LocationId ON OrderLines(LocationId);

CREATE TABLE Payments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    CONSTRAINT FK_Payments_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id)
);
CREATE INDEX IX_Payments_OrderId ON Payments(OrderId);
CREATE INDEX IX_Payments_LocationId ON Payments(LocationId);

CREATE TABLE CashTransactions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ShiftId UNIQUEIDENTIFIER NULL,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(255) NULL,
    Description NVARCHAR(255) NULL,
    OccurredAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_CashTransactions_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id),
    CONSTRAINT FK_CashTransactions_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id)
);
CREATE INDEX IX_CashTransactions_ShiftId ON CashTransactions(ShiftId);
CREATE INDEX IX_CashTransactions_LocationId ON CashTransactions(LocationId);
CREATE INDEX IX_CashTransactions_OccurredAt ON CashTransactions(OccurredAt);

CREATE TABLE ExpenseCategories (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsCOGS BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
CREATE INDEX IX_ExpenseCategories_Name ON ExpenseCategories(Name);
CREATE INDEX IX_ExpenseCategories_IsCOGS ON ExpenseCategories(IsCOGS);

CREATE TABLE Expenses (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ExpenseCategoryId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    ExpenseDate DATETIME2 NOT NULL,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    ShiftId UNIQUEIDENTIFIER NULL,
    InventoryCostRecordId UNIQUEIDENTIFIER NULL,
    CashTransactionId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Expenses_ExpenseCategories FOREIGN KEY (ExpenseCategoryId) REFERENCES ExpenseCategories(Id),
    CONSTRAINT FK_Expenses_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id),
    CONSTRAINT FK_Expenses_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id),
    CONSTRAINT FK_Expenses_InventoryCostRecords FOREIGN KEY (InventoryCostRecordId) REFERENCES InventoryCostRecords(Id),
    CONSTRAINT FK_Expenses_CashTransactions FOREIGN KEY (CashTransactionId) REFERENCES CashTransactions(Id)
);
CREATE INDEX IX_Expenses_ExpenseDate ON Expenses(ExpenseDate);
CREATE INDEX IX_Expenses_LocationId ON Expenses(LocationId);
CREATE INDEX IX_Expenses_ExpenseCategoryId ON Expenses(ExpenseCategoryId);
CREATE INDEX IX_Expenses_InventoryCostRecordId ON Expenses(InventoryCostRecordId);
CREATE INDEX IX_Expenses_CashTransactionId ON Expenses(CashTransactionId);

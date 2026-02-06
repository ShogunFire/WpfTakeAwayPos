-- Create database tables for RestaurantApi
-- All tables use UNIQUEIDENTIFIER (GUID) as primary key named 'Id'

-- Locations table (master data, no shift)
IF OBJECT_ID('Locations', 'U') IS NULL
BEGIN
    CREATE TABLE Locations (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );

    CREATE INDEX IX_Locations_Code ON Locations(Code);
END;

-- Shifts table
IF OBJECT_ID('Shifts', 'U') IS NULL
BEGIN
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
END;

-- ProcessedEvents table (system table, no shift)
IF OBJECT_ID('ProcessedEvents', 'U') IS NULL
BEGIN
    CREATE TABLE ProcessedEvents (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        EventType NVARCHAR(100) NOT NULL,
        Payload NVARCHAR(MAX) NULL,
        Status NVARCHAR(20) NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ReceivedAt DATETIME2 NOT NULL,
        LastAttemptAt DATETIME2 NULL,
        AttemptCount INT NOT NULL DEFAULT(0),
        ProcessedAt DATETIME2 NULL,
        DeviceId NVARCHAR(255) NULL
    );
    
    CREATE INDEX IX_ProcessedEvents_Status ON ProcessedEvents(Status);
    CREATE INDEX IX_ProcessedEvents_ReceivedAt ON ProcessedEvents(ReceivedAt);
END;

-- InventoryItems table (master data, no shift)
IF OBJECT_ID('InventoryItems', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryItems (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        Unit NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
    
    CREATE INDEX IX_InventoryItems_Name ON InventoryItems(Name);
END;

-- MenuItems table (master data, no shift)
IF OBJECT_ID('MenuItems', 'U') IS NULL
BEGIN
    CREATE TABLE MenuItems (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Price DECIMAL(18,2) NOT NULL,
        Category NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
    
    CREATE INDEX IX_MenuItems_Name ON MenuItems(Name);
    CREATE INDEX IX_MenuItems_Category ON MenuItems(Category);
END;

-- MenuItemComponents table (links menu items to inventory items with quantities)
IF OBJECT_ID('MenuItemComponents', 'U') IS NULL
BEGIN
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
END;

-- LocationInventory table (per-location stock, no shift needed)
IF OBJECT_ID('LocationInventory', 'U') IS NULL
BEGIN
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
END;

-- InventoryCostRecords table (linked to shift for cost tracking)
IF OBJECT_ID('InventoryCostRecords', 'U') IS NULL
BEGIN
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
END;

-- Orders table
IF OBJECT_ID('Orders', 'U') IS NULL
BEGIN
    CREATE TABLE Orders (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ShiftId UNIQUEIDENTIFIER NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        Tax DECIMAL(18,2) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        TotalPaid DECIMAL(18,2) NOT NULL,
        Remaining DECIMAL(18,2) NOT NULL,
        TotalChange DECIMAL(18,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_Orders_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id)
    );
    
    CREATE INDEX IX_Orders_ShiftId ON Orders(ShiftId);
    CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);
END;

-- OrderLines table (transitive via Order → Shift)
IF OBJECT_ID('OrderLines', 'U') IS NULL
BEGIN
    CREATE TABLE OrderLines (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        MenuItemName NVARCHAR(255) NOT NULL,
        MenuItemId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_OrderLines_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_OrderLines_OrderId ON OrderLines(OrderId);
END;

-- Payments table (transitive via Order → Shift)
IF OBJECT_ID('Payments', 'U') IS NULL
BEGIN
    CREATE TABLE Payments (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id)
    );
    
    CREATE INDEX IX_Payments_OrderId ON Payments(OrderId);
END;

-- CashTransactions table
IF OBJECT_ID('CashTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE CashTransactions (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ShiftId UNIQUEIDENTIFIER NULL,
        TransactionType NVARCHAR(50) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Reason NVARCHAR(255) NULL,
        Description NVARCHAR(255) NULL,
        OccurredAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_CashTransactions_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id)
    );

    CREATE INDEX IX_CashTransactions_ShiftId ON CashTransactions(ShiftId);
    CREATE INDEX IX_CashTransactions_OccurredAt ON CashTransactions(OccurredAt);
END;

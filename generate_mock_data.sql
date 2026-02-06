-- Mock Data Generation Script for RestaurantPOS Database
-- Generates data for the last 30 days

DECLARE @LocationId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111'; -- Main Branch
DECLARE @StartDate DATETIME2 = DATEADD(DAY, -30, GETDATE());
DECLARE @EndDate DATETIME2 = GETDATE();
DECLARE @CurrentDate DATETIME2;
DECLARE @DayCounter INT = 0;

-- Generate Shifts and Orders for each day
WHILE @DayCounter < 30
BEGIN
    SET @CurrentDate = DATEADD(DAY, @DayCounter, @StartDate);
    
    -- Morning Shift (9 AM - 3 PM)
    DECLARE @MorningShiftId UNIQUEIDENTIFIER = NEWID();
    DECLARE @MorningOpenTime DATETIME2 = DATEADD(HOUR, 9, CAST(CAST(@CurrentDate AS DATE) AS DATETIME2));
    DECLARE @MorningCloseTime DATETIME2 = DATEADD(HOUR, 15, CAST(CAST(@CurrentDate AS DATE) AS DATETIME2));
    
    INSERT INTO Shifts (Id, LocationId, OpenedAt, ClosedAt, OpeningCash, ClosingCash, CreatedAt, UpdatedAt)
    VALUES (@MorningShiftId, @LocationId, @MorningOpenTime, @MorningCloseTime, 100.00, 
            100.00 + (RAND() * 500 + 200), @MorningOpenTime, @MorningCloseTime);
    
    -- Evening Shift (3 PM - 10 PM)
    DECLARE @EveningShiftId UNIQUEIDENTIFIER = NEWID();
    DECLARE @EveningOpenTime DATETIME2 = DATEADD(HOUR, 15, CAST(CAST(@CurrentDate AS DATE) AS DATETIME2));
    DECLARE @EveningCloseTime DATETIME2 = DATEADD(HOUR, 22, CAST(CAST(@CurrentDate AS DATE) AS DATETIME2));
    
    INSERT INTO Shifts (Id, LocationId, OpenedAt, ClosedAt, OpeningCash, ClosingCash, CreatedAt, UpdatedAt)
    VALUES (@EveningShiftId, @LocationId, @EveningOpenTime, @EveningCloseTime, 100.00, 
            100.00 + (RAND() * 800 + 400), @EveningOpenTime, @EveningCloseTime);
    
    -- Generate 5-10 orders for morning shift
    DECLARE @MorningOrderCount INT = CAST(RAND() * 5 + 5 AS INT);
    DECLARE @OrderCounter INT = 0;
    
    WHILE @OrderCounter < @MorningOrderCount
    BEGIN
        DECLARE @OrderId UNIQUEIDENTIFIER = NEWID();
        DECLARE @OrderTime DATETIME2 = DATEADD(MINUTE, RAND() * 360, @MorningOpenTime);
        DECLARE @Subtotal DECIMAL(18,2) = 0;
        DECLARE @Tax DECIMAL(18,2) = 0;
        DECLARE @Total DECIMAL(18,2) = 0;
        
        -- Create order with placeholder values (will update after lines)
        INSERT INTO Orders (Id, ShiftId, Subtotal, Tax, TotalAmount, TotalPaid, Remaining, TotalChange, CreatedAt, UpdatedAt)
        VALUES (@OrderId, @MorningShiftId, 0, 0, 0, 0, 0, 0, @OrderTime, @OrderTime);
        
        -- Add 1-4 order lines
        DECLARE @LineCount INT = CAST(RAND() * 3 + 1 AS INT);
        DECLARE @LineCounter INT = 0;
        
        WHILE @LineCounter < @LineCount
        BEGIN
            -- Randomly select a menu item from existing MenuItems
            DECLARE @MenuItemId UNIQUEIDENTIFIER;
            DECLARE @MenuItemName NVARCHAR(255);
            DECLARE @UnitPrice DECIMAL(18,2);
            DECLARE @Quantity INT = CAST(RAND() * 2 + 1 AS INT);
            
            -- Select random existing menu item
            SELECT TOP 1 @MenuItemId = Id, @MenuItemName = Name, @UnitPrice = Price
            FROM MenuItems
            WHERE IsActive = 1
            ORDER BY NEWID();
            
            DECLARE @LineTotal DECIMAL(18,2) = @UnitPrice * @Quantity;
            SET @Subtotal = @Subtotal + @LineTotal;
            
            INSERT INTO OrderLines (Id, OrderId, MenuItemId, MenuItemName, Quantity, UnitPrice, LineTotal)
            VALUES (NEWID(), @OrderId, @MenuItemId, @MenuItemName, @Quantity, @UnitPrice, @LineTotal);
            
            SET @LineCounter = @LineCounter + 1;
        END
        
        -- Calculate tax and total
        SET @Tax = @Subtotal * 0.08; -- 8% tax
        SET @Total = @Subtotal + @Tax;
        
        -- Update order totals
        UPDATE Orders 
        SET Subtotal = @Subtotal,
            Tax = @Tax,
            TotalAmount = @Total,
            TotalPaid = @Total,
            Remaining = 0,
            TotalChange = 0
        WHERE Id = @OrderId;
        
        SET @OrderCounter = @OrderCounter + 1;
    END
    
    -- Generate 8-15 orders for evening shift (busier)
    DECLARE @EveningOrderCount INT = CAST(RAND() * 7 + 8 AS INT);
    SET @OrderCounter = 0;
    
    WHILE @OrderCounter < @EveningOrderCount
    BEGIN
        SET @OrderId = NEWID();
        SET @OrderTime = DATEADD(MINUTE, RAND() * 420, @EveningOpenTime);
        SET @Subtotal = 0;
        SET @Tax = 0;
        SET @Total = 0;
        
        INSERT INTO Orders (Id, ShiftId, Subtotal, Tax, TotalAmount, TotalPaid, Remaining, TotalChange, CreatedAt, UpdatedAt)
        VALUES (@OrderId, @EveningShiftId, 0, 0, 0, 0, 0, 0, @OrderTime, @OrderTime);
        
        SET @LineCount = CAST(RAND() * 4 + 1 AS INT);
        SET @LineCounter = 0;
        
        WHILE @LineCounter < @LineCount
        BEGIN
            -- Select random existing menu item
            SELECT TOP 1 @MenuItemId = Id, @MenuItemName = Name, @UnitPrice = Price
            FROM MenuItems
            WHERE IsActive = 1
            ORDER BY NEWID();
            
            SET @Quantity = CAST(RAND() * 2 + 1 AS INT);
            SET @LineTotal = @UnitPrice * @Quantity;
            SET @Subtotal = @Subtotal + @LineTotal;
            
            INSERT INTO OrderLines (Id, OrderId, MenuItemId, MenuItemName, Quantity, UnitPrice, LineTotal)
            VALUES (NEWID(), @OrderId, @MenuItemId, @MenuItemName, @Quantity, @UnitPrice, @LineTotal);
            
            SET @LineCounter = @LineCounter + 1;
        END
        
        SET @Tax = @Subtotal * 0.08;
        SET @Total = @Subtotal + @Tax;
        
        UPDATE Orders 
        SET Subtotal = @Subtotal,
            Tax = @Tax,
            TotalAmount = @Total,
            TotalPaid = @Total,
            Remaining = 0,
            TotalChange = 0
        WHERE Id = @OrderId;
        
        SET @OrderCounter = @OrderCounter + 1;
    END
    
    -- Generate 1-3 inventory cost records per day (deliveries)
    DECLARE @DeliveryCount INT = CAST(RAND() * 2 + 1 AS INT);
    DECLARE @DeliveryCounter INT = 0;
    
    WHILE @DeliveryCounter < @DeliveryCount
    BEGIN
        -- Random delivery time during morning shift
        DECLARE @DeliveryTime DATETIME2 = DATEADD(MINUTE, RAND() * 180, @MorningOpenTime);
        
        -- Select random inventory item from database
        DECLARE @InventoryItemId UNIQUEIDENTIFIER;
        DECLARE @ItemName NVARCHAR(255);
        DECLARE @QuantityReceived DECIMAL(18,4);
        DECLARE @TotalCost DECIMAL(18,2);
        
        SELECT TOP 1 @InventoryItemId = Id, @ItemName = Name
        FROM InventoryItems
        ORDER BY NEWID();
        
        SET @QuantityReceived = RAND() * 50 + 10;
        SET @TotalCost = @QuantityReceived * (RAND() * 20 + 5);
        
        INSERT INTO InventoryCostRecords (Id, EventId, ShiftId, LocationId, InventoryItemId, QuantityReceived, TotalCost, RecordedAt)
        VALUES (NEWID(), NEWID(), @MorningShiftId, @LocationId, @InventoryItemId, @QuantityReceived, @TotalCost, @DeliveryTime);
        
        -- Update LocationInventory
        IF EXISTS (SELECT 1 FROM LocationInventory WHERE LocationId = @LocationId AND InventoryItemId = @InventoryItemId)
        BEGIN
            UPDATE LocationInventory 
            SET Quantity = Quantity + @QuantityReceived
            WHERE LocationId = @LocationId AND InventoryItemId = @InventoryItemId;
        END
        ELSE
        BEGIN
            INSERT INTO LocationInventory (LocationId, InventoryItemId, Quantity)
            VALUES (@LocationId, @InventoryItemId, @QuantityReceived);
        END
        
        SET @DeliveryCounter = @DeliveryCounter + 1;
    END
    
    -- Generate 2-5 inventory removals per day (usage/waste)
    DECLARE @RemovalCount INT = CAST(RAND() * 3 + 2 AS INT);
    DECLARE @RemovalCounter INT = 0;
    
    WHILE @RemovalCounter < @RemovalCount
    BEGIN
        -- Random removal time during shifts
        DECLARE @RemovalTime DATETIME2 = DATEADD(MINUTE, RAND() * 780, @MorningOpenTime);
        
        -- Select random inventory item from database
        DECLARE @RemovalInventoryItemId UNIQUEIDENTIFIER;
        DECLARE @RemovalItemName NVARCHAR(255);
        DECLARE @QuantityRemoved DECIMAL(18,4);
        
        SELECT TOP 1 @RemovalInventoryItemId = Id, @RemovalItemName = Name
        FROM InventoryItems
        ORDER BY NEWID();
        
        SET @QuantityRemoved = RAND() * 10 + 2; -- Remove 2-12 units
        
        -- Update LocationInventory (remove quantity)
        IF EXISTS (SELECT 1 FROM LocationInventory WHERE LocationId = @LocationId AND InventoryItemId = @RemovalInventoryItemId)
        BEGIN
            UPDATE LocationInventory 
            SET Quantity = CASE WHEN Quantity - @QuantityRemoved < 0 THEN 0 ELSE Quantity - @QuantityRemoved END
            WHERE LocationId = @LocationId AND InventoryItemId = @RemovalInventoryItemId;
        END
        
        SET @RemovalCounter = @RemovalCounter + 1;
    END
    
    SET @DayCounter = @DayCounter + 1;
END

PRINT 'Mock data generation complete!';
PRINT 'Generated:';
PRINT '  - 60 Shifts (2 per day for 30 days)';
PRINT '  - ~390 Orders (5-10 morning, 8-15 evening per day)';
PRINT '  - ~1000 Order Lines (1-4 per order using existing MenuItems)';
PRINT '  - ~60 Inventory Cost Records (1-3 deliveries per day)';
PRINT '  - ~105 Inventory Removals (2-5 per day for usage/waste)';

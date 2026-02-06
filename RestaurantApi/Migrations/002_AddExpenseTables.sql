-- Migration: Add Expense Tracking Tables
-- Date: 2026-02-05

-- Create ExpenseCategories table
CREATE TABLE ExpenseCategories (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    IsCOGS BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Create Expenses table
CREATE TABLE Expenses (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExpenseCategoryId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    ExpenseDate DATETIME2 NOT NULL,
    LocationId UNIQUEIDENTIFIER NOT NULL,
    ShiftId UNIQUEIDENTIFIER NULL,
    
    -- Optional links to source records
    InventoryCostRecordId UNIQUEIDENTIFIER NULL,
    CashTransactionId UNIQUEIDENTIFIER NULL,
    
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    -- Foreign keys
    CONSTRAINT FK_Expenses_ExpenseCategories FOREIGN KEY (ExpenseCategoryId) REFERENCES ExpenseCategories(Id),
    CONSTRAINT FK_Expenses_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id),
    CONSTRAINT FK_Expenses_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(Id),
    CONSTRAINT FK_Expenses_InventoryCostRecords FOREIGN KEY (InventoryCostRecordId) REFERENCES InventoryCostRecords(Id),
    CONSTRAINT FK_Expenses_CashTransactions FOREIGN KEY (CashTransactionId) REFERENCES CashTransactions(Id)
);

-- Create indexes for common queries
CREATE INDEX IX_Expenses_ExpenseDate ON Expenses(ExpenseDate);
CREATE INDEX IX_Expenses_LocationId ON Expenses(LocationId);
CREATE INDEX IX_Expenses_ExpenseCategoryId ON Expenses(ExpenseCategoryId);
CREATE INDEX IX_Expenses_InventoryCostRecordId ON Expenses(InventoryCostRecordId);
CREATE INDEX IX_Expenses_CashTransactionId ON Expenses(CashTransactionId);

-- Insert default expense categories
INSERT INTO ExpenseCategories (Id, Name, IsCOGS, IsActive) VALUES
(NEWID(), 'COGS - Inventory', 1, 1),
(NEWID(), 'Rent', 0, 1),
(NEWID(), 'Utilities', 0, 1),
(NEWID(), 'Payroll', 0, 1),
(NEWID(), 'Marketing', 0, 1),
(NEWID(), 'Equipment', 0, 1),
(NEWID(), 'Supplies', 0, 1),
(NEWID(), 'Maintenance', 0, 1),
(NEWID(), 'Insurance', 0, 1),
(NEWID(), 'Other', 0, 1);

PRINT 'Expense tracking tables created successfully';

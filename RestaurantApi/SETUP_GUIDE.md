# RestaurantApi Quick Setup Guide

## Prerequisites
- .NET 8.0 SDK
- SQL Server (any version)
- Visual Studio Code or Visual Studio

## Setup Steps

### 1. Create Database Tables
First, get the database schema ready:

```sql
-- Option 1: Copy entire schema from Data/Schema.sql
-- Option 2: Use SQL script below

CREATE TABLE ProcessedEvents (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EventId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    EventType NVARCHAR(100) NOT NULL,
    ProcessedAt DATETIME2 NOT NULL,
    DeviceId NVARCHAR(255) NULL
);

CREATE TABLE Orders (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    ShiftId BIGINT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    Tax DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    TotalPaid DECIMAL(18,2) NOT NULL,
    Remaining DECIMAL(18,2) NOT NULL,
    TotalChange DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE TABLE OrderLines (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    MenuItemName NVARCHAR(255) NOT NULL,
    MenuItemId UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
);

CREATE TABLE InventoryItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    InventoryItemId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Name NVARCHAR(255) NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    Unit NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE TABLE Payments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PaymentId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    OrderId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
```

### 2. Update Connection String
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Examples:
- **Local SQL Server**: `Server=.;Database=RestaurantApi;Trusted_Connection=True;TrustServerCertificate=True;`
- **LocalDB**: `Server=(localdb)\\mssqllocaldb;Database=RestaurantApi;Trusted_Connection=true;`
- **Remote SQL Server with credentials**: `Server=myserver.database.windows.net;Database=RestaurantApi;User Id=sa;Password=MyPassword;`

### 3. Build the Project
```bash
cd RestaurantApi
dotnet build
```

### 4. Run the API
```bash
dotnet run
```

The API will start on `https://localhost:7000` (check console for actual port).

### 5. Test the API
Health check endpoint:
```bash
curl https://localhost:7000/api/events/health
```

Expected response:
```json
{
  "success": true,
  "message": "Event API is healthy",
  "data": null,
  "alreadyProcessed": false
}
```

## First Event

Send your first event:
```bash
curl -X POST https://localhost:7000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "type": "inventory_item_added",
    "payload": {
      "inventoryItemId": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Tomatoes",
      "quantity": 50,
      "unit": "kg"
    },
    "createdAt": "2024-01-15T10:30:00Z",
    "deviceId": "pos-device-001"
  }'
```

## Connection String Reference

### SQL Server Authentication
```
Server=myserver.com;Database=mydb;User Id=myuser;Password=mypassword;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
```

### Windows Authentication
```
Server=myserver;Database=mydb;Trusted_Connection=True;TrustServerCertificate=True;
```

### LocalDB
```
Server=(localdb)\mssqllocaldb;Database=mydb;Trusted_Connection=true;
```

### Azure SQL Database
```
Server=myserver.database.windows.net;Database=mydb;User Id=myuser;Password=mypassword;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
```

## Troubleshooting

### "Connection string 'DefaultConnection' not found"
Check that `appsettings.json` has the ConnectionStrings section configured.

### "Invalid object name 'Orders'"
Run the SQL schema creation script (step 1 above).

### "Login failed for user"
Verify your SQL Server credentials and permissions in the connection string.

### "Cannot open database"
Ensure the database name in the connection string exists on your SQL Server.

## API Documentation
See [EVENT_API_DOCUMENTATION.md](EVENT_API_DOCUMENTATION.md) for complete API documentation.

## Testing with .http file
Use VS Code REST Client extension to test via [RestaurantApi.http](RestaurantApi.http).

## Project Structure
```
RestaurantApi/
├── Controllers/
│   └── EventsController.cs      # Event endpoints
├── Data/
│   ├── Models/                  # Database models
│   ├── Repositories/            # Data access layer (Dapper)
│   ├── SqlConnectionProvider.cs
│   └── Schema.sql               # Database schema
├── Services/
│   └── EventHandlers/           # Event processing logic
├── DTOs/                        # Data transfer objects
├── Program.cs                   # Service registration
└── appsettings.json            # Configuration
```

## Next Steps
1. ✅ Setup database
2. ✅ Configure connection string
3. ✅ Build and run
4. 📚 Review [EVENT_API_DOCUMENTATION.md](EVENT_API_DOCUMENTATION.md)
5. 🧪 Test endpoints using [RestaurantApi.http](RestaurantApi.http)
6. 🔧 Customize event handlers as needed

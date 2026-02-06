# Restaurant API - Event-Based System

## Overview

The Restaurant API is an event-driven system that receives events from the POS (Point of Sale) application and processes them to maintain a central database of orders, inventory, payments, and other operations. Built with **Dapper** for lightweight data access.

## Architecture

### Key Components

1. **Event Receiver** - `EventsController.cs`
   - REST endpoint that accepts events from the POS system
   - Validates incoming events
   - Supports both single event and batch processing

2. **Event Processor** - `EventProcessor.cs`
   - Orchestrates event handling
   - Prevents duplicate event processing by tracking processed events
   - Routes events to appropriate handlers

3. **Event Handlers** - Located in `Services/EventHandlers/`
   - `InventoryEventHandler`: Handles inventory-related events
   - `OrderEventHandler`: Handles order-related events
   - `PaymentEventHandler`: Handles payment-related events
   - Extensible pattern for adding new event types

4. **Data Access Layer** - Uses **Dapper** for lightweight, performant data access
   - `Repositories/` folder contains repository interfaces and implementations
   - `ProcessedEventRepository`: Tracks processed events
   - `InventoryItemRepository`: Manages inventory data
   - `OrderRepository`: Manages order data
   - `OrderLineRepository`: Manages order line items
   - `PaymentRepository`: Manages payment data
   - Direct SQL queries using Dapper with automatic parameter mapping

## Event Types

### Inventory Events
- `inventory_item_added` - Creates a new inventory item
- `inventory_item_updated` - Updates quantity of an existing inventory item
- `inventory_item_removed` - Removes an inventory item from the system

### Order Events
- `order_created` - Creates a new order
- `order_updated` - Updates an existing order
- `order_completed` - Marks order as completed

### Payment Events
- `payment_processed` - Records a payment
- `payment_refunded` - Refunds a payment

## API Endpoints

### Process Single Event
```
POST /api/events
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "inventory_item_added",
  "payload": {
    "inventoryItemId": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Tomatoes",
    "quantity": 10,
    "unit": "kg"
  },
  "createdAt": "2024-01-15T10:30:00Z",
  "deviceId": "device-001"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Event processed successfully",
  "data": null,
  "alreadyProcessed": false
}
```

### Batch Process Events
```
POST /api/events/batch
Content-Type: application/json

[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "type": "order_created",
    "payload": {...},
    "createdAt": "2024-01-15T10:30:00Z",
    "deviceId": "device-001"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "type": "payment_processed",
    "payload": {...},
    "createdAt": "2024-01-15T10:31:00Z",
    "deviceId": "device-001"
  }
]
```

**Response:**
```json
{
  "success": true,
  "message": "Processed 2 events (2 successful)",
  "data": [
    {
      "eventId": "550e8400-e29b-41d4-a716-446655440000",
      "eventType": "order_created",
      "success": true,
      "alreadyProcessed": false,
      "message": "Processed successfully"
    },
    {
      "eventId": "550e8400-e29b-41d4-a716-446655440001",
      "eventType": "payment_processed",
      "success": true,
      "alreadyProcessed": false,
      "message": "Processed successfully"
    }
  ]
}
```

### Health Check
```
GET /api/events/health
```

## Event Structure

### EventDto
```csharp
public class EventDto
{
    public Guid Id { get; set; }              // Unique event identifier
    public string Type { get; set; }          // Event type (use EventTypes constants)
    public object? Payload { get; set; }      // Event data (serialized payload)
    public DateTime CreatedAt { get; set; }   // When the event was created
    public string? DeviceId { get; set; }     // POS device identifier
}
```

### Event Payloads

#### Inventory Item Added
```json
{
  "inventoryItemId": "guid",
  "name": "Item Name",
  "quantity": 10,
  "unit": "kg"
}
```

#### Order Created
```json
{
  "orderId": "guid",
  "shiftId": 123,
  "subtotal": 95.28,
  "tax": 10.72,
  "totalAmount": 106.00,
  "totalPaid": 110.00,
  "remaining": 0,
  "totalChange": 4.00,
  "orderLines": [
    {
      "menuItemId": "guid",
      "menuItemName": "Burger",
      "quantity": 2,
      "unitPrice": 12.50,
      "lineTotal": 25.00
    }
  ]
}
```

#### Payment Processed
```json
{
  "paymentId": "guid",
  "orderId": 1,
  "amount": 110.00,
  "paymentMethod": "Cash"
}
```

## Duplicate Event Prevention

The system prevents duplicate processing by:

1. **Event ID Requirement**: Every event must have a unique ID (Guid)
2. **Processed Event Tracking**: The `ProcessedEvent` table tracks all processed events
3. **Idempotent Processing**: If an event with the same ID is received again:
   - The system recognizes it has been processed
   - Returns success with `alreadyProcessed: true`
   - Does NOT process it again
   - Does NOT duplicate data in the database

This mechanism ensures that even if events are retransmitted (network issues, retries, etc.), the system remains consistent.

## Database Schema

The database schema is defined in [Data/Schema.sql](Data/Schema.sql). When using Dapper, you need to manually create these tables before running the application.

### Schema Details

#### ProcessedEvent Table
Tracks which events have been processed to prevent duplicates
- `Id`: Primary key (auto-increment)
- `EventId`: Unique event identifier (Guid, indexed - UNIQUE)
- `EventType`: Type of event (nvarchar)
- `ProcessedAt`: When the event was processed (datetime2)
- `DeviceId`: Source device identifier (nvarchar, nullable)

#### Order Table
Stores order information
- `Id`: Database primary key (auto-increment)
- `OrderId`: Unique order identifier (Guid, indexed - UNIQUE)
- `ShiftId`: Associated shift (bigint, nullable)
- `Subtotal`: Order subtotal before tax (decimal)
- `Tax`: Tax amount (decimal)
- `TotalAmount`: Total order amount (decimal)
- `TotalPaid`: Amount paid (decimal)
- `Remaining`: Amount remaining (decimal)
- `TotalChange`: Change given (decimal)
- `CreatedAt`: Creation timestamp (datetime2)
- `UpdatedAt`: Last update timestamp (datetime2)

#### OrderLine Table
Individual items in an order
- `Id`: Primary key (auto-increment)
- `OrderId`: Foreign key to Order (cascade delete)
- `Quantity`: Item quantity (int)
- `UnitPrice`: Price per unit (decimal)
- `LineTotal`: Total price for line (decimal)
- `MenuItemName`: Menu item name (nvarchar)
- `MenuItemId`: Menu item reference (Guid)

#### InventoryItem Table
Inventory tracking
- `Id`: Primary key (auto-increment)
- `InventoryItemId`: Unique item identifier (Guid, indexed - UNIQUE)
- `Name`: Item name (nvarchar)
- `Quantity`: Current quantity (decimal)
- `Unit`: Unit of measurement (nvarchar)
- `CreatedAt`: Creation timestamp (datetime2)
- `UpdatedAt`: Last update timestamp (datetime2)

#### Payment Table
Payment records
- `Id`: Primary key (auto-increment)
- `PaymentId`: Unique payment identifier (Guid, indexed - UNIQUE)
- `OrderId`: Related order ID (int)
- `Amount`: Payment amount (decimal)
- `PaymentMethod`: Payment method (nvarchar)
- `CreatedAt`: Creation timestamp (datetime2)
- `UpdatedAt`: Last update timestamp (datetime2)

## Changes to RestaurantPOS Models

To support the event-based system, the following models were updated:

### Order.cs
- Added `OrderGuid` property (Guid with Guid.NewGuid() default)
- Used for unique identification across the distributed system

### Payment.cs
- Added `PaymentGuid` property (Guid with Guid.NewGuid() default)
- Added `using System;` for Guid support
- Used for unique identification across the distributed system

## How to Add a New Event Type

1. Add the event type constant to `DTOs/EventTypes.cs`:
   ```csharp
   public const string MyNewEvent = "my_new_event";
   ```

2. Create necessary repository (if working with a new entity):
   ```csharp
   public interface IMyRepository
   {
       Task<MyEntity?> GetByIdAsync(int id);
       Task<int> AddAsync(MyEntity entity);
       Task<bool> UpdateAsync(MyEntity entity);
   }
   
   public class MyRepository : IMyRepository
   {
       private readonly string _connectionString;
       
       public MyRepository(string connectionString)
       {
           _connectionString = connectionString;
       }
       
       // Implement methods using Dapper...
   }
   ```

3. Create a new event handler by implementing `IEventHandler`:
   ```csharp
   public class MyEventHandler : IEventHandler
   {
       private readonly IMyRepository _repository;
       
       public MyEventHandler(IMyRepository repository, ILogger<MyEventHandler> logger)
       {
           _repository = repository;
           _logger = logger;
       }

       public bool CanHandle(string eventType)
       {
           return eventType == EventTypes.MyNewEvent;
       }

       public async Task<bool> HandleAsync(EventDto @event)
       {
           // Process the event
           return true;
       }
   }
   ```

4. Register in `Program.cs`:
   ```csharp
   // Register repository
   builder.Services.AddScoped<IMyRepository>(sp => new MyRepository(connectionString));
   
   // Register handler
   builder.Services.AddScoped<IEventHandler, MyEventHandler>();
   ```

5. Add corresponding table to [Data/Schema.sql](Data/Schema.sql) if needed

## Configuration

### Connection String
Edit `appsettings.json` to configure the database:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ScanApp;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Logging
Configure logging levels in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Running the API

1. **Create the database schema** (run [Data/Schema.sql](Data/Schema.sql) in SQL Server):
   ```sql
   -- Execute the schema.sql file in your SQL Server instance
   -- This creates all required tables with proper relationships and indexes
   ```

2. **Update connection string** if needed in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=ScanApp;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Run the API**:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:7xxx` (port shown in console output).

## Testing

You can test the API using tools like:
- **Postman**: Import the endpoint definitions
- **curl**: Command-line HTTP client
- **RestaurantApi.http**: Use the `.http` file with VS Code REST Client extension

Example curl request:
```bash
curl -X POST https://localhost:7000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "type": "inventory_item_added",
    "payload": {
      "inventoryItemId": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Tomatoes",
      "quantity": 10,
      "unit": "kg"
    },
    "createdAt": "2024-01-15T10:30:00Z",
    "deviceId": "device-001"
  }'
```

## Best Practices

1. **Always include unique IDs**: Every event must have a unique ID to enable idempotent processing
2. **Handle network failures gracefully**: The POS application can retry events safely
3. **Monitor processed events**: Review the `ProcessedEvent` table for audit trail
4. **Batch operations**: Use the batch endpoint for multiple events to improve performance
5. **Include device information**: Always include `DeviceId` for better tracking and debugging
6. **Use proper timestamps**: Events should have accurate `CreatedAt` timestamps

## Error Handling

The API returns appropriate HTTP status codes:
- `200 OK`: Event processed successfully (or already processed)
- `400 Bad Request`: Invalid event data or no handler found
- `500 Internal Server Error`: Unexpected error during processing

All responses include a message indicating what happened and whether the event was already processed.

## Troubleshooting

### Database Schema Not Created
1. Make sure to run [Data/Schema.sql](Data/Schema.sql) first
2. Connect to your SQL Server instance and execute the SQL script
3. Verify all tables were created with `SELECT * FROM INFORMATION_SCHEMA.TABLES`

### Connection String Issues
1. Update the connection string in [appsettings.json](appsettings.json)
2. Test the connection with SQL Server Management Studio or Azure Data Studio
3. Common formats:
   - SQL Server with Windows Auth: `Server=localhost;Database=RestaurantApiDb;Trusted_Connection=True;TrustServerCertificate=True;`
   - SQL Server with SQL Auth: `Server=localhost;Database=RestaurantApiDb;User Id=sa;Password=YourPassword;`
   - LocalDB: `Server=(localdb)\\mssqllocaldb;Database=RestaurantApiDb;Trusted_Connection=true;`

### Event Not Being Processed
1. Check that the event type exists in `EventTypes`
2. Verify the event handler is registered in `Program.cs`
3. Check logs for error messages
4. Ensure the payload format matches the handler's expectations

### Duplicate Events
If the same event is received multiple times:
- The system will process it only once
- Subsequent calls will return `alreadyProcessed: true`
- This is the expected behavior and provides idempotency

## Why Dapper?

Dapper is a lightweight micro-ORM that offers several advantages for this event-based API:

1. **Performance**: Minimal overhead compared to Entity Framework
2. **Simplicity**: Direct control over SQL queries with clean syntax
3. **Flexibility**: Easy to write optimized queries for specific scenarios
4. **Speed**: Excellent for high-throughput event processing
5. **Learning Curve**: Simple and straightforward to understand
6. **Minimal Dependencies**: Only requires `System.Data.SqlClient` or similar

### Dapper Features Used in This API
- **Auto-mapping**: Objects automatically mapped from SQL results using property names
- **Parameter binding**: Automatic parameter mapping prevents SQL injection
- **Async support**: Full async/await support for non-blocking operations
- **Query execution**: Simple Execute and QueryAsync methods

### Repository Pattern
The API uses a repository pattern for data access:
- Each entity has its own repository interface and implementation
- Repositories handle all database operations for that entity
- Easy to mock for unit testing
- Centralized SQL queries and data mapping logic

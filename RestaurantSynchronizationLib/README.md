# RestaurantSynchronizationLib

A comprehensive event synchronization library that bridges RestaurantPOS (SQLite-based POS system) with RestaurantApi (event-driven API backend).

## Overview

This library provides:
- **SQLite Event Repository**: Read pending events from RestaurantPOS database
- **Event Synchronization Engine**: Transform and send events to RestaurantApi
- **Background Service**: Automated periodic synchronization
- **HTTP Client**: Secure communication with RestaurantApi
- **Configuration Management**: Centralized settings for all components

## Architecture

```
RestaurantPOS (Event Producer)
    ↓
    SQLite Database (SyncEvents table)
    ↓
RestaurantSynchronizationLib
    ├── SyncEventRepository (reads from SQLite)
    ├── EventSynchronizer (orchestrates sync)
    ├── ApiEventClient (HTTP communication)
    └── TimedSyncService (background worker)
    ↓
RestaurantApi (Event Consumer)
    ↓
    SQL Server Database (ProcessedEvents tracking)
```

## Components

### SyncEventRepository
Reads `SyncEvent` records from RestaurantPOS SQLite database.

**Methods:**
- `GetUnsyncedEventsAsync()` - Get all events not yet synced
- `GetUnsyncedEventsByTypeAsync(type)` - Get unsynced events by type
- `MarkAsSyncedAsync(eventId)` - Mark single event as synced
- `MarkAsSyncedAsync(eventIds)` - Mark multiple events as synced
- `DeleteEventAsync(eventId)` - Delete synced event
- `GetUnsyncedEventCountAsync()` - Get count of pending events

### EventSynchronizer
Orchestrates the entire synchronization process.

**Features:**
- Connects to SQLite via SyncEventRepository
- Retrieves unsynced events
- Validates API availability
- Converts events to API format
- Sends via batch or individual endpoints
- Marks events as synced on success
- Comprehensive error handling and logging

**Methods:**
- `SynchronizeAsync()` - Execute full sync cycle
- `GetStatisticsAsync()` - Get pending events count and API status

### ApiEventClient
HTTP client for communication with RestaurantApi.

**Methods:**
- `SendEventAsync(dto)` - Send single event
- `SendEventsAsync(dtos)` - Send batch of events
- `IsHealthyAsync()` - Check API availability

**Response Handling:**
- Detects duplicate events via `AlreadyProcessed` flag
- Marks duplicates as synced (same effect as successful send)
- Comprehensive error logging

### TimedSyncService
Background service for automated synchronization.

**Features:**
- Configurable sync interval (default: 60 seconds)
- Auto-starts on creation
- Can be manually triggered
- Provides sync status and statistics

**Methods:**
- `Start()` - Begin periodic synchronization
- `Stop()` - Stop the service
- `SyncNowAsync()` - Execute sync immediately
- `GetStatusAsync()` - Get current statistics

## Installation

### 1. Reference the Library
```powershell
# In RestaurantPOS project directory
dotnet add reference ../RestaurantSynchronizationLib/RestaurantSynchronizationLib.csproj
```

### 2. Configure Dependency Injection
In your `Program.cs` or `App.xaml.cs`:

```csharp
using RestaurantSynchronizationLib;
using RestaurantSynchronizationLib.Configuration;

// Create configuration
var syncConfig = new SyncConfiguration
{
    ApiBaseAddress = "https://localhost:7195",
    DeviceId = Environment.MachineName,
    DatabaseConnectionString = "Data Source=pos.db;Version=3;",
    RequestTimeoutSeconds = 30,
    UseBatchEndpoint = true,
    BatchSize = 10
};

// Register services
var services = new ServiceCollection();
services.AddLogging();
services.AddRestaurantSynchronization(syncConfig, timedSyncIntervalSeconds: 30);

var serviceProvider = services.BuildServiceProvider();
var syncService = serviceProvider.GetRequiredService<TimedSyncService>();
```

### 3. Start the Service
```csharp
syncService.Start(); // Starts periodic synchronization

// Later: Stop on shutdown
syncService.Stop();
syncService.Dispose();
```

## Usage

### Automatic Synchronization (Recommended)
```csharp
var syncService = serviceProvider.GetRequiredService<TimedSyncService>();
syncService.Start(); // Runs every N seconds automatically

// Later, check status
var stats = await syncService.GetStatusAsync();
Console.WriteLine($"Pending events: {stats.UnsyncedEventCount}");
```

### Manual Synchronization
```csharp
var syncService = serviceProvider.GetRequiredService<TimedSyncService>();
var result = await syncService.SyncNowAsync();

if (result.Success)
{
    Console.WriteLine($"Synced {result.SyncedCount} events");
}
else
{
    Console.WriteLine($"Sync failed: {result.Message}");
}
```

## Event Creation in RestaurantPOS

When creating orders, payments, or inventory items, create corresponding `SyncEvent` records:

```csharp
var order = new Order
{
    OrderId = Guid.NewGuid(), // Important: Use GUID
    // ... other properties
};

// Save order to database first
await orderRepository.SaveAsync(order);

// Create sync event
var syncEvent = new SyncEvent
{
    Id = Guid.NewGuid(),
    Type = "order_created",
    Payload = JsonSerializer.Serialize(new
    {
        OrderId = order.OrderId,
        ShiftId = order.ShiftId,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt
    }),
    CreatedAt = DateTime.UtcNow,
    SyncedAt = null, // Will be set by sync service
    DeviceId = Environment.MachineName
};

// Save sync event to database
await syncEventRepository.SaveAsync(syncEvent);
```

## Event Types

The following event types should be created:

### Inventory Events
- `inventory_item_added` - New inventory item
- `inventory_item_updated` - Inventory quantity/price change
- `inventory_item_removed` - Item deleted

### Order Events
- `order_created` - New order placed
- `order_updated` - Order modified
- `order_completed` - Order finished/paid

### Payment Events
- `payment_processed` - Payment received
- `payment_refunded` - Payment reversed

## Configuration

All configuration is centralized in `SyncConfiguration`:

```csharp
var config = new SyncConfiguration
{
    // API settings
    ApiBaseAddress = "https://api.restaurant.local",
    EventsEndpoint = "/api/events", // Single event endpoint
    BatchEndpoint = "/api/events/batch", // Batch endpoint
    
    // Device identification
    DeviceId = "POS-01",
    
    // Database
    DatabaseConnectionString = "Data Source=restaurant.db;Version=3;",
    
    // HTTP
    RequestTimeoutSeconds = 30,
    
    // Sync behavior
    UseBatchEndpoint = true, // Use batch endpoint for better performance
    BatchSize = 10 // Max events per batch
};
```

## Error Handling

### Automatic Retry
The service doesn't have built-in retry logic - events stay in the database until synced.

### Manual Retry
```csharp
// Events will be retried on next sync cycle
// To force retry: stop service and start again
syncService.Stop();
syncService.Start();

// Or sync immediately
var result = await syncService.SyncNowAsync();
```

### Logging
All components use `ILogger<T>` for detailed logging:

```csharp
services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});
```

## Database Schema (SQLite)

The RestaurantPOS SQLite database must have a `SyncEvents` table:

```sql
CREATE TABLE IF NOT EXISTS SyncEvents (
    Id TEXT PRIMARY KEY,
    Type TEXT NOT NULL,
    Payload TEXT,
    CreatedAt DATETIME NOT NULL,
    SyncedAt DATETIME,
    DeviceId TEXT
);

CREATE INDEX IF NOT EXISTS idx_SyncEvents_SyncedAt 
ON SyncEvents(SyncedAt);
```

## Performance Considerations

1. **Batch Size**: Larger batches are faster but use more memory. Default is 10.
2. **Sync Interval**: Shorter intervals mean lower latency but higher CPU/network. Default is 60 seconds.
3. **Database Indexing**: Ensure `SyncedAt` is indexed in SQLite for fast queries.
4. **Event Cleanup**: Consider deleting synced events periodically to keep database size manageable.

## Troubleshooting

### Events Not Syncing
1. Check `SyncEventRepository.GetUnsyncedEventCountAsync()` - are there pending events?
2. Check `ApiEventClient.IsHealthyAsync()` - is the API reachable?
3. Check logs for error details
4. Verify connection strings are correct

### High Memory Usage
1. Reduce `BatchSize` in configuration
2. Increase `SyncIntervalSeconds` to reduce frequency
3. Implement periodic cleanup of old synced events

### API Rejecting Events
1. Verify event `Type` matches expected values
2. Ensure `Payload` is valid JSON
3. Check that DeviceId is set
4. Use `GET /api/events/health` to verify API state

## Testing

### Unit Testing
Mock `SyncEventRepository` and `ApiEventClient` for isolated testing:

```csharp
var mockRepo = new Mock<SyncEventRepository>();
var mockClient = new Mock<ApiEventClient>();
var synchronizer = new EventSynchronizer(config, mockRepo.Object, mockClient.Object, logger);

var result = await synchronizer.SynchronizeAsync();
Assert.IsTrue(result.Success);
```

### Integration Testing
Use actual SQLite database and test API:

```csharp
var config = new SyncConfiguration { /* ... */ };
var services = new ServiceCollection();
services.AddLogging();
services.AddRestaurantSynchronization(config);
var sp = services.BuildServiceProvider();

var syncService = sp.GetRequiredService<TimedSyncService>();
var result = await syncService.SyncNowAsync();
```

## API Endpoints Used

### Single Event
```
POST /api/events
Content-Type: application/json

{
    "id": "guid",
    "type": "order_created",
    "payload": { /* event data */ },
    "createdAt": "2024-01-01T12:00:00Z",
    "deviceId": "POS-01"
}
```

### Batch Events
```
POST /api/events/batch
Content-Type: application/json

[
    { /* event 1 */ },
    { /* event 2 */ },
    ...
]
```

### Health Check
```
GET /api/events/health
```

## License

Internal use only

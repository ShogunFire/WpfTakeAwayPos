# RestaurantSynchronizationLib - Complete Implementation Guide

## What Has Been Created

The `RestaurantSynchronizationLib` is a complete bridge between RestaurantPOS (SQLite-based) and RestaurantApi (event-driven). It handles all aspects of event synchronization.

### Project Structure

```
RestaurantSynchronizationLib/
├── Configuration/
│   └── SyncConfiguration.cs          # Centralized settings
├── Models/
│   ├── EventDto.cs                   # API event format
│   └── ApiResponse.cs                # API response parsing
├── Persistence/
│   └── SyncEventRepository.cs        # SQLite data access
├── Services/
│   ├── ApiEventClient.cs             # HTTP communication with API
│   └── TimedSyncService.cs           # Background sync worker
├── EventSynchronizer.cs              # Orchestrates entire sync
├── ServiceCollectionExtensions.cs    # Dependency injection setup
├── SyncDatabase.sql                  # SQLite schema for sync events
├── README.md                         # Complete documentation
└── INTEGRATION_EXAMPLE.cs            # Code example for RestaurantPOS
```

### File Descriptions

#### Configuration Layer

**SyncConfiguration.cs**
- Holds all settings needed for synchronization
- Properties: ApiBaseAddress, DeviceId, DatabaseConnectionString, timeouts, batch settings
- Can be read from appsettings.json or created programmatically

#### Data Access Layer

**SyncEventRepository.cs**
- Reads `SyncEvent` records from RestaurantPOS SQLite database
- Methods to query unsynced events and mark them as synced
- Queries are parameterized to prevent SQL injection
- Comprehensive logging for troubleshooting

#### Communication Layer

**ApiEventClient.cs**
- HttpClient wrapper for communication with RestaurantApi
- Two sending modes:
  - Single: `SendEventAsync()` for one event at a time
  - Batch: `SendEventsAsync()` for multiple events
- Detects and handles duplicate events (AlreadyProcessed flag)
- Automatic timeout handling and comprehensive error logging

#### Business Logic Layer

**EventSynchronizer.cs**
- Orchestrates the complete sync workflow:
  1. Checks API availability
  2. Retrieves unsynced events from SQLite
  3. Converts to API format (EventDto)
  4. Sends via ApiEventClient (batch or single)
  5. Marks successful events as synced
  6. Returns detailed result with statistics

- Provides statistics: unsynced count, API availability

#### Background Service

**TimedSyncService.cs**
- Runs synchronization periodically on a background timer
- Configurable interval (default 60 seconds)
- Can be manually triggered with `SyncNowAsync()`
- Graceful start/stop with proper resource cleanup

#### Dependency Injection

**ServiceCollectionExtensions.cs**
- Extension methods for easy service registration
- Two approaches:
  1. Pass `SyncConfiguration` object
  2. Pass individual parameters
- Registers all services: repositories, HTTP client, synchronizer, timed service

## Data Flow

```
RestaurantPOS                           RestaurantSynchronizationLib
    │
    ├─ Create Order
    ├─ Save to SQLite
    └─ Create SyncEvent record
                                        ↓
                            SyncEventRepository
                            (reads unsynced events)
                                        ↓
                            EventSynchronizer
                            (orchestrates sync)
                                        ↓
                            EventDto conversion
                                        ↓
                            ApiEventClient
                            (sends via HTTP)
                                        ↓
                            Mark as synced
                                        │
                                        → API processes event
                                        → Stores in SQL Server
                                        → Prevents duplicates
```

## Integration Steps

### Step 1: Add Reference
```bash
cd RestaurantPOS
dotnet add reference ../RestaurantSynchronizationLib/RestaurantSynchronizationLib.csproj
```

### Step 2: Setup SQLite Schema
Run `SyncDatabase.sql` in RestaurantPOS SQLite database:
- Creates `SyncEvents` table
- Creates indexes for performance
- Creates views for monitoring
- Creates optional audit log table

### Step 3: Configure Services
In `App.xaml.cs` or `Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using RestaurantSynchronizationLib;

var services = new ServiceCollection();
services.AddLogging();

// Option A: With SyncConfiguration object
var config = new SyncConfiguration
{
    ApiBaseAddress = "https://localhost:7195",
    DeviceId = Environment.MachineName,
    DatabaseConnectionString = "Data Source=restaurant.db;Version=3;",
};
services.AddRestaurantSynchronization(config, timedSyncIntervalSeconds: 30);

// or Option B: With individual parameters
services.AddRestaurantSynchronization(
    apiBaseAddress: "https://localhost:7195",
    deviceId: Environment.MachineName,
    databaseConnectionString: "Data Source=restaurant.db;Version=3;",
    timedSyncIntervalSeconds: 30
);

var serviceProvider = services.BuildServiceProvider();
```

### Step 4: Start Service
```csharp
var syncService = serviceProvider.GetRequiredService<TimedSyncService>();
syncService.Start(); // Starts periodic sync

// On shutdown
syncService.Stop();
syncService.Dispose();
```

### Step 5: Create Events When Data Changes
When you create an order, payment, or inventory item:

```csharp
using System.Text.Json;

// 1. Create and save the business object
var order = new Order { /* ... */ };
await database.SaveOrderAsync(order);

// 2. Create a SyncEvent
var syncEvent = new SyncEvent
{
    Id = Guid.NewGuid(),
    Type = "order_created",
    Payload = JsonSerializer.Serialize(new
    {
        order.OrderId,
        order.ShiftId,
        order.TotalAmount,
        order.CreatedAt
    }),
    CreatedAt = DateTime.UtcNow,
    SyncedAt = null,
    DeviceId = Environment.MachineName
};

// 3. Save the sync event
await database.SaveSyncEventAsync(syncEvent);
```

The synchronization service will automatically pick up this event and send it to the API.

## Event Types

Your application should create these event types:

### Order Events
- `order_created` - New order placed
- `order_updated` - Order modified (items, quantities)
- `order_completed` - Order finished/paid

### Payment Events
- `payment_processed` - Payment received
- `payment_refunded` - Payment reversed/cancelled

### Inventory Events
- `inventory_item_added` - New item added to inventory
- `inventory_item_updated` - Quantity or price changed
- `inventory_item_removed` - Item deleted from inventory

### Event Payload Format
Each event should have a JSON payload containing the relevant data:

**Order Created**
```json
{
    "OrderId": "guid",
    "ShiftId": "guid",
    "TotalAmount": 45.99,
    "CreatedAt": "2024-01-01T12:00:00Z"
}
```

**Payment Processed**
```json
{
    "PaymentId": "guid",
    "OrderId": "guid",
    "Amount": 45.99,
    "Method": "CASH",
    "CreatedAt": "2024-01-01T12:05:00Z"
}
```

**Inventory Item Updated**
```json
{
    "InventoryItemId": "guid",
    "Name": "Flour",
    "Quantity": 50,
    "Unit": "kg",
    "UpdatedAt": "2024-01-01T12:10:00Z"
}
```

## Monitoring and Troubleshooting

### Check Sync Status
```csharp
var syncService = serviceProvider.GetRequiredService<TimedSyncService>();
var stats = await syncService.GetStatusAsync();

Console.WriteLine($"Pending events: {stats.UnsyncedEventCount}");
Console.WriteLine($"API available: {stats.ApiAvailable}");
```

### Use SQLite Views
Query the database to see sync status:

```sql
-- See overall status
SELECT * FROM v_SyncStatus;

-- See pending events
SELECT * FROM v_PendingEvents;

-- See recently synced events
SELECT * FROM v_RecentActivity;
```

### Check Logs
All components use `ILogger<T>`:

```csharp
config.AddLogging(log =>
{
    log.AddConsole();
    log.AddFile("sync.log");
    log.SetMinimumLevel(LogLevel.Debug); // For detailed debugging
});
```

## Common Issues and Solutions

### Events Not Syncing

**Check 1: Are events being created?**
```sql
SELECT COUNT(*) FROM SyncEvents;
```

**Check 2: Is the API reachable?**
```csharp
var client = serviceProvider.GetRequiredService<ApiEventClient>();
var isHealthy = await client.IsHealthyAsync();
```

**Check 3: Are there errors in logs?**
- Look for ERROR or WARNING level messages
- Check API logs for request failures

### High CPU/Memory Usage

1. Reduce batch size: `BatchSize = 5`
2. Increase sync interval: `timedSyncIntervalSeconds: 120`
3. Clean up old events:
   ```sql
   DELETE FROM SyncEvents 
   WHERE SyncedAt IS NOT NULL 
   AND SyncedAt < DATETIME('now', '-30 days');
   ```

### Connection String Issues

SQLite: `Data Source=path/to/database.db;Version=3;`

Make sure:
- File path is correct and file exists
- Application has permission to read/write
- Use forward slashes or escaped backslashes in connection string

### API Endpoint Mismatch

Verify in RestaurantApi `SyncConfiguration`:
- `EventsEndpoint = "/api/events"` (single)
- `BatchEndpoint = "/api/events/batch"` (batch)

These must match the actual API routes.

## Performance Optimization

### For High Event Volume
```csharp
var config = new SyncConfiguration
{
    UseBatchEndpoint = true,  // Use batch for 10x+ throughput
    BatchSize = 50,           // Larger batches
    RequestTimeoutSeconds = 60 // More time for large batches
};
services.AddRestaurantSynchronization(config, timedSyncIntervalSeconds: 15);
```

### For Low Latency
```csharp
var config = new SyncConfiguration
{
    UseBatchEndpoint = true,
    BatchSize = 5,            // Smaller batches
    RequestTimeoutSeconds = 30
};
services.AddRestaurantSynchronization(config, timedSyncIntervalSeconds: 10);
```

### Database Optimization
Ensure indexes exist:
```sql
CREATE INDEX IF NOT EXISTS idx_SyncEvents_SyncedAt ON SyncEvents(SyncedAt);
CREATE INDEX IF NOT EXISTS idx_SyncEvents_CreatedAt ON SyncEvents(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_SyncEvents_Type ON SyncEvents(Type);
```

## Testing

### Unit Tests
```csharp
[TestMethod]
public async Task SyncService_Successfully_Syncs_Events()
{
    // Arrange
    var mockRepo = new Mock<SyncEventRepository>();
    var mockClient = new Mock<ApiEventClient>();
    var synchronizer = new EventSynchronizer(config, mockRepo.Object, mockClient.Object, logger);

    mockRepo
        .Setup(r => r.GetUnsyncedEventsAsync())
        .ReturnsAsync(new List<SyncEventRecord> { /* test data */ });

    mockClient
        .Setup(c => c.SendEventAsync(It.IsAny<EventDto>()))
        .ReturnsAsync((true, false));

    // Act
    var result = await synchronizer.SynchronizeAsync();

    // Assert
    Assert.IsTrue(result.Success);
    Assert.AreEqual(1, result.SyncedCount);
}
```

### Integration Tests
```csharp
[TestMethod]
public async Task Full_Sync_Flow_Works_End_To_End()
{
    // Create real services with test databases
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddRestaurantSynchronization(testConfig);
    
    var sp = services.BuildServiceProvider();
    var syncService = sp.GetRequiredService<TimedSyncService>();
    
    // Manually add test events to SQLite
    // ...
    
    // Execute sync
    var result = await syncService.SyncNowAsync();
    
    // Verify events were synced
    Assert.IsTrue(result.Success);
}
```

## Next Steps

1. **Add SyncDatabase.sql schema to RestaurantPOS**
2. **Add RestaurantSynchronizationLib reference to RestaurantPOS**
3. **Create SyncEvent records** whenever you create orders, payments, or inventory changes
4. **Configure and start** TimedSyncService in App.xaml.cs
5. **Monitor logs** to verify synchronization is working
6. **Test with sample events** before production deployment

## Support & Debugging

If events aren't syncing:

1. Check SQLite `SyncEvents` table has records with `SyncedAt = NULL`
2. Verify API is reachable: `curl https://localhost:7195/api/events/health`
3. Check application logs for ERROR messages
4. Increase logging level to DEBUG for detailed information
5. Verify connection strings are correct
6. Ensure RestaurantApi is running and database tables are created

## Files Reference

| File | Purpose |
|------|---------|
| `SyncConfiguration.cs` | Settings and configuration |
| `SyncEventRepository.cs` | SQLite data access |
| `ApiEventClient.cs` | HTTP communication |
| `EventSynchronizer.cs` | Sync orchestration |
| `TimedSyncService.cs` | Background worker |
| `ServiceCollectionExtensions.cs` | DI setup |
| `EventDto.cs` | API event format |
| `SyncDatabase.sql` | SQLite schema |
| `README.md` | API documentation |
| `INTEGRATION_EXAMPLE.cs` | Code examples |


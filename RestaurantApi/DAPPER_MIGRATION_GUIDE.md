# Migration from Entity Framework to Dapper

This document explains the changes made when migrating the RestaurantApi from Entity Framework Core to Dapper.

## What Changed

### Removed Dependencies
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`

### Added Dependencies
- `Dapper` - Lightweight micro-ORM
- `System.Data.SqlClient` - SQL Server data provider

### Removed Files
The following file is **no longer needed** and can be deleted:
- `RestaurantApi/Data/RestaurantContext.cs` - Entity Framework DbContext (replaced by repositories)

### New Files Added

#### Data Access Layer
- `Data/SqlConnectionProvider.cs` - Provides SQL connections
- `Data/Repositories/` - Repository pattern implementation:
  - `IProcessedEventRepository` / `ProcessedEventRepository`
  - `IInventoryItemRepository` / `InventoryItemRepository`
  - `IOrderRepository` / `OrderRepository`
  - `IOrderLineRepository` / `OrderLineRepository`
  - `IPaymentRepository` / `PaymentRepository`

#### Database Schema
- `Data/Schema.sql` - SQL script to create all required tables

## Key Differences

### Database Setup
**Before (Entity Framework)**:
```bash
dotnet ef database update
```

**After (Dapper)**:
1. Execute `Data/Schema.sql` in SQL Server Management Studio
2. Run the application normally

### Data Access Pattern
**Before (Entity Framework)**:
```csharp
using (var dbContext = new RestaurantContext())
{
    var item = await dbContext.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
    item.Quantity += 10;
    await dbContext.SaveChangesAsync();
}
```

**After (Dapper + Repository Pattern)**:
```csharp
var item = await _inventoryRepository.GetByIdAsync(id);
item.Quantity += 10;
await _inventoryRepository.UpdateAsync(item);
```

### Dependency Injection
**Before**:
```csharp
builder.Services.AddDbContext<RestaurantContext>(options =>
    options.UseSqlServer(connectionString));
```

**After**:
```csharp
builder.Services.AddScoped<IInventoryItemRepository>(
    sp => new InventoryItemRepository(connectionString));
```

## Advantages of Dapper

1. **Performance**: Minimal overhead with direct SQL execution
2. **Simplicity**: No complex ORM overhead
3. **Flexibility**: Full control over SQL queries
4. **Learning Curve**: Easier to understand and debug
5. **Lightweight**: Minimal memory footprint
6. **Speed**: Faster query execution for high-throughput scenarios

## Event Handlers

Event handlers were updated to use repositories instead of EF Core:

### InventoryEventHandler
- Uses `IInventoryItemRepository` instead of `DbContext.InventoryItems`

### OrderEventHandler  
- Uses `IOrderRepository` and `IOrderLineRepository` instead of DbContext navigation properties

### PaymentEventHandler
- Uses `IPaymentRepository` instead of `DbContext.Payments`

## EventProcessor

The `EventProcessor` now uses `IProcessedEventRepository` to track processed events instead of EF Core's change tracking.

## Adding New Entities

To add a new entity type with Dapper:

1. **Create the model** in `Data/Models/`
2. **Create repository interface and implementation** in `Data/Repositories/`
3. **Add SQL table** to `Data/Schema.sql`
4. **Register in `Program.cs`**:
   ```csharp
   builder.Services.AddScoped<IMyRepository>(sp => new MyRepository(connectionString));
   ```

## Database Queries with Dapper

Example queries using Dapper:

```csharp
// Query single row
var item = await connection.QueryFirstOrDefaultAsync<InventoryItem>(
    "SELECT * FROM InventoryItems WHERE Id = @Id", 
    new { Id = id });

// Query multiple rows
var items = await connection.QueryAsync<InventoryItem>(
    "SELECT * FROM InventoryItems");

// Insert record
var sql = @"
    INSERT INTO InventoryItems (Name, Quantity, Unit)
    VALUES (@Name, @Quantity, @Unit);
    SELECT CAST(SCOPE_IDENTITY() as int);";
var newId = await connection.QuerySingleAsync<int>(sql, item);

// Update record
var result = await connection.ExecuteAsync(
    "UPDATE InventoryItems SET Quantity = @Quantity WHERE Id = @Id",
    new { Quantity = item.Quantity, Id = item.Id });

// Delete record
await connection.ExecuteAsync(
    "DELETE FROM InventoryItems WHERE Id = @Id",
    new { Id = id });
```

## Troubleshooting

### "Invalid object name 'Orders'"
The database tables haven't been created yet. Execute `Data/Schema.sql` in SQL Server.

### Connection Issues
Verify your connection string in `appsettings.json` is correct for Dapper's `SqlConnection`.

### Differences in Behavior
- No lazy loading (Dapper loads only what you query)
- No change tracking (you manually call Update methods)
- No automatic migrations (use SQL scripts instead)

## Rollback to Entity Framework

If you need to revert to Entity Framework:
1. Restore EF Core NuGet packages in `.csproj`
2. Restore the `RestaurantContext.cs` file
3. Update services in `Program.cs` to use DbContext
4. Update event handlers and repositories to use DbContext
5. Use `dotnet ef database update` for migrations

## Summary

The migration to Dapper provides:
- ✅ Better performance for event processing
- ✅ Simpler, more transparent data access
- ✅ Lower memory footprint
- ✅ Easier debugging and SQL optimization
- ✅ Full control over database interactions

using System.Text.Json;
using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

public class InventoryEventHandler : IEventHandler
{
    private readonly IInventoryItemRepository _inventoryRepository;
    private readonly ILocationInventoryRepository _locationInventoryRepository;
    private readonly IInventoryCostRecordRepository _inventoryCostRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IExpenseCategoryRepository _expenseCategoryRepository;
    private readonly IMenuItemCostService _menuItemCostService;
    private readonly ILogger<InventoryEventHandler> _logger;

    public InventoryEventHandler(
        IInventoryItemRepository inventoryRepository,
        ILocationInventoryRepository locationInventoryRepository,
        IInventoryCostRecordRepository inventoryCostRepository,
        IExpenseRepository expenseRepository,
        IExpenseCategoryRepository expenseCategoryRepository,
        IMenuItemCostService menuItemCostService,
        ILogger<InventoryEventHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _locationInventoryRepository = locationInventoryRepository;
        _inventoryCostRepository = inventoryCostRepository;
        _expenseRepository = expenseRepository;
        _expenseCategoryRepository = expenseCategoryRepository;
        _menuItemCostService = menuItemCostService;
        _logger = logger;
    }

    public bool CanHandle(string eventType)
    {
        return eventType == EventTypes.InventoryItemAdded ||
               eventType == EventTypes.InventoryItemRemoved;
    }

    public async Task HandleAsync(EventDto @event)
    {
        switch (@event.Type)
        {
            case EventTypes.InventoryItemAdded:
                await HandleInventoryItemAdded(@event);
                break;
            case EventTypes.InventoryItemRemoved:
                await HandleInventoryItemRemoved(@event);
                break;
            default:
                throw new InvalidOperationException($"Unsupported event type: {@event.Type}");
        }
    }

    private async Task HandleInventoryItemAdded(EventDto @event)
    {
        var payload = DeserializePayload<InventoryItemPayload>(@event.Payload);
        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize inventory item payload. Payload is null or invalid.");

        // LocationId comes from EventDto
        if (@event.LocationId == Guid.Empty || @event.LocationId == null)
            throw new InvalidOperationException("InventoryItemAdded event missing LocationId. Cannot process inventory without a valid location.");

        var locationId = @event.LocationId.Value;

        if (payload.InventoryItemId == null || payload.InventoryItemId == Guid.Empty)
            throw new InvalidOperationException("InventoryItemId is required for inventory updates.");

        if (payload.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero for inventory adds.");

        var item = await _inventoryRepository.GetByIdAsync(payload.InventoryItemId.Value);

        if (item == null)
        {
            if (string.IsNullOrWhiteSpace(payload.Name) || string.IsNullOrWhiteSpace(payload.Unit))
                throw new InvalidOperationException("Inventory item does not exist and Name/Unit were not provided.");

            item = new InventoryItem
            {
                Id = payload.InventoryItemId.Value,
                Name = payload.Name.Trim(),
                Unit = payload.Unit.Trim(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _inventoryRepository.AddAsync(item);
        }

        var currentQuantity = await _locationInventoryRepository.GetQuantityAsync(locationId, item.Id);
        var newQuantity = currentQuantity + payload.Quantity;
        await _locationInventoryRepository.UpsertQuantityAsync(locationId, item.Id, newQuantity);

        if (payload.TotalCost.HasValue && payload.TotalCost.Value > 0)
        {
            var costRecord = new InventoryCostRecord
            {
                Id = Guid.NewGuid(),
                EventId = @event.Id,
                ShiftId = payload.ShiftId,
                LocationId = locationId,
                InventoryItemId = item.Id,
                QuantityReceived = payload.Quantity,
                TotalCost = payload.TotalCost.Value,
                RecordedAt = @event.CreatedAt
            };

            await _inventoryCostRepository.AddAsync(costRecord);

            // Create expense record if this was paid for
            if (payload.PaidWithCash)
            {
                // Check if expense already exists for this cost record
                var existingExpense = await _expenseRepository.GetByInventoryCostRecordIdAsync(costRecord.Id);
                if (existingExpense == null)
                {
                    // Get COGS category
                    var cogsCategory = await _expenseCategoryRepository.GetByNameAsync("COGS - Inventory");
                    if (cogsCategory != null)
                    {
                        var expense = new Expense
                        {
                            Id = Guid.NewGuid(),
                            ExpenseCategoryId = cogsCategory.Id,
                            Amount = payload.TotalCost.Value,
                            Description = $"Inventory: {item.Name}",
                            ExpenseDate = @event.CreatedAt,
                            LocationId = locationId,
                            ShiftId = payload.ShiftId,
                            InventoryCostRecordId = costRecord.Id,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        await _expenseRepository.AddAsync(expense);

                        _logger.LogInformation(
                            "Created expense record for inventory purchase: {ItemName} @ ${Amount}",
                            item.Name, payload.TotalCost.Value);
                    }
                    else
                    {
                        _logger.LogWarning("COGS category not found, cannot create expense for inventory purchase");
                    }
                }
            }

            // Update inventory item's current unit cost and trigger MenuItem COGS recalculation
            try
            {
                decimal unitCost = payload.TotalCost.Value / payload.Quantity;
                item.CurrentUnitCost = unitCost;
                item.LastCostUpdate = DateTime.Now;
                item.UpdatedAt = DateTime.Now;

                await _inventoryRepository.UpdateAsync(item);

                // Recalculate COGS for all MenuItems using this ingredient
                await _menuItemCostService.RecalculateMenuItemsCOGSByIngredientAsync(item.Id);

                _logger.LogInformation(
                    "Updated unit cost for {ItemName}: ${UnitCost:F2}/unit, triggered MenuItems COGS recalculation",
                    item.Name, unitCost);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating unit cost and recalculating COGS for inventory item {ItemId}",
                    item.Id);
            }
        }

        _logger.LogInformation(
            "Inventory item added: {ItemName} ({Quantity} {Unit}) at location {LocationId}",
            item.Name,
            payload.Quantity,
            item.Unit,
            locationId);
    }

    private async Task HandleInventoryItemRemoved(EventDto @event)
    {
        var payload = DeserializePayload<InventoryItemPayload>(@event.Payload);
        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize inventory item remove payload. Payload is null or invalid.");

        // LocationId comes from EventDto
        if (@event.LocationId == Guid.Empty || @event.LocationId == null)
            throw new InvalidOperationException("InventoryItemRemoved event missing LocationId. Cannot process inventory without a valid location.");

        var locationId = @event.LocationId.Value;

        if (payload.InventoryItemId == null || payload.InventoryItemId == Guid.Empty)
            throw new InvalidOperationException("InventoryItemId is required for inventory updates.");

        if (payload.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero for inventory removals.");

        var item = await _inventoryRepository.GetByIdAsync(payload.InventoryItemId.Value);

        if (item == null)
        {
            throw new InvalidOperationException($"Inventory item not found: {payload.InventoryItemId}. Cannot remove inventory.");
        }

        var currentQuantity = await _locationInventoryRepository.GetQuantityAsync(locationId, item.Id);
        var newQuantity = currentQuantity - payload.Quantity;
        if (newQuantity < 0)
            newQuantity = 0;

        await _locationInventoryRepository.UpsertQuantityAsync(locationId, item.Id, newQuantity);

        _logger.LogInformation(
            "Inventory item removed: {ItemName} ({Quantity} {Unit}) at location {LocationId} Reason: {Reason}",
            item.Name,
            payload.Quantity,
            item.Unit,
            locationId,
            payload.Reason ?? "N/A");
    }

    private class InventoryItemPayload
    {
        public Guid? InventoryItemId { get; set; }
        public Guid? ShiftId { get; set; }
        public string? Name { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Reason { get; set; }
        public decimal? TotalCost { get; set; }
        public bool PaidWithCash { get; set; }
    }

    private static T? DeserializePayload<T>(object? payload) where T : class
    {
        if (payload == null)
            return null;

        if (payload is JsonElement element)
        {
            try
            {
                return element.Deserialize<T>();
            }
            catch
            {
                return null;
            }
        }

        if (payload is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(text);
            }
            catch
            {
                return null;
            }
        }

        try
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(payload));
        }
        catch
        {
            return null;
        }
    }
}

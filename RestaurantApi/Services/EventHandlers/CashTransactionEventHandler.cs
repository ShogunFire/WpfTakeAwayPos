using System.Text.Json;
using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

public class CashTransactionEventHandler : IEventHandler
{
    private readonly ICashTransactionRepository _cashTransactionRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IExpenseCategoryRepository _expenseCategoryRepository;
    private readonly ILogger<CashTransactionEventHandler> _logger;

    public CashTransactionEventHandler(
        ICashTransactionRepository cashTransactionRepository,
        IExpenseRepository expenseRepository,
        IExpenseCategoryRepository expenseCategoryRepository,
        ILogger<CashTransactionEventHandler> logger)
    {
        _cashTransactionRepository = cashTransactionRepository;
        _expenseRepository = expenseRepository;
        _expenseCategoryRepository = expenseCategoryRepository;
        _logger = logger;
    }

    public bool CanHandle(string eventType)
    {
        return eventType == EventTypes.CashTransactionCreated;
    }

    public async Task HandleAsync(EventDto @event)
    {
        await HandleCashTransactionCreated(@event);
    }

    private async Task HandleCashTransactionCreated(EventDto @event)
    {
        var payload = DeserializePayload<CashTransactionPayload>(@event.Payload);
        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize cash transaction payload. Payload is null or invalid.");

        if (@event.LocationId == Guid.Empty || @event.LocationId == null)
        {
            throw new InvalidOperationException("Cash transaction event missing LocationId. Cannot process transaction without a valid location.");
        }

        var transaction = new CashTransaction
        {
            Id = payload.TransactionGuid ?? Guid.NewGuid(),
            ShiftId = payload.ShiftId,
            LocationId = @event.LocationId.Value,
            TransactionType = payload.Type ?? "Unknown",
            Amount = payload.Amount,
            Reason = payload.Reason,
            Description = payload.Description,
            OccurredAt = payload.Timestamp == default ? DateTime.Now : payload.Timestamp,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _cashTransactionRepository.AddAsync(transaction);

        _logger.LogInformation("Cash transaction recorded: {TransactionId}, Amount: {Amount}, Type: {Type}",
            transaction.Id, transaction.Amount, transaction.TransactionType);

        // Create or link expense record if this is an expense
        if (payload.IsExpense)
        {
            // Inventory purchase expenses are created by InventoryItemAdded processing.
            // Skip creation here to avoid duplicate expense records for the same purchase.
            if (payload.IsInventoryAdd)
            {
                _logger.LogInformation(
                    "Skipping expense creation for inventory cash transaction {TransactionId}; inventory handler owns inventory expenses",
                    transaction.Id);
                return;
            }

            // Create new expense (non-inventory expense like rent, utilities)
            // Determine category from reason/description
            var categoryName = DetermineExpenseCategory(payload.Reason, payload.Description);
            var category = await _expenseCategoryRepository.GetByNameAsync(categoryName);

            if (category != null)
            {
                var newExpense = new Expense
                {
                    Id = Guid.NewGuid(),
                    ExpenseCategoryId = category.Id,
                    Amount = Math.Abs(payload.Amount), // Expenses are positive
                    Description = string.IsNullOrWhiteSpace(payload.Description)
                        ? payload.Reason ?? "Cash expense"
                        : payload.Description,
                    ExpenseDate = payload.Timestamp == default ? DateTime.Now : payload.Timestamp,
                    LocationId = @event.LocationId ?? Guid.Empty,
                    ShiftId = payload.ShiftId,
                    CashTransactionId = transaction.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                await _expenseRepository.AddAsync(newExpense);

                _logger.LogInformation(
                    "Created expense record for cash transaction: {Category} @ ${Amount}",
                    categoryName, newExpense.Amount);
            }
            else
            {
                _logger.LogWarning(
                    "Expense category '{CategoryName}' not found, cannot create expense for cash transaction",
                    categoryName);
            }
        }
    }

    private string DetermineExpenseCategory(string? reason, string? description)
    {
        var text = $"{reason} {description}".ToLowerInvariant();

        if (text.Contains("rent")) return "Rent";
        if (text.Contains("utility") || text.Contains("utilities") || text.Contains("electric") || text.Contains("water") || text.Contains("gas")) return "Utilities";
        if (text.Contains("payroll") || text.Contains("salary") || text.Contains("wage")) return "Payroll";
        if (text.Contains("marketing") || text.Contains("advertis")) return "Marketing";
        if (text.Contains("equipment") || text.Contains("appliance")) return "Equipment";
        if (text.Contains("supply") || text.Contains("supplies")) return "Supplies";
        if (text.Contains("maintenance") || text.Contains("repair")) return "Maintenance";
        if (text.Contains("insurance")) return "Insurance";

        return "Other";
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

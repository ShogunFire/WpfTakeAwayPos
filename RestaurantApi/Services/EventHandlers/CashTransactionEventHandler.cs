using System.Text.Json;
using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

public class CashTransactionEventHandler : IEventHandler
{
    private readonly ICashTransactionRepository _cashTransactionRepository;
    private readonly ILogger<CashTransactionEventHandler> _logger;

    public CashTransactionEventHandler(ICashTransactionRepository cashTransactionRepository, ILogger<CashTransactionEventHandler> logger)
    {
        _cashTransactionRepository = cashTransactionRepository;
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

        var transaction = new CashTransaction
        {
            Id = payload.TransactionGuid ?? Guid.NewGuid(),
            ShiftId = payload.ShiftId,
            TransactionType = payload.Type ?? "Unknown",
            Amount = payload.Amount,
            Reason = payload.Reason,
            Description = payload.Description,
            OccurredAt = payload.Timestamp == default ? DateTime.UtcNow : payload.Timestamp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _cashTransactionRepository.AddAsync(transaction);

        _logger.LogInformation("Cash transaction recorded: {TransactionId}, Amount: {Amount}, Type: {Type}",
            transaction.Id, transaction.Amount, transaction.TransactionType);
    }

    private class CashTransactionPayload
    {
        public Guid? TransactionGuid { get; set; }
        public Guid? ShiftId { get; set; }
        public string? Type { get; set; }
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
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

using System.Text.Json;
using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

public class PaymentEventHandler : IEventHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentEventHandler> _logger;

    public PaymentEventHandler(IPaymentRepository paymentRepository, IOrderRepository orderRepository, ILogger<PaymentEventHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public bool CanHandle(string eventType)
    {
        return eventType == EventTypes.PaymentProcessed;
    }

    public async Task HandleAsync(EventDto @event)
    {
        if (@event.Type == EventTypes.PaymentProcessed)
        {
            await HandlePaymentProcessed(@event);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported event type: {@event.Type}");
        }
    }

    private async Task HandlePaymentProcessed(EventDto @event)
    {
        var payload = DeserializePayload<PaymentPayload>(@event.Payload);
        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize payment payload. Payload is null or invalid.");

        if (payload.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException($"Payment payload missing OrderId for PaymentId {payload.PaymentId}. Cannot process payment without a valid order.");
        }

        if (@event.LocationId == Guid.Empty || @event.LocationId == null)
        {
            throw new InvalidOperationException("Payment event missing LocationId. Cannot process payment without a valid location.");
        }

        var order = await _orderRepository.GetByIdAsync(payload.OrderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Order not found for Payment. OrderId: {payload.OrderId}. Cannot process payment for non-existent order.");
        }

        var payment = new Payment
        {
            Id = payload.PaymentId ?? Guid.NewGuid(),
            OrderId = order.Id,
            LocationId = @event.LocationId.Value,
            Amount = payload.Amount,
            PaymentMethod = payload.PaymentMethod ?? "Unknown",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _paymentRepository.AddAsync(payment);

        _logger.LogInformation("Payment processed: {PaymentId}, Amount: {Amount}, Method: {Method}", 
            payment.Id, payment.Amount, payment.PaymentMethod);
    }

    private class PaymentPayload
    {
        public Guid? PaymentId { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
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

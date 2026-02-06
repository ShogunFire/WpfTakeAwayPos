using System.Text.Json;
using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

public class OrderEventHandler : IEventHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly ILogger<OrderEventHandler> _logger;

    public OrderEventHandler(IOrderRepository orderRepository, IOrderLineRepository orderLineRepository, ILogger<OrderEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _logger = logger;
    }

    public bool CanHandle(string eventType)
    {
        return eventType == EventTypes.OrderCompleted;
    }

    public async Task HandleAsync(EventDto @event)
    {
        if (@event.Type == EventTypes.OrderCompleted)
        {
            await HandleOrderCompleted(@event);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported event type: {@event.Type}");
        }
    }

    private async Task HandleOrderCompleted(EventDto @event)
    {
        var payload = DeserializePayload<OrderPayload>(@event.Payload);
        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize order completed payload. Payload is null or invalid.");

        var order = new Order
        {
            Id = payload.OrderId ?? Guid.NewGuid(),
            ShiftId = payload.ShiftId,
            Subtotal = payload.Subtotal,
            Tax = payload.Tax,
            TotalAmount = payload.TotalAmount,
            TotalPaid = payload.TotalPaid,
            Remaining = payload.Remaining,
            TotalChange = payload.TotalChange,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddAsync(order);

        // Add order lines if provided
        if (payload.OrderLines != null)
        {
            foreach (var linePayload in payload.OrderLines)
            {
                var orderLine = new OrderLine
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Quantity = linePayload.Quantity,
                    UnitPrice = linePayload.UnitPrice,
                    LineTotal = linePayload.LineTotal,
                    MenuItemName = linePayload.MenuItemName ?? "Unknown",
                    MenuItemId = linePayload.MenuItemId ?? Guid.NewGuid()
                };

                await _orderLineRepository.AddAsync(orderLine);
            }
        }

        _logger.LogInformation("Order completed: {OrderId}, Total: {Total}", order.Id, order.TotalAmount);
    }

    private class OrderPayload
    {
        public Guid? OrderId { get; set; }
        public Guid? ShiftId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Remaining { get; set; }
        public decimal TotalChange { get; set; }
        public List<OrderLinePayload>? OrderLines { get; set; }
    }

    private class OrderLinePayload
    {
        public Guid? MenuItemId { get; set; }
        public string? MenuItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
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

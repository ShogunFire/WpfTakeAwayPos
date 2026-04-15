using System.Text.Json;
using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

public class OrderEventHandler : IEventHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemCostService _menuItemCostService;
    private readonly ILogger<OrderEventHandler> _logger;

    public OrderEventHandler(
        IOrderRepository orderRepository,
        IOrderLineRepository orderLineRepository,
        IShiftRepository shiftRepository,
        IMenuItemRepository menuItemRepository,
        IMenuItemCostService menuItemCostService,
        ILogger<OrderEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _shiftRepository = shiftRepository;
        _menuItemRepository = menuItemRepository;
        _menuItemCostService = menuItemCostService;
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

        // LocationId comes from EventDto
        if (@event.LocationId == Guid.Empty || @event.LocationId == null)
            throw new InvalidOperationException("Order event missing LocationId. Cannot process order without a valid location.");

        var locationId = @event.LocationId.Value;

        // Calculate total COGS from order lines
        decimal totalCOGS = 0;
        var orderLines = new List<OrderLine>();

        if (payload.OrderLines != null)
        {
            foreach (var linePayload in payload.OrderLines)
            {
                var menuItem = await _menuItemRepository.GetByIdAsync(linePayload.MenuItemId ?? Guid.Empty);
                if (menuItem != null)
                {
                    // COGS for this line = quantity × MenuItem's CurrentCOGS
                    totalCOGS += linePayload.Quantity * menuItem.CurrentCOGS;
                }

                var orderLine = new OrderLine
                {
                    Id = Guid.NewGuid(),
                    OrderId = payload.OrderId ?? Guid.NewGuid(),
                    LocationId = locationId,
                    Quantity = linePayload.Quantity,
                    UnitPrice = linePayload.UnitPrice,
                    LineTotal = linePayload.LineTotal,
                    MenuItemName = linePayload.MenuItemName ?? "Unknown",
                    MenuItemId = linePayload.MenuItemId ?? Guid.NewGuid()
                };

                orderLines.Add(orderLine);
            }
        }

        // Calculate profit metrics
        decimal grossProfit = payload.TotalAmount - totalCOGS;
        decimal profitMargin = payload.TotalAmount > 0
            ? Math.Round((decimal)(grossProfit / payload.TotalAmount * 100), 2)
            : 0;

        var order = new Order
        {
            Id = payload.OrderId ?? Guid.NewGuid(),
            ShiftId = payload.ShiftId,
            LocationId = locationId,
            Subtotal = payload.Subtotal,
            Tax = payload.Tax,
            TotalAmount = payload.TotalAmount,
            TotalPaid = payload.TotalPaid,
            Remaining = payload.Remaining,
            TotalChange = payload.TotalChange,
            TotalCOGS = totalCOGS,
            GrossProfit = grossProfit,
            ProfitMargin = profitMargin,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _orderRepository.AddAsync(order);

        // Add order lines if provided
        foreach (var orderLine in orderLines)
        {
            orderLine.OrderId = order.Id;
            await _orderLineRepository.AddAsync(orderLine);
        }

        // Create gross profit history records for each menu item in the order
        if (payload.OrderLines != null)
        {
            foreach (var linePayload in payload.OrderLines)
            {
                try
                {
                    if (linePayload.MenuItemId.HasValue && linePayload.MenuItemId.Value != Guid.Empty)
                    {
                        await _menuItemCostService.CreateGrossProfitHistoryAsync(linePayload.MenuItemId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to create gross profit history for MenuItem {MenuItemId}",
                        linePayload.MenuItemId);
                }
            }
        }

        _logger.LogInformation(
            "Order completed: {OrderId}, Total: {Total}, TotalCOGS: {TotalCOGS}, GrossProfit: {GrossProfit:F2}, ProfitMargin: {Margin:F2}%",
            order.Id, order.TotalAmount, totalCOGS, grossProfit, profitMargin);
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

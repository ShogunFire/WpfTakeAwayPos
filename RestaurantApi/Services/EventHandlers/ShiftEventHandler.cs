using System.Text.Json;
using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

public class ShiftEventHandler : IEventHandler
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<ShiftEventHandler> _logger;

    public ShiftEventHandler(IShiftRepository shiftRepository, ILogger<ShiftEventHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public bool CanHandle(string eventType)
    {
        return eventType == EventTypes.ShiftStarted || eventType == EventTypes.ShiftEnded;
    }

    public async Task HandleAsync(EventDto @event)
    {
        if (@event.Type == EventTypes.ShiftStarted)
        {
            await HandleShiftStarted(@event);
        }
        else if (@event.Type == EventTypes.ShiftEnded)
        {
            await HandleShiftEnded(@event);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported event type: {@event.Type}");
        }
    }

    private async Task HandleShiftStarted(EventDto @event)
    {
        var payload = DeserializePayload<ShiftStartedPayload>(@event.Payload);
        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize shift started payload. Payload is null or invalid.");

        // LocationId comes from EventDto
        if (@event.LocationId == Guid.Empty || @event.LocationId == null)
        {
            _logger.LogWarning("Shift {ShiftId} started without LocationId in event", payload.ShiftId);
            return;
        }

        var locationId = @event.LocationId.Value;

        // Check if shift already exists
        var existingShift = await _shiftRepository.GetByIdAsync(payload.ShiftId);
        if (existingShift != null)
        {
            _logger.LogWarning("Shift {ShiftId} already exists, skipping creation", payload.ShiftId);
            return;
        }

        var shift = new Shift
        {
            Id = payload.ShiftId,
            LocationId = locationId,
            OpenedAt = payload.StartDateTime,
            OpeningCash = payload.OpeningCash,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _shiftRepository.AddAsync(shift);
        _logger.LogInformation("Shift started: {ShiftId} at location {LocationId}", shift.Id, shift.LocationId);
    }

    private async Task HandleShiftEnded(EventDto @event)
    {
        var payload = DeserializePayload<ShiftEndedPayload>(@event.Payload);
        if (payload == null)
            throw new InvalidOperationException("Failed to deserialize shift ended payload. Payload is null or invalid.");

        var shift = await _shiftRepository.GetByIdAsync(payload.ShiftId);
        if (shift == null)
        {
            _logger.LogWarning("Shift {ShiftId} not found, cannot end shift", payload.ShiftId);
            return;
        }

        shift.ClosedAt = payload.EndDateTime;
        shift.ClosingCash = payload.DeclaredCash;
        shift.UpdatedAt = DateTime.Now;

        await _shiftRepository.UpdateAsync(shift);
        _logger.LogInformation("Shift ended: {ShiftId}", shift.Id);
    }

    private class ShiftStartedPayload
    {
        public Guid ShiftId { get; set; }
        public DateTime StartDateTime { get; set; }
        public decimal OpeningCash { get; set; }
    }

    private class ShiftEndedPayload
    {
        public Guid ShiftId { get; set; }
        public DateTime? EndDateTime { get; set; }
        public decimal? DeclaredCash { get; set; }
        public decimal? ExpectedCash { get; set; }
        public decimal? Difference { get; set; }
        public string? Notes { get; set; }
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

        if (payload is T typedPayload)
            return typedPayload;

        return null;
    }
}

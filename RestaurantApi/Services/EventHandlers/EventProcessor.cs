using RestaurantApi.Data.Models;
using RestaurantApi.Data.Repositories;
using RestaurantShared.DTOs;
using System.Text.Json;

namespace RestaurantApi.Services.EventHandlers;

/// <summary>
/// Orchestrates event processing and prevents duplicate event handling
/// </summary>
public interface IEventProcessor
{
    /// <summary>
    /// Processes an event and stores it if successful
    /// </summary>
    Task<(bool Success, bool AlreadyProcessed)> ProcessEventAsync(EventDto @event);
}

public class EventProcessor : IEventProcessor
{
    private readonly IProcessedEventRepository _processedEventRepository;
    private readonly IEnumerable<IEventHandler> _handlers;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(IProcessedEventRepository processedEventRepository, IEnumerable<IEventHandler> handlers, ILogger<EventProcessor> logger)
    {
        _processedEventRepository = processedEventRepository;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task<(bool Success, bool AlreadyProcessed)> ProcessEventAsync(EventDto @event)
    {
        try
        {
            // Check if event has already been processed
            var existingEvent = await _processedEventRepository.GetByIdAsync(@event.Id);

            if (existingEvent != null)
            {
                if (string.Equals(existingEvent.Status, "Processed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Event already processed: {EventId} ({EventType})", @event.Id, @event.Type);
                    return (true, true);
                }
            }

            var payloadJson = JsonSerializer.Serialize(@event.Payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Queue event for processing by background service
            var processedEvent = new ProcessedEvent
            {
                Id = @event.Id,
                EventType = @event.Type,
                DeviceId = @event.DeviceId,
                LocationId = @event.LocationId,
                Payload = payloadJson,
                Status = "Queued",
                ErrorMessage = null,
                EventCreatedAt = @event.CreatedAt,
                ReceivedAt = DateTime.Now,
                LastAttemptAt = null,
                AttemptCount = 0,
                ProcessedAt = null
            };

            await _processedEventRepository.AddOrUpdateAsync(processedEvent);
            _logger.LogInformation("Event queued for processing: {EventId} ({EventType})", @event.Id, @event.Type);
            return (true, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing event: {EventId} ({EventType})", @event.Id, @event.Type);
            throw;
        }
    }
}

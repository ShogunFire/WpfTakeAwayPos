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

                _logger.LogWarning("Retrying event with status {Status}: {EventId} ({EventType})", existingEvent.Status, @event.Id, @event.Type);
            }

            var payloadJson = JsonSerializer.Serialize(@event.Payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Ensure event is stored even if processing fails
            var processedEvent = new ProcessedEvent
            {
                Id = @event.Id,
                EventType = @event.Type,
                DeviceId = @event.DeviceId,
                Payload = payloadJson,
                Status = existingEvent == null ? "Received" : existingEvent.Status,
                ErrorMessage = null,
                ReceivedAt = existingEvent?.ReceivedAt ?? DateTime.UtcNow,
                LastAttemptAt = null,
                AttemptCount = existingEvent?.AttemptCount ?? 0,
                ProcessedAt = null
            };

            await _processedEventRepository.AddOrUpdateAsync(processedEvent);

            // Find appropriate handler
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(@event.Type));
            if (handler == null)
            {
                _logger.LogWarning("No handler found for event type: {EventType}", @event.Type);
                return (false, false);
            }

            _logger.LogInformation("Processing event: {EventId} ({EventType}) with handler {HandlerType}", 
                @event.Id, @event.Type, handler.GetType().Name);

            // Process event
            try
            {
                await handler.HandleAsync(@event);

                await _processedEventRepository.UpdateStatusAsync(@event.Id, "Processed", null, DateTime.UtcNow);

                _logger.LogInformation("Event processed successfully: {EventId} ({EventType})", @event.Id, @event.Type);
                return (true, false);
            }
            catch (Exception ex)
            {
                var errorDetails = $"{ex.GetType().Name}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    errorDetails += $"\n\nInner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
                }
                
                _logger.LogError(ex, "Exception processing event: {EventId} ({EventType})", @event.Id, @event.Type);
                await _processedEventRepository.UpdateStatusAsync(@event.Id, "Failed", errorDetails, null);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error processing event: {EventId} ({EventType})", @event.Id, @event.Type);
            throw;
        }
    }
}

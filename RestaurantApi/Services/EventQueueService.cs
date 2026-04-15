using System.Text.Json;
using RestaurantApi.Data.Repositories;
using RestaurantApi.Services.EventHandlers;
using RestaurantShared.DTOs;

namespace RestaurantApi.Services;

/// <summary>
/// Background service that processes queued events every 5 seconds
/// </summary>
public class EventQueueService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventQueueService> _logger;
    private const int ProcessingIntervalSeconds = 5;
    private const int MaxRetries = 3;

    public EventQueueService(IServiceProvider serviceProvider, ILogger<EventQueueService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventQueueService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending events");
            }

            // Wait for the specified interval
            await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("EventQueueService stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processedEventRepository = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();
        var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IEventHandler>>();

        // Get all queued events
        var pendingEvents = await processedEventRepository.GetPendingEventsAsync();

        if (!pendingEvents.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending events", pendingEvents.Count);

        foreach (var processedEvent in pendingEvents)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                // Rebuild EventDto from stored data
                // Payload is stored as JSON string in database, deserialize it back to JsonElement
                object? payload = null;
                if (!string.IsNullOrWhiteSpace(processedEvent.Payload))
                {
                    try
                    {
                        payload = System.Text.Json.JsonDocument.Parse(processedEvent.Payload).RootElement;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse payload JSON for event {EventId}", processedEvent.Id);
                        payload = processedEvent.Payload; // Fall back to string if JSON parsing fails
                    }
                }

                var eventDto = new EventDto
                {
                    Id = processedEvent.Id,
                    Type = processedEvent.EventType,
                    DeviceId = processedEvent.DeviceId,
                    Payload = payload,
                    CreatedAt = processedEvent.ReceivedAt,
                    LocationId = processedEvent.LocationId
                };

                // Find handler
                var handler = handlers.FirstOrDefault(h => h.CanHandle(eventDto.Type));
                if (handler == null)
                {
                    _logger.LogWarning("No handler found for event type: {EventType}", eventDto.Type);
                    await processedEventRepository.UpdateStatusAsync(
                        processedEvent.Id, 
                        "Failed", 
                        $"No handler found for event type: {eventDto.Type}", 
                        DateTime.Now);
                    continue;
                }

                _logger.LogInformation(
                    "Processing queued event: {EventId} ({EventType}) with handler {HandlerType}",
                    eventDto.Id, eventDto.Type, handler.GetType().Name);

                // Process event
                try
                {
                    await handler.HandleAsync(eventDto);

                    await processedEventRepository.UpdateStatusAsync(
                        processedEvent.Id,
                        "Processed",
                        null,
                        DateTime.Now);

                    _logger.LogInformation("Event processed successfully: {EventId} ({EventType})", eventDto.Id, eventDto.Type);
                }
                catch (Exception handlerEx)
                {
                    var errorMessage = $"{handlerEx.GetType().Name}: {handlerEx.Message}";
                    var attemptCount = processedEvent.AttemptCount + 1;

                    if (attemptCount >= MaxRetries)
                    {
                        _logger.LogError(
                            handlerEx,
                            "Event processing failed after {AttemptCount} attempts: {EventId} ({EventType})",
                            attemptCount,
                            eventDto.Id,
                            eventDto.Type);

                        await processedEventRepository.UpdateStatusAsync(
                            processedEvent.Id,
                            "Failed",
                            $"Max retries exceeded ({MaxRetries}). Last error: {errorMessage}",
                            DateTime.Now);
                    }
                    else
                    {
                        _logger.LogWarning(
                            handlerEx,
                            "Event processing failed (attempt {AttemptCount}/{MaxRetries}): {EventId} ({EventType})",
                            attemptCount,
                            MaxRetries,
                            eventDto.Id,
                            eventDto.Type);

                        await processedEventRepository.UpdateStatusAsync(
                            processedEvent.Id,
                            "Queued",
                            errorMessage,
                            DateTime.Now,
                            attemptCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing event {EventId}", processedEvent.Id);
            }
        }
    }
}

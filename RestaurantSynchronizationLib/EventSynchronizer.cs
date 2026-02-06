using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RestaurantSynchronizationLib.Configuration;
using RestaurantShared.DTOs;
using RestaurantSynchronizationLib.Persistence;
using RestaurantSynchronizationLib.Services;
using Microsoft.Extensions.Logging;

namespace RestaurantSynchronizationLib;

/// <summary>
/// Orchestrates synchronization of events from SQLite to the RestaurantApi
/// </summary>
public class EventSynchronizer
{
    private readonly SyncConfiguration _config;
    private readonly SyncEventRepository _eventRepository;
    private readonly ApiEventClient _apiClient;
    private readonly ILogger<EventSynchronizer> _logger;

    public EventSynchronizer(
        SyncConfiguration config,
        SyncEventRepository eventRepository,
        ApiEventClient apiClient,
        ILogger<EventSynchronizer> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Synchronize all unsynced events from the database to the API
    /// </summary>
    public async Task<SyncResult> SynchronizeAsync()
    {
        var result = new SyncResult();

        try
        {
            _logger.LogInformation("Starting event synchronization");

            // Check if API is available
            var isHealthy = await _apiClient.IsHealthyAsync();
            if (!isHealthy)
            {
                _logger.LogWarning("API is not available");
                result.Success = false;
                result.Message = "API is not available";
                return result;
            }

            // Get unsynced events from database
            var unsyncedEvents = await _eventRepository.GetUnsyncedEventsAsync();

            if (!unsyncedEvents.Any())
            {
                _logger.LogInformation("No unsynced events to synchronize");
                result.Success = true;
                result.Message = "No events to synchronize";
                return result;
            }

            result.TotalEvents = unsyncedEvents.Count;
            _logger.LogInformation("Found {Count} events to synchronize", unsyncedEvents.Count);

            // Convert to DTOs and send
            if (_config.UseBatchEndpoint)
            {
                await SynchronizeUsingBatchAsync(unsyncedEvents, result);
            }
            else
            {
                await SynchronizeOneByOneAsync(unsyncedEvents, result);
            }

            result.Success = result.SyncedCount > 0;
            result.Message = $"Synchronized {result.SyncedCount}/{result.TotalEvents} events";

            _logger.LogInformation("Event synchronization completed: {Message}", result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during event synchronization");
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Synchronize events using the batch endpoint
    /// </summary>
    private async Task SynchronizeUsingBatchAsync(List<SyncEventRecord> unsyncedEvents, SyncResult result)
    {
        var eventDtos = ConvertToEventDtos(unsyncedEvents);
        var successfulEventIds = new List<Guid>();

        // Process in batches
        for (int i = 0; i < eventDtos.Count; i += _config.BatchSize)
        {
            var batch = eventDtos.Skip(i).Take(_config.BatchSize).ToList();
            _logger.LogInformation("Sending batch {Batch}/{Total} with {Count} events", 
                (i / _config.BatchSize) + 1, 
                Math.Ceiling((double)eventDtos.Count / _config.BatchSize),
                batch.Count);

            var syncResults = await _apiClient.SendEventsAsync(batch);

            foreach (var syncResult in syncResults)
            {
                _logger.LogDebug("Sync result - EventId: {EventId}, Success: {Success}, AlreadyProcessed: {AlreadyProcessed}, Error: {Error}", 
                    syncResult.EventId, syncResult.Success, syncResult.AlreadyProcessed, syncResult.Error);
                    
                if (syncResult.Success && !syncResult.AlreadyProcessed)
                {
                    successfulEventIds.Add(syncResult.EventId);
                    result.SyncedCount++;
                }
                else if (syncResult.AlreadyProcessed)
                {
                    result.AlreadyProcessedCount++;
                    successfulEventIds.Add(syncResult.EventId); // Mark as synced even if already processed
                }
                else
                {
                    result.FailedCount++;
                    _logger.LogWarning("Failed to sync event {EventId}: {Error}", syncResult.EventId, syncResult.Error);
                }
            }
        }

        // Mark successful events as synced in database
        if (successfulEventIds.Any())
        {
            _logger.LogInformation("Marking {Count} events as synced", successfulEventIds.Count);
            try
            {
                await _eventRepository.MarkAsSyncedAsync(successfulEventIds);
                _logger.LogInformation("Successfully marked {Count} events as synced", successfulEventIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking {Count} events as synced", successfulEventIds.Count);
                throw;
            }
        }
        else
        {
            _logger.LogInformation("No events to mark as synced. Synced: {Synced}, Already processed: {AlreadyProcessed}, Failed: {Failed}", 
                result.SyncedCount, result.AlreadyProcessedCount, result.FailedCount);
        }
    }

    /// <summary>
    /// Synchronize events one by one
    /// </summary>
    private async Task SynchronizeOneByOneAsync(List<SyncEventRecord> unsyncedEvents, SyncResult result)
    {
        var eventDtos = ConvertToEventDtos(unsyncedEvents);

        foreach (var eventDto in eventDtos)
        {
            var (success, alreadyProcessed) = await _apiClient.SendEventAsync(eventDto);

            if (success && !alreadyProcessed)
            {
                result.SyncedCount++;
                await _eventRepository.MarkAsSyncedAsync(eventDto.Id);
            }
            else if (alreadyProcessed)
            {
                result.AlreadyProcessedCount++;
                await _eventRepository.MarkAsSyncedAsync(eventDto.Id);
            }
            else
            {
                result.FailedCount++;
            }
        }
    }

    /// <summary>
    /// Convert SyncEventRecord to EventDto
    /// </summary>
    private List<EventDto> ConvertToEventDtos(List<SyncEventRecord> events)
    {
        var dtos = new List<EventDto>();

        foreach (var @event in events)
        {
            try
            {
                // Parse payload if it's JSON
                object? payload = null;
                if (!string.IsNullOrEmpty(@event.Payload))
                {
                    try
                    {
                        payload = JsonSerializer.Deserialize<object>(@event.Payload);
                    }
                    catch
                    {
                        // If it fails to parse as JSON, keep it as string
                        payload = @event.Payload;
                    }
                }

                dtos.Add(new EventDto
                {
                    Id = @event.Id,
                    Type = @event.Type,
                    Payload = payload,
                    CreatedAt = @event.CreatedAt,
                    DeviceId = @event.DeviceId ?? _config.DeviceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting event {EventId} to DTO", @event.Id);
            }
        }

        return dtos;
    }

    /// <summary>
    /// Get synchronization statistics
    /// </summary>
    public async Task<SyncStatistics> GetStatisticsAsync()
    {
        var unsyncedCount = await _eventRepository.GetUnsyncedEventCountAsync();

        return new SyncStatistics
        {
            UnsyncedEventCount = unsyncedCount,
            ApiAvailable = await _apiClient.IsHealthyAsync()
        };
    }
}

/// <summary>
/// Result of a synchronization operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalEvents { get; set; }
    public int SyncedCount { get; set; }
    public int FailedCount { get; set; }
    public int AlreadyProcessedCount { get; set; }
}

/// <summary>
/// Statistics about synchronization status
/// </summary>
public class SyncStatistics
{
    public int UnsyncedEventCount { get; set; }
    public bool ApiAvailable { get; set; }
}

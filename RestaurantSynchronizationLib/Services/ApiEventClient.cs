using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using RestaurantSynchronizationLib.Configuration;
using RestaurantSynchronizationLib.Models;
using RestaurantShared.DTOs;
using Microsoft.Extensions.Logging;

namespace RestaurantSynchronizationLib.Services;

/// <summary>
/// HTTP client for communicating with the RestaurantApi
/// </summary>
public class ApiEventClient
{
    private readonly HttpClient _httpClient;
    private readonly SyncConfiguration _config;
    private readonly ILogger<ApiEventClient> _logger;

    public ApiEventClient(SyncConfiguration config, ILogger<ApiEventClient> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create HttpClient with base address
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.ApiBaseAddress),
            Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds)
        };
    }

    /// <summary>
    /// Send a single event to the API
    /// </summary>
    public async Task<(bool Success, bool AlreadyProcessed)> SendEventAsync(EventDto @event)
    {
        try
        {
            _logger.LogInformation("Sending event {EventId} ({EventType}) to API", @event.Id, @event.Type);

            var response = await _httpClient.PostAsJsonAsync(_config.EventsEndpoint, @event);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status {StatusCode} for event {EventId}", response.StatusCode, @event.Id);
                return (false, false);
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            
            if (result.AlreadyProcessed)
            {
                _logger.LogInformation("Event {EventId} was already processed", @event.Id);
            }

            return (result.Success, result.AlreadyProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event {EventId} to API", @event.Id);
            return (false, false);
        }
    }

    /// <summary>
    /// Send multiple events in a batch
    /// </summary>
    public async Task<List<EventSyncResult>> SendEventsAsync(List<EventDto> events)
    {
        var results = new List<EventSyncResult>();

        if (!events.Any())
        {
            return results;
        }

        try
        {
            _logger.LogInformation("Sending batch of {Count} events to API", events.Count);

            var response = await _httpClient.PostAsJsonAsync(_config.BatchEndpoint, events);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API batch endpoint returned status {StatusCode}", response.StatusCode);
                
                // Return individual results as failed
                foreach (var @event in events)
                {
                    results.Add(new EventSyncResult
                    {
                        EventId = @event.Id,
                        Success = false,
                        AlreadyProcessed = false,
                        Error = $"API returned status {response.StatusCode}"
                    });
                }

                return results;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<EventProcessingResult>>>();

            if (apiResponse != null && apiResponse.Data != null)
            {
                foreach (var processingResult in apiResponse.Data)
                {
                    results.Add(new EventSyncResult
                    {
                        EventId = processingResult.EventId,
                        Success = processingResult.Success,
                        AlreadyProcessed = processingResult.AlreadyProcessed,
                        Error = processingResult.Message
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending batch of {Count} events to API", events.Count);

            // Return all as failed with error message
            foreach (var @event in events)
            {
                results.Add(new EventSyncResult
                {
                    EventId = @event.Id,
                    Success = false,
                    AlreadyProcessed = false,
                    Error = ex.Message
                });
            }

            return results;
        }
    }

    /// <summary>
    /// Check if the API is healthy
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/events/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Result of syncing a single event
/// </summary>
public class EventSyncResult
{
    public Guid EventId { get; set; }
    public bool Success { get; set; }
    public bool AlreadyProcessed { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result from API batch processing
/// </summary>
public class EventProcessingResult
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool AlreadyProcessed { get; set; }
    public string Message { get; set; } = string.Empty;
}

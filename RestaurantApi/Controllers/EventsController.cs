using Microsoft.AspNetCore.Mvc;
using RestaurantApi.DTOs;
using RestaurantApi.Services.EventHandlers;
using EventDto = RestaurantShared.DTOs.EventDto;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventProcessor _eventProcessor;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventProcessor eventProcessor, ILogger<EventsController> logger)
    {
        _eventProcessor = eventProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Receive and process an event from the POS system
    /// </summary>
    /// <param name="eventDto">The event to process</param>
    /// <returns>Response indicating success and whether event was already processed</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> ProcessEvent([FromBody] EventDto eventDto)
    {
        if (eventDto == null)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Event cannot be null"
            });
        }

        if (string.IsNullOrWhiteSpace(eventDto.Type))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Event type is required"
            });
        }

        _logger.LogInformation("Received event: {EventId} ({EventType})", eventDto.Id, eventDto.Type);

        var (success, alreadyProcessed) = await _eventProcessor.ProcessEventAsync(eventDto);

        if (!success)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = alreadyProcessed 
                    ? "Event has already been processed" 
                    : "Failed to process event",
                AlreadyProcessed = alreadyProcessed
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = alreadyProcessed ? "Event was already processed" : "Event processed successfully",
            AlreadyProcessed = alreadyProcessed
        });
    }

    /// <summary>
    /// Batch process multiple events
    /// </summary>
    /// <param name="events">Collection of events to process</param>
    /// <returns>Results for each event</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<List<EventProcessingResult>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<EventProcessingResult>>>> ProcessEventsBatch([FromBody] List<EventDto> events)
    {
        if (events == null || !events.Any())
        {
            return BadRequest(new ApiResponse<List<EventProcessingResult>>
            {
                Success = false,
                Message = "At least one event is required"
            });
        }

        var results = new List<EventProcessingResult>();

        foreach (var eventDto in events)
        {
            try
            {
                var (success, alreadyProcessed) = await _eventProcessor.ProcessEventAsync(eventDto);
                results.Add(new EventProcessingResult
                {
                    EventId = eventDto.Id,
                    EventType = eventDto.Type,
                    Success = success,
                    AlreadyProcessed = alreadyProcessed,
                    Message = success 
                        ? (alreadyProcessed ? "Already processed" : "Processed successfully")
                        : "Failed to process"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event in batch: {EventId} - {ErrorMessage}", eventDto.Id, ex.Message);
                results.Add(new EventProcessingResult
                {
                    EventId = eventDto.Id,
                    EventType = eventDto.Type,
                    Success = false,
                    AlreadyProcessed = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        return Ok(new ApiResponse<List<EventProcessingResult>>
        {
            Success = results.All(r => r.Success),
            Message = $"Processed {results.Count} events ({results.Count(r => r.Success)} successful)",
            Data = results
        });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> Health()
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Event API is healthy"
        });
    }
}

/// <summary>
/// Result of processing a single event
/// </summary>
public class EventProcessingResult
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool AlreadyProcessed { get; set; }
    public string Message { get; set; } = string.Empty;
}

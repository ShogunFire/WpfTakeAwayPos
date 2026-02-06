using RestaurantShared.DTOs;

namespace RestaurantApi.Services.EventHandlers;

/// <summary>
/// Interface for handling specific event types
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Determines if this handler can process the given event type
    /// </summary>
    bool CanHandle(string eventType);

    /// <summary>
    /// Processes the event
    /// </summary>
    Task HandleAsync(EventDto @event);
}

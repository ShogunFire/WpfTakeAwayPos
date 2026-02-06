namespace RestaurantApi.Data.Models;

/// <summary>
/// Tracks processed events to prevent duplicate processing
/// </summary>
public class ProcessedEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string Status { get; set; } = "Received";
    public string? Payload { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

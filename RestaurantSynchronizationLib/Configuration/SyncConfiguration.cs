namespace RestaurantSynchronizationLib.Configuration;

/// <summary>
/// Configuration for event synchronization
/// </summary>
public class SyncConfiguration
{
    /// <summary>
    /// The API base address (e.g., https://localhost:7000)
    /// </summary>
    public string ApiBaseAddress { get; set; } = string.Empty;

    /// <summary>
    /// The endpoint path for posting events (e.g., /api/events)
    /// </summary>
    public string EventsEndpoint { get; set; } = "/api/events";

    /// <summary>
    /// The endpoint path for batch posting events (e.g., /api/events/batch)
    /// </summary>
    public string BatchEndpoint { get; set; } = "/api/events/batch";

    /// <summary>
    /// Device identifier for tracking which device sent the event
    /// </summary>
    public string DeviceId { get; set; } = "pos-device";

    /// <summary>
    /// Connection string to the SQLite database
    /// </summary>
    public string DatabaseConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Timeout for API requests in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to use batch endpoint for sending multiple events at once
    /// </summary>
    public bool UseBatchEndpoint { get; set; } = true;

    /// <summary>
    /// Maximum number of events to send in a batch
    /// </summary>
    public int BatchSize { get; set; } = 10;
}

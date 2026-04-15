using System;
using System.Text.Json.Serialization;

namespace RestaurantShared.DTOs;

/// <summary>
/// Data transfer object for sending events between RestaurantPOS and RestaurantApi
/// </summary>
public class EventDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("locationId")]
    public Guid? LocationId { get; set; }
}

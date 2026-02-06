using System.Text.Json.Serialization;

namespace RestaurantSynchronizationLib.Models;

/// <summary>
/// Response from the RestaurantApi
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("alreadyProcessed")]
    public bool AlreadyProcessed { get; set; }
}

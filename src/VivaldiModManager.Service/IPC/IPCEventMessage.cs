using System.Text.Json.Serialization;

namespace VivaldiModManager.Service.IPC;

/// <summary>
/// Represents an IPC event message sent from service to subscribed clients.
/// </summary>
public class IPCEventMessage
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [JsonPropertyName("event")]
    public IPCEvent Event { get; set; }

    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

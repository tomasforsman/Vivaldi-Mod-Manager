using System.Text.Json.Serialization;

namespace VivaldiModManager.Service.IPC;

/// <summary>
/// Represents an IPC command message sent from client to service.
/// </summary>
public class IPCCommandMessage
{
    /// <summary>
    /// Gets or sets the command to execute.
    /// </summary>
    [JsonPropertyName("command")]
    public IPCCommand Command { get; set; }

    /// <summary>
    /// Gets or sets optional parameters for the command.
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the message ID for tracking request/response pairs.
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
}

using System.Text.Json.Serialization;

namespace VivaldiModManager.Service.IPC;

/// <summary>
/// Represents an IPC response message sent from service to client.
/// </summary>
public class IPCResponseMessage
{
    /// <summary>
    /// Gets or sets the message ID of the corresponding request.
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the command was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message if the command failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

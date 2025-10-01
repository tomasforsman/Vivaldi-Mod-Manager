using System.Text.Json.Serialization;

namespace VivaldiModManager.Service.Models;

/// <summary>
/// Represents an entry in the heal history.
/// </summary>
public class HealHistoryEntry
{
    /// <summary>
    /// Gets or sets the installation ID.
    /// </summary>
    [JsonPropertyName("installationId")]
    public string InstallationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the heal attempt.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the trigger reason for the heal.
    /// </summary>
    [JsonPropertyName("triggerReason")]
    public string TriggerReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the heal was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the heal failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the retry count for this heal attempt.
    /// </summary>
    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the duration of the heal attempt.
    /// </summary>
    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; set; }
}

namespace VivaldiModManager.Service.Models;

/// <summary>
/// Represents a heal request for an installation.
/// </summary>
public class HealRequest
{
    /// <summary>
    /// Gets or sets the installation ID.
    /// </summary>
    public string InstallationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trigger reason for the heal.
    /// </summary>
    public string TriggerReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the request was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the retry count for this request.
    /// </summary>
    public int RetryCount { get; set; }
}

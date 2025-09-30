using System.Text.Json.Serialization;

namespace VivaldiModManager.Service.Models;

/// <summary>
/// Represents the current status of file system monitoring and integrity checks.
/// </summary>
public class MonitoringStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether monitoring is enabled in the manifest.
    /// </summary>
    [JsonPropertyName("monitoringEnabled")]
    public bool MonitoringEnabled { get; set; }

    /// <summary>
    /// Gets or sets the count of active file system watchers.
    /// </summary>
    [JsonPropertyName("activeWatcherCount")]
    public int ActiveWatcherCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of file changes detected.
    /// </summary>
    [JsonPropertyName("totalFileChanges")]
    public long TotalFileChanges { get; set; }

    /// <summary>
    /// Gets or sets the total number of Vivaldi installation changes detected.
    /// </summary>
    [JsonPropertyName("totalVivaldiChanges")]
    public long TotalVivaldiChanges { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last detected change.
    /// </summary>
    [JsonPropertyName("lastChangeTime")]
    public DateTimeOffset? LastChangeTime { get; set; }

    /// <summary>
    /// Gets or sets the total number of integrity checks run.
    /// </summary>
    [JsonPropertyName("totalChecksRun")]
    public long TotalChecksRun { get; set; }

    /// <summary>
    /// Gets or sets the total number of integrity violations detected.
    /// </summary>
    [JsonPropertyName("totalViolationsDetected")]
    public long TotalViolationsDetected { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last integrity check.
    /// </summary>
    [JsonPropertyName("lastCheckTime")]
    public DateTimeOffset? LastCheckTime { get; set; }

    /// <summary>
    /// Gets or sets the count of installations currently with violations.
    /// </summary>
    [JsonPropertyName("installationsWithViolations")]
    public int InstallationsWithViolations { get; set; }
}

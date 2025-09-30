using System.Text.Json.Serialization;

namespace VivaldiModManager.Service.Models;

/// <summary>
/// Represents the current status of the service.
/// </summary>
public class ServiceStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether the service is running.
    /// </summary>
    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets the service start time.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the service uptime.
    /// </summary>
    [JsonPropertyName("uptime")]
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether monitoring is enabled.
    /// </summary>
    [JsonPropertyName("monitoringEnabled")]
    public bool MonitoringEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether auto-heal is enabled.
    /// </summary>
    [JsonPropertyName("autoHealEnabled")]
    public bool AutoHealEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether safe mode is active.
    /// </summary>
    [JsonPropertyName("safeModeActive")]
    public bool SafeModeActive { get; set; }

    /// <summary>
    /// Gets or sets the number of managed installations.
    /// </summary>
    [JsonPropertyName("managedInstallations")]
    public int ManagedInstallations { get; set; }

    /// <summary>
    /// Gets or sets the last operation performed.
    /// </summary>
    [JsonPropertyName("lastOperation")]
    public string? LastOperation { get; set; }

    /// <summary>
    /// Gets or sets the time of the last operation.
    /// </summary>
    [JsonPropertyName("lastOperationTime")]
    public DateTimeOffset? LastOperationTime { get; set; }

    /// <summary>
    /// Gets or sets the total number of heal attempts.
    /// </summary>
    [JsonPropertyName("totalHealsAttempted")]
    public int TotalHealsAttempted { get; set; }

    /// <summary>
    /// Gets or sets the total number of successful heals.
    /// </summary>
    [JsonPropertyName("totalHealsSucceeded")]
    public int TotalHealsSucceeded { get; set; }

    /// <summary>
    /// Gets or sets the total number of failed heals.
    /// </summary>
    [JsonPropertyName("totalHealsFailed")]
    public int TotalHealsFailed { get; set; }
}

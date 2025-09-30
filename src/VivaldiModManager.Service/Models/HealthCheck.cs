using System.Text.Json.Serialization;

namespace VivaldiModManager.Service.Models;

/// <summary>
/// Represents the health check status of the service.
/// </summary>
public class HealthCheck
{
    /// <summary>
    /// Gets or sets a value indicating whether the service is running.
    /// </summary>
    [JsonPropertyName("serviceRunning")]
    public bool ServiceRunning { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the manifest is loaded.
    /// </summary>
    [JsonPropertyName("manifestLoaded")]
    public bool ManifestLoaded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the IPC server is running.
    /// </summary>
    [JsonPropertyName("ipcServerRunning")]
    public bool IPCServerRunning { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether monitoring is active.
    /// </summary>
    [JsonPropertyName("monitoringActive")]
    public bool MonitoringActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether integrity check is active.
    /// </summary>
    [JsonPropertyName("integrityCheckActive")]
    public bool IntegrityCheckActive { get; set; }

    /// <summary>
    /// Gets or sets the time of the last health check.
    /// </summary>
    [JsonPropertyName("lastHealthCheckTime")]
    public DateTimeOffset LastHealthCheckTime { get; set; }

    /// <summary>
    /// Gets or sets the list of current error conditions.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new List<string>();
}

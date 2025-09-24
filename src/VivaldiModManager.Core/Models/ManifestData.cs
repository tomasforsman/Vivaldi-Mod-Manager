using System.Text.Json.Serialization;

namespace VivaldiModManager.Core.Models;

/// <summary>
/// Represents global settings for the mod manager.
/// </summary>
public class GlobalSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether auto-heal is enabled.
    /// </summary>
    [JsonPropertyName("autoHealEnabled")]
    public bool AutoHealEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether file system monitoring is enabled.
    /// </summary>
    [JsonPropertyName("monitoringEnabled")]
    public bool MonitoringEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days to retain backup files.
    /// </summary>
    [JsonPropertyName("backupRetentionDays")]
    public int BackupRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    [JsonPropertyName("logLevel")]
    public string LogLevel { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the path to the mods root directory.
    /// </summary>
    [JsonPropertyName("modsRootPath")]
    public string? ModsRootPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Safe Mode is currently active.
    /// </summary>
    [JsonPropertyName("safeModeActive")]
    public bool SafeModeActive { get; set; }
}

/// <summary>
/// Root manifest container including schema version for future migrations,
/// list of mods and global settings, metadata tracking and loader information.
/// </summary>
public class ManifestData
{
    /// <summary>
    /// Gets or sets the schema version for future migrations.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timestamp when the manifest was last updated.
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the global settings.
    /// </summary>
    [JsonPropertyName("settings")]
    public GlobalSettings Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of mods.
    /// </summary>
    [JsonPropertyName("mods")]
    public List<ModInfo> Mods { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of detected Vivaldi installations.
    /// </summary>
    [JsonPropertyName("installations")]
    public List<VivaldiInstallation> Installations { get; set; } = new();

    /// <summary>
    /// Gets or sets metadata about the manifest itself.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the version of the mod manager that created this manifest.
    /// </summary>
    [JsonPropertyName("createdByVersion")]
    public string? CreatedByVersion { get; set; }

    /// <summary>
    /// Gets or sets the version of the mod manager that last updated this manifest.
    /// </summary>
    [JsonPropertyName("lastUpdatedByVersion")]
    public string? LastUpdatedByVersion { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the manifest was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
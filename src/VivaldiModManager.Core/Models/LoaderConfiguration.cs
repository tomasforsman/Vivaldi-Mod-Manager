using System.Text.Json.Serialization;

namespace VivaldiModManager.Core.Models;

/// <summary>
/// Configuration for generating loader.js with enabled mods list, version tracking,
/// fingerprinting, and generation timestamps.
/// </summary>
public class LoaderConfiguration
{
    /// <summary>
    /// Gets or sets the list of enabled mods in their load order.
    /// </summary>
    [JsonPropertyName("enabledMods")]
    public List<string> EnabledMods { get; set; } = new();

    /// <summary>
    /// Gets or sets the version of the loader configuration.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the fingerprint used to verify loader integrity.
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the loader was generated.
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the loader was last updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the version of the mod manager that generated this loader.
    /// </summary>
    [JsonPropertyName("generatedByVersion")]
    public string? GeneratedByVersion { get; set; }

    /// <summary>
    /// Gets or sets the target Vivaldi version for this loader configuration.
    /// </summary>
    [JsonPropertyName("targetVivaldiVersion")]
    public string? TargetVivaldiVersion { get; set; }

    /// <summary>
    /// Gets or sets any additional configuration options.
    /// </summary>
    [JsonPropertyName("options")]
    public Dictionary<string, object> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the hash of the generated loader content for integrity checking.
    /// </summary>
    [JsonPropertyName("contentHash")]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a backup/rollback configuration.
    /// </summary>
    [JsonPropertyName("isBackup")]
    public bool IsBackup { get; set; }

    /// <summary>
    /// Gets or sets backup metadata if this is a backup configuration.
    /// </summary>
    [JsonPropertyName("backupMetadata")]
    public Dictionary<string, string>? BackupMetadata { get; set; }
}
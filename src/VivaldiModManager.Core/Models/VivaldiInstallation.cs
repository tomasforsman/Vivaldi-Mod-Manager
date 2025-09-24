using System.Text.Json.Serialization;

namespace VivaldiModManager.Core.Models;

/// <summary>
/// Represents the type of Vivaldi installation.
/// </summary>
public enum VivaldiInstallationType
{
    /// <summary>
    /// Standard installation (installed via installer).
    /// </summary>
    Standard,

    /// <summary>
    /// Portable installation.
    /// </summary>
    Portable,

    /// <summary>
    /// Snapshot/development build.
    /// </summary>
    Snapshot
}

/// <summary>
/// Represents a detected Vivaldi installation with installation paths, version info,
/// installation type, management state, and detection timestamps.
/// </summary>
public class VivaldiInstallation
{
    /// <summary>
    /// Gets or sets the unique identifier for this installation.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for this installation.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the Vivaldi installation directory.
    /// </summary>
    [JsonPropertyName("installationPath")]
    public string InstallationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the user data directory.
    /// </summary>
    [JsonPropertyName("userDataPath")]
    public string UserDataPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the application directory containing the executable.
    /// </summary>
    [JsonPropertyName("applicationPath")]
    public string ApplicationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of this Vivaldi installation.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of installation.
    /// </summary>
    [JsonPropertyName("installationType")]
    public VivaldiInstallationType InstallationType { get; set; } = VivaldiInstallationType.Standard;

    /// <summary>
    /// Gets or sets a value indicating whether this installation is currently managed by the mod manager.
    /// </summary>
    [JsonPropertyName("isManaged")]
    public bool IsManaged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this installation is currently active/selected.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this installation was first detected.
    /// </summary>
    [JsonPropertyName("detectedAt")]
    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this installation was last verified.
    /// </summary>
    [JsonPropertyName("lastVerifiedAt")]
    public DateTimeOffset LastVerifiedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when injection was last performed on this installation.
    /// </summary>
    [JsonPropertyName("lastInjectionAt")]
    public DateTimeOffset? LastInjectionAt { get; set; }

    /// <summary>
    /// Gets or sets the last known injection status.
    /// </summary>
    [JsonPropertyName("lastInjectionStatus")]
    public string? LastInjectionStatus { get; set; }

    /// <summary>
    /// Gets or sets the injection fingerprint to detect tampering.
    /// </summary>
    [JsonPropertyName("injectionFingerprint")]
    public string? InjectionFingerprint { get; set; }

    /// <summary>
    /// Gets or sets any additional metadata about this installation.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}
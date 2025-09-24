using System.Text.Json.Serialization;

namespace VivaldiModManager.Core.Models;

/// <summary>
/// Represents a single mod with metadata including unique identifier, file information,
/// enable/disable state, load order, user notes, compatibility tracking, and checksum verification.
/// </summary>
public class ModInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for this mod.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original filename of the mod.
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this mod is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the load order of this mod. Lower values are loaded first.
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets user notes about this mod.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA256 checksum of the mod file for integrity verification.
    /// </summary>
    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the mod file was last modified.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Gets or sets the version of the mod, if available.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL scopes where this mod should be active.
    /// Empty list means the mod is active on all pages.
    /// </summary>
    [JsonPropertyName("urlScopes")]
    public List<string> UrlScopes { get; set; } = new();

    /// <summary>
    /// Gets or sets the last known compatible Vivaldi version.
    /// </summary>
    [JsonPropertyName("lastKnownCompatibleVivaldi")]
    public string? LastKnownCompatibleVivaldi { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this mod info was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this mod info was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this mod has been validated.
    /// </summary>
    [JsonPropertyName("isValidated")]  
    public bool IsValidated { get; set; }
}
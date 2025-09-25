using System.Text.Json.Serialization;

namespace VivaldiModManager.Core.Models;

/// <summary>
/// Represents the status of injection for a Vivaldi installation.
/// </summary>
public class InjectionStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether the injection is present and valid.
    /// </summary>
    [JsonPropertyName("isInjected")]
    public bool IsInjected { get; set; }

    /// <summary>
    /// Gets or sets the fingerprint of the current injection.
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public string? Fingerprint { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the injection was last performed.
    /// </summary>
    [JsonPropertyName("lastInjectionAt")]
    public DateTimeOffset? LastInjectionAt { get; set; }

    /// <summary>
    /// Gets or sets the number of target files that have been successfully injected.
    /// </summary>
    [JsonPropertyName("injectedTargetCount")]
    public int InjectedTargetCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of target files found.
    /// </summary>
    [JsonPropertyName("totalTargetCount")]
    public int TotalTargetCount { get; set; }

    /// <summary>
    /// Gets or sets the validation status of the injection.
    /// </summary>
    [JsonPropertyName("validationStatus")]
    public InjectionValidationStatus ValidationStatus { get; set; }

    /// <summary>
    /// Gets or sets any validation errors that were detected.
    /// </summary>
    [JsonPropertyName("validationErrors")]
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets the target files and their injection status.
    /// </summary>
    [JsonPropertyName("targetFiles")]
    public Dictionary<string, InjectionTargetStatus> TargetFiles { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether the injection is fully intact (all targets injected and valid).
    /// </summary>
    [JsonIgnore]
    public bool IsFullyIntact => IsInjected && 
                                InjectedTargetCount == TotalTargetCount && 
                                ValidationStatus == InjectionValidationStatus.Valid;

    /// <summary>
    /// Gets a value indicating whether the injection needs repair (partial injection or validation issues).
    /// </summary>
    [JsonIgnore]
    public bool NeedsRepair => !IsFullyIntact && 
                              (InjectedTargetCount > 0 || ValidationStatus != InjectionValidationStatus.NotInjected);
}

/// <summary>
/// Represents the validation status of an injection.
/// </summary>
public enum InjectionValidationStatus
{
    /// <summary>
    /// No injection detected.
    /// </summary>
    NotInjected,

    /// <summary>
    /// Injection is present and valid.
    /// </summary>
    Valid,

    /// <summary>
    /// Injection is present but has validation errors.
    /// </summary>
    Invalid,

    /// <summary>
    /// Injection is partially present (some targets missing).
    /// </summary>
    Partial,

    /// <summary>
    /// Injection fingerprint doesn't match expected value.
    /// </summary>
    FingerprintMismatch,

    /// <summary>
    /// Unable to validate injection (file access errors, etc.).
    /// </summary>
    ValidationFailed
}

/// <summary>
/// Represents the injection status for a specific target file.
/// </summary>
public class InjectionTargetStatus
{
    /// <summary>
    /// Gets or sets the file path of the target.
    /// </summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this target is injected.
    /// </summary>
    [JsonPropertyName("isInjected")]
    public bool IsInjected { get; set; }

    /// <summary>
    /// Gets or sets the fingerprint found in this target.
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public string? Fingerprint { get; set; }

    /// <summary>
    /// Gets or sets the validation status for this target.
    /// </summary>
    [JsonPropertyName("validationStatus")]
    public InjectionValidationStatus ValidationStatus { get; set; }

    /// <summary>
    /// Gets or sets any validation errors specific to this target.
    /// </summary>
    [JsonPropertyName("validationErrors")]
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether a backup exists for this target.
    /// </summary>
    [JsonPropertyName("hasBackup")]
    public bool HasBackup { get; set; }
}
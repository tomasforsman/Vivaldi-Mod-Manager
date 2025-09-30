namespace VivaldiModManager.Service.IPC;

/// <summary>
/// Defines the available IPC event types.
/// </summary>
public enum IPCEvent
{
    /// <summary>
    /// Injection completed successfully.
    /// </summary>
    InjectionCompleted,

    /// <summary>
    /// Injection failed.
    /// </summary>
    InjectionFailed,

    /// <summary>
    /// Integrity violation detected.
    /// </summary>
    IntegrityViolation,

    /// <summary>
    /// Vivaldi update detected.
    /// </summary>
    VivaldiUpdateDetected,

    /// <summary>
    /// Safe mode state changed.
    /// </summary>
    SafeModeChanged,

    /// <summary>
    /// Monitoring state changed.
    /// </summary>
    MonitoringStateChanged,

    /// <summary>
    /// Service health status changed.
    /// </summary>
    ServiceHealthChanged,

    /// <summary>
    /// Manifest updated.
    /// </summary>
    ManifestUpdated
}

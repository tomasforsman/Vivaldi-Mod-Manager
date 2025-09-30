namespace VivaldiModManager.Service.IPC;

/// <summary>
/// Defines the available IPC commands.
/// </summary>
public enum IPCCommand
{
    /// <summary>
    /// Get the current service status.
    /// </summary>
    GetServiceStatus,

    /// <summary>
    /// Get detailed health check information.
    /// </summary>
    GetHealthCheck,

    /// <summary>
    /// Trigger auto-heal operation (placeholder for issue #38).
    /// </summary>
    TriggerAutoHeal,

    /// <summary>
    /// Enable safe mode (placeholder for issue #38).
    /// </summary>
    EnableSafeMode,

    /// <summary>
    /// Disable safe mode (placeholder for issue #38).
    /// </summary>
    DisableSafeMode,

    /// <summary>
    /// Reload the manifest (placeholder for issue #37).
    /// </summary>
    ReloadManifest,

    /// <summary>
    /// Pause monitoring (placeholder for issue #37).
    /// </summary>
    PauseMonitoring,

    /// <summary>
    /// Resume monitoring (placeholder for issue #37).
    /// </summary>
    ResumeMonitoring,

    /// <summary>
    /// Get monitoring status (placeholder for issue #37).
    /// </summary>
    GetMonitoringStatus
}

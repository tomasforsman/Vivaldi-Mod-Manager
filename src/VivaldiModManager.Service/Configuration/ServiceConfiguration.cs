using Microsoft.Extensions.Configuration;

namespace VivaldiModManager.Service.Configuration;

/// <summary>
/// Provides service configuration settings with environment variable expansion support.
/// </summary>
public class ServiceConfiguration
{
    /// <summary>
    /// Gets or sets the path to the manifest file.
    /// </summary>
    public string ManifestPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory path for log files.
    /// </summary>
    public string LogDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the named pipe name for IPC communication.
    /// </summary>
    public string IPCPipeName { get; set; } = "VivaldiModManagerPipe";

    /// <summary>
    /// Gets or sets the IPC timeout in seconds.
    /// </summary>
    public int IPCTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of concurrent operations.
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 5;

    /// <summary>
    /// Gets or sets the service startup timeout in seconds.
    /// </summary>
    public int ServiceStartupTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the monitoring debounce time in milliseconds.
    /// </summary>
    public int MonitoringDebounceMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the integrity check interval in seconds.
    /// </summary>
    public int IntegrityCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether integrity checks should be staggered.
    /// </summary>
    public bool IntegrityCheckStaggeringEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of consecutive failures before escalating alerts.
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = 5;

    /// <summary>
    /// Gets or sets the retry delays for auto-heal in seconds.
    /// </summary>
    public int[] AutoHealRetryDelays { get; set; } = new[] { 5, 30, 120 };

    /// <summary>
    /// Gets or sets the maximum number of auto-heal retries.
    /// </summary>
    public int AutoHealMaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the cooldown period between heal attempts for the same installation in seconds.
    /// </summary>
    public int AutoHealCooldownSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum wait time for Vivaldi folder stabilization in seconds.
    /// </summary>
    public int VivaldiFolderStabilizationMaxWaitSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of heal history entries to retain.
    /// </summary>
    public int HealHistoryMaxEntries { get; set; } = 50;

    /// <summary>
    /// Gets or sets the path to the heal history file.
    /// </summary>
    public string HealHistoryFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Loads configuration from the provided IConfiguration and expands environment variables.
    /// </summary>
    /// <param name="configuration">The configuration source.</param>
    /// <returns>A ServiceConfiguration instance with expanded paths.</returns>
    public static ServiceConfiguration LoadFromConfiguration(IConfiguration configuration)
    {
        var config = new ServiceConfiguration();
        configuration.GetSection("ServiceConfiguration").Bind(config);

        // Expand environment variables in paths
        config.ManifestPath = Environment.ExpandEnvironmentVariables(config.ManifestPath);
        config.LogDirectory = Environment.ExpandEnvironmentVariables(config.LogDirectory);
        config.HealHistoryFilePath = Environment.ExpandEnvironmentVariables(config.HealHistoryFilePath);

        // Validate monitoring settings
        if (config.MonitoringDebounceMs < 0)
        {
            config.MonitoringDebounceMs = 2000;
        }

        if (config.IntegrityCheckIntervalSeconds < 1)
        {
            config.IntegrityCheckIntervalSeconds = 60;
        }

        if (config.MaxConsecutiveFailures < 1)
        {
            config.MaxConsecutiveFailures = 5;
        }

        // Validate auto-heal settings
        if (config.AutoHealMaxRetries < 1)
        {
            config.AutoHealMaxRetries = 3;
        }

        if (config.AutoHealCooldownSeconds < 0)
        {
            config.AutoHealCooldownSeconds = 30;
        }

        if (config.VivaldiFolderStabilizationMaxWaitSeconds < 1)
        {
            config.VivaldiFolderStabilizationMaxWaitSeconds = 30;
        }

        if (config.HealHistoryMaxEntries < 1)
        {
            config.HealHistoryMaxEntries = 50;
        }

        if (config.AutoHealRetryDelays == null || config.AutoHealRetryDelays.Length == 0)
        {
            config.AutoHealRetryDelays = new[] { 5, 30, 120 };
        }

        return config;
    }
}

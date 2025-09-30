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

        return config;
    }
}

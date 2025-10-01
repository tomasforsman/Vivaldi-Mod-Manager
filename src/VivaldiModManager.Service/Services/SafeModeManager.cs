using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.Configuration;

namespace VivaldiModManager.Service.Services;

/// <summary>
/// Manages Safe Mode operations for the service.
/// </summary>
public class SafeModeManager
{
    private readonly ILogger<SafeModeManager> _logger;
    private readonly ServiceConfiguration _config;
    private readonly IManifestService _manifestService;
    private readonly IInjectionService _injectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeModeManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="config">The service configuration.</param>
    /// <param name="manifestService">The manifest service.</param>
    /// <param name="injectionService">The injection service.</param>
    public SafeModeManager(
        ILogger<SafeModeManager> logger,
        ServiceConfiguration config,
        IManifestService manifestService,
        IInjectionService injectionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _injectionService = injectionService ?? throw new ArgumentNullException(nameof(injectionService));
    }

    /// <summary>
    /// Activates Safe Mode by disabling all injections and setting the flag.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of installations processed.</returns>
    public async Task<int> ActivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating Safe Mode");

        try
        {
            // Load manifest
            if (!_manifestService.ManifestExists(_config.ManifestPath))
            {
                _logger.LogWarning("Manifest not found, cannot activate Safe Mode");
                return 0;
            }

            var manifest = await _manifestService.LoadManifestAsync(_config.ManifestPath, cancellationToken);

            // Set Safe Mode flag
            manifest.Settings.SafeModeActive = true;

            // Remove injections from all installations
            int processedCount = 0;
            foreach (var installation in manifest.Installations)
            {
                try
                {
                    _logger.LogInformation("Removing injection for {InstallationName} (Safe Mode)", installation.Name);
                    await _injectionService.RemoveInjectionAsync(installation, cancellationToken);
                    installation.LastInjectionStatus = "Safe Mode - Disabled";
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to remove injection for {InstallationName}", installation.Name);
                    installation.LastInjectionStatus = $"Safe Mode - Failed: {ex.Message}";
                }
            }

            // Save manifest
            await _manifestService.SaveManifestAsync(manifest, _config.ManifestPath, cancellationToken);

            _logger.LogInformation("Safe Mode activated, processed {Count} installations", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate Safe Mode");
            throw;
        }
    }

    /// <summary>
    /// Deactivates Safe Mode by clearing the flag. Healing will be triggered by the caller.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of installations that need healing.</returns>
    public async Task<int> DeactivateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating Safe Mode");

        try
        {
            // Load manifest
            if (!_manifestService.ManifestExists(_config.ManifestPath))
            {
                _logger.LogWarning("Manifest not found, cannot deactivate Safe Mode");
                return 0;
            }

            var manifest = await _manifestService.LoadManifestAsync(_config.ManifestPath, cancellationToken);

            // Clear Safe Mode flag
            manifest.Settings.SafeModeActive = false;

            // Save manifest
            await _manifestService.SaveManifestAsync(manifest, _config.ManifestPath, cancellationToken);

            int installationCount = manifest.Installations.Count;
            _logger.LogInformation("Safe Mode deactivated, {Count} installations need healing", installationCount);
            return installationCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate Safe Mode");
            throw;
        }
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.Configuration;

namespace VivaldiModManager.Service.BackgroundServices;

/// <summary>
/// Event arguments for integrity violation events.
/// </summary>
public class IntegrityViolationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the installation with the violation.
    /// </summary>
    public VivaldiInstallation Installation { get; init; } = null!;

    /// <summary>
    /// Gets the list of violation descriptions.
    /// </summary>
    public List<string> Violations { get; init; } = new();

    /// <summary>
    /// Gets the number of consecutive failures for this installation.
    /// </summary>
    public int ConsecutiveFailures { get; init; }

    /// <summary>
    /// Gets the timestamp of the violation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Background service that performs periodic integrity checks on Vivaldi installations.
/// </summary>
public class IntegrityCheckService : IHostedService, IDisposable
{
    private readonly ILogger<IntegrityCheckService> _logger;
    private readonly ServiceConfiguration _config;
    private readonly IManifestService _manifestService;
    private readonly IVivaldiService _vivaldiService;
    private readonly Dictionary<string, int> _consecutiveFailures;
    private readonly object _statsLock = new object();
    private Timer? _checkTimer;
    private bool _disposed;
    private ManifestData? _manifest;

    /// <summary>
    /// Event raised when an integrity violation is detected.
    /// </summary>
    public event EventHandler<IntegrityViolationEventArgs>? IntegrityViolation;

    /// <summary>
    /// Gets the total number of integrity checks run.
    /// </summary>
    public long TotalChecksRun { get; private set; }

    /// <summary>
    /// Gets the total number of violations detected.
    /// </summary>
    public long TotalViolationsDetected { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last check.
    /// </summary>
    public DateTimeOffset? LastCheckTime { get; private set; }

    /// <summary>
    /// Gets the count of installations currently with violations.
    /// </summary>
    public int InstallationsWithViolations => _consecutiveFailures.Count(kvp => kvp.Value > 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrityCheckService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="config">The service configuration.</param>
    /// <param name="manifestService">The manifest service.</param>
    /// <param name="vivaldiService">The Vivaldi service.</param>
    public IntegrityCheckService(
        ILogger<IntegrityCheckService> logger,
        ServiceConfiguration config,
        IManifestService manifestService,
        IVivaldiService vivaldiService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _vivaldiService = vivaldiService ?? throw new ArgumentNullException(nameof(vivaldiService));
        _consecutiveFailures = new Dictionary<string, int>();
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Integrity Check Service");

        try
        {
            // Load manifest
            if (!_manifestService.ManifestExists(_config.ManifestPath))
            {
                _logger.LogWarning("Manifest not found at {ManifestPath}, integrity checks will not start", 
                    _config.ManifestPath);
                return;
            }

            _manifest = await _manifestService.LoadManifestAsync(_config.ManifestPath, cancellationToken);

            // Check if monitoring is enabled (integrity checks are part of monitoring)
            if (!_manifest.Settings.MonitoringEnabled)
            {
                _logger.LogWarning("Monitoring is disabled in manifest settings, integrity checks will not start");
                return;
            }

            // Check if auto-heal is enabled
            if (!_manifest.Settings.AutoHealEnabled)
            {
                _logger.LogWarning("Auto-heal is disabled in manifest settings, integrity checks will not start");
                return;
            }

            // Get installations and configure staggered checks
            var installations = await _vivaldiService.DetectInstallationsAsync(cancellationToken);
            
            if (_config.IntegrityCheckStaggeringEnabled && installations.Count >= 3)
            {
                var staggerDelayMs = (_config.IntegrityCheckIntervalSeconds * 1000) / installations.Count;
                _logger.LogInformation(
                    "Integrity checks will be staggered: {Count} installations, {Delay}ms between checks",
                    installations.Count, staggerDelayMs);
            }

            // Start periodic checks
            var intervalMs = _config.IntegrityCheckIntervalSeconds * 1000;
            _checkTimer = new Timer(async _ => await PerformIntegrityChecksAsync(), null, 
                TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(intervalMs));

            _logger.LogInformation("Integrity Check Service started with {IntervalSeconds}s interval", 
                _config.IntegrityCheckIntervalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Integrity Check Service");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Integrity Check Service");

        _checkTimer?.Change(Timeout.Infinite, Timeout.Infinite);

        _logger.LogInformation("Integrity Check Service stopped");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _checkTimer?.Dispose();
        _disposed = true;
    }

    private async Task PerformIntegrityChecksAsync()
    {
        try
        {
            // Reload manifest to check current settings
            if (!_manifestService.ManifestExists(_config.ManifestPath))
            {
                return;
            }

            _manifest = await _manifestService.LoadManifestAsync(_config.ManifestPath, CancellationToken.None);

            // Skip if safe mode is active
            if (_manifest.Settings.SafeModeActive)
            {
                _logger.LogDebug("Skipping integrity checks: Safe mode is active");
                return;
            }

            // Skip if auto-heal is disabled
            if (!_manifest.Settings.AutoHealEnabled)
            {
                _logger.LogDebug("Skipping integrity checks: Auto-heal is disabled");
                return;
            }

            // Get installations
            var installations = await _vivaldiService.DetectInstallationsAsync(CancellationToken.None);

            if (installations.Count == 0)
            {
                _logger.LogDebug("No Vivaldi installations found for integrity checks");
                return;
            }

            // Calculate stagger delay
            int staggerDelayMs = 0;
            if (_config.IntegrityCheckStaggeringEnabled && installations.Count >= 3)
            {
                staggerDelayMs = (_config.IntegrityCheckIntervalSeconds * 1000) / installations.Count;
            }

            // Check each installation
            for (int i = 0; i < installations.Count; i++)
            {
                if (i > 0 && staggerDelayMs > 0)
                {
                    await Task.Delay(staggerDelayMs);
                }

                await CheckInstallationIntegrityAsync(installations[i]);
            }

            lock (_statsLock)
            {
                LastCheckTime = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing integrity checks");
        }
    }

    private async Task CheckInstallationIntegrityAsync(VivaldiInstallation installation)
    {
        try
        {
            lock (_statsLock)
            {
                TotalChecksRun++;
            }

            _logger.LogTrace("Checking integrity for installation {Id}", installation.Id);

            var violations = new List<string>();

            // Get injection targets
            IReadOnlyDictionary<string, string> targets;
            try
            {
                targets = await _vivaldiService.FindInjectionTargetsAsync(installation, CancellationToken.None);
            }
            catch (Exception ex)
            {
                violations.Add($"Failed to find injection targets: {ex.Message}");
                await RecordViolationAsync(installation, violations);
                return;
            }

            // Check each target file for injection stub
            foreach (var target in targets)
            {
                try
                {
                    if (!File.Exists(target.Value))
                    {
                        violations.Add($"Target file not found: {target.Key}");
                        continue;
                    }

                    var content = await File.ReadAllTextAsync(target.Value);

                    // Check for injection stub
                    if (!content.Contains("Vivaldi Mod Manager - Injection Stub"))
                    {
                        violations.Add($"Injection stub missing in {target.Key}");
                        continue;
                    }

                    // Check fingerprint if available
                    if (!string.IsNullOrWhiteSpace(installation.InjectionFingerprint))
                    {
                        if (!content.Contains(installation.InjectionFingerprint))
                        {
                            violations.Add($"Fingerprint mismatch in {target.Key}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    violations.Add($"Error checking {target.Key}: {ex.Message}");
                }
            }

            // Check loader.js exists
            if (!string.IsNullOrWhiteSpace(_manifest?.Settings.ModsRootPath))
            {
                var loaderPath = Path.Combine(_manifest.Settings.ModsRootPath, "loader.js");
                if (!File.Exists(loaderPath))
                {
                    violations.Add("loader.js not found in vivaldi-mods folder");
                }
            }

            // Check enabled mods exist
            if (_manifest != null && !string.IsNullOrWhiteSpace(_manifest.Settings.ModsRootPath))
            {
                var modsPath = Path.Combine(_manifest.Settings.ModsRootPath, "mods");
                var enabledMods = _manifest.Mods.Where(m => m.Enabled).ToList();

                foreach (var mod in enabledMods)
                {
                    if (!string.IsNullOrWhiteSpace(mod.Filename))
                    {
                        var modPath = Path.Combine(modsPath, mod.Filename);
                        if (!File.Exists(modPath))
                        {
                            violations.Add($"Enabled mod file not found: {mod.Filename}");
                        }
                    }
                }
            }

            // Record result
            if (violations.Count > 0)
            {
                await RecordViolationAsync(installation, violations);
            }
            else
            {
                RecordSuccess(installation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking integrity for installation {Id}", installation.Id);
        }
    }

    private async Task RecordViolationAsync(VivaldiInstallation installation, List<string> violations)
    {
        // Track consecutive failures
        if (!_consecutiveFailures.ContainsKey(installation.Id))
        {
            _consecutiveFailures[installation.Id] = 0;
        }

        _consecutiveFailures[installation.Id]++;
        var failureCount = _consecutiveFailures[installation.Id];

        lock (_statsLock)
        {
            TotalViolationsDetected++;
        }

        // Log at appropriate level
        var violationsText = string.Join(", ", violations);
        if (failureCount == 1)
        {
            _logger.LogWarning("Integrity violation detected for installation {Id}: {Violations}", 
                installation.Id, violationsText);
        }
        else if (failureCount <= 3)
        {
            _logger.LogWarning("Integrity violation detected for installation {Id} (consecutive: {Count}): {Violations}", 
                installation.Id, failureCount, violationsText);
        }
        else
        {
            _logger.LogError("Integrity violation detected for installation {Id} (consecutive: {Count}): {Violations}", 
                installation.Id, failureCount, violationsText);
        }

        // Raise event
        IntegrityViolation?.Invoke(this, new IntegrityViolationEventArgs
        {
            Installation = installation,
            Violations = violations,
            ConsecutiveFailures = failureCount,
            Timestamp = DateTimeOffset.UtcNow
        });

        await Task.CompletedTask;
    }

    private void RecordSuccess(VivaldiInstallation installation)
    {
        // Reset consecutive failure count
        if (_consecutiveFailures.ContainsKey(installation.Id))
        {
            var previousFailures = _consecutiveFailures[installation.Id];
            _consecutiveFailures[installation.Id] = 0;

            if (previousFailures > 0)
            {
                _logger.LogInformation("Integrity check passed for installation {Id} after {Count} consecutive failures", 
                    installation.Id, previousFailures);
            }
        }

        _logger.LogTrace("Integrity check passed for installation {Id}", installation.Id);
    }
}

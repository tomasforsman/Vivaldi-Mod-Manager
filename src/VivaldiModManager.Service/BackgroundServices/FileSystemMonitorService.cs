using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.Configuration;

namespace VivaldiModManager.Service.BackgroundServices;

/// <summary>
/// Event arguments for file change events.
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the changed file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp of the change.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Event arguments for Vivaldi installation change events.
/// </summary>
public class VivaldiChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the changed file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp of the change.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the installation ID.
    /// </summary>
    public string InstallationId { get; init; } = string.Empty;
}

/// <summary>
/// Background service that monitors file system changes to mods and Vivaldi installations.
/// </summary>
public class FileSystemMonitorService : IHostedService, IDisposable
{
    private readonly ILogger<FileSystemMonitorService> _logger;
    private readonly ServiceConfiguration _config;
    private readonly IManifestService _manifestService;
    private readonly IVivaldiService _vivaldiService;
    private readonly List<FileSystemWatcher> _watchers;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pendingChanges;
    private readonly Timer? _debounceTimer;
    private readonly object _statsLock = new object();
    private bool _isPaused;
    private bool _disposed;
    private ManifestData? _manifest;

    /// <summary>
    /// Event raised when a file change is detected in the mods directory.
    /// </summary>
    public event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Event raised when a file change is detected in a Vivaldi installation.
    /// </summary>
    public event EventHandler<VivaldiChangedEventArgs>? VivaldiChanged;

    /// <summary>
    /// Gets the total number of file changes detected.
    /// </summary>
    public long TotalFileChanges { get; private set; }

    /// <summary>
    /// Gets the total number of Vivaldi installation changes detected.
    /// </summary>
    public long TotalVivaldiChanges { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last change detected.
    /// </summary>
    public DateTimeOffset? LastChangeTime { get; private set; }

    /// <summary>
    /// Gets the count of active watchers.
    /// </summary>
    public int ActiveWatcherCount => _watchers.Count(w => w.EnableRaisingEvents);

    /// <summary>
    /// Gets a value indicating whether monitoring is paused.
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemMonitorService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="config">The service configuration.</param>
    /// <param name="manifestService">The manifest service.</param>
    /// <param name="vivaldiService">The Vivaldi service.</param>
    public FileSystemMonitorService(
        ILogger<FileSystemMonitorService> logger,
        ServiceConfiguration config,
        IManifestService manifestService,
        IVivaldiService vivaldiService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _vivaldiService = vivaldiService ?? throw new ArgumentNullException(nameof(vivaldiService));
        _watchers = new List<FileSystemWatcher>();
        _pendingChanges = new ConcurrentDictionary<string, DateTimeOffset>();
        _debounceTimer = new Timer(ProcessPendingChanges, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting File System Monitor Service");

        try
        {
            // Load manifest
            if (!_manifestService.ManifestExists(_config.ManifestPath))
            {
                _logger.LogWarning("Manifest not found at {ManifestPath}, monitoring will not start", _config.ManifestPath);
                return;
            }

            _manifest = await _manifestService.LoadManifestAsync(_config.ManifestPath, cancellationToken);

            // Check if monitoring is enabled
            if (!_manifest.Settings.MonitoringEnabled)
            {
                _logger.LogWarning("File system monitoring is disabled in manifest settings");
                return;
            }

            // Start monitoring
            await InitializeWatchersAsync(cancellationToken);

            _logger.LogInformation("File System Monitor Service started with {WatcherCount} watchers", _watchers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting File System Monitor Service");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping File System Monitor Service");

        StopAllWatchers();

        _logger.LogInformation("File System Monitor Service stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pauses file system monitoring.
    /// </summary>
    public void PauseMonitoring()
    {
        if (_isPaused)
        {
            return;
        }

        _logger.LogInformation("Pausing file system monitoring");
        _isPaused = true;
        StopAllWatchers();
    }

    /// <summary>
    /// Resumes file system monitoring.
    /// </summary>
    public async Task ResumeMonitoringAsync()
    {
        if (!_isPaused)
        {
            return;
        }

        _logger.LogInformation("Resuming file system monitoring");
        _isPaused = false;

        try
        {
            await InitializeWatchersAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming file system monitoring");
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _debounceTimer?.Dispose();

        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
        _disposed = true;
    }

    private async Task InitializeWatchersAsync(CancellationToken cancellationToken)
    {
        // Clear existing watchers
        StopAllWatchers();

        if (_manifest == null)
        {
            return;
        }

        // Watch ModsRootPath
        if (!string.IsNullOrWhiteSpace(_manifest.Settings.ModsRootPath))
        {
            CreateModsWatcher(_manifest.Settings.ModsRootPath);
        }
        else
        {
            _logger.LogWarning("ModsRootPath is not set in manifest, skipping mods directory monitoring");
        }

        // Watch Vivaldi installations
        try
        {
            var installations = await _vivaldiService.DetectInstallationsAsync(cancellationToken);
            foreach (var installation in installations)
            {
                CreateVivaldiWatcher(installation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting Vivaldi installations for monitoring");
        }

        // Start debounce timer
        _debounceTimer?.Change(_config.MonitoringDebounceMs, _config.MonitoringDebounceMs);
    }

    private void CreateModsWatcher(string modsPath)
    {
        try
        {
            if (!Directory.Exists(modsPath))
            {
                _logger.LogWarning("Mods directory does not exist: {ModsPath}", modsPath);
                return;
            }

            var watcher = new FileSystemWatcher(modsPath, "*.js")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            watcher.Changed += OnModsFileChanged;
            watcher.Created += OnModsFileChanged;
            watcher.Deleted += OnModsFileChanged;
            watcher.Renamed += OnModsFileRenamed;
            watcher.Error += OnWatcherError;

            _watchers.Add(watcher);
            _logger.LogDebug("Created file system watcher for mods directory: {ModsPath}", modsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file system watcher for mods directory: {ModsPath}", modsPath);
        }
    }

    private void CreateVivaldiWatcher(VivaldiInstallation installation)
    {
        try
        {
            var resourcesPath = Path.Combine(installation.InstallationPath, "Application", "resources", "vivaldi");
            if (!Directory.Exists(resourcesPath))
            {
                _logger.LogWarning("Vivaldi resources directory does not exist: {ResourcesPath}", resourcesPath);
                return;
            }

            var watcher = new FileSystemWatcher(resourcesPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            watcher.Changed += (s, e) => OnVivaldiFileChanged(s, e, installation.Id);
            watcher.Created += (s, e) => OnVivaldiFileChanged(s, e, installation.Id);
            watcher.Deleted += (s, e) => OnVivaldiFileChanged(s, e, installation.Id);
            watcher.Renamed += (s, e) => OnVivaldiFileRenamed(s, e, installation.Id);
            watcher.Error += OnWatcherError;

            _watchers.Add(watcher);
            _logger.LogDebug("Created file system watcher for Vivaldi installation {Id}: {Path}", 
                installation.Id, resourcesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file system watcher for Vivaldi installation {Id}", 
                installation.Id);
        }
    }

    private void OnModsFileChanged(object sender, FileSystemEventArgs e)
    {
        if (IsTemporaryFile(e.Name ?? string.Empty))
        {
            return;
        }

        _logger.LogTrace("Mods file change detected: {ChangeType} - {FilePath}", e.ChangeType, e.FullPath);
        _pendingChanges[e.FullPath] = DateTimeOffset.UtcNow;
    }

    private void OnModsFileRenamed(object sender, RenamedEventArgs e)
    {
        if (IsTemporaryFile(e.Name ?? string.Empty))
        {
            return;
        }

        _logger.LogTrace("Mods file renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
        _pendingChanges[e.FullPath] = DateTimeOffset.UtcNow;
    }

    private void OnVivaldiFileChanged(object sender, FileSystemEventArgs e, string installationId)
    {
        if (IsTemporaryFile(e.Name ?? string.Empty))
        {
            return;
        }

        _logger.LogTrace("Vivaldi file change detected for {InstallationId}: {ChangeType} - {FilePath}", 
            installationId, e.ChangeType, e.FullPath);
        _pendingChanges[$"{installationId}:{e.FullPath}"] = DateTimeOffset.UtcNow;
    }

    private void OnVivaldiFileRenamed(object sender, RenamedEventArgs e, string installationId)
    {
        if (IsTemporaryFile(e.Name ?? string.Empty))
        {
            return;
        }

        _logger.LogTrace("Vivaldi file renamed for {InstallationId}: {OldPath} -> {NewPath}", 
            installationId, e.OldFullPath, e.FullPath);
        _pendingChanges[$"{installationId}:{e.FullPath}"] = DateTimeOffset.UtcNow;
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _logger.LogError(exception, "File system watcher error occurred");
    }

    private void ProcessPendingChanges(object? state)
    {
        if (_pendingChanges.IsEmpty)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var debounceThreshold = now.AddMilliseconds(-_config.MonitoringDebounceMs);

        var readyChanges = _pendingChanges
            .Where(kvp => kvp.Value <= debounceThreshold)
            .ToList();

        foreach (var change in readyChanges)
        {
            _pendingChanges.TryRemove(change.Key, out _);

            try
            {
                // Check if this is a Vivaldi installation change (contains colon separator)
                if (change.Key.Contains(':'))
                {
                    var parts = change.Key.Split(':', 2);
                    var installationId = parts[0];
                    var filePath = parts[1];

                    lock (_statsLock)
                    {
                        TotalVivaldiChanges++;
                        LastChangeTime = DateTimeOffset.UtcNow;
                    }

                    _logger.LogDebug("Processing Vivaldi file change for {InstallationId}: {FilePath}", 
                        installationId, filePath);

                    VivaldiChanged?.Invoke(this, new VivaldiChangedEventArgs
                    {
                        FilePath = filePath,
                        Timestamp = DateTimeOffset.UtcNow,
                        InstallationId = installationId
                    });
                }
                else
                {
                    lock (_statsLock)
                    {
                        TotalFileChanges++;
                        LastChangeTime = DateTimeOffset.UtcNow;
                    }

                    _logger.LogDebug("Processing mods file change: {FilePath}", change.Key);

                    FileChanged?.Invoke(this, new FileChangedEventArgs
                    {
                        FilePath = change.Key,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file change: {FilePath}", change.Key);
            }
        }
    }

    private void StopAllWatchers()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing file system watcher");
            }
        }

        _watchers.Clear();
        _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private static bool IsTemporaryFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return true;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".tmp" || extension == ".bak" || extension == ".swp" || fileName.EndsWith('~');
    }
}

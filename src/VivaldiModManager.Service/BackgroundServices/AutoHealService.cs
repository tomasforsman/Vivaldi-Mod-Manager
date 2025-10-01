using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.Configuration;
using VivaldiModManager.Service.Models;

namespace VivaldiModManager.Service.BackgroundServices;

/// <summary>
/// Background service that automatically heals mod injections in response to monitoring events.
/// </summary>
public class AutoHealService : IHostedService, IDisposable
{
    private readonly ILogger<AutoHealService> _logger;
    private readonly ServiceConfiguration _config;
    private readonly IManifestService _manifestService;
    private readonly IVivaldiService _vivaldiService;
    private readonly ILoaderService _loaderService;
    private readonly IInjectionService _injectionService;
    private readonly FileSystemMonitorService _fileSystemMonitor;
    private readonly IntegrityCheckService _integrityCheckService;

    private readonly ConcurrentQueue<HealRequest> _healQueue;
    private readonly SemaphoreSlim _processSemaphore;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastHealAttempt;
    private readonly ConcurrentDictionary<string, int> _retryCount;
    private readonly LinkedList<HealHistoryEntry> _healHistory;
    private readonly object _historyLock = new object();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;
    private bool _disposed;

    // Metrics (using Interlocked for thread safety)
    private int _totalHealsAttempted;
    private int _totalHealsSucceeded;
    private int _totalHealsFailed;

    /// <summary>
    /// Gets the total number of heal attempts.
    /// </summary>
    public int TotalHealsAttempted => _totalHealsAttempted;

    /// <summary>
    /// Gets the total number of successful heals.
    /// </summary>
    public int TotalHealsSucceeded => _totalHealsSucceeded;

    /// <summary>
    /// Gets the total number of failed heals.
    /// </summary>
    public int TotalHealsFailed => _totalHealsFailed;

    /// <summary>
    /// Gets the last operation performed.
    /// </summary>
    public string? LastOperation { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last operation.
    /// </summary>
    public DateTimeOffset? LastOperationTime { get; private set; }

    /// <summary>
    /// Gets the number of pending heal requests.
    /// </summary>
    public int PendingHealRequests => _healQueue.Count;

    /// <summary>
    /// Gets the list of installations with active cooldowns.
    /// </summary>
    public IReadOnlyDictionary<string, DateTimeOffset> ActiveCooldowns => _lastHealAttempt;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoHealService"/> class.
    /// </summary>
    public AutoHealService(
        ILogger<AutoHealService> logger,
        ServiceConfiguration config,
        IManifestService manifestService,
        IVivaldiService vivaldiService,
        ILoaderService loaderService,
        IInjectionService injectionService,
        FileSystemMonitorService fileSystemMonitor,
        IntegrityCheckService integrityCheckService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _vivaldiService = vivaldiService ?? throw new ArgumentNullException(nameof(vivaldiService));
        _loaderService = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
        _injectionService = injectionService ?? throw new ArgumentNullException(nameof(injectionService));
        _fileSystemMonitor = fileSystemMonitor ?? throw new ArgumentNullException(nameof(fileSystemMonitor));
        _integrityCheckService = integrityCheckService ?? throw new ArgumentNullException(nameof(integrityCheckService));

        _healQueue = new ConcurrentQueue<HealRequest>();
        _processSemaphore = new SemaphoreSlim(1, 1);
        _lastHealAttempt = new ConcurrentDictionary<string, DateTimeOffset>();
        _retryCount = new ConcurrentDictionary<string, int>();
        _healHistory = new LinkedList<HealHistoryEntry>();
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Auto-Heal Service");

        // Load heal history from disk
        await LoadHealHistoryAsync(cancellationToken);

        // Subscribe to events
        _integrityCheckService.IntegrityViolation += OnIntegrityViolation;
        _fileSystemMonitor.VivaldiChanged += OnVivaldiChanged;

        // Start processing queue
        _cancellationTokenSource = new CancellationTokenSource();
        _processingTask = Task.Run(() => ProcessHealQueueAsync(_cancellationTokenSource.Token), cancellationToken);

        _logger.LogInformation("Auto-Heal Service started");
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Auto-Heal Service");

        // Unsubscribe from events
        _integrityCheckService.IntegrityViolation -= OnIntegrityViolation;
        _fileSystemMonitor.VivaldiChanged -= OnVivaldiChanged;

        // Cancel processing
        _cancellationTokenSource?.Cancel();

        // Wait for processing task to complete
        if (_processingTask != null)
        {
            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        // Save heal history
        await SaveHealHistoryAsync(cancellationToken);

        _logger.LogInformation("Auto-Heal Service stopped");
    }

    /// <summary>
    /// Queues a heal request manually.
    /// </summary>
    /// <param name="installationId">The installation ID to heal.</param>
    /// <param name="triggerReason">The reason for the heal.</param>
    public void QueueHealRequest(string installationId, string triggerReason)
    {
        var request = new HealRequest
        {
            InstallationId = installationId,
            TriggerReason = triggerReason,
            Timestamp = DateTimeOffset.UtcNow,
            RetryCount = _retryCount.GetValueOrDefault(installationId, 0)
        };

        _healQueue.Enqueue(request);
        _logger.LogInformation("Heal request queued for {InstallationId} (reason: {Reason})", installationId, triggerReason);
    }

    /// <summary>
    /// Gets the heal history.
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries to return.</param>
    /// <returns>List of heal history entries.</returns>
    public List<HealHistoryEntry> GetHealHistory(int maxEntries = 50)
    {
        lock (_historyLock)
        {
            return _healHistory.Take(maxEntries).ToList();
        }
    }

    private void OnIntegrityViolation(object? sender, IntegrityViolationEventArgs e)
    {
        _logger.LogInformation("Integrity violation detected for {InstallationId}, queueing heal request", e.Installation.Id);
        QueueHealRequest(e.Installation.Id, "IntegrityViolation");
    }

    private void OnVivaldiChanged(object? sender, VivaldiChangedEventArgs e)
    {
        // Check if this is a new version folder appearing
        var directoryPath = Path.GetDirectoryName(e.FilePath);
        if (directoryPath != null && directoryPath.Contains("Application\\") && Directory.Exists(directoryPath))
        {
            _logger.LogInformation("Vivaldi update detected for {InstallationId}, queueing heal request", e.InstallationId);
            QueueHealRequest(e.InstallationId, "VivaldiUpdate");
        }
    }

    private async Task ProcessHealQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Heal queue processor started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a heal request
                if (!_healQueue.TryDequeue(out var request))
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                // Process the request
                await _processSemaphore.WaitAsync(cancellationToken);
                try
                {
                    await ProcessHealRequestAsync(request, cancellationToken);
                }
                finally
                {
                    _processSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heal queue processor");
            }
        }

        _logger.LogInformation("Heal queue processor stopped");
    }

    private async Task ProcessHealRequestAsync(HealRequest request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var historyEntry = new HealHistoryEntry
        {
            InstallationId = request.InstallationId,
            Timestamp = DateTimeOffset.UtcNow,
            TriggerReason = request.TriggerReason,
            RetryCount = request.RetryCount
        };

        try
        {
            _logger.LogInformation("Processing heal request for {InstallationId} (attempt {RetryCount})", 
                request.InstallationId, request.RetryCount + 1);

            Interlocked.Increment(ref _totalHealsAttempted);
            LastOperation = $"Heal {request.InstallationId}";
            LastOperationTime = DateTimeOffset.UtcNow;

            // Check cooldown
            if (_lastHealAttempt.TryGetValue(request.InstallationId, out var lastAttempt))
            {
                var timeSinceLastAttempt = DateTimeOffset.UtcNow - lastAttempt;
                if (timeSinceLastAttempt.TotalSeconds < _config.AutoHealCooldownSeconds)
                {
                    var waitTime = TimeSpan.FromSeconds(_config.AutoHealCooldownSeconds) - timeSinceLastAttempt;
                    _logger.LogInformation("Cooldown active for {InstallationId}, requeuing for {WaitTime}s", 
                        request.InstallationId, waitTime.TotalSeconds);
                    
                    await Task.Delay(waitTime, cancellationToken);
                    _healQueue.Enqueue(request);
                    return;
                }
            }

            // Update last attempt time
            _lastHealAttempt[request.InstallationId] = DateTimeOffset.UtcNow;

            // Load manifest
            if (!_manifestService.ManifestExists(_config.ManifestPath))
            {
                throw new InvalidOperationException("Manifest not found");
            }

            var manifest = await _manifestService.LoadManifestAsync(_config.ManifestPath, cancellationToken);

            // Check if auto-heal is enabled and Safe Mode is not active
            if (!manifest.Settings.AutoHealEnabled)
            {
                _logger.LogInformation("Auto-heal is disabled, skipping heal for {InstallationId}", request.InstallationId);
                return;
            }

            if (manifest.Settings.SafeModeActive)
            {
                _logger.LogInformation("Safe Mode is active, skipping heal for {InstallationId}", request.InstallationId);
                return;
            }

            // Find the installation
            var installation = manifest.Installations.FirstOrDefault(i => i.Id == request.InstallationId);
            if (installation == null)
            {
                throw new InvalidOperationException($"Installation {request.InstallationId} not found");
            }

            // Wait for folder to stabilize
            await WaitForFolderStabilizationAsync(installation, cancellationToken);

            // Perform the heal operation
            await PerformHealAsync(installation, manifest, cancellationToken);

            // Success
            sw.Stop();
            historyEntry.Success = true;
            historyEntry.Duration = sw.Elapsed;
            Interlocked.Increment(ref _totalHealsSucceeded);
            
            // Reset retry count on success
            _retryCount.TryRemove(request.InstallationId, out _);

            _logger.LogInformation("Heal succeeded for {InstallationId} in {Duration}ms", 
                request.InstallationId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            historyEntry.Success = false;
            historyEntry.ErrorMessage = ex.Message;
            historyEntry.Duration = sw.Elapsed;

            _logger.LogError(ex, "Heal failed for {InstallationId}", request.InstallationId);

            // Handle retry logic
            var currentRetryCount = _retryCount.AddOrUpdate(request.InstallationId, 1, (_, count) => count + 1);
            
            if (currentRetryCount < _config.AutoHealMaxRetries)
            {
                // Schedule retry with exponential backoff
                var delayIndex = Math.Min(currentRetryCount - 1, _config.AutoHealRetryDelays.Length - 1);
                var delaySeconds = _config.AutoHealRetryDelays[delayIndex];
                
                _logger.LogInformation("Scheduling retry {RetryCount} for {InstallationId} in {Delay}s", 
                    currentRetryCount, request.InstallationId, delaySeconds);

                _ = Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken)
                    .ContinueWith(_ => QueueHealRequest(request.InstallationId, request.TriggerReason), 
                        cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
            else
            {
                _logger.LogError("Max retries exceeded for {InstallationId}", request.InstallationId);
                historyEntry.ErrorMessage = $"Max retries exceeded: {ex.Message}";
                Interlocked.Increment(ref _totalHealsFailed);
                _retryCount.TryRemove(request.InstallationId, out _);
            }
        }
        finally
        {
            // Add to history
            AddToHealHistory(historyEntry);
            await SaveHealHistoryAsync(cancellationToken);
        }
    }

    private async Task WaitForFolderStabilizationAsync(VivaldiInstallation installation, CancellationToken cancellationToken)
    {
        var maxWaitTime = TimeSpan.FromSeconds(_config.VivaldiFolderStabilizationMaxWaitSeconds);
        var sw = Stopwatch.StartNew();

        _logger.LogDebug("Waiting for folder stabilization for {InstallationId}", installation.Id);

        while (sw.Elapsed < maxWaitTime)
        {
            try
            {
                // Try to open target files exclusively to check if they're still being written
                var targetFiles = await _vivaldiService.FindInjectionTargetsAsync(installation, cancellationToken);
                bool allAccessible = true;

                foreach (var targetFile in targetFiles.Values)
                {
                    try
                    {
                        using var stream = File.Open(targetFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        stream.Close();
                    }
                    catch (IOException)
                    {
                        allAccessible = false;
                        break;
                    }
                }

                if (allAccessible)
                {
                    _logger.LogDebug("Folder stabilized for {InstallationId} after {ElapsedMs}ms", 
                        installation.Id, sw.ElapsedMilliseconds);
                    return;
                }

                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking file accessibility for {InstallationId}", installation.Id);
                await Task.Delay(1000, cancellationToken);
            }
        }

        _logger.LogWarning("Folder stabilization timeout for {InstallationId} after {MaxWait}s, proceeding anyway", 
            installation.Id, _config.VivaldiFolderStabilizationMaxWaitSeconds);
    }

    private async Task PerformHealAsync(VivaldiInstallation installation, ManifestData manifest, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing heal for {InstallationId}", installation.Id);

        // Get enabled mods
        var enabledMods = manifest.Mods.Where(m => m.Enabled).OrderBy(m => m.Order).ToList();
        if (enabledMods.Count == 0)
        {
            _logger.LogInformation("No enabled mods, skipping injection for {InstallationId}", installation.Id);
            return;
        }

        // Resolve mods root path
        var modsRootPath = manifest.Settings.ModsRootPath;
        if (string.IsNullOrWhiteSpace(modsRootPath))
        {
            throw new InvalidOperationException("ModsRootPath is not configured");
        }

        modsRootPath = Environment.ExpandEnvironmentVariables(modsRootPath);

        try
        {
            // Find injection targets to determine where to place loader.js
            var targets = await _vivaldiService.FindInjectionTargetsAsync(installation, cancellationToken);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("No injection targets found");
            }

            // Determine loader path (in vivaldi-mods folder)
            var firstTargetDir = Path.GetDirectoryName(targets.Values.First());
            var vivaldiModsDir = Path.Combine(firstTargetDir!, "vivaldi-mods");
            Directory.CreateDirectory(vivaldiModsDir);
            var loaderPath = Path.Combine(vivaldiModsDir, "loader.js");

            // Generate loader.js
            await _loaderService.GenerateLoaderAsync(manifest, loaderPath, cancellationToken);

            // Copy enabled mod files
            var modsTargetDir = Path.Combine(vivaldiModsDir, "mods");
            Directory.CreateDirectory(modsTargetDir);

            foreach (var mod in enabledMods)
            {
                var sourcePath = Path.Combine(modsRootPath, mod.Filename);
                var targetPath = Path.Combine(modsTargetDir, mod.Filename);

                if (!File.Exists(sourcePath))
                {
                    _logger.LogWarning("Mod file not found: {FileName}", mod.Filename);
                    continue;
                }

                File.Copy(sourcePath, targetPath, true);
            }

            // Inject stub
            await _injectionService.InjectAsync(installation, loaderPath, cancellationToken);
            
            // Get injection status to retrieve fingerprint
            var injectionStatus = await _injectionService.GetInjectionStatusAsync(installation, cancellationToken);

            // Update manifest
            installation.LastInjectionAt = DateTimeOffset.UtcNow;
            installation.LastInjectionStatus = "Success";
            installation.InjectionFingerprint = injectionStatus.Fingerprint;

            await _manifestService.SaveManifestAsync(manifest, _config.ManifestPath, cancellationToken);

            _logger.LogInformation("Heal completed successfully for {InstallationId}", installation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heal operation failed for {InstallationId}, attempting rollback", installation.Id);

            // Attempt rollback
            try
            {
                await _injectionService.RemoveInjectionAsync(installation, cancellationToken);
                _logger.LogInformation("Rollback completed for {InstallationId}", installation.Id);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Rollback failed for {InstallationId}", installation.Id);
            }

            throw;
        }
    }

    private void AddToHealHistory(HealHistoryEntry entry)
    {
        lock (_historyLock)
        {
            _healHistory.AddFirst(entry);

            // Maintain max size
            while (_healHistory.Count > _config.HealHistoryMaxEntries)
            {
                _healHistory.RemoveLast();
            }
        }
    }

    private async Task LoadHealHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_config.HealHistoryFilePath))
            {
                _logger.LogDebug("Heal history file not found, starting with empty history");
                return;
            }

            var json = await File.ReadAllTextAsync(_config.HealHistoryFilePath, cancellationToken);
            var entries = JsonSerializer.Deserialize<List<HealHistoryEntry>>(json);

            if (entries != null)
            {
                lock (_historyLock)
                {
                    _healHistory.Clear();
                    foreach (var entry in entries.Take(_config.HealHistoryMaxEntries))
                    {
                        _healHistory.AddLast(entry);
                    }
                }

                _logger.LogInformation("Loaded {Count} heal history entries", _healHistory.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load heal history, starting with empty history");
        }
    }

    private async Task SaveHealHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            List<HealHistoryEntry> entries;
            lock (_historyLock)
            {
                entries = _healHistory.ToList();
            }

            var directory = Path.GetDirectoryName(_config.HealHistoryFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_config.HealHistoryFilePath, json, cancellationToken);

            _logger.LogDebug("Saved {Count} heal history entries", entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save heal history");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource?.Dispose();
        _processSemaphore?.Dispose();
        _disposed = true;
    }
}

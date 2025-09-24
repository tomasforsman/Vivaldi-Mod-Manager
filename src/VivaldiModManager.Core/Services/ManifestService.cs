using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Constants;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Implementation of <see cref="IManifestService"/> providing JSON persistence with atomic operations,
/// schema validation, backup and restore capabilities.
/// </summary>
public class ManifestService : IManifestService
{
    private readonly ILogger<ManifestService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ManifestService(ILogger<ManifestService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = CreateJsonOptions();
    }

    /// <inheritdoc />
    public async Task<ManifestData> LoadManifestAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new ManifestNotFoundException(filePath);
        }

        try
        {
            _logger.LogDebug("Loading manifest from: {FilePath}", filePath);
            
            string jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            
            var manifest = JsonSerializer.Deserialize<ManifestData>(jsonContent, _jsonOptions);
            if (manifest == null)
            {
                throw new ManifestCorruptedException(filePath, "Deserialized manifest is null");
            }

            // Validate and migrate if necessary
            if (!ValidateManifest(manifest))
            {
                throw new ManifestCorruptedException(filePath, "Manifest validation failed");
            }

            manifest = MigrateManifest(manifest);

            _logger.LogInformation("Successfully loaded manifest from {FilePath} with {ModCount} mods", 
                filePath, manifest.Mods.Count);
            
            return manifest;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error loading manifest from {FilePath}", filePath);
            throw new ManifestCorruptedException(filePath, "Invalid JSON format", ex);
        }
        catch (Exception ex) when (!(ex is ManifestException))
        {
            _logger.LogError(ex, "Unexpected error loading manifest from {FilePath}", filePath);
            throw new ManifestIOException(filePath, "load", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveManifestAsync(ManifestData manifest, string filePath, CancellationToken cancellationToken = default)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        try
        {
            _logger.LogDebug("Saving manifest to: {FilePath}", filePath);

            // Update timestamps
            manifest.LastUpdated = DateTimeOffset.UtcNow;
            manifest.LastUpdatedByVersion = GetType().Assembly.GetName().Version?.ToString();

            // Validate before saving
            if (!ValidateManifest(manifest))
            {
                throw new ArgumentException("Manifest validation failed", nameof(manifest));
            }

            // Use atomic operation: write to temp file, then move
            var tempFilePath = filePath + ManifestConstants.TempFileSuffix;
            var directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string jsonContent = JsonSerializer.Serialize(manifest, _jsonOptions);
            await File.WriteAllTextAsync(tempFilePath, jsonContent, cancellationToken);

            // Atomic move
            File.Move(tempFilePath, filePath, overwrite: true);

            _logger.LogInformation("Successfully saved manifest to {FilePath} with {ModCount} mods", 
                filePath, manifest.Mods.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving manifest to {FilePath}", filePath);
            
            // Clean up temp file if it exists
            var tempFilePath = filePath + ManifestConstants.TempFileSuffix;
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up temp file: {TempFilePath}", tempFilePath);
                }
            }

            throw new ManifestIOException(filePath, "save", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(string filePath, string? backupPath = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}", filePath);
        }

        try
        {
            backupPath ??= GenerateBackupPath(filePath);
            
            var backupDirectory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            _logger.LogDebug("Creating backup from {SourcePath} to {BackupPath}", filePath, backupPath);

            using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var targetStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(targetStream, cancellationToken);

            _logger.LogInformation("Successfully created backup: {BackupPath}", backupPath);
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup from {SourcePath} to {BackupPath}", filePath, backupPath);
            throw new ManifestIOException(filePath, "backup", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task RestoreFromBackupAsync(string backupPath, string targetPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            throw new ArgumentException("Backup path cannot be null or empty.", nameof(backupPath));
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));
        }

        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException($"Backup file not found: {backupPath}", backupPath);
        }

        try
        {
            _logger.LogDebug("Restoring from backup {BackupPath} to {TargetPath}", backupPath, targetPath);

            // Verify the backup is valid before restoring
            await LoadManifestAsync(backupPath, cancellationToken);

            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            using var sourceStream = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(targetStream, cancellationToken);

            _logger.LogInformation("Successfully restored from backup {BackupPath} to {TargetPath}", backupPath, targetPath);
        }
        catch (ManifestException)
        {
            throw; // Re-throw manifest exceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring from backup {BackupPath} to {TargetPath}", backupPath, targetPath);
            throw new ManifestIOException(backupPath, "restore", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public bool ValidateManifest(ManifestData manifest)
    {
        if (manifest == null)
        {
            _logger.LogWarning("Manifest validation failed: manifest is null");
            return false;
        }

        try
        {
            // Check schema version
            if (manifest.SchemaVersion <= 0)
            {
                _logger.LogWarning("Manifest validation failed: invalid schema version {SchemaVersion}", manifest.SchemaVersion);
                return false;
            }

            // Validate mod IDs are unique
            var modIds = manifest.Mods.Select(m => m.Id).ToList();
            if (modIds.Count != modIds.Distinct().Count())
            {
                _logger.LogWarning("Manifest validation failed: duplicate mod IDs found");
                return false;
            }

            // Validate mod orders are unique
            var enabledMods = manifest.Mods.Where(m => m.Enabled).ToList();
            var modOrders = enabledMods.Select(m => m.Order).ToList();
            if (modOrders.Count != modOrders.Distinct().Count())
            {
                _logger.LogWarning("Manifest validation failed: duplicate mod orders found");
                return false;
            }

            // Validate installation IDs are unique
            var installationIds = manifest.Installations.Select(i => i.Id).ToList();
            if (installationIds.Count != installationIds.Distinct().Count())
            {
                _logger.LogWarning("Manifest validation failed: duplicate installation IDs found");
                return false;
            }

            _logger.LogDebug("Manifest validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manifest validation");
            return false;
        }
    }

    /// <inheritdoc />
    public ManifestData MigrateManifest(ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (manifest.SchemaVersion == ManifestConstants.CurrentSchemaVersion)
        {
            return manifest; // No migration needed
        }

        if (manifest.SchemaVersion > ManifestConstants.CurrentSchemaVersion)
        {
            throw new ManifestSchemaException(manifest.SchemaVersion, ManifestConstants.CurrentSchemaVersion);
        }

        _logger.LogInformation("Migrating manifest from schema version {CurrentVersion} to {NewVersion}",
            manifest.SchemaVersion, ManifestConstants.CurrentSchemaVersion);

        // Future migrations would go here
        // For now, just update the schema version
        manifest.SchemaVersion = ManifestConstants.CurrentSchemaVersion;
        manifest.LastUpdated = DateTimeOffset.UtcNow;

        return manifest;
    }

    /// <inheritdoc />
    public bool ManifestExists(string filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
    }

    /// <inheritdoc />
    public ManifestData CreateDefaultManifest()
    {
        _logger.LogDebug("Creating default manifest");

        return new ManifestData
        {
            SchemaVersion = ManifestConstants.CurrentSchemaVersion,
            LastUpdated = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByVersion = GetType().Assembly.GetName().Version?.ToString(),
            LastUpdatedByVersion = GetType().Assembly.GetName().Version?.ToString(),
            Settings = new GlobalSettings
            {
                AutoHealEnabled = true,
                MonitoringEnabled = true,
                BackupRetentionDays = ManifestConstants.DefaultBackupRetentionDays,
                LogLevel = ManifestConstants.DefaultLogLevel
            },
            Mods = new List<ModInfo>(),
            Installations = new List<VivaldiInstallation>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    /// <inheritdoc />
    public Task<IEnumerable<FileInfo>> GetAvailableBackupsAsync(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("Manifest path cannot be null or empty.", nameof(manifestPath));
        }

        try
        {
            var directory = Path.GetDirectoryName(manifestPath);
            var fileName = Path.GetFileNameWithoutExtension(manifestPath);
            
            if (string.IsNullOrEmpty(directory))
            {
                return Task.FromResult(Enumerable.Empty<FileInfo>());
            }

            var backupDirectory = Path.Combine(directory, ManifestConstants.BackupDirectoryName);
            
            if (!Directory.Exists(backupDirectory))
            {
                return Task.FromResult(Enumerable.Empty<FileInfo>());
            }

            var pattern = $"{fileName}*.backup{ManifestConstants.JsonFileExtension}";
            var backupFiles = Directory.GetFiles(backupDirectory, pattern)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .AsEnumerable();

            return Task.FromResult(backupFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available backups for {ManifestPath}", manifestPath);
            return Task.FromResult(Enumerable.Empty<FileInfo>());
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldBackupsAsync(string manifestPath, int retentionDays, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("Manifest path cannot be null or empty.", nameof(manifestPath));
        }

        if (retentionDays < 0)
        {
            throw new ArgumentException("Retention days cannot be negative.", nameof(retentionDays));
        }

        try
        {
            var backups = await GetAvailableBackupsAsync(manifestPath);
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var oldBackups = backups.Where(b => b.CreationTime < cutoffDate).ToList();

            var cleanedUpCount = 0;
            foreach (var backup in oldBackups)
            {
                try
                {
                    File.Delete(backup.FullName);
                    cleanedUpCount++;
                    _logger.LogDebug("Deleted old backup: {BackupPath}", backup.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old backup: {BackupPath}", backup.FullName);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (cleanedUpCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old backup files for {ManifestPath}", cleanedUpCount, manifestPath);
            }

            return cleanedUpCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old backups for {ManifestPath}", manifestPath);
            throw;
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private static string GenerateBackupPath(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        
        var backupDirectory = Path.Combine(directory, ManifestConstants.BackupDirectoryName);
        return Path.Combine(backupDirectory, $"{fileName}_{timestamp}.backup{extension}");
    }
}
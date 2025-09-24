using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Provides JSON persistence services for manifest data with schema validation,
/// migration support, backup and restore capabilities, and atomic file operations.
/// </summary>
public interface IManifestService
{
    /// <summary>
    /// Loads the manifest from the specified file path asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the manifest file.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The loaded manifest data.</returns>
    /// <exception cref="ManifestNotFoundException">Thrown when the manifest file is not found.</exception>
    /// <exception cref="ManifestCorruptedException">Thrown when the manifest file is corrupted.</exception>
    /// <exception cref="ManifestSchemaException">Thrown when the manifest schema version is incompatible.</exception>
    Task<ManifestData> LoadManifestAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the manifest to the specified file path asynchronously using atomic operations.
    /// </summary>
    /// <param name="manifest">The manifest data to save.</param>
    /// <param name="filePath">The path where to save the manifest file.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <exception cref="ManifestIOException">Thrown when an I/O error occurs during save.</exception>
    Task SaveManifestAsync(ManifestData manifest, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup of the manifest file.
    /// </summary>
    /// <param name="filePath">The path to the manifest file to backup.</param>
    /// <param name="backupPath">The path where to save the backup. If null, generates a default backup path.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The path to the created backup file.</returns>
    Task<string> CreateBackupAsync(string filePath, string? backupPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the manifest from a backup file.
    /// </summary>
    /// <param name="backupPath">The path to the backup file.</param>
    /// <param name="targetPath">The path where to restore the manifest.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task RestoreFromBackupAsync(string backupPath, string targetPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the manifest data and schema version.
    /// </summary>
    /// <param name="manifest">The manifest data to validate.</param>
    /// <returns>True if the manifest is valid; otherwise, false.</returns>
    bool ValidateManifest(ManifestData manifest);

    /// <summary>
    /// Migrates the manifest to the current schema version if needed.
    /// </summary>
    /// <param name="manifest">The manifest data to migrate.</param>
    /// <returns>The migrated manifest data.</returns>
    ManifestData MigrateManifest(ManifestData manifest);

    /// <summary>
    /// Checks if a manifest file exists at the specified path.
    /// </summary>
    /// <param name="filePath">The path to check.</param>
    /// <returns>True if the manifest file exists; otherwise, false.</returns>
    bool ManifestExists(string filePath);

    /// <summary>
    /// Creates a new empty manifest with default settings.
    /// </summary>
    /// <returns>A new manifest with default configuration.</returns>
    ManifestData CreateDefaultManifest();

    /// <summary>
    /// Gets information about available backup files for a manifest.
    /// </summary>
    /// <param name="manifestPath">The path to the original manifest file.</param>
    /// <returns>A list of backup file information.</returns>
    Task<IEnumerable<FileInfo>> GetAvailableBackupsAsync(string manifestPath);

    /// <summary>
    /// Cleans up old backup files based on retention policy.
    /// </summary>
    /// <param name="manifestPath">The path to the original manifest file.</param>
    /// <param name="retentionDays">The number of days to retain backups.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The number of backup files cleaned up.</returns>
    Task<int> CleanupOldBackupsAsync(string manifestPath, int retentionDays, CancellationToken cancellationToken = default);
}
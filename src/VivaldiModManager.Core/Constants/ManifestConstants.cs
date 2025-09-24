namespace VivaldiModManager.Core.Constants;

/// <summary>
/// Constants for manifest file management and configuration defaults.
/// </summary>
public static class ManifestConstants
{
    /// <summary>
    /// Default filename for the manifest file.
    /// </summary>
    public const string DefaultManifestFilename = "manifest.json";

    /// <summary>
    /// Default filename for the loader file.
    /// </summary>
    public const string DefaultLoaderFilename = "loader.js";

    /// <summary>
    /// Default filename for the backup manifest file.
    /// </summary>
    public const string DefaultBackupManifestFilename = "manifest.backup.json";

    /// <summary>
    /// File extension for JavaScript mod files.
    /// </summary>
    public const string ModFileExtension = ".js";

    /// <summary>
    /// File extension for JSON files.
    /// </summary>
    public const string JsonFileExtension = ".json";

    /// <summary>
    /// Current schema version for the manifest.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Default backup retention period in days.
    /// </summary>
    public const int DefaultBackupRetentionDays = 30;

    /// <summary>
    /// Default log level.
    /// </summary>
    public const string DefaultLogLevel = "Info";

    /// <summary>
    /// Temporary file suffix for atomic operations.
    /// </summary>
    public const string TempFileSuffix = ".tmp";

    /// <summary>
    /// Backup directory name.
    /// </summary>
    public const string BackupDirectoryName = "backups";

    /// <summary>
    /// Logs directory name.
    /// </summary>
    public const string LogsDirectoryName = "logs";

    /// <summary>
    /// Maximum file size for mod files in bytes (10 MB).
    /// </summary>
    public const long MaxModFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum number of backups to retain per file.
    /// </summary>
    public const int MaxBackupsPerFile = 10;

    /// <summary>
    /// JSON serialization indent string.
    /// </summary>
    public const string JsonIndent = "  ";

    /// <summary>
    /// Default timeout for file operations in milliseconds.
    /// </summary>
    public const int DefaultFileOperationTimeoutMs = 5000;
}
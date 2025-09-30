using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Constants;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Implementation of <see cref="IInjectionService"/> providing mod loader stub injection into Vivaldi HTML files.
/// Handles JavaScript injection, fingerprint management, backup operations, and integrity validation.
/// </summary>
[SupportedOSPlatform("windows")]
public class InjectionService : IInjectionService
{
    private readonly ILogger<InjectionService> _logger;
    private readonly IVivaldiService _vivaldiService;
    private readonly ILoaderService _loaderService;
    private readonly IHashService _hashService;

    private static readonly Regex InjectionRegex = new(
        @"<!--\s*Vivaldi Mod Manager - Injection Stub.*?-->\s*<!--\s*Fingerprint:.*?-->\s*<!--\s*Generated:.*?-->\s*<script[^>]*></script>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FingerprintRegex = new(
        @"<!--\s*Fingerprint:\s*([a-fA-F0-9]+)\s*-->",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="vivaldiService">The Vivaldi service for target file discovery.</param>
    /// <param name="loaderService">The loader service for deploying generated loaders.</param>
    /// <param name="hashService">The hash service for fingerprint generation.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public InjectionService(
        ILogger<InjectionService> logger,
        IVivaldiService vivaldiService,
        ILoaderService loaderService,
        IHashService hashService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _vivaldiService = vivaldiService ?? throw new ArgumentNullException(nameof(vivaldiService));
        _loaderService = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
        _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
    }

    /// <inheritdoc />
    public async Task InjectAsync(VivaldiInstallation installation, string loaderPath, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        if (string.IsNullOrWhiteSpace(loaderPath))
        {
            throw new ArgumentException("Loader path cannot be null or empty.", nameof(loaderPath));
        }

        _logger.LogInformation("Starting injection for installation: {InstallationName}", installation.Name);

        try
        {
            // Generate unique fingerprint for this injection
            var fingerprint = GenerateInjectionFingerprint(installation, loaderPath);
            
            // Find injection targets
            var targets = await _vivaldiService.FindInjectionTargetsAsync(installation, cancellationToken);
            if (targets.Count == 0)
            {
                throw new InjectionException(installation.Id, "injection", "No injection targets found");
            }

            // Create backups before injection
            await BackupTargetFilesAsync(installation, cancellationToken);

            var successfulInjections = 0;
            var injectionErrors = new List<string>();

            // Inject stub into each target
            foreach (var target in targets)
            {
                try
                {
                    await InjectIntoTargetAsync(target.Value, loaderPath, fingerprint, cancellationToken);
                    successfulInjections++;
                    _logger.LogDebug("Successfully injected into {TargetName}: {TargetPath}", target.Key, target.Value);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to inject into {target.Key}: {ex.Message}";
                    injectionErrors.Add(errorMessage);
                    _logger.LogError(ex, "Failed to inject into {TargetName}: {TargetPath}", target.Key, target.Value);
                }
            }

            if (successfulInjections == 0)
            {
                throw new InjectionException(installation.Id, "injection", 
                    $"Failed to inject into any targets: {string.Join("; ", injectionErrors)}");
            }

            // Update installation metadata
            installation.LastInjectionAt = DateTimeOffset.UtcNow;
            installation.InjectionFingerprint = fingerprint;
            installation.LastInjectionStatus = successfulInjections == targets.Count ? "Success" : "Partial";

            _logger.LogInformation("Injection completed for {InstallationName}: {SuccessCount}/{TotalCount} targets successful",
                installation.Name, successfulInjections, targets.Count);

            if (injectionErrors.Count > 0)
            {
                _logger.LogWarning("Injection partially failed: {ErrorCount} error(s): {Errors}",
                    injectionErrors.Count, string.Join("; ", injectionErrors));
            }
        }
        catch (Exception ex) when (!(ex is InjectionException))
        {
            _logger.LogError(ex, "Unexpected error during injection for {InstallationName}", installation.Name);
            throw new InjectionException(installation.Id, "injection", $"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task RemoveInjectionAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        _logger.LogInformation("Removing injection for installation: {InstallationName}", installation.Name);

        try
        {
            // Find injection targets
            var targets = await _vivaldiService.FindInjectionTargetsAsync(installation, cancellationToken);
            if (targets.Count == 0)
            {
                _logger.LogWarning("No injection targets found for removal in installation: {InstallationName}", installation.Name);
                return;
            }

            var successfulRemovals = 0;
            var removalErrors = new List<string>();

            // Remove injection from each target
            foreach (var target in targets)
            {
                try
                {
                    await RemoveInjectionFromTargetAsync(target.Value, cancellationToken);
                    successfulRemovals++;
                    _logger.LogDebug("Successfully removed injection from {TargetName}: {TargetPath}", target.Key, target.Value);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to remove injection from {target.Key}: {ex.Message}";
                    removalErrors.Add(errorMessage);
                    _logger.LogError(ex, "Failed to remove injection from {TargetName}: {TargetPath}", target.Key, target.Value);
                }
            }

            // Update installation metadata
            if (successfulRemovals > 0)
            {
                installation.LastInjectionAt = null;
                installation.InjectionFingerprint = null;
                installation.LastInjectionStatus = successfulRemovals == targets.Count ? "Removed" : "PartiallyRemoved";
            }

            _logger.LogInformation("Injection removal completed for {InstallationName}: {SuccessCount}/{TotalCount} targets successful",
                installation.Name, successfulRemovals, targets.Count);

            if (removalErrors.Count > 0)
            {
                _logger.LogWarning("Injection removal partially failed: {ErrorCount} error(s): {Errors}",
                    removalErrors.Count, string.Join("; ", removalErrors));
            }
        }
        catch (Exception ex) when (!(ex is InjectionException))
        {
            _logger.LogError(ex, "Unexpected error during injection removal for {InstallationName}", installation.Name);
            throw new InjectionException(installation.Id, "removal", $"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateInjectionAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        try
        {
            var status = await GetInjectionStatusAsync(installation, cancellationToken);
            return status.IsFullyIntact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating injection for {InstallationName}", installation.Name);
            throw new InjectionValidationException(installation.Id, $"Validation error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> BackupTargetFilesAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        _logger.LogInformation("Creating backups for installation: {InstallationName}", installation.Name);

        try
        {
            var targets = await _vivaldiService.FindInjectionTargetsAsync(installation, cancellationToken);
            var backups = new Dictionary<string, string>();

            foreach (var target in targets)
            {
                try
                {
                    var backupPath = GenerateBackupPath(target.Value, installation.Id);
                    await CreateFileBackupAsync(target.Value, backupPath, cancellationToken);
                    backups[target.Key] = backupPath;
                    _logger.LogDebug("Created backup for {TargetName}: {BackupPath}", target.Key, backupPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create backup for {TargetName}: {TargetPath}", target.Key, target.Value);
                    throw new InjectionBackupException(installation.Id, target.Value, "create", ex.Message, ex);
                }
            }

            _logger.LogInformation("Successfully created {BackupCount} backup(s) for {InstallationName}",
                backups.Count, installation.Name);

            return backups.AsReadOnly();
        }
        catch (Exception ex) when (!(ex is InjectionBackupException))
        {
            _logger.LogError(ex, "Unexpected error creating backups for {InstallationName}", installation.Name);
            throw new InjectionBackupException(installation.Id, "", "create", $"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task RestoreTargetFilesAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        _logger.LogInformation("Restoring target files for installation: {InstallationName}", installation.Name);

        try
        {
            var targets = await _vivaldiService.FindInjectionTargetsAsync(installation, cancellationToken);
            var successfulRestores = 0;
            var restoreErrors = new List<string>();

            foreach (var target in targets)
            {
                try
                {
                    var backupPath = GenerateBackupPath(target.Value, installation.Id);
                    if (File.Exists(backupPath))
                    {
                        await RestoreFileFromBackupAsync(backupPath, target.Value, cancellationToken);
                        successfulRestores++;
                        _logger.LogDebug("Successfully restored {TargetName} from backup", target.Key);
                    }
                    else
                    {
                        var errorMessage = $"Backup not found for {target.Key}";
                        restoreErrors.Add(errorMessage);
                        _logger.LogWarning("Backup not found for {TargetName}: {BackupPath}", target.Key, backupPath);
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to restore {target.Key}: {ex.Message}";
                    restoreErrors.Add(errorMessage);
                    _logger.LogError(ex, "Failed to restore {TargetName}: {TargetPath}", target.Key, target.Value);
                }
            }

            _logger.LogInformation("Restore completed for {InstallationName}: {SuccessCount}/{TotalCount} targets successful",
                installation.Name, successfulRestores, targets.Count);

            if (restoreErrors.Count > 0)
            {
                throw new InjectionBackupException(installation.Id, "", "restore",
                    $"Restore partially failed: {string.Join("; ", restoreErrors)}");
            }

            // Update installation metadata
            installation.LastInjectionAt = null;
            installation.InjectionFingerprint = null;
            installation.LastInjectionStatus = "Restored";
        }
        catch (Exception ex) when (!(ex is InjectionBackupException))
        {
            _logger.LogError(ex, "Unexpected error restoring files for {InstallationName}", installation.Name);
            throw new InjectionBackupException(installation.Id, "", "restore", $"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<InjectionStatus> GetInjectionStatusAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        try
        {
            var targets = await _vivaldiService.FindInjectionTargetsAsync(installation, cancellationToken);
            var status = new InjectionStatus
            {
                TotalTargetCount = targets.Count,
                LastInjectionAt = installation.LastInjectionAt,
                Fingerprint = installation.InjectionFingerprint
            };

            var validationErrors = new List<string>();
            var injectedCount = 0;
            var validCount = 0;

            foreach (var target in targets)
            {
                var targetStatus = await ValidateTargetInjectionAsync(target.Value, installation.InjectionFingerprint, cancellationToken);
                status.TargetFiles[target.Key] = targetStatus;

                if (targetStatus.IsInjected)
                {
                    injectedCount++;
                }

                if (targetStatus.ValidationStatus == InjectionValidationStatus.Valid)
                {
                    validCount++;
                }

                validationErrors.AddRange(targetStatus.ValidationErrors);
            }

            status.InjectedTargetCount = injectedCount;
            status.ValidationErrors = validationErrors;
            status.IsInjected = injectedCount > 0;

            // Determine overall validation status
            if (injectedCount == 0)
            {
                status.ValidationStatus = InjectionValidationStatus.NotInjected;
            }
            else if (validCount == targets.Count)
            {
                status.ValidationStatus = InjectionValidationStatus.Valid;
            }
            else if (injectedCount < targets.Count)
            {
                status.ValidationStatus = InjectionValidationStatus.Partial;
            }
            else if (status.TargetFiles.Values.Any(t => t.ValidationStatus == InjectionValidationStatus.FingerprintMismatch))
            {
                status.ValidationStatus = InjectionValidationStatus.FingerprintMismatch;
            }
            else
            {
                status.ValidationStatus = InjectionValidationStatus.Invalid;
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting injection status for {InstallationName}", installation.Name);
            return new InjectionStatus
            {
                ValidationStatus = InjectionValidationStatus.ValidationFailed,
                ValidationErrors = new List<string> { $"Status check failed: {ex.Message}" }
            };
        }
    }

    /// <inheritdoc />
    public async Task RepairInjectionAsync(VivaldiInstallation installation, string loaderPath, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        if (string.IsNullOrWhiteSpace(loaderPath))
        {
            throw new ArgumentException("Loader path cannot be null or empty.", nameof(loaderPath));
        }

        _logger.LogInformation("Repairing injection for installation: {InstallationName}", installation.Name);

        try
        {
            // First, remove any existing injections
            await RemoveInjectionAsync(installation, cancellationToken);

            // Then, perform a fresh injection
            await InjectAsync(installation, loaderPath, cancellationToken);

            _logger.LogInformation("Successfully repaired injection for {InstallationName}", installation.Name);
        }
        catch (Exception ex) when (!(ex is InjectionException))
        {
            _logger.LogError(ex, "Unexpected error repairing injection for {InstallationName}", installation.Name);
            throw new InjectionException(installation.Id, "repair", $"Unexpected error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates a unique fingerprint for an injection.
    /// </summary>
    /// <param name="installation">The installation being injected.</param>
    /// <param name="loaderPath">The loader path being injected.</param>
    /// <returns>A unique fingerprint string.</returns>
    private string GenerateInjectionFingerprint(VivaldiInstallation installation, string loaderPath)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var input = $"{installation.Id}|{loaderPath}|{timestamp}";
        var hash = _hashService.ComputeStringHash(input);
        // Use first 16 characters, or the entire hash if it's shorter
        return hash.Length >= 16 ? hash[..16] : hash;
    }

    /// <summary>
    /// Generates the injection stub JavaScript code.
    /// </summary>
    /// <param name="loaderPath">The path to the loader.js file.</param>
    /// <param name="fingerprint">The injection fingerprint.</param>
    /// <returns>The complete injection stub JavaScript code.</returns>
    private static string GenerateInjectionStub(string loaderPath, string fingerprint)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var relativeLoaderPath = Path.GetFileName(Path.GetDirectoryName(loaderPath)) + "/" + Path.GetFileName(loaderPath);
        
        // Use external script reference to avoid CSP inline script restrictions
        return $@"<!-- {ManifestConstants.InjectionCommentMarker} v{ManifestConstants.InjectionStubVersion} -->
<!-- Fingerprint: {fingerprint} -->
<!-- Generated: {timestamp} -->
<script type=""module"" src=""./{relativeLoaderPath}""></script>";
    }

    /// <summary>
    /// Injects the stub code into a specific target file.
    /// </summary>
    /// <param name="targetPath">The path to the target HTML file.</param>
    /// <param name="loaderPath">The path to the loader.js file.</param>
    /// <param name="fingerprint">The injection fingerprint.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InjectIntoTargetAsync(string targetPath, string loaderPath, string fingerprint, CancellationToken cancellationToken)
    {
        if (!File.Exists(targetPath))
        {
            throw new InjectionException($"Target file not found: {targetPath}");
        }

        // Read the current content
        var content = await File.ReadAllTextAsync(targetPath, cancellationToken);

        // Remove any existing injections
        content = InjectionRegex.Replace(content, string.Empty);

        // Generate the injection stub
        var injectionStub = GenerateInjectionStub(loaderPath, fingerprint);

        // Find the best insertion point (before closing </body> or </html>)
        var insertionPoint = FindInsertionPoint(content);
        if (insertionPoint == -1)
        {
            throw new InjectionException($"Could not find suitable insertion point in {targetPath}");
        }

        // Insert the injection stub
        var newContent = content.Insert(insertionPoint, injectionStub + Environment.NewLine);

        // Write the modified content atomically
        await WriteFileAtomicallyAsync(targetPath, newContent, cancellationToken);
    }

    /// <summary>
    /// Removes injection from a specific target file.
    /// </summary>
    /// <param name="targetPath">The path to the target HTML file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RemoveInjectionFromTargetAsync(string targetPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(targetPath))
        {
            _logger.LogWarning("Target file not found for injection removal: {TargetPath}", targetPath);
            return;
        }

        // Read the current content
        var content = await File.ReadAllTextAsync(targetPath, cancellationToken);

        // Remove all injections
        var newContent = InjectionRegex.Replace(content, string.Empty);

        // Only write if content changed
        if (newContent != content)
        {
            await WriteFileAtomicallyAsync(targetPath, newContent, cancellationToken);
        }
    }

    /// <summary>
    /// Validates injection in a specific target file.
    /// </summary>
    /// <param name="targetPath">The path to the target HTML file.</param>
    /// <param name="expectedFingerprint">The expected fingerprint.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The validation status for the target.</returns>
    private async Task<InjectionTargetStatus> ValidateTargetInjectionAsync(string targetPath, string? expectedFingerprint, CancellationToken cancellationToken)
    {
        var status = new InjectionTargetStatus
        {
            FilePath = targetPath,
            HasBackup = File.Exists(GenerateBackupPath(targetPath, ""))
        };

        try
        {
            if (!File.Exists(targetPath))
            {
                status.ValidationStatus = InjectionValidationStatus.ValidationFailed;
                status.ValidationErrors.Add("Target file not found");
                return status;
            }

            var content = await File.ReadAllTextAsync(targetPath, cancellationToken);
            var match = InjectionRegex.Match(content);

            if (!match.Success)
            {
                status.ValidationStatus = InjectionValidationStatus.NotInjected;
                return status;
            }

            status.IsInjected = true;

            // Extract fingerprint
            var fingerprintMatch = FingerprintRegex.Match(match.Value);
            if (fingerprintMatch.Success)
            {
                status.Fingerprint = fingerprintMatch.Groups[1].Value;
            }

            // Validate fingerprint if expected
            if (!string.IsNullOrEmpty(expectedFingerprint))
            {
                if (status.Fingerprint != expectedFingerprint)
                {
                    status.ValidationStatus = InjectionValidationStatus.FingerprintMismatch;
                    status.ValidationErrors.Add($"Fingerprint mismatch: expected {expectedFingerprint}, found {status.Fingerprint}");
                    return status;
                }
            }

            // Check if injection looks valid
            var snippet = match.Value;
            var containsMarker = snippet.Contains("Vivaldi Mod Manager", StringComparison.Ordinal);
            var containsScriptTag = snippet.Contains("<script", StringComparison.OrdinalIgnoreCase);
            var containsModuleType = snippet.Contains("type=\"module\"", StringComparison.OrdinalIgnoreCase);
            var containsSrc = snippet.Contains("src=\"", StringComparison.OrdinalIgnoreCase);
            var containsAwaitImport = snippet.Contains("await import", StringComparison.Ordinal);

            if (containsMarker && containsScriptTag && containsModuleType && (containsSrc || containsAwaitImport))
            {
                status.ValidationStatus = InjectionValidationStatus.Valid;
            }
            else
            {
                status.ValidationStatus = InjectionValidationStatus.Invalid;
                status.ValidationErrors.Add("Injection content appears corrupted");
            }
        }
        catch (Exception ex)
        {
            status.ValidationStatus = InjectionValidationStatus.ValidationFailed;
            status.ValidationErrors.Add($"Validation error: {ex.Message}");
        }

        return status;
    }

    /// <summary>
    /// Finds the best insertion point for the injection stub in HTML content.
    /// </summary>
    /// <param name="content">The HTML content.</param>
    /// <returns>The insertion point index, or -1 if not found.</returns>
    private static int FindInsertionPoint(string content)
    {
        // Try to find closing </body> tag first
        var bodyEnd = content.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyEnd != -1)
        {
            return bodyEnd;
        }

        // Fallback to closing </html> tag
        var htmlEnd = content.LastIndexOf("</html>", StringComparison.OrdinalIgnoreCase);
        if (htmlEnd != -1)
        {
            return htmlEnd;
        }

        // Last resort: end of file
        return content.Length;
    }

    /// <summary>
    /// Generates a backup path for a target file.
    /// </summary>
    /// <param name="targetPath">The target file path.</param>
    /// <param name="installationId">The installation ID.</param>
    /// <returns>The backup file path.</returns>
    private static string GenerateBackupPath(string targetPath, string installationId)
    {
        var directory = Path.GetDirectoryName(targetPath) ?? string.Empty;
        var filename = Path.GetFileName(targetPath);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        
        var backupFilename = string.IsNullOrEmpty(installationId) 
            ? $"{filename}{ManifestConstants.InjectionBackupSuffix}"
            : $"{filename}{ManifestConstants.InjectionBackupSuffix}_{installationId}_{timestamp}";

        return Path.Combine(directory, backupFilename);
    }

    /// <summary>
    /// Creates a backup of a file.
    /// </summary>
    /// <param name="sourcePath">The source file path.</param>
    /// <param name="backupPath">The backup file path.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CreateFileBackupAsync(string sourcePath, string backupPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        }

        // Ensure backup directory exists
        var backupDir = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
        {
            Directory.CreateDirectory(backupDir);
        }

        // Copy file with streaming to handle large files
        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var backupStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write);
        await sourceStream.CopyToAsync(backupStream, cancellationToken);

        _logger.LogDebug("Created backup: {SourcePath} -> {BackupPath}", sourcePath, backupPath);
    }

    /// <summary>
    /// Restores a file from backup.
    /// </summary>
    /// <param name="backupPath">The backup file path.</param>
    /// <param name="targetPath">The target file path.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RestoreFileFromBackupAsync(string backupPath, string targetPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException($"Backup file not found: {backupPath}");
        }

        // Ensure target directory exists
        var targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Use atomic write operation
        var tempPath = targetPath + ManifestConstants.TempFileSuffix;

        try
        {
            // Copy backup content to temp file
            using (var backupStream = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var tempStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                await backupStream.CopyToAsync(tempStream, cancellationToken);
            } // Ensure streams are disposed before file move

            // Atomic move to target location
            File.Move(tempPath, targetPath, overwrite: true);

            _logger.LogDebug("Restored from backup: {BackupPath} -> {TargetPath}", backupPath, targetPath);
        }
        finally
        {
            // Clean up temp file if it still exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temp file: {TempPath}", tempPath);
                }
            }
        }
    }

    /// <summary>
    /// Writes content to a file atomically.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task WriteFileAtomicallyAsync(string filePath, string content, CancellationToken cancellationToken)
    {
        var tempPath = filePath + ManifestConstants.TempFileSuffix;

        try
        {
            // Write to temp file first
            await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8, cancellationToken);

            // Atomic move to target location
            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            // Clean up temp file if it still exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temp file: {TempPath}", tempPath);
                }
            }
        }
    }
}

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Constants;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Extensions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Implementation of <see cref="ILoaderService"/> providing loader generation with JavaScript content creation,
/// configuration persistence, backup and restore capabilities, and content validation.
/// </summary>
public class LoaderService : ILoaderService
{
    private readonly ILogger<LoaderService> _logger;
    private readonly IManifestService _manifestService;
    private readonly IHashService _hashService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="manifestService">The manifest service for backup operations.</param>
    /// <param name="hashService">The hash service for content integrity verification.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public LoaderService(ILogger<LoaderService> logger, IManifestService manifestService, IHashService hashService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
    }

    /// <inheritdoc />
    public async Task<LoaderConfiguration> GenerateLoaderAsync(ManifestData manifest, string outputPath, CancellationToken cancellationToken = default)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentNullException(nameof(outputPath));
        }

        _logger.LogInformation("Generating loader at path: {OutputPath}", outputPath);

        try
        {
            // Generate the JavaScript content
            var jsContent = GenerateLoaderJavaScript(manifest);

            // Validate the generated content
            if (!ValidateLoaderContent(jsContent))
            {
                throw new LoaderValidationException("Generated JavaScript content failed validation");
            }

            // Create loader configuration
            var configuration = await CreateLoaderConfigurationAsync(manifest, cancellationToken).ConfigureAwait(false);
            
            // Calculate content hash
            configuration.ContentHash = _hashService.ComputeStringHash(jsContent);

            // Write to file using atomic operations (similar to ManifestService pattern)
            await WriteLoaderFileAsync(outputPath, jsContent, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully generated loader with {ModCount} enabled mods", 
                manifest.GetEnabledModsInOrder().Count());

            return configuration;
        }
        catch (Exception ex) when (!(ex is LoaderException))
        {
            _logger.LogError(ex, "Unexpected error generating loader at path: {OutputPath}", outputPath);
            throw new LoaderGenerationException(outputPath, "Unexpected error occurred during generation", ex);
        }
    }

    /// <inheritdoc />
    public async Task<LoaderConfiguration> CreateLoaderConfigurationAsync(ManifestData manifest, CancellationToken cancellationToken = default)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        _logger.LogDebug("Creating loader configuration from manifest");

        try
        {
            var enabledMods = manifest.GetEnabledModsInOrder().ToList();
            
            var configuration = new LoaderConfiguration
            {
                EnabledMods = enabledMods.Select(m => m.Filename).ToList(),
                Version = ManifestConstants.DefaultLoaderVersion,
                Fingerprint = await GenerateLoaderFingerprintAsync(manifest, cancellationToken).ConfigureAwait(false),
                GeneratedAt = DateTimeOffset.UtcNow,
                IsBackup = false,
                Options = new Dictionary<string, object>
                {
                    ["modCount"] = enabledMods.Count,
                    ["manifestVersion"] = manifest.SchemaVersion,
                    ["generatorVersion"] = ManifestConstants.DefaultLoaderVersion
                }
            };

            _logger.LogDebug("Created loader configuration with {ModCount} enabled mods", enabledMods.Count);
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loader configuration");
            throw new LoaderGenerationException("Failed to create loader configuration");
        }
    }

    /// <inheritdoc />
    public bool ValidateLoaderContent(string loaderContent)
    {
        if (loaderContent == null)
        {
            throw new ArgumentNullException(nameof(loaderContent));
        }

        _logger.LogDebug("Validating loader JavaScript content");

        var errors = new List<string>();

        try
        {
            // Basic structure validation
            if (string.IsNullOrWhiteSpace(loaderContent))
            {
                errors.Add("Loader content is empty or whitespace");
            }
            else
            {
                // Check for required components
                if (!loaderContent.Contains("LOADER_VERSION"))
                {
                    errors.Add("Missing LOADER_VERSION constant");
                }

                if (!loaderContent.Contains("LOADER_FINGERPRINT"))
                {
                    errors.Add("Missing LOADER_FINGERPRINT constant");
                }

                if (!loaderContent.Contains("GENERATED_AT"))
                {
                    errors.Add("Missing GENERATED_AT constant");
                }

                // Check for basic JavaScript syntax requirements
                if (!loaderContent.Contains("try") || !loaderContent.Contains("catch"))
                {
                    errors.Add("Missing error handling (try/catch) structure");
                }

                // Check for ES6 import syntax
                if (loaderContent.Contains("import(") && !loaderContent.Contains("await import("))
                {
                    errors.Add("Import statements should use await for proper error handling");
                }

                // Check for balanced braces (basic syntax check)
                var openBraces = loaderContent.Count(c => c == '{');
                var closeBraces = loaderContent.Count(c => c == '}');
                if (openBraces != closeBraces)
                {
                    errors.Add($"Unbalanced braces: {openBraces} open, {closeBraces} close");
                }
            }

            if (errors.Any())
            {
                _logger.LogWarning("Loader content validation failed with errors: {Errors}", string.Join(", ", errors));
                throw new LoaderValidationException(errors);
            }

            _logger.LogDebug("Loader content validation passed");
            return true;
        }
        catch (LoaderValidationException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during loader content validation");
            throw new LoaderValidationException("Unexpected validation error occurred");
        }
    }

    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(string loaderPath, string? backupPath = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loaderPath))
        {
            throw new ArgumentException("Loader path cannot be null or empty", nameof(loaderPath));
        }

        if (!File.Exists(loaderPath))
        {
            throw new FileNotFoundException($"Loader file not found at path: {loaderPath}");
        }

        _logger.LogInformation("Creating backup of loader file: {LoaderPath}", loaderPath);

        try
        {
            // Generate backup path if not provided
            backupPath ??= GenerateBackupPath(loaderPath);

            // Create backup directory if it doesn't exist
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // Copy file to backup location
            await File.WriteAllBytesAsync(backupPath, await File.ReadAllBytesAsync(loaderPath, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully created backup at path: {BackupPath}", backupPath);
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup of loader file: {LoaderPath}", loaderPath);
            throw new LoaderException($"Failed to create backup of loader file at '{loaderPath}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task RestoreFromBackupAsync(string backupPath, string targetPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            throw new ArgumentException("Backup path cannot be null or empty", nameof(backupPath));
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException("Target path cannot be null or empty", nameof(targetPath));
        }

        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException($"Backup file not found at path: {backupPath}");
        }

        _logger.LogInformation("Restoring loader from backup: {BackupPath} to {TargetPath}", backupPath, targetPath);

        try
        {
            // Create target directory if it doesn't exist
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Use atomic write operation (similar to ManifestService)
            var tempPath = targetPath + ManifestConstants.TempFileSuffix;
            
            try
            {
                // Read backup content and write to temp file
                var backupContent = await File.ReadAllBytesAsync(backupPath, cancellationToken).ConfigureAwait(false);
                await File.WriteAllBytesAsync(tempPath, backupContent, cancellationToken).ConfigureAwait(false);

                // Atomic move to target location
                File.Move(tempPath, targetPath, overwrite: true);

                _logger.LogInformation("Successfully restored loader from backup to: {TargetPath}", targetPath);
            }
            finally
            {
                // Clean up temp file if it exists
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to clean up temp file: {TempPath}", tempPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring loader from backup: {BackupPath}", backupPath);
            throw new LoaderException($"Failed to restore loader from backup at '{backupPath}'", ex);
        }
    }

    /// <inheritdoc />
    public string GenerateLoaderJavaScript(ManifestData manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        _logger.LogDebug("Generating JavaScript content for loader");

        try
        {
            var enabledMods = manifest.GetEnabledModsInOrder().ToList();
            var sb = new StringBuilder();

            // Header with metadata
            sb.AppendLine("// Vivaldi Mod Manager - Generated Loader");
            sb.AppendLine($"// Generated at: {DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            sb.AppendLine($"// Total enabled mods: {enabledMods.Count}");
            sb.AppendLine();

            // Constants
            sb.AppendLine($"const LOADER_VERSION = \"{ManifestConstants.DefaultLoaderVersion}\";");
            sb.AppendLine($"const LOADER_FINGERPRINT = \"{GenerateSimpleFingerprint(enabledMods)}\";");
            sb.AppendLine($"const GENERATED_AT = \"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\";");
            sb.AppendLine();

            // Main loading function
            sb.AppendLine("(async function loadMods() {");
            sb.AppendLine("  console.log('Vivaldi Mod Manager: Starting mod loading...');");
            sb.AppendLine("  console.log(`Loader version: ${LOADER_VERSION}, Generated: ${GENERATED_AT}`);");
            sb.AppendLine();

            if (enabledMods.Any())
            {
                sb.AppendLine("  const modsToLoad = [");
                foreach (var mod in enabledMods)
                {
                    // Escape filename for JavaScript string
                    var escapedFilename = EscapeJavaScriptString(mod.Filename);
                    sb.AppendLine($"    {{ filename: '{escapedFilename}', id: '{EscapeJavaScriptString(mod.Id)}' }},");
                }
                sb.AppendLine("  ];");
                sb.AppendLine();

                sb.AppendLine("  let loadedMods = 0;");
                sb.AppendLine("  let failedMods = 0;");
                sb.AppendLine();

                sb.AppendLine("  try {");
                sb.AppendLine("    for (const mod of modsToLoad) {");
                sb.AppendLine("      try {");
                sb.AppendLine("        console.log(`Loading mod: ${mod.filename}`);");
                sb.AppendLine("        await import(`./mods/${mod.filename}`);");
                sb.AppendLine("        loadedMods++;");
                sb.AppendLine("        console.log(`Successfully loaded mod: ${mod.filename}`);");
                sb.AppendLine("      } catch (error) {");
                sb.AppendLine("        failedMods++;");
                sb.AppendLine("        console.error(`Failed to load mod '${mod.filename}':`, error);");
                sb.AppendLine("      }");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    console.log(`Mod loading complete: ${loadedMods} loaded, ${failedMods} failed`);");
                sb.AppendLine("  } catch (error) {");
                sb.AppendLine("    console.error('Error during mod loading:', error);");
                sb.AppendLine("  }");
            }
            else
            {
                sb.AppendLine("  try {");
                sb.AppendLine("    console.log('No enabled mods to load');");
                sb.AppendLine("  } catch (error) {");
                sb.AppendLine("    console.error('Error in loader:', error);");
                sb.AppendLine("  }");
            }

            sb.AppendLine("})().catch(error => {");
            sb.AppendLine("  console.error('Critical error in mod loader:', error);");
            sb.AppendLine("});");

            var jsContent = sb.ToString();
            _logger.LogDebug("Generated JavaScript content with {Length} characters", jsContent.Length);

            return jsContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JavaScript content");
            throw new LoaderGenerationException("Failed to generate JavaScript content");
        }
    }

    /// <inheritdoc />
    public bool LoaderExists(string loaderPath)
    {
        return !string.IsNullOrWhiteSpace(loaderPath) && File.Exists(loaderPath);
    }

    /// <summary>
    /// Writes the loader JavaScript content to a file using atomic operations.
    /// </summary>
    /// <param name="outputPath">The path where the file should be written.</param>
    /// <param name="content">The JavaScript content to write.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task WriteLoaderFileAsync(string outputPath, string content, CancellationToken cancellationToken)
    {
        var tempPath = outputPath + ManifestConstants.TempFileSuffix;

        try
        {
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Write to temp file first
            await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            // Atomic move to final location
            File.Move(tempPath, outputPath, overwrite: true);

            _logger.LogDebug("Successfully wrote loader file to: {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up temp file: {TempPath}", tempPath);
                }
            }

            throw new LoaderGenerationException(outputPath, "Failed to write loader file", ex);
        }
    }

    /// <summary>
    /// Generates a fingerprint for the loader based on the manifest.
    /// </summary>
    /// <param name="manifest">The manifest data.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the fingerprint.</returns>
    private async Task<string> GenerateLoaderFingerprintAsync(ManifestData manifest, CancellationToken cancellationToken)
    {
        var enabledMods = manifest.GetEnabledModsInOrder().ToList();
        var fingerprintData = new
        {
            ModIds = enabledMods.Select(m => m.Id).ToList(),
            ModOrder = enabledMods.Select(m => new { m.Id, m.Order }).ToList(),
            Timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Version = ManifestConstants.DefaultLoaderVersion
        };

        var json = JsonSerializer.Serialize(fingerprintData);
        return await Task.FromResult(_hashService.ComputeStringHash(json)).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a simple fingerprint for enabled mods.
    /// </summary>
    /// <param name="enabledMods">The list of enabled mods.</param>
    /// <returns>A simple fingerprint string.</returns>
    private static string GenerateSimpleFingerprint(IEnumerable<ModInfo> enabledMods)
    {
        var modIds = enabledMods.Select(m => m.Id).OrderBy(id => id);
        var combined = string.Join("|", modIds);
        return combined.Length > 16 ? combined[..16] : combined.PadRight(16, '0');
    }

    /// <summary>
    /// Escapes a string for safe use in JavaScript.
    /// </summary>
    /// <param name="input">The input string to escape.</param>
    /// <returns>The escaped string.</returns>
    private static string EscapeJavaScriptString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return input
            .Replace("\\", "\\\\")  // Escape backslashes
            .Replace("'", "\\'")    // Escape single quotes
            .Replace("\"", "\\\"")  // Escape double quotes
            .Replace("\n", "\\n")   // Escape newlines
            .Replace("\r", "\\r")   // Escape carriage returns
            .Replace("\t", "\\t");  // Escape tabs
    }

    /// <summary>
    /// Generates a backup path for a loader file.
    /// </summary>
    /// <param name="loaderPath">The original loader file path.</param>
    /// <returns>The generated backup path.</returns>
    private static string GenerateBackupPath(string loaderPath)
    {
        var directory = Path.GetDirectoryName(loaderPath) ?? string.Empty;
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(loaderPath);
        var extension = Path.GetExtension(loaderPath);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");

        return Path.Combine(directory, $"{filenameWithoutExt}{ManifestConstants.LoaderBackupSuffix}_{timestamp}{extension}");
    }
}
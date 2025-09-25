using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Implementation of <see cref="IVivaldiService"/> providing Vivaldi browser detection and management
/// services for Windows environments. Handles registry scanning, version extraction, and installation validation.
/// </summary>
[SupportedOSPlatform("windows")]
public class VivaldiService : IVivaldiService
{
    private readonly ILogger<VivaldiService> _logger;
    private readonly IManifestService _manifestService;

    // Registry keys for Vivaldi detection
    private static readonly string[] RegistryKeys = {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

    // Common installation paths to scan
    private static readonly string[] CommonPaths = {
        @"%ProgramFiles%\Vivaldi",
        @"%LocalAppData%\Vivaldi",
        @"%AppData%\Vivaldi",
        @"%ProgramFiles(x86)%\Vivaldi"
    };

    // Portable installation locations
    private static readonly string[] PortablePaths = {
        @"C:\PortableApps\VivaldiPortable",
        @"D:\PortableApps\VivaldiPortable",
        @"%USERPROFILE%\Desktop\VivaldiPortable",
        @"%USERPROFILE%\Downloads\VivaldiPortable"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="manifestService">The manifest service for installation persistence.</param>
    public VivaldiService(ILogger<VivaldiService> logger, IManifestService manifestService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VivaldiInstallation>> DetectInstallationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting Vivaldi installation detection");
        
        var installations = new List<VivaldiInstallation>();
        var detectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Scan registry for standard installations
            await ScanRegistryForInstallationsAsync(installations, detectedPaths, cancellationToken);

            // Scan common paths for any missed installations
            await ScanCommonPathsAsync(installations, detectedPaths, cancellationToken);

            // Scan for portable installations
            await ScanPortablePathsAsync(installations, detectedPaths, cancellationToken);

            _logger.LogInformation("Detected {Count} Vivaldi installation(s)", installations.Count);
            
            return installations.AsReadOnly();
        }
        catch (Exception ex) when (!(ex is VivaldiException) && !(ex is OperationCanceledException))
        {
            _logger.LogError(ex, "Unexpected error during Vivaldi installation detection");
            throw new VivaldiDetectionException($"Unexpected error during installation detection: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<VivaldiInstallation?> GetInstallationInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        try
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(path);
            if (!Directory.Exists(expandedPath))
            {
                _logger.LogDebug("Path does not exist: {Path}", expandedPath);
                return null;
            }

            _logger.LogDebug("Analyzing Vivaldi installation at: {Path}", expandedPath);

            var installation = await AnalyzeInstallationAsync(expandedPath, cancellationToken);
            if (installation != null)
            {
                _logger.LogDebug("Successfully analyzed installation: {Name} v{Version}", 
                    installation.Name, installation.Version);
            }

            return installation;
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _logger.LogError(ex, "Error analyzing installation at path: {Path}", path);
            throw new VivaldiDetectionException(path, "analyze", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateInstallationAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        try
        {
            _logger.LogDebug("Validating installation: {Name} at {Path}", 
                installation.Name, installation.InstallationPath);

            // Check if installation directory exists
            if (!Directory.Exists(installation.InstallationPath))
            {
                _logger.LogWarning("Installation directory does not exist: {Path}", installation.InstallationPath);
                return false;
            }

            // Check if application directory exists
            if (!Directory.Exists(installation.ApplicationPath))
            {
                _logger.LogWarning("Application directory does not exist: {Path}", installation.ApplicationPath);
                return false;
            }

            // Find and validate the main executable
            var executablePath = await FindExecutableAsync(installation.ApplicationPath, cancellationToken);
            if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
            {
                _logger.LogWarning("Vivaldi executable not found in: {Path}", installation.ApplicationPath);
                return false;
            }

            // Validate version matches what we expect
            var currentVersion = await GetVersionAsync(executablePath, cancellationToken);
            if (string.IsNullOrEmpty(currentVersion))
            {
                _logger.LogWarning("Could not determine version for executable: {Path}", executablePath);
                return false;
            }

            // Check if version has changed
            if (!string.Equals(installation.Version, currentVersion, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Version mismatch for installation {Name}. Expected: {Expected}, Found: {Found}",
                    installation.Name, installation.Version, currentVersion);
                return false;
            }

            // Validate user data directory if specified
            if (!string.IsNullOrEmpty(installation.UserDataPath) && !Directory.Exists(installation.UserDataPath))
            {
                _logger.LogWarning("User data directory does not exist: {Path}", installation.UserDataPath);
                return false;
            }

            _logger.LogDebug("Installation validation successful: {Name}", installation.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating installation: {Name}", installation.Name);
            throw new VivaldiException($"Validation failed for installation '{installation.Name}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task<string?> GetVersionAsync(string executablePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(executablePath));
        }

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException($"Executable file not found: {executablePath}");
        }

        try
        {
            _logger.LogDebug("Extracting version from: {Path}", executablePath);

            // Use FileVersionInfo to extract version information
            var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            var version = versionInfo.ProductVersion ?? versionInfo.FileVersion;

            if (!string.IsNullOrEmpty(version))
            {
                // Clean up version string (remove any trailing information)
                var cleanVersion = version.Split(' ')[0];
                _logger.LogDebug("Extracted version: {Version} from {Path}", cleanVersion, executablePath);
                return Task.FromResult<string?>(cleanVersion);
            }

            _logger.LogWarning("Could not extract version from: {Path}", executablePath);
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting version from: {Path}", executablePath);
            throw new VivaldiException($"Failed to extract version from '{executablePath}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> FindInjectionTargetsAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        try
        {
            _logger.LogDebug("Finding injection targets for: {Name}", installation.Name);

            var targets = new Dictionary<string, string>();

            // Look for resources/vivaldi directory
            var resourcesPath = Path.Combine(installation.ApplicationPath, "resources", "vivaldi");
            if (!Directory.Exists(resourcesPath))
            {
                // Try alternative path structure
                var versionDirectories = Directory.GetDirectories(installation.ApplicationPath)
                    .Where(d => Version.TryParse(Path.GetFileName(d), out _))
                    .OrderByDescending(d => Path.GetFileName(d))
                    .ToArray();

                if (versionDirectories.Length > 0)
                {
                    resourcesPath = Path.Combine(versionDirectories[0], "resources", "vivaldi");
                }
            }

            if (Directory.Exists(resourcesPath))
            {
                // Look for window.html
                var windowHtmlPath = Path.Combine(resourcesPath, "window.html");
                if (File.Exists(windowHtmlPath))
                {
                    targets["window.html"] = windowHtmlPath;
                    _logger.LogDebug("Found window.html at: {Path}", windowHtmlPath);
                }

                // Look for browser.html
                var browserHtmlPath = Path.Combine(resourcesPath, "browser.html");
                if (File.Exists(browserHtmlPath))
                {
                    targets["browser.html"] = browserHtmlPath;
                    _logger.LogDebug("Found browser.html at: {Path}", browserHtmlPath);
                }
            }

            if (targets.Count == 0)
            {
                _logger.LogWarning("No injection targets found for installation: {Name}", installation.Name);
                throw new VivaldiException($"No injection targets found for installation '{installation.Name}'");
            }

            _logger.LogDebug("Found {Count} injection target(s) for: {Name}", targets.Count, installation.Name);
            return targets.AsReadOnly();
        }
        catch (Exception ex) when (!(ex is VivaldiException))
        {
            _logger.LogError(ex, "Error finding injection targets for: {Name}", installation.Name);
            throw new VivaldiException($"Failed to find injection targets for '{installation.Name}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public bool IsInstallationCompatible(VivaldiInstallation installation, string minVersion)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        if (string.IsNullOrWhiteSpace(minVersion))
        {
            throw new ArgumentException("Minimum version cannot be null or empty.", nameof(minVersion));
        }

        try
        {
            if (string.IsNullOrEmpty(installation.Version))
            {
                _logger.LogWarning("Installation {Name} has no version information", installation.Name);
                return false;
            }

            if (Version.TryParse(installation.Version, out var installationVersion) &&
                Version.TryParse(minVersion, out var minimumVersion))
            {
                var isCompatible = installationVersion >= minimumVersion;
                _logger.LogDebug("Compatibility check for {Name}: {InstallationVersion} >= {MinVersion} = {IsCompatible}",
                    installation.Name, installationVersion, minimumVersion, isCompatible);
                return isCompatible;
            }

            _logger.LogWarning("Could not parse version strings for compatibility check. Installation: {InstallationVersion}, Required: {MinVersion}",
                installation.Version, minVersion);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking compatibility for installation: {Name}", installation.Name);
            throw new VivaldiException($"Failed to check compatibility for '{installation.Name}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<VivaldiInstallation> RefreshInstallationAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        try
        {
            _logger.LogDebug("Refreshing installation: {Name} at {Path}", 
                installation.Name, installation.InstallationPath);

            // Get fresh installation info
            var refreshedInfo = await GetInstallationInfoAsync(installation.InstallationPath, cancellationToken);
            if (refreshedInfo == null)
            {
                throw new VivaldiException($"Could not refresh installation '{installation.Name}' - path no longer contains a valid installation");
            }

            // Preserve important metadata from original installation
            refreshedInfo.Id = installation.Id;
            refreshedInfo.IsManaged = installation.IsManaged;
            refreshedInfo.IsActive = installation.IsActive;
            refreshedInfo.DetectedAt = installation.DetectedAt;
            refreshedInfo.LastInjectionAt = installation.LastInjectionAt;
            refreshedInfo.LastInjectionStatus = installation.LastInjectionStatus;
            refreshedInfo.InjectionFingerprint = installation.InjectionFingerprint;

            // Update verification timestamp
            refreshedInfo.LastVerifiedAt = DateTimeOffset.UtcNow;

            // Copy any custom metadata
            foreach (var kvp in installation.Metadata)
            {
                if (!refreshedInfo.Metadata.ContainsKey(kvp.Key))
                {
                    refreshedInfo.Metadata[kvp.Key] = kvp.Value;
                }
            }

            _logger.LogInformation("Successfully refreshed installation: {Name} v{Version}", 
                refreshedInfo.Name, refreshedInfo.Version);

            return refreshedInfo;
        }
        catch (Exception ex) when (!(ex is VivaldiException))
        {
            _logger.LogError(ex, "Error refreshing installation: {Name}", installation.Name);
            throw new VivaldiException($"Failed to refresh installation '{installation.Name}': {ex.Message}", ex);
        }
    }

    // Private helper methods

    private async Task ScanRegistryForInstallationsAsync(List<VivaldiInstallation> installations, HashSet<string> detectedPaths, CancellationToken cancellationToken)
    {
        foreach (var registryKey in RegistryKeys)
        {
            await ScanRegistryKeyAsync(RegistryHive.LocalMachine, registryKey, installations, detectedPaths, cancellationToken);
            await ScanRegistryKeyAsync(RegistryHive.CurrentUser, registryKey, installations, detectedPaths, cancellationToken);
        }
    }

    private async Task ScanRegistryKeyAsync(RegistryHive hive, string keyPath, List<VivaldiInstallation> installations, HashSet<string> detectedPaths, CancellationToken cancellationToken)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var uninstallKey = baseKey.OpenSubKey(keyPath);
            
            if (uninstallKey == null)
            {
                return;
            }

            var subKeyNames = uninstallKey.GetSubKeyNames();
            foreach (var subKeyName in subKeyNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!subKeyName.Contains("Vivaldi", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var subKey = uninstallKey.OpenSubKey(subKeyName);
                if (subKey == null) continue;

                var displayName = subKey.GetValue("DisplayName") as string;
                var installLocation = subKey.GetValue("InstallLocation") as string;

                if (!string.IsNullOrEmpty(displayName) && 
                    displayName.Contains("Vivaldi", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(installLocation) &&
                    Directory.Exists(installLocation))
                {
                    var normalizedPath = Path.GetFullPath(installLocation).TrimEnd(Path.DirectorySeparatorChar);
                    if (detectedPaths.Add(normalizedPath))
                    {
                        var installation = await AnalyzeInstallationAsync(installLocation, cancellationToken);
                        if (installation != null)
                        {
                            installations.Add(installation);
                            _logger.LogDebug("Found Vivaldi installation via registry: {Name} at {Path}", 
                                installation.Name, installation.InstallationPath);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning registry key: {Hive}\\{KeyPath}", hive, keyPath);
        }
    }

    private async Task ScanCommonPathsAsync(List<VivaldiInstallation> installations, HashSet<string> detectedPaths, CancellationToken cancellationToken)
    {
        foreach (var path in CommonPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ScanPathAsync(path, installations, detectedPaths, VivaldiInstallationType.Standard, cancellationToken);
        }
    }

    private async Task ScanPortablePathsAsync(List<VivaldiInstallation> installations, HashSet<string> detectedPaths, CancellationToken cancellationToken)
    {
        foreach (var path in PortablePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ScanPathAsync(path, installations, detectedPaths, VivaldiInstallationType.Portable, cancellationToken);
        }
    }

    private async Task ScanPathAsync(string path, List<VivaldiInstallation> installations, HashSet<string> detectedPaths, VivaldiInstallationType expectedType, CancellationToken cancellationToken)
    {
        try
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(path);
            if (!Directory.Exists(expandedPath))
            {
                return;
            }

            var normalizedPath = Path.GetFullPath(expandedPath).TrimEnd(Path.DirectorySeparatorChar);
            if (detectedPaths.Add(normalizedPath))
            {
                var installation = await AnalyzeInstallationAsync(expandedPath, cancellationToken);
                if (installation != null)
                {
                    // Override installation type if we have a better guess
                    if (installation.InstallationType == VivaldiInstallationType.Standard && expectedType != VivaldiInstallationType.Standard)
                    {
                        installation.InstallationType = expectedType;
                    }

                    installations.Add(installation);
                    _logger.LogDebug("Found Vivaldi installation via path scan: {Name} at {Path}", 
                        installation.Name, installation.InstallationPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error scanning path: {Path}", path);
        }
    }

    private async Task<VivaldiInstallation?> AnalyzeInstallationAsync(string installationPath, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedPath = Path.GetFullPath(installationPath).TrimEnd(Path.DirectorySeparatorChar);
            
            // Try to find the application directory
            var applicationPath = await FindApplicationDirectoryAsync(normalizedPath, cancellationToken);
            if (string.IsNullOrEmpty(applicationPath))
            {
                _logger.LogDebug("Could not find application directory in: {Path}", normalizedPath);
                return null;
            }

            // Try to find the executable
            var executablePath = await FindExecutableAsync(applicationPath, cancellationToken);
            if (string.IsNullOrEmpty(executablePath))
            {
                _logger.LogDebug("Could not find Vivaldi executable in: {Path}", applicationPath);
                return null;
            }

            // Extract version
            var version = await GetVersionAsync(executablePath, cancellationToken);
            if (string.IsNullOrEmpty(version))
            {
                _logger.LogDebug("Could not extract version from: {Path}", executablePath);
                return null;
            }

            // Determine installation type
            var installationType = DetermineInstallationType(normalizedPath, applicationPath);

            // Determine user data path
            var userDataPath = DetermineUserDataPath(normalizedPath, installationType);

            // Generate installation details
            var installation = new VivaldiInstallation
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = GenerateInstallationName(installationType, version, normalizedPath),
                InstallationPath = normalizedPath,
                ApplicationPath = applicationPath,
                UserDataPath = userDataPath,
                Version = version,
                InstallationType = installationType,
                DetectedAt = DateTimeOffset.UtcNow,
                LastVerifiedAt = DateTimeOffset.UtcNow
            };

            return installation;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error analyzing installation at: {Path}", installationPath);
            return null;
        }
    }

    private Task<string?> FindApplicationDirectoryAsync(string installationPath, CancellationToken cancellationToken)
    {
        // Check for Application subdirectory (standard installation)
        var appDir = Path.Combine(installationPath, "Application");
        if (Directory.Exists(appDir))
        {
            return Task.FromResult<string?>(appDir);
        }

        // Check if the installation path itself contains the executable (portable)
        var executableInRoot = Path.Combine(installationPath, "vivaldi.exe");
        if (File.Exists(executableInRoot))
        {
            return Task.FromResult<string?>(installationPath);
        }

        // Look for version directories
        try
        {
            var directories = Directory.GetDirectories(installationPath)
                .Where(d => Version.TryParse(Path.GetFileName(d), out _))
                .OrderByDescending(d => Path.GetFileName(d))
                .ToArray();

            foreach (var versionDir in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var executableInVersion = Path.Combine(versionDir, "vivaldi.exe");
                if (File.Exists(executableInVersion))
                {
                    return Task.FromResult<string?>(versionDir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching for version directories in: {Path}", installationPath);
        }

        return Task.FromResult<string?>(null);
    }

    private Task<string?> FindExecutableAsync(string applicationPath, CancellationToken cancellationToken)
    {
        var executablePath = Path.Combine(applicationPath, "vivaldi.exe");
        if (File.Exists(executablePath))
        {
            return Task.FromResult<string?>(executablePath);
        }

        // Look in subdirectories for the executable
        try
        {
            var files = Directory.GetFiles(applicationPath, "vivaldi.exe", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return Task.FromResult<string?>(files[0]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching for executable in: {Path}", applicationPath);
        }

        return Task.FromResult<string?>(null);
    }

    private static VivaldiInstallationType DetermineInstallationType(string installationPath, string applicationPath)
    {
        var installationName = Path.GetFileName(installationPath).ToLowerInvariant();
        
        if (installationName.Contains("snapshot") || installationName.Contains("dev"))
        {
            return VivaldiInstallationType.Snapshot;
        }

        if (installationName.Contains("portable") || installationPath.Contains("PortableApps"))
        {
            return VivaldiInstallationType.Portable;
        }

        // If executable is directly in installation path, it's likely portable
        if (string.Equals(installationPath.TrimEnd(Path.DirectorySeparatorChar), 
                         applicationPath.TrimEnd(Path.DirectorySeparatorChar), 
                         StringComparison.OrdinalIgnoreCase))
        {
            return VivaldiInstallationType.Portable;
        }

        return VivaldiInstallationType.Standard;
    }

    private static string DetermineUserDataPath(string installationPath, VivaldiInstallationType installationType)
    {
        if (installationType == VivaldiInstallationType.Portable)
        {
            var userDataPath = Path.Combine(installationPath, "User Data");
            if (Directory.Exists(userDataPath))
            {
                return userDataPath;
            }
        }

        // Default user data location
        var defaultUserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vivaldi", "User Data");
        return defaultUserData;
    }

    private static string GenerateInstallationName(VivaldiInstallationType installationType, string version, string installationPath)
    {
        var baseName = installationType switch
        {
            VivaldiInstallationType.Snapshot => "Vivaldi Snapshot",
            VivaldiInstallationType.Portable => "Vivaldi Portable",
            _ => "Vivaldi"
        };

        var pathInfo = "";
        if (installationType == VivaldiInstallationType.Portable)
        {
            var parentDir = Path.GetFileName(Path.GetDirectoryName(installationPath));
            if (!string.IsNullOrEmpty(parentDir) && !parentDir.Equals("PortableApps", StringComparison.OrdinalIgnoreCase))
            {
                pathInfo = $" ({parentDir})";
            }
        }

        return $"{baseName} {version}{pathInfo}";
    }
}
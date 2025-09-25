using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Provides services for detecting, discovering, and managing Vivaldi browser installations on Windows.
/// Handles registry scanning, version extraction, installation validation, and path resolution for mod injection.
/// </summary>
public interface IVivaldiService
{
    /// <summary>
    /// Detects all Vivaldi installations on the system asynchronously.
    /// Scans registry entries and common installation paths to find Standard, Portable, and Snapshot installations.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of detected Vivaldi installations.</returns>
    /// <exception cref="VivaldiDetectionException">Thrown when detection fails due to registry or file system errors.</exception>
    Task<IReadOnlyList<VivaldiInstallation>> DetectInstallationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific Vivaldi installation path asynchronously.
    /// Extracts version, installation type, and validates the installation structure.
    /// </summary>
    /// <param name="path">The path to the Vivaldi installation directory.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>Detailed installation information, or null if the path is not a valid Vivaldi installation.</returns>
    /// <exception cref="ArgumentException">Thrown when path is null or empty.</exception>
    /// <exception cref="VivaldiDetectionException">Thrown when there's an error analyzing the installation.</exception>
    Task<VivaldiInstallation?> GetInstallationInfoAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a Vivaldi installation to ensure it's complete and functional asynchronously.
    /// Checks for required files, proper directory structure, and executable integrity.
    /// </summary>
    /// <param name="installation">The installation to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>True if the installation is valid and functional; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="VivaldiException">Thrown when validation encounters unexpected errors.</exception>
    Task<bool> ValidateInstallationAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the version from a Vivaldi executable file asynchronously.
    /// Parses PE file properties or uses FileVersionInfo to extract version information.
    /// </summary>
    /// <param name="executablePath">The path to the vivaldi.exe file.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The version string, or null if the version cannot be determined.</returns>
    /// <exception cref="ArgumentException">Thrown when executablePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the executable file is not found.</exception>
    /// <exception cref="VivaldiException">Thrown when version extraction fails.</exception>
    Task<string?> GetVersionAsync(string executablePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds injection target files (window.html, browser.html) for a Vivaldi installation asynchronously.
    /// Locates the HTML files within the resources/vivaldi directory for mod injection.
    /// </summary>
    /// <param name="installation">The installation to find injection targets for.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A dictionary mapping target names to file paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="VivaldiException">Thrown when injection targets cannot be found or accessed.</exception>
    Task<IReadOnlyDictionary<string, string>> FindInjectionTargetsAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a Vivaldi installation is compatible with a minimum required version.
    /// Performs semantic version comparison to determine compatibility.
    /// </summary>
    /// <param name="installation">The installation to check for compatibility.</param>
    /// <param name="minVersion">The minimum required version string.</param>
    /// <returns>True if the installation version meets or exceeds the minimum version; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="ArgumentException">Thrown when minVersion is null or empty.</exception>
    /// <exception cref="VivaldiException">Thrown when version comparison fails due to invalid version formats.</exception>
    bool IsInstallationCompatible(VivaldiInstallation installation, string minVersion);

    /// <summary>
    /// Refreshes the metadata for an existing installation asynchronously.
    /// Updates version information, validates paths, and refreshes installation status.
    /// </summary>
    /// <param name="installation">The installation to refresh.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The updated installation with refreshed metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="VivaldiException">Thrown when the installation cannot be refreshed or is no longer valid.</exception>
    Task<VivaldiInstallation> RefreshInstallationAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);
}
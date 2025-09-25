using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Provides services for injecting mod loader stubs into Vivaldi HTML files.
/// Handles JavaScript injection, fingerprint management, backup operations, and integrity validation.
/// </summary>
public interface IInjectionService
{
    /// <summary>
    /// Injects the mod loader stub into all injection targets for the specified Vivaldi installation.
    /// </summary>
    /// <param name="installation">The Vivaldi installation to inject into.</param>
    /// <param name="loaderPath">The path to the loader.js file to be imported by the stub.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation or loaderPath is null.</exception>
    /// <exception cref="InjectionException">Thrown when injection operation fails.</exception>
    Task InjectAsync(VivaldiInstallation installation, string loaderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the mod loader stub from all injection targets for the specified Vivaldi installation.
    /// </summary>
    /// <param name="installation">The Vivaldi installation to remove injection from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="InjectionException">Thrown when injection removal fails.</exception>
    Task RemoveInjectionAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the integrity of existing injections for the specified Vivaldi installation.
    /// </summary>
    /// <param name="installation">The Vivaldi installation to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns true if injections are valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="InjectionValidationException">Thrown when validation fails.</exception>
    Task<bool> ValidateInjectionAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates backups of all injection target files for the specified Vivaldi installation.
    /// </summary>
    /// <param name="installation">The Vivaldi installation to backup files for.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns a dictionary of target names to backup paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="InjectionBackupException">Thrown when backup creation fails.</exception>
    Task<IReadOnlyDictionary<string, string>> BackupTargetFilesAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores injection target files from backups for the specified Vivaldi installation.
    /// </summary>
    /// <param name="installation">The Vivaldi installation to restore files for.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    /// <exception cref="InjectionBackupException">Thrown when restore operation fails.</exception>
    Task RestoreTargetFilesAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current injection status for the specified Vivaldi installation.
    /// </summary>
    /// <param name="installation">The Vivaldi installation to get status for.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the injection status.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation is null.</exception>
    Task<InjectionStatus> GetInjectionStatusAsync(VivaldiInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Repairs broken injections for the specified Vivaldi installation.
    /// </summary>
    /// <param name="installation">The Vivaldi installation to repair.</param>
    /// <param name="loaderPath">The path to the loader.js file to be imported by the stub.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>  
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when installation or loaderPath is null.</exception>
    /// <exception cref="InjectionException">Thrown when repair operation fails.</exception>
    Task RepairInjectionAsync(VivaldiInstallation installation, string loaderPath, CancellationToken cancellationToken = default);
}
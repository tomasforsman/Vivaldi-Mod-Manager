using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Provides services for generating and managing loader.js files that dynamically import enabled mods.
/// Handles JavaScript generation, configuration persistence, backup/restore, and validation.
/// </summary>
public interface ILoaderService
{
    /// <summary>
    /// Generates the loader.js file asynchronously based on the manifest configuration.
    /// </summary>
    /// <param name="manifest">The manifest data containing enabled mods and configuration.</param>
    /// <param name="outputPath">The path where the loader.js file should be written.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the generated loader configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when manifest or outputPath is null.</exception>
    /// <exception cref="LoaderGenerationException">Thrown when loader generation fails.</exception>
    /// <exception cref="LoaderValidationException">Thrown when generated content validation fails.</exception>
    Task<LoaderConfiguration> GenerateLoaderAsync(ManifestData manifest, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a loader configuration object based on the manifest data without writing files.
    /// </summary>
    /// <param name="manifest">The manifest data containing enabled mods and configuration.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the loader configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when manifest is null.</exception>
    /// <exception cref="LoaderGenerationException">Thrown when configuration creation fails.</exception>
    Task<LoaderConfiguration> CreateLoaderConfigurationAsync(ManifestData manifest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the content of a generated loader JavaScript file.
    /// </summary>
    /// <param name="loaderContent">The JavaScript content to validate.</param>
    /// <returns>True if the loader content is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when loaderContent is null.</exception>
    /// <exception cref="LoaderValidationException">Thrown when validation fails with specific errors.</exception>
    bool ValidateLoaderContent(string loaderContent);

    /// <summary>
    /// Creates a backup of an existing loader file asynchronously.
    /// </summary>
    /// <param name="loaderPath">The path to the loader file to backup.</param>
    /// <param name="backupPath">Optional custom backup path. If null, a default backup path will be generated.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the path to the created backup file.</returns>
    /// <exception cref="ArgumentException">Thrown when loaderPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the loader file to backup is not found.</exception>
    /// <exception cref="LoaderException">Thrown when backup creation fails.</exception>
    Task<string> CreateBackupAsync(string loaderPath, string? backupPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a loader file from a backup asynchronously.
    /// </summary>
    /// <param name="backupPath">The path to the backup file to restore from.</param>
    /// <param name="targetPath">The path where the loader file should be restored.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when backupPath or targetPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the backup file is not found.</exception>
    /// <exception cref="LoaderException">Thrown when restore operation fails.</exception>
    Task RestoreFromBackupAsync(string backupPath, string targetPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the JavaScript content for the loader based on enabled mods.
    /// </summary>
    /// <param name="manifest">The manifest data containing enabled mods.</param>
    /// <returns>The generated JavaScript content as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when manifest is null.</exception>
    /// <exception cref="LoaderGenerationException">Thrown when JavaScript generation fails.</exception>
    string GenerateLoaderJavaScript(ManifestData manifest);

    /// <summary>
    /// Checks if a loader file exists at the specified path.
    /// </summary>
    /// <param name="loaderPath">The path to check for the loader file.</param>
    /// <returns>True if the loader file exists; otherwise, false.</returns>
    bool LoaderExists(string loaderPath);
}
namespace VivaldiModManager.Core.Services;

/// <summary>
/// Provides file integrity services for computing and verifying checksums.
/// </summary>
public interface IHashService
{
    /// <summary>
    /// Computes the SHA256 hash of the specified file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file to hash.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the SHA256 hash of the specified string.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    string ComputeStringHash(string input);

    /// <summary>
    /// Verifies that the specified file's hash matches the expected hash.
    /// </summary>
    /// <param name="filePath">The path to the file to verify.</param>
    /// <param name="expectedHash">The expected SHA256 hash as a hexadecimal string.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>True if the file's hash matches the expected hash; otherwise, false.</returns>
    Task<bool> VerifyFileHashAsync(string filePath, string expectedHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the SHA256 hash of the specified byte array.
    /// </summary>
    /// <param name="data">The byte array to hash.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    string ComputeByteArrayHash(byte[] data);
}
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace VivaldiModManager.Core.Services;

/// <summary>
/// Implementation of <see cref="IHashService"/> providing SHA256 hash computation and verification.
/// </summary>
public class HashService : IHashService
{
    private readonly ILogger<HashService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public HashService(ILogger<HashService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        try
        {
            _logger.LogDebug("Computing SHA256 hash for file: {FilePath}", filePath);

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var sha256 = SHA256.Create();
            
            var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            _logger.LogDebug("Computed SHA256 hash for file {FilePath}: {Hash}", filePath, hash);
            return hash;
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            _logger.LogError(ex, "Error computing hash for file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public string ComputeStringHash(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        try
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            return ComputeByteArrayHash(inputBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing hash for string input");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            throw new ArgumentException("Expected hash cannot be null or empty.", nameof(expectedHash));
        }

        try
        {
            var actualHash = await ComputeFileHashAsync(filePath, cancellationToken);
            var matches = string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);

            _logger.LogDebug("Hash verification for file {FilePath}: Expected={ExpectedHash}, Actual={ActualHash}, Matches={Matches}",
                filePath, expectedHash, actualHash, matches);

            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying hash for file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public string ComputeByteArrayHash(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing hash for byte array");
            throw;
        }
    }
}
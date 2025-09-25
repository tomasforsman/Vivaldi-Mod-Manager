using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Services;
using Xunit;

namespace VivaldiModManager.Core.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="HashService"/> class.
/// </summary>
public class HashServiceTests : IDisposable
{
    private readonly Mock<ILogger<HashService>> _loggerMock;
    private readonly HashService _hashService;
    private readonly string _tempDirectory;

    public HashServiceTests()
    {
        _loggerMock = new Mock<ILogger<HashService>>();
        _hashService = new HashService(_loggerMock.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HashService(null!));
    }

    [Fact]
    public async Task ComputeFileHashAsync_WithValidFile_ReturnsCorrectHash()
    {
        // Arrange
        var testContent = "Hello, World!";
        var filePath = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, testContent);

        // Expected SHA256 hash of "Hello, World!"
        var expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f";

        // Act
        var actualHash = await _hashService.ComputeFileHashAsync(filePath);

        // Assert
        actualHash.Should().Be(expectedHash);
    }

    [Fact]
    public async Task ComputeFileHashAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.txt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _hashService.ComputeFileHashAsync(nonExistentPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ComputeFileHashAsync_WithInvalidPath_ThrowsArgumentException(string? filePath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _hashService.ComputeFileHashAsync(filePath!));
    }

    [Fact]
    public void ComputeStringHash_WithValidString_ReturnsCorrectHash()
    {
        // Arrange
        var input = "Hello, World!";
        var expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f";

        // Act
        var actualHash = _hashService.ComputeStringHash(input);

        // Assert
        actualHash.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeStringHash_WithEmptyString_ReturnsCorrectHash()
    {
        // Arrange
        var input = "";
        var expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var actualHash = _hashService.ComputeStringHash(input);

        // Assert
        actualHash.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeStringHash_WithNullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _hashService.ComputeStringHash(null!));
    }

    [Fact]
    public void ComputeByteArrayHash_WithValidByteArray_ReturnsCorrectHash()
    {
        // Arrange
        var input = Encoding.UTF8.GetBytes("Hello, World!");
        var expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f";

        // Act
        var actualHash = _hashService.ComputeByteArrayHash(input);

        // Assert
        actualHash.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeByteArrayHash_WithEmptyByteArray_ReturnsCorrectHash()
    {
        // Arrange
        var input = Array.Empty<byte>();
        var expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var actualHash = _hashService.ComputeByteArrayHash(input);

        // Assert
        actualHash.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeByteArrayHash_WithNullByteArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _hashService.ComputeByteArrayHash(null!));
    }

    [Fact]
    public async Task VerifyFileHashAsync_WithMatchingHash_ReturnsTrue()
    {
        // Arrange
        var testContent = "Hello, World!";
        var filePath = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, testContent);
        var expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f";

        // Act
        var result = await _hashService.VerifyFileHashAsync(filePath, expectedHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyFileHashAsync_WithNonMatchingHash_ReturnsFalse()
    {
        // Arrange
        var testContent = "Hello, World!";
        var filePath = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, testContent);
        var wrongHash = "1234567890abcdef";

        // Act
        var result = await _hashService.VerifyFileHashAsync(filePath, wrongHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyFileHashAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.txt");
        var someHash = "1234567890abcdef";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _hashService.VerifyFileHashAsync(nonExistentPath, someHash));
    }

    [Theory]
    [InlineData("", "validhash")]
    [InlineData("   ", "validhash")]
    [InlineData(null, "validhash")]
    [InlineData("validpath", "")]
    [InlineData("validpath", "   ")]
    [InlineData("validpath", null)]
    public async Task VerifyFileHashAsync_WithInvalidParameters_ThrowsArgumentException(string? filePath, string? expectedHash)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _hashService.VerifyFileHashAsync(filePath!, expectedHash!));
    }

    [Fact]
    public async Task ComputeFileHashAsync_WithLargeFile_ComputesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "largefile.txt");
        var content = new string('A', 10000); // 10KB of 'A's
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var hash1 = await _hashService.ComputeFileHashAsync(filePath);
        var hash2 = await _hashService.ComputeFileHashAsync(filePath);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2); // Should be deterministic
        hash1.Should().HaveLength(64); // SHA256 produces 64 character hex string
    }

    [Fact]
    public async Task ComputeFileHashAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, "test content");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException inherits from OperationCanceledException
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            _hashService.ComputeFileHashAsync(filePath, cts.Token));
        
        // Verify it's the expected cancellation behavior
        exception.Should().BeOfType<TaskCanceledException>();
    }

    [Fact]
    public void ComputeStringHash_ProducesConsistentResults()
    {
        // Arrange
        var input = "Test String";

        // Act
        var hash1 = _hashService.ComputeStringHash(input);
        var hash2 = _hashService.ComputeStringHash(input);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64); // SHA256 produces 64 character hex string
    }

    [Fact]
    public void ComputeStringHash_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var input = "Hello 世界 🌍";

        // Act
        var hash = _hashService.ComputeStringHash(input);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
    }
}
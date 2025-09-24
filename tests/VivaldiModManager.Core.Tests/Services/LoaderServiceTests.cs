using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Constants;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;

namespace VivaldiModManager.Core.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="LoaderService"/> class.
/// </summary>
public sealed class LoaderServiceTests : IDisposable
{
    private readonly Mock<ILogger<LoaderService>> _loggerMock;
    private readonly Mock<IManifestService> _manifestServiceMock;
    private readonly Mock<IHashService> _hashServiceMock;
    private readonly LoaderService _loaderService;
    private readonly string _tempDirectory;

    public LoaderServiceTests()
    {
        _loggerMock = new Mock<ILogger<LoaderService>>();
        _manifestServiceMock = new Mock<IManifestService>();
        _hashServiceMock = new Mock<IHashService>();
        _loaderService = new LoaderService(_loggerMock.Object, _manifestServiceMock.Object, _hashServiceMock.Object);
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
        Assert.Throws<ArgumentNullException>(() => 
            new LoaderService(null!, _manifestServiceMock.Object, _hashServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullManifestService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new LoaderService(_loggerMock.Object, null!, _hashServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullHashService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new LoaderService(_loggerMock.Object, _manifestServiceMock.Object, null!));
    }

    [Fact]
    public async Task GenerateLoaderAsync_WithNullManifest_ThrowsArgumentNullException()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "loader.js");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _loaderService.GenerateLoaderAsync(null!, outputPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GenerateLoaderAsync_WithInvalidOutputPath_ThrowsArgumentNullException(string? outputPath)
    {
        // Arrange
        var manifest = CreateTestManifest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _loaderService.GenerateLoaderAsync(manifest, outputPath!));
    }

    [Fact]
    public async Task GenerateLoaderAsync_WithValidManifest_GeneratesLoaderSuccessfully()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var outputPath = Path.Combine(_tempDirectory, "loader.js");
        var expectedHash = "test-hash";

        _hashServiceMock.Setup(h => h.ComputeStringHash(It.IsAny<string>()))
            .Returns(expectedHash);

        // Act
        var result = await _loaderService.GenerateLoaderAsync(manifest, outputPath);

        // Assert
        result.Should().NotBeNull();
        result.EnabledMods.Should().HaveCount(2);
        result.EnabledMods.Should().Contain("mod1.js");
        result.EnabledMods.Should().Contain("mod3.js");
        result.Version.Should().Be(ManifestConstants.DefaultLoaderVersion);
        result.ContentHash.Should().Be(expectedHash);
        result.IsBackup.Should().BeFalse();

        // Verify file was created
        File.Exists(outputPath).Should().BeTrue();

        // Verify file content
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("LOADER_VERSION");
        content.Should().Contain("LOADER_FINGERPRINT");
        content.Should().Contain("GENERATED_AT");
        content.Should().Contain("mod1.js");
        content.Should().Contain("mod3.js");
        content.Should().NotContain("mod2.js"); // Disabled mod
    }

    [Fact]
    public async Task GenerateLoaderAsync_WithEmptyModsList_GeneratesLoaderWithNoMods()
    {
        // Arrange
        var manifest = new ManifestData();
        var outputPath = Path.Combine(_tempDirectory, "loader.js");
        var expectedHash = "empty-hash";

        _hashServiceMock.Setup(h => h.ComputeStringHash(It.IsAny<string>()))
            .Returns(expectedHash);

        // Act
        var result = await _loaderService.GenerateLoaderAsync(manifest, outputPath);

        // Assert
        result.Should().NotBeNull();
        result.EnabledMods.Should().BeEmpty();
        result.ContentHash.Should().Be(expectedHash);

        // Verify file content
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("No enabled mods to load");
    }

    [Fact]
    public async Task CreateLoaderConfigurationAsync_WithNullManifest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _loaderService.CreateLoaderConfigurationAsync(null!));
    }

    [Fact]
    public async Task CreateLoaderConfigurationAsync_WithValidManifest_ReturnsConfiguration()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var expectedFingerprint = "test-fingerprint";

        _hashServiceMock.Setup(h => h.ComputeStringHash(It.IsAny<string>()))
            .Returns(expectedFingerprint);

        // Act
        var result = await _loaderService.CreateLoaderConfigurationAsync(manifest);

        // Assert
        result.Should().NotBeNull();
        result.EnabledMods.Should().HaveCount(2);
        result.EnabledMods.Should().Contain("mod1.js");
        result.EnabledMods.Should().Contain("mod3.js");
        result.Version.Should().Be(ManifestConstants.DefaultLoaderVersion);
        result.Fingerprint.Should().Be(expectedFingerprint);
        result.IsBackup.Should().BeFalse();
        result.Options.Should().ContainKey("modCount");
        result.Options["modCount"].Should().Be(2);
        result.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidateLoaderContent_WithNullContent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _loaderService.ValidateLoaderContent(null!));
    }

    [Fact]
    public void ValidateLoaderContent_WithEmptyContent_ThrowsLoaderValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<LoaderValidationException>(() => 
            _loaderService.ValidateLoaderContent(""));

        exception.ValidationErrors.Should().Contain("Loader content is empty or whitespace");
    }

    [Fact]
    public void ValidateLoaderContent_WithValidContent_ReturnsTrue()
    {
        // Arrange
        var validContent = @"
const LOADER_VERSION = '1.0.0';
const LOADER_FINGERPRINT = 'test123';
const GENERATED_AT = '2025-01-20T10:30:00Z';

try {
    await import('./mods/test.js');
} catch (error) {
    console.error('Error:', error);
}";

        // Act & Assert
        var result = _loaderService.ValidateLoaderContent(validContent);
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateLoaderContent_WithMissingConstants_ThrowsLoaderValidationException()
    {
        // Arrange
        var invalidContent = @"
try {
    await import('./mods/test.js');
} catch (error) {
    console.error('Error:', error);
}";

        // Act & Assert
        var exception = Assert.Throws<LoaderValidationException>(() => 
            _loaderService.ValidateLoaderContent(invalidContent));

        exception.ValidationErrors.Should().Contain("Missing LOADER_VERSION constant");
        exception.ValidationErrors.Should().Contain("Missing LOADER_FINGERPRINT constant");
        exception.ValidationErrors.Should().Contain("Missing GENERATED_AT constant");
    }

    [Fact]
    public void ValidateLoaderContent_WithUnbalancedBraces_ThrowsLoaderValidationException()
    {
        // Arrange
        var invalidContent = @"
const LOADER_VERSION = '1.0.0';
const LOADER_FINGERPRINT = 'test123';
const GENERATED_AT = '2025-01-20T10:30:00Z';

try {
    await import('./mods/test.js');
    // Missing closing brace for try block
} catch (error) {
    console.error('Error:', error);
// Missing closing brace for catch block";

        // Act & Assert
        var exception = Assert.Throws<LoaderValidationException>(() => 
            _loaderService.ValidateLoaderContent(invalidContent));

        exception.ValidationErrors.Should().Contain(e => e.Contains("Unbalanced braces"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateBackupAsync_WithInvalidLoaderPath_ThrowsArgumentException(string? loaderPath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _loaderService.CreateBackupAsync(loaderPath!));
    }

    [Fact]
    public async Task CreateBackupAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.js");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _loaderService.CreateBackupAsync(nonExistentPath));
    }

    [Fact]
    public async Task CreateBackupAsync_WithValidFile_CreatesBackupSuccessfully()
    {
        // Arrange
        var loaderPath = Path.Combine(_tempDirectory, "loader.js");
        var testContent = "test loader content";
        await File.WriteAllTextAsync(loaderPath, testContent);

        // Act
        var backupPath = await _loaderService.CreateBackupAsync(loaderPath);

        // Assert
        backupPath.Should().NotBeNullOrEmpty();
        File.Exists(backupPath).Should().BeTrue();
        
        var backupContent = await File.ReadAllTextAsync(backupPath);
        backupContent.Should().Be(testContent);
        
        backupPath.Should().Contain("loader.backup_");
        backupPath.Should().EndWith(".js");
    }

    [Fact]
    public async Task CreateBackupAsync_WithCustomBackupPath_UsesSpecifiedPath()
    {
        // Arrange
        var loaderPath = Path.Combine(_tempDirectory, "loader.js");
        var customBackupPath = Path.Combine(_tempDirectory, "custom_backup.js");
        var testContent = "test loader content";
        await File.WriteAllTextAsync(loaderPath, testContent);

        // Act
        var result = await _loaderService.CreateBackupAsync(loaderPath, customBackupPath);

        // Assert
        result.Should().Be(customBackupPath);
        File.Exists(customBackupPath).Should().BeTrue();
        
        var backupContent = await File.ReadAllTextAsync(customBackupPath);
        backupContent.Should().Be(testContent);
    }

    [Theory]
    [InlineData("", "target.js")]
    [InlineData("   ", "target.js")]
    [InlineData(null, "target.js")]
    [InlineData("backup.js", "")]
    [InlineData("backup.js", "   ")]
    [InlineData("backup.js", null)]
    public async Task RestoreFromBackupAsync_WithInvalidPaths_ThrowsArgumentException(string? backupPath, string? targetPath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _loaderService.RestoreFromBackupAsync(backupPath!, targetPath!));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNonExistentBackup_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentBackup = Path.Combine(_tempDirectory, "nonexistent.backup.js");
        var targetPath = Path.Combine(_tempDirectory, "target.js");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _loaderService.RestoreFromBackupAsync(nonExistentBackup, targetPath));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_RestoresSuccessfully()
    {
        // Arrange
        var backupPath = Path.Combine(_tempDirectory, "backup.js");
        var targetPath = Path.Combine(_tempDirectory, "restored.js");
        var backupContent = "backup loader content";
        await File.WriteAllTextAsync(backupPath, backupContent);

        // Act
        await _loaderService.RestoreFromBackupAsync(backupPath, targetPath);

        // Assert
        File.Exists(targetPath).Should().BeTrue();
        
        var restoredContent = await File.ReadAllTextAsync(targetPath);
        restoredContent.Should().Be(backupContent);
    }

    [Fact]
    public void GenerateLoaderJavaScript_WithNullManifest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _loaderService.GenerateLoaderJavaScript(null!));
    }

    [Fact]
    public void GenerateLoaderJavaScript_WithEnabledMods_GeneratesValidJavaScript()
    {
        // Arrange
        var manifest = CreateTestManifest();

        // Act
        var result = _loaderService.GenerateLoaderJavaScript(manifest);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("LOADER_VERSION");
        result.Should().Contain("LOADER_FINGERPRINT");
        result.Should().Contain("GENERATED_AT");
        result.Should().Contain("mod1.js");
        result.Should().Contain("mod3.js");
        result.Should().NotContain("mod2.js"); // Disabled mod
        result.Should().Contain("await import");
        result.Should().Contain("try");
        result.Should().Contain("catch");
    }

    [Fact]
    public void GenerateLoaderJavaScript_WithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "test'mod", Filename = "test's mod.js", Enabled = true, Order = 1 },
                new() { Id = "test\"mod", Filename = "test\"mod.js", Enabled = true, Order = 2 }
            }
        };

        // Act
        var result = _loaderService.GenerateLoaderJavaScript(manifest);

        // Assert
        result.Should().Contain("test\\'s mod.js");
        result.Should().Contain("test\\\"mod.js");
        result.Should().Contain("test\\'mod");
        result.Should().Contain("test\\\"mod");
    }

    [Fact]
    public void GenerateLoaderJavaScript_WithNoEnabledMods_GeneratesEmptyLoader()
    {
        // Arrange
        var manifest = new ManifestData();

        // Act
        var result = _loaderService.GenerateLoaderJavaScript(manifest);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("LOADER_VERSION");
        result.Should().Contain("No enabled mods to load");
        result.Should().NotContain("modsToLoad");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LoaderExists_WithInvalidPath_ReturnsFalse(string? path)
    {
        // Act & Assert
        _loaderService.LoaderExists(path!).Should().BeFalse();
    }

    [Fact]
    public void LoaderExists_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.js");

        // Act & Assert
        _loaderService.LoaderExists(nonExistentPath).Should().BeFalse();
    }

    [Fact]
    public void LoaderExists_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "existing.js");
        File.WriteAllText(filePath, "test content");

        // Act & Assert
        _loaderService.LoaderExists(filePath).Should().BeTrue();
    }

    private static ManifestData CreateTestManifest()
    {
        return new ManifestData
        {
            Mods = new List<ModInfo>
            {
                new() { Id = "mod1", Filename = "mod1.js", Enabled = true, Order = 1 },
                new() { Id = "mod2", Filename = "mod2.js", Enabled = false, Order = 2 },
                new() { Id = "mod3", Filename = "mod3.js", Enabled = true, Order = 3 }
            }
        };
    }
}
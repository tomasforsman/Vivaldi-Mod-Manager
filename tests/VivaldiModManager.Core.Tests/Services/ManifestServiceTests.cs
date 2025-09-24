using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Constants;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using Xunit;

namespace VivaldiModManager.Core.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ManifestService"/> class.
/// </summary>
public class ManifestServiceTests : IDisposable
{
    private readonly Mock<ILogger<ManifestService>> _loggerMock;
    private readonly ManifestService _manifestService;
    private readonly string _tempDirectory;

    public ManifestServiceTests()
    {
        _loggerMock = new Mock<ILogger<ManifestService>>();
        _manifestService = new ManifestService(_loggerMock.Object);
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
        Assert.Throws<ArgumentNullException>(() => new ManifestService(null!));
    }

    [Fact]
    public void CreateDefaultManifest_ReturnsValidManifest()
    {
        // Act
        var manifest = _manifestService.CreateDefaultManifest();

        // Assert
        manifest.Should().NotBeNull();
        manifest.SchemaVersion.Should().Be(ManifestConstants.CurrentSchemaVersion);
        manifest.Settings.Should().NotBeNull();
        manifest.Settings.AutoHealEnabled.Should().BeTrue();
        manifest.Settings.MonitoringEnabled.Should().BeTrue();
        manifest.Settings.BackupRetentionDays.Should().Be(ManifestConstants.DefaultBackupRetentionDays);
        manifest.Settings.LogLevel.Should().Be(ManifestConstants.DefaultLogLevel);
        manifest.Mods.Should().NotBeNull().And.BeEmpty();
        manifest.Installations.Should().NotBeNull().And.BeEmpty();
        manifest.Metadata.Should().NotBeNull().And.BeEmpty();
        manifest.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        manifest.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SaveManifestAsync_WithValidManifest_SavesSuccessfully()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        var filePath = Path.Combine(_tempDirectory, "test-manifest.json");

        // Act
        await _manifestService.SaveManifestAsync(manifest, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("\"schemaVersion\":");
    }

    [Fact]
    public async Task SaveManifestAsync_UpdatesTimestamps()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        var originalTimestamp = manifest.LastUpdated;
        var filePath = Path.Combine(_tempDirectory, "test-manifest.json");

        // Wait a small amount to ensure timestamp difference
        await Task.Delay(10);

        // Act
        await _manifestService.SaveManifestAsync(manifest, filePath);

        // Assert
        manifest.LastUpdated.Should().BeAfter(originalTimestamp);
        manifest.LastUpdatedByVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveManifestAsync_WithNullManifest_ThrowsArgumentNullException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test-manifest.json");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _manifestService.SaveManifestAsync(null!, filePath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SaveManifestAsync_WithInvalidPath_ThrowsArgumentException(string? filePath)
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manifestService.SaveManifestAsync(manifest, filePath!));
    }

    [Fact]
    public async Task LoadManifestAsync_WithValidFile_LoadsSuccessfully()
    {
        // Arrange
        var originalManifest = _manifestService.CreateDefaultManifest();
        originalManifest.Mods.Add(new ModInfo
        {
            Id = Guid.NewGuid().ToString(),
            Filename = "test-mod.js",
            Enabled = true,
            Order = 1
        });

        var filePath = Path.Combine(_tempDirectory, "test-manifest.json");
        await _manifestService.SaveManifestAsync(originalManifest, filePath);

        // Act
        var loadedManifest = await _manifestService.LoadManifestAsync(filePath);

        // Assert
        loadedManifest.Should().NotBeNull();
        loadedManifest.SchemaVersion.Should().Be(originalManifest.SchemaVersion);
        loadedManifest.Mods.Should().HaveCount(1);
        loadedManifest.Mods[0].Filename.Should().Be("test-mod.js");
    }

    [Fact]
    public async Task LoadManifestAsync_WithNonExistentFile_ThrowsManifestNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ManifestNotFoundException>(() =>
            _manifestService.LoadManifestAsync(nonExistentPath));

        exception.ManifestPath.Should().Be(nonExistentPath);
    }

    [Fact]
    public async Task LoadManifestAsync_WithInvalidJson_ThrowsManifestCorruptedException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "invalid.json");
        await File.WriteAllTextAsync(filePath, "{ invalid json content");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ManifestCorruptedException>(() =>
            _manifestService.LoadManifestAsync(filePath));

        exception.ManifestPath.Should().Be(filePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LoadManifestAsync_WithInvalidPath_ThrowsArgumentException(string? filePath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manifestService.LoadManifestAsync(filePath!));
    }

    [Fact]
    public async Task CreateBackupAsync_WithValidFile_CreatesBackup()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        var originalPath = Path.Combine(_tempDirectory, "original.json");
        await _manifestService.SaveManifestAsync(manifest, originalPath);

        // Act
        var backupPath = await _manifestService.CreateBackupAsync(originalPath);

        // Assert
        File.Exists(backupPath).Should().BeTrue();
        backupPath.Should().Contain("backups");
        backupPath.Should().Contain(".backup");

        // Verify backup content matches original
        var originalContent = await File.ReadAllTextAsync(originalPath);
        var backupContent = await File.ReadAllTextAsync(backupPath);
        backupContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task CreateBackupAsync_WithCustomBackupPath_UsesSpecifiedPath()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        var originalPath = Path.Combine(_tempDirectory, "original.json");
        var customBackupPath = Path.Combine(_tempDirectory, "custom-backup.json");
        await _manifestService.SaveManifestAsync(manifest, originalPath);

        // Act
        var resultBackupPath = await _manifestService.CreateBackupAsync(originalPath, customBackupPath);

        // Assert
        resultBackupPath.Should().Be(customBackupPath);
        File.Exists(customBackupPath).Should().BeTrue();
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_RestoresSuccessfully()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        var originalPath = Path.Combine(_tempDirectory, "original.json");
        await _manifestService.SaveManifestAsync(manifest, originalPath);

        var backupPath = await _manifestService.CreateBackupAsync(originalPath);
        
        // Modify original
        manifest.Settings.LogLevel = "Debug";
        await _manifestService.SaveManifestAsync(manifest, originalPath);

        var targetPath = Path.Combine(_tempDirectory, "restored.json");

        // Act
        await _manifestService.RestoreFromBackupAsync(backupPath, targetPath);

        // Assert
        File.Exists(targetPath).Should().BeTrue();
        var restoredManifest = await _manifestService.LoadManifestAsync(targetPath);
        restoredManifest.Settings.LogLevel.Should().Be(ManifestConstants.DefaultLogLevel); // Should be original value
    }

    [Fact]
    public void ValidateManifest_WithValidManifest_ReturnsTrue()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        manifest.Mods.Add(new ModInfo
        {
            Id = "mod1",
            Filename = "mod1.js",
            Enabled = true,
            Order = 1
        });
        manifest.Mods.Add(new ModInfo
        {
            Id = "mod2",
            Filename = "mod2.js",
            Enabled = true,
            Order = 2
        });

        // Act
        var isValid = _manifestService.ValidateManifest(manifest);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateManifest_WithNullManifest_ReturnsFalse()
    {
        // Act
        var isValid = _manifestService.ValidateManifest(null!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateManifest_WithDuplicateModIds_ReturnsFalse()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        manifest.Mods.Add(new ModInfo { Id = "duplicate", Filename = "mod1.js" });
        manifest.Mods.Add(new ModInfo { Id = "duplicate", Filename = "mod2.js" });

        // Act
        var isValid = _manifestService.ValidateManifest(manifest);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateManifest_WithDuplicateModOrders_ReturnsFalse()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        manifest.Mods.Add(new ModInfo 
        { 
            Id = "mod1", 
            Filename = "mod1.js", 
            Enabled = true, 
            Order = 1 
        });
        manifest.Mods.Add(new ModInfo 
        { 
            Id = "mod2", 
            Filename = "mod2.js", 
            Enabled = true, 
            Order = 1 
        });

        // Act
        var isValid = _manifestService.ValidateManifest(manifest);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateManifest_WithInvalidSchemaVersion_ReturnsFalse()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        manifest.SchemaVersion = 0;

        // Act
        var isValid = _manifestService.ValidateManifest(manifest);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void MigrateManifest_WithCurrentSchemaVersion_ReturnsUnchanged()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        var originalVersion = manifest.SchemaVersion;

        // Act
        var migratedManifest = _manifestService.MigrateManifest(manifest);

        // Assert
        migratedManifest.SchemaVersion.Should().Be(originalVersion);
        migratedManifest.Should().BeSameAs(manifest);
    }

    [Fact]
    public void MigrateManifest_WithFutureSchemaVersion_ThrowsManifestSchemaException()
    {
        // Arrange
        var manifest = _manifestService.CreateDefaultManifest();
        manifest.SchemaVersion = ManifestConstants.CurrentSchemaVersion + 1;

        // Act & Assert
        var exception = Assert.Throws<ManifestSchemaException>(() =>
            _manifestService.MigrateManifest(manifest));

        exception.CurrentVersion.Should().Be(ManifestConstants.CurrentSchemaVersion + 1);
        exception.ExpectedVersion.Should().Be(ManifestConstants.CurrentSchemaVersion);
    }

    [Fact]
    public void ManifestExists_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "existing.json");
        File.WriteAllText(filePath, "{}");

        // Act
        var exists = _manifestService.ManifestExists(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void ManifestExists_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "nonexistent.json");

        // Act
        var exists = _manifestService.ManifestExists(filePath);

        // Assert
        exists.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ManifestExists_WithInvalidPath_ReturnsFalse(string? filePath)
    {
        // Act
        var exists = _manifestService.ManifestExists(filePath!);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAndLoadManifest_RoundTrip_PreservesData()
    {
        // Arrange
        var originalManifest = _manifestService.CreateDefaultManifest();
        originalManifest.Settings.LogLevel = "Debug";
        originalManifest.Settings.BackupRetentionDays = 60;
        
        originalManifest.Mods.Add(new ModInfo
        {
            Id = Guid.NewGuid().ToString(),
            Filename = "test-mod.js",
            Enabled = true,
            Order = 1,
            Notes = "Test notes",
            Version = "1.0.0",
            UrlScopes = new List<string> { "*://example.com/*" },
            FileSize = 1024,
            IsValidated = true
        });

        originalManifest.Installations.Add(new VivaldiInstallation
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Installation",
            InstallationPath = @"C:\Test\Vivaldi",
            ApplicationPath = @"C:\Test\Vivaldi\Application",
            Version = "6.5.0",
            InstallationType = VivaldiInstallationType.Standard,
            IsManaged = true
        });

        var filePath = Path.Combine(_tempDirectory, "roundtrip-test.json");

        // Act
        await _manifestService.SaveManifestAsync(originalManifest, filePath);
        var loadedManifest = await _manifestService.LoadManifestAsync(filePath);

        // Assert
        loadedManifest.Should().NotBeNull();
        loadedManifest.Settings.LogLevel.Should().Be(originalManifest.Settings.LogLevel);
        loadedManifest.Settings.BackupRetentionDays.Should().Be(originalManifest.Settings.BackupRetentionDays);
        
        loadedManifest.Mods.Should().HaveCount(1);
        loadedManifest.Mods[0].Id.Should().Be(originalManifest.Mods[0].Id);
        loadedManifest.Mods[0].Filename.Should().Be(originalManifest.Mods[0].Filename);
        loadedManifest.Mods[0].UrlScopes.Should().BeEquivalentTo(originalManifest.Mods[0].UrlScopes);
        
        loadedManifest.Installations.Should().HaveCount(1);
        loadedManifest.Installations[0].Id.Should().Be(originalManifest.Installations[0].Id);
        loadedManifest.Installations[0].InstallationType.Should().Be(originalManifest.Installations[0].InstallationType);
    }
}
using System.Runtime.Versioning;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using Xunit;

namespace VivaldiModManager.Core.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="InjectionService"/> class.
/// </summary>
[SupportedOSPlatform("windows")]
public class InjectionServiceTests : IDisposable
{
    private readonly Mock<ILogger<InjectionService>> _loggerMock;
    private readonly Mock<IVivaldiService> _vivaldiServiceMock;
    private readonly Mock<ILoaderService> _loaderServiceMock;
    private readonly Mock<IHashService> _hashServiceMock;
    private readonly InjectionService _injectionService;
    private readonly string _tempDirectory;

    public InjectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<InjectionService>>();
        _vivaldiServiceMock = new Mock<IVivaldiService>();
        _loaderServiceMock = new Mock<ILoaderService>();
        _hashServiceMock = new Mock<IHashService>();
        
        _injectionService = new InjectionService(
            _loggerMock.Object,
            _vivaldiServiceMock.Object,
            _loaderServiceMock.Object,
            _hashServiceMock.Object);

        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Disposes test resources.
    /// </summary>
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
        Assert.Throws<ArgumentNullException>(() => new InjectionService(
            null!,
            _vivaldiServiceMock.Object,
            _loaderServiceMock.Object,
            _hashServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullVivaldiService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InjectionService(
            _loggerMock.Object,
            null!,
            _loaderServiceMock.Object,
            _hashServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLoaderService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InjectionService(
            _loggerMock.Object,
            _vivaldiServiceMock.Object,
            null!,
            _hashServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullHashService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InjectionService(
            _loggerMock.Object,
            _vivaldiServiceMock.Object,
            _loaderServiceMock.Object,
            null!));
    }

    [Fact]
    public async Task InjectAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _injectionService.InjectAsync(null!, "/path/to/loader.js"));
    }

    [Fact]
    public async Task InjectAsync_WithNullLoaderPath_ThrowsArgumentException()
    {
        // Arrange
        var installation = CreateTestInstallation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _injectionService.InjectAsync(installation, null!));
    }

    [Fact]
    public async Task InjectAsync_WithEmptyLoaderPath_ThrowsArgumentException()
    {
        // Arrange
        var installation = CreateTestInstallation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _injectionService.InjectAsync(installation, ""));
    }

    [Fact]
    public async Task InjectAsync_WithNoTargets_ThrowsInjectionException()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var loaderPath = "/path/to/loader.js";

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>().AsReadOnly());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InjectionException>(() =>
            _injectionService.InjectAsync(installation, loaderPath));

        exception.InstallationId.Should().Be(installation.Id);
        exception.Operation.Should().Be("injection");
    }

    [Fact]
    public async Task InjectAsync_WithValidInputs_SuccessfullyInjects()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var loaderPath = Path.Combine(_tempDirectory, "vivaldi-mods", "loader.js");
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        var browserHtmlPath = Path.Combine(_tempDirectory, "browser.html");

        // Create test files
        Directory.CreateDirectory(Path.GetDirectoryName(loaderPath)!);
        await File.WriteAllTextAsync(loaderPath, "// loader content");
        await File.WriteAllTextAsync(windowHtmlPath, "<html><head></head><body></body></html>");
        await File.WriteAllTextAsync(browserHtmlPath, "<html><head></head><body></body></html>");

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath,
            ["browser.html"] = browserHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        _hashServiceMock
            .Setup(h => h.ComputeStringHash(It.IsAny<string>()))
            .Returns("abcd1234efgh56781234567890abcdef");

        // Act
        await _injectionService.InjectAsync(installation, loaderPath);

        // Assert
        installation.LastInjectionAt.Should().NotBeNull();
        installation.InjectionFingerprint.Should().Be("abcd1234efgh5678");
        installation.LastInjectionStatus.Should().Be("Success");

        // Verify files were modified
        var windowContent = await File.ReadAllTextAsync(windowHtmlPath);
        var browserContent = await File.ReadAllTextAsync(browserHtmlPath);

        windowContent.Should().Contain("Vivaldi Mod Manager - Injection Stub");
        browserContent.Should().Contain("Vivaldi Mod Manager - Injection Stub");
        windowContent.Should().Contain("vivaldi-mods/loader.js");
        browserContent.Should().Contain("vivaldi-mods/loader.js");
    }

    [Fact]
    public async Task RemoveInjectionAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _injectionService.RemoveInjectionAsync(null!));
    }

    [Fact]
    public async Task RemoveInjectionAsync_WithExistingInjection_RemovesSuccessfully()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        var browserHtmlPath = Path.Combine(_tempDirectory, "browser.html");

        // Create test files with injections
        var htmlWithInjection = @"<html><head></head><body>
<!-- Vivaldi Mod Manager - Injection Stub v1.0 -->
<!-- Fingerprint: abcd1234 -->
<!-- Generated: 2025-01-21T10:30:00Z -->
<script type=""module"" src=""./vivaldi-mods/loader.js""></script>
</body></html>";

        await File.WriteAllTextAsync(windowHtmlPath, htmlWithInjection);
        await File.WriteAllTextAsync(browserHtmlPath, htmlWithInjection);

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath,
            ["browser.html"] = browserHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Act
        await _injectionService.RemoveInjectionAsync(installation);

        // Assert
        installation.LastInjectionAt.Should().BeNull();
        installation.InjectionFingerprint.Should().BeNull();
        installation.LastInjectionStatus.Should().Be("Removed");

        // Verify files were cleaned
        var windowContent = await File.ReadAllTextAsync(windowHtmlPath);
        var browserContent = await File.ReadAllTextAsync(browserHtmlPath);

        windowContent.Should().NotContain("Vivaldi Mod Manager - Injection Stub");
        browserContent.Should().NotContain("Vivaldi Mod Manager - Injection Stub");
    }

    [Fact]
    public async Task ValidateInjectionAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _injectionService.ValidateInjectionAsync(null!));
    }

    [Fact]
    public async Task ValidateInjectionAsync_WithValidInjection_ReturnsTrue()
    {
        // Arrange
        var installation = CreateTestInstallation();
        installation.InjectionFingerprint = "abcd1234";
        
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        var htmlWithValidInjection = @"<html><head></head><body>
<!-- Vivaldi Mod Manager - Injection Stub v1.0 -->
<!-- Fingerprint: abcd1234 -->
<!-- Generated: 2025-01-21T10:30:00Z -->
<script type=""module"" src=""./vivaldi-mods/loader.js""></script>
</body></html>";

        await File.WriteAllTextAsync(windowHtmlPath, htmlWithValidInjection);

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Act
        var result = await _injectionService.ValidateInjectionAsync(installation);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task BackupTargetFilesAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _injectionService.BackupTargetFilesAsync(null!));
    }

    [Fact]
    public async Task BackupTargetFilesAsync_WithValidInstallation_CreatesBackups()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        var browserHtmlPath = Path.Combine(_tempDirectory, "browser.html");

        await File.WriteAllTextAsync(windowHtmlPath, "<html>window content</html>");
        await File.WriteAllTextAsync(browserHtmlPath, "<html>browser content</html>");

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath,
            ["browser.html"] = browserHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Act
        var backups = await _injectionService.BackupTargetFilesAsync(installation);

        // Assert
        backups.Should().HaveCount(2);
        backups.Should().ContainKey("window.html");
        backups.Should().ContainKey("browser.html");

        // Verify backup files exist and have correct content
        File.Exists(backups["window.html"]).Should().BeTrue();
        File.Exists(backups["browser.html"]).Should().BeTrue();

        var windowBackupContent = await File.ReadAllTextAsync(backups["window.html"]);
        var browserBackupContent = await File.ReadAllTextAsync(backups["browser.html"]);

        windowBackupContent.Should().Be("<html>window content</html>");
        browserBackupContent.Should().Be("<html>browser content</html>");
    }

    [Fact]
    public async Task RestoreTargetFilesAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _injectionService.RestoreTargetFilesAsync(null!));
    }

    [Fact]
    public async Task GetInjectionStatusAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _injectionService.GetInjectionStatusAsync(null!));
    }

    [Fact]
    public async Task GetInjectionStatusAsync_WithNoInjection_ReturnsCorrectStatus()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");

        await File.WriteAllTextAsync(windowHtmlPath, "<html><head></head><body></body></html>");

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Act
        var status = await _injectionService.GetInjectionStatusAsync(installation);

        // Assert
        status.IsInjected.Should().BeFalse();
        status.TotalTargetCount.Should().Be(1);
        status.InjectedTargetCount.Should().Be(0);
        status.ValidationStatus.Should().Be(InjectionValidationStatus.NotInjected);
        status.IsFullyIntact.Should().BeFalse();
        status.NeedsRepair.Should().BeFalse();
    }

    [Fact]
    public async Task GetInjectionStatusAsync_WithValidInjection_ReturnsCorrectStatus()
    {
        // Arrange
        var installation = CreateTestInstallation();
        installation.InjectionFingerprint = "abcd1234";
        
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");
        var htmlWithValidInjection = @"<html><head></head><body>
<!-- Vivaldi Mod Manager - Injection Stub v1.0 -->
<!-- Fingerprint: abcd1234 -->
<!-- Generated: 2025-01-21T10:30:00Z -->
<script type=""module"" src=""./vivaldi-mods/loader.js""></script>
</body></html>";

        await File.WriteAllTextAsync(windowHtmlPath, htmlWithValidInjection);

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        // Act
        var status = await _injectionService.GetInjectionStatusAsync(installation);

        // Assert
        status.IsInjected.Should().BeTrue();
        status.TotalTargetCount.Should().Be(1);
        status.InjectedTargetCount.Should().Be(1);
        status.ValidationStatus.Should().Be(InjectionValidationStatus.Valid);
        status.IsFullyIntact.Should().BeTrue();
        status.NeedsRepair.Should().BeFalse();
        status.TargetFiles.Should().ContainKey("window.html");
        status.TargetFiles["window.html"].IsInjected.Should().BeTrue();
        status.TargetFiles["window.html"].Fingerprint.Should().Be("abcd1234");
        status.TargetFiles["window.html"].ValidationStatus.Should().Be(InjectionValidationStatus.Valid);
    }

    [Fact]
    public async Task RepairInjectionAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _injectionService.RepairInjectionAsync(null!, "/path/to/loader.js"));
    }

    [Fact]
    public async Task RepairInjectionAsync_WithNullLoaderPath_ThrowsArgumentException()
    {
        // Arrange
        var installation = CreateTestInstallation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _injectionService.RepairInjectionAsync(installation, null!));
    }

    [Fact]
    public async Task RepairInjectionAsync_WithBrokenInjection_RepairsSuccessfully()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var loaderPath = Path.Combine(_tempDirectory, "vivaldi-mods", "loader.js");
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");

        // Create test files with broken injection
        Directory.CreateDirectory(Path.GetDirectoryName(loaderPath)!);
        await File.WriteAllTextAsync(loaderPath, "// loader content");
        
        var htmlWithBrokenInjection = @"<html><head></head><body>
<!-- Vivaldi Mod Manager - Injection Stub v1.0 -->
<!-- Fingerprint: oldfingerprint -->
<!-- Generated: 2025-01-21T10:30:00Z -->
<script type=""module"" src=""./vivaldi-mods/loader.js""></script>
</body></html>";

        await File.WriteAllTextAsync(windowHtmlPath, htmlWithBrokenInjection);

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        _hashServiceMock
            .Setup(h => h.ComputeStringHash(It.IsAny<string>()))
            .Returns("newfingerprint1234567890abcdef");

        // Act
        await _injectionService.RepairInjectionAsync(installation, loaderPath);

        // Assert
        installation.LastInjectionAt.Should().NotBeNull();
        installation.InjectionFingerprint.Should().Be("newfingerprint12");
        installation.LastInjectionStatus.Should().Be("Success");

        // Verify file was repaired
        var windowContent = await File.ReadAllTextAsync(windowHtmlPath);
        windowContent.Should().Contain("Vivaldi Mod Manager - Injection Stub");
        windowContent.Should().Contain("newfingerprint12");
        windowContent.Should().Contain("vivaldi-mods/loader.js");
        windowContent.Should().NotContain("oldfingerprint");
        windowContent.Should().NotContain("broken content");
    }

    /// <summary>
    /// Tests that generated injection stubs use external script references to avoid CSP issues.
    /// </summary>
    [Fact]
    public async Task InjectAsync_GeneratesStubWithExternalScript()
    {
        // Arrange
        var installation = CreateTestInstallation();
        var loaderPath = Path.Combine(_tempDirectory, "vivaldi-mods", "loader.js");
        var windowHtmlPath = Path.Combine(_tempDirectory, "window.html");

        Directory.CreateDirectory(Path.GetDirectoryName(loaderPath)!);
        await File.WriteAllTextAsync(loaderPath, "// loader content");
        await File.WriteAllTextAsync(windowHtmlPath, "<html><head></head><body></body></html>");

        var targets = new Dictionary<string, string>
        {
            ["window.html"] = windowHtmlPath
        }.AsReadOnly();

        _vivaldiServiceMock
            .Setup(v => v.FindInjectionTargetsAsync(installation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        _hashServiceMock
            .Setup(h => h.ComputeStringHash(It.IsAny<string>()))
            .Returns("abcd1234567890ef");

        _loaderServiceMock
            .Setup(l => l.GenerateLoaderAsync(It.IsAny<ManifestData>(), loaderPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoaderConfiguration());

        // Act
        await _injectionService.InjectAsync(installation, loaderPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(windowHtmlPath);
        
        // Verify the script tag uses external src reference (no inline content)
        htmlContent.Should().Contain("<script type=\"module\" src=\"./vivaldi-mods/loader.js\"></script>");
        htmlContent.Should().NotContain("integrity="); // No integrity hash needed for external scripts
        htmlContent.Should().NotContain("await import"); // No inline JavaScript
        
        // Verify it contains the expected comment structure
        htmlContent.Should().Contain("<!-- Vivaldi Mod Manager - Injection Stub");
        htmlContent.Should().Contain("<!-- Fingerprint:");
        
        installation.LastInjectionAt.Should().NotBeNull();
        installation.InjectionFingerprint.Should().Be("abcd1234567890ef");
        installation.LastInjectionStatus.Should().Be("Success");
    }

    /// <summary>
    /// Creates a test installation for testing purposes.
    /// </summary>
    /// <returns>A test VivaldiInstallation instance.</returns>
    private static VivaldiInstallation CreateTestInstallation()
    {
        return new VivaldiInstallation
        {
            Id = "test-installation-id",
            Name = "Test Vivaldi Installation",
            InstallationPath = "/test/vivaldi",
            UserDataPath = "/test/vivaldi/userData",
            Version = "6.0.0",
            InstallationType = VivaldiInstallationType.Standard,
            IsActive = true,
            IsManaged = true
        };
    }
}
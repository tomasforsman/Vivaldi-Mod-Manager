using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Exceptions;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;

namespace VivaldiModManager.Core.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="VivaldiService"/> class.
/// </summary>
public sealed class VivaldiServiceTests : IDisposable
{
    private readonly Mock<ILogger<VivaldiService>> _loggerMock;
    private readonly Mock<IManifestService> _manifestServiceMock;
    private readonly VivaldiService _vivaldiService;
    private readonly string _tempDirectory;

    public VivaldiServiceTests()
    {
        _loggerMock = new Mock<ILogger<VivaldiService>>();
        _manifestServiceMock = new Mock<IManifestService>();
        _vivaldiService = new VivaldiService(_loggerMock.Object, _manifestServiceMock.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new VivaldiService(null!, _manifestServiceMock.Object));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullManifestService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new VivaldiService(_loggerMock.Object, null!));
        
        exception.ParamName.Should().Be("manifestService");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetInstallationInfoAsync_WithInvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _vivaldiService.GetInstallationInfoAsync(path!));
        
        exception.ParamName.Should().Be("path");
    }

    [Fact]
    public async Task GetInstallationInfoAsync_WithNonExistentPath_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent");

        // Act
        var result = await _vivaldiService.GetInstallationInfoAsync(nonExistentPath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateInstallationAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _vivaldiService.ValidateInstallationAsync(null!));
        
        exception.ParamName.Should().Be("installation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetVersionAsync_WithInvalidExecutablePath_ThrowsArgumentException(string? executablePath)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _vivaldiService.GetVersionAsync(executablePath!));
        
        exception.ParamName.Should().Be("executablePath");
    }

    [Fact]
    public async Task GetVersionAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.exe");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _vivaldiService.GetVersionAsync(nonExistentPath));
        
        exception.Message.Should().Contain(nonExistentPath);
    }

    [Fact]
    public async Task FindInjectionTargetsAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _vivaldiService.FindInjectionTargetsAsync(null!));
        
        exception.ParamName.Should().Be("installation");
    }

    [Fact]
    public void IsInstallationCompatible_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _vivaldiService.IsInstallationCompatible(null!, "6.0.0"));
        
        exception.ParamName.Should().Be("installation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsInstallationCompatible_WithInvalidMinVersion_ThrowsArgumentException(string? minVersion)
    {
        // Arrange
        var installation = CreateTestInstallation();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _vivaldiService.IsInstallationCompatible(installation, minVersion!));
        
        exception.ParamName.Should().Be("minVersion");
    }

    [Fact]
    public void IsInstallationCompatible_WithCompatibleVersion_ReturnsTrue()
    {
        // Arrange
        var installation = CreateTestInstallation("6.5.0");

        // Act
        var result = _vivaldiService.IsInstallationCompatible(installation, "6.0.0");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInstallationCompatible_WithIncompatibleVersion_ReturnsFalse()
    {
        // Arrange
        var installation = CreateTestInstallation("5.9.0");

        // Act
        var result = _vivaldiService.IsInstallationCompatible(installation, "6.0.0");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInstallationCompatible_WithEqualVersion_ReturnsTrue()
    {
        // Arrange
        var installation = CreateTestInstallation("6.0.0");

        // Act
        var result = _vivaldiService.IsInstallationCompatible(installation, "6.0.0");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInstallationCompatible_WithEmptyInstallationVersion_ReturnsFalse()
    {
        // Arrange
        var installation = CreateTestInstallation("");

        // Act
        var result = _vivaldiService.IsInstallationCompatible(installation, "6.0.0");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInstallationCompatible_WithInvalidVersionFormat_ReturnsFalse()
    {
        // Arrange
        var installation = CreateTestInstallation("invalid-version");

        // Act
        var result = _vivaldiService.IsInstallationCompatible(installation, "6.0.0");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshInstallationAsync_WithNullInstallation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _vivaldiService.RefreshInstallationAsync(null!));
        
        exception.ParamName.Should().Be("installation");
    }

    [Fact]
    public async Task ValidateInstallationAsync_WithInvalidInstallationPath_ReturnsFalse()
    {
        // Arrange
        var installation = new VivaldiInstallation
        {
            Id = "test-id",
            Name = "Test Installation",
            InstallationPath = Path.Combine(_tempDirectory, "nonexistent"),
            ApplicationPath = Path.Combine(_tempDirectory, "nonexistent", "Application"),
            Version = "6.0.0"
        };

        // Act
        var result = await _vivaldiService.ValidateInstallationAsync(installation);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateInstallationAsync_WithValidStructure_CallsVersionCheck()
    {
        // Arrange
        var installationPath = Path.Combine(_tempDirectory, "Vivaldi");
        var applicationPath = Path.Combine(installationPath, "Application");
        var executablePath = Path.Combine(applicationPath, "vivaldi.exe");

        Directory.CreateDirectory(applicationPath);
        
        // Create a mock executable file (we can't test actual version extraction without a real Vivaldi executable)
        await File.WriteAllTextAsync(executablePath, "mock executable");

        var installation = new VivaldiInstallation
        {
            Id = "test-id",
            Name = "Test Installation",
            InstallationPath = installationPath,
            ApplicationPath = applicationPath,
            Version = "6.0.0"
        };

        // Act & Assert
        // This will fail because our mock file won't have version info, but it should reach the version check
        var result = await _vivaldiService.ValidateInstallationAsync(installation);
        result.Should().BeFalse(); // Expected to fail at version extraction step
    }

    [Fact]
    public async Task FindInjectionTargetsAsync_WithMissingResourcesDirectory_ThrowsVivaldiException()
    {
        // Arrange
        var installationPath = Path.Combine(_tempDirectory, "Vivaldi");
        var applicationPath = Path.Combine(installationPath, "Application");
        Directory.CreateDirectory(applicationPath);

        var installation = new VivaldiInstallation
        {
            Id = "test-id",
            Name = "Test Installation",
            InstallationPath = installationPath,
            ApplicationPath = applicationPath,
            Version = "6.0.0"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<VivaldiException>(() =>
            _vivaldiService.FindInjectionTargetsAsync(installation));
        
        exception.Message.Should().Contain("No injection targets found");
    }

    [Fact]
    public async Task FindInjectionTargetsAsync_WithValidTargets_ReturnsTargets()
    {
        // Arrange
        var installationPath = Path.Combine(_tempDirectory, "Vivaldi");
        var applicationPath = Path.Combine(installationPath, "Application");
        var resourcesPath = Path.Combine(applicationPath, "resources", "vivaldi");
        Directory.CreateDirectory(resourcesPath);

        var windowHtmlPath = Path.Combine(resourcesPath, "window.html");
        var browserHtmlPath = Path.Combine(resourcesPath, "browser.html");
        
        await File.WriteAllTextAsync(windowHtmlPath, "<html>window</html>");
        await File.WriteAllTextAsync(browserHtmlPath, "<html>browser</html>");

        var installation = new VivaldiInstallation
        {
            Id = "test-id",
            Name = "Test Installation",
            InstallationPath = installationPath,
            ApplicationPath = applicationPath,
            Version = "6.0.0"
        };

        // Act
        var result = await _vivaldiService.FindInjectionTargetsAsync(installation);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("window.html");
        result.Should().ContainKey("browser.html");
        result["window.html"].Should().Be(windowHtmlPath);
        result["browser.html"].Should().Be(browserHtmlPath);
    }

    [Fact]
    public async Task DetectInstallationsAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _vivaldiService.DetectInstallationsAsync(cts.Token));
    }

    private static VivaldiInstallation CreateTestInstallation(string version = "6.0.0")
    {
        return new VivaldiInstallation
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test Vivaldi",
            InstallationPath = @"C:\Program Files\Vivaldi",
            ApplicationPath = @"C:\Program Files\Vivaldi\Application",
            UserDataPath = @"C:\Users\Test\AppData\Local\Vivaldi\User Data",
            Version = version,
            InstallationType = VivaldiInstallationType.Standard,
            IsManaged = false,
            IsActive = false,
            DetectedAt = DateTimeOffset.UtcNow,
            LastVerifiedAt = DateTimeOffset.UtcNow
        };
    }
}
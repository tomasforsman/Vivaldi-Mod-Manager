using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.BackgroundServices;
using VivaldiModManager.Service.Configuration;

namespace VivaldiModManager.Service.Tests.BackgroundServices;

/// <summary>
/// Unit tests for the <see cref="FileSystemMonitorService"/> class.
/// </summary>
public class FileSystemMonitorServiceTests
{
    private readonly Mock<ILogger<FileSystemMonitorService>> _loggerMock;
    private readonly Mock<IManifestService> _manifestServiceMock;
    private readonly Mock<IVivaldiService> _vivaldiServiceMock;
    private readonly ServiceConfiguration _config;

    public FileSystemMonitorServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileSystemMonitorService>>();
        _manifestServiceMock = new Mock<IManifestService>();
        _vivaldiServiceMock = new Mock<IVivaldiService>();
        _config = new ServiceConfiguration
        {
            ManifestPath = Path.Combine(Path.GetTempPath(), "test-manifest.json"),
            MonitoringDebounceMs = 2000
        };
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FileSystemMonitorService(null!, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FileSystemMonitorService(_loggerMock.Object, null!, _manifestServiceMock.Object, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullManifestService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FileSystemMonitorService(_loggerMock.Object, _config, null!, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullVivaldiService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Assert
        service.Should().NotBeNull();
        service.IsPaused.Should().BeFalse();
        service.TotalFileChanges.Should().Be(0);
        service.TotalVivaldiChanges.Should().Be(0);
        service.LastChangeTime.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_WhenManifestNotFound_DoesNotStartWatchers()
    {
        // Arrange
        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(false);

        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        service.ActiveWatcherCount.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WhenMonitoringDisabled_DoesNotStartWatchers()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Settings = new GlobalSettings
            {
                MonitoringEnabled = false,
                ModsRootPath = Path.GetTempPath()
            }
        };

        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(true);
        _manifestServiceMock.Setup(m => m.LoadManifestAsync(_config.ManifestPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        service.ActiveWatcherCount.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WhenModsRootPathNotSet_LogsWarning()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Settings = new GlobalSettings
            {
                MonitoringEnabled = true,
                ModsRootPath = null
            }
        };

        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(true);
        _manifestServiceMock.Setup(m => m.LoadManifestAsync(_config.ManifestPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);
        _vivaldiServiceMock.Setup(v => v.DetectInstallationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VivaldiInstallation>());

        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        service.ActiveWatcherCount.Should().Be(0);
    }

    [Fact]
    public void PauseMonitoring_StopsWatchers()
    {
        // Arrange
        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        service.PauseMonitoring();

        // Assert
        service.IsPaused.Should().BeTrue();
        service.ActiveWatcherCount.Should().Be(0);
    }

    [Fact]
    public async Task ResumeMonitoringAsync_WhenPaused_RestartsWatchers()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Settings = new GlobalSettings
            {
                MonitoringEnabled = true,
                ModsRootPath = null
            }
        };

        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(true);
        _manifestServiceMock.Setup(m => m.LoadManifestAsync(_config.ManifestPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);
        _vivaldiServiceMock.Setup(v => v.DetectInstallationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VivaldiInstallation>());

        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service.StartAsync(CancellationToken.None);
        service.PauseMonitoring();

        // Act
        await service.ResumeMonitoringAsync();

        // Assert
        service.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Statistics_InitiallyZero()
    {
        // Arrange
        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Assert
        service.TotalFileChanges.Should().Be(0);
        service.TotalVivaldiChanges.Should().Be(0);
        service.LastChangeTime.Should().BeNull();
    }

    [Fact]
    public void Dispose_DisposesResourcesProperly()
    {
        // Arrange
        var service = new FileSystemMonitorService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        service.Dispose();

        // Assert - no exception should be thrown
        service.ActiveWatcherCount.Should().Be(0);
    }
}

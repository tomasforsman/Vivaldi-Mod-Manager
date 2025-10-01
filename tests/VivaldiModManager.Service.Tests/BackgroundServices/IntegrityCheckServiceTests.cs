using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Models;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.BackgroundServices;
using VivaldiModManager.Service.Configuration;

namespace VivaldiModManager.Service.Tests.BackgroundServices;

/// <summary>
/// Unit tests for the <see cref="IntegrityCheckService"/> class.
/// </summary>
public class IntegrityCheckServiceTests
{
    private readonly Mock<ILogger<IntegrityCheckService>> _loggerMock;
    private readonly Mock<IManifestService> _manifestServiceMock;
    private readonly Mock<IVivaldiService> _vivaldiServiceMock;
    private readonly ServiceConfiguration _config;

    public IntegrityCheckServiceTests()
    {
        _loggerMock = new Mock<ILogger<IntegrityCheckService>>();
        _manifestServiceMock = new Mock<IManifestService>();
        _vivaldiServiceMock = new Mock<IVivaldiService>();
        _config = new ServiceConfiguration
        {
            ManifestPath = Path.Combine(Path.GetTempPath(), "test-manifest.json"),
            IntegrityCheckIntervalSeconds = 60,
            IntegrityCheckStaggeringEnabled = true,
            MaxConsecutiveFailures = 5
        };
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IntegrityCheckService(null!, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IntegrityCheckService(_loggerMock.Object, null!, _manifestServiceMock.Object, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullManifestService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IntegrityCheckService(_loggerMock.Object, _config, null!, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullVivaldiService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        var service = new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Assert
        service.Should().NotBeNull();
        service.TotalChecksRun.Should().Be(0);
        service.TotalViolationsDetected.Should().Be(0);
        service.LastCheckTime.Should().BeNull();
        service.InstallationsWithViolations.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WhenManifestNotFound_DoesNotStartChecks()
    {
        // Arrange
        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(false);

        var service = new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - service should start but not perform checks
        service.TotalChecksRun.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WhenMonitoringDisabled_DoesNotStartChecks()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Settings = new GlobalSettings
            {
                MonitoringEnabled = false,
                AutoHealEnabled = true
            }
        };

        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(true);
        _manifestServiceMock.Setup(m => m.LoadManifestAsync(_config.ManifestPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var service = new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - service should start but not perform checks
        service.TotalChecksRun.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WhenAutoHealDisabled_DoesNotStartChecks()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Settings = new GlobalSettings
            {
                MonitoringEnabled = true,
                AutoHealEnabled = false
            }
        };

        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(true);
        _manifestServiceMock.Setup(m => m.LoadManifestAsync(_config.ManifestPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var service = new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - service should start but not perform checks
        service.TotalChecksRun.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WithMultipleInstallations_LogsStaggering()
    {
        // Arrange
        var manifest = new ManifestData
        {
            Settings = new GlobalSettings
            {
                MonitoringEnabled = true,
                AutoHealEnabled = true
            }
        };

        var installations = new List<VivaldiInstallation>
        {
            new VivaldiInstallation { Id = "install1" },
            new VivaldiInstallation { Id = "install2" },
            new VivaldiInstallation { Id = "install3" }
        };

        _manifestServiceMock.Setup(m => m.ManifestExists(_config.ManifestPath))
            .Returns(true);
        _manifestServiceMock.Setup(m => m.LoadManifestAsync(_config.ManifestPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);
        _vivaldiServiceMock.Setup(v => v.DetectInstallationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(installations);

        var service = new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - verify logging of stagger information
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("staggered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Statistics_InitiallyZero()
    {
        // Arrange
        var service = new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Assert
        service.TotalChecksRun.Should().Be(0);
        service.TotalViolationsDetected.Should().Be(0);
        service.LastCheckTime.Should().BeNull();
        service.InstallationsWithViolations.Should().Be(0);
    }

    [Fact]
    public void Dispose_DisposesResourcesProperly()
    {
        // Arrange
        var service = new IntegrityCheckService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        service.Dispose();

        // Assert - no exception should be thrown
        service.TotalChecksRun.Should().Be(0);
    }
}

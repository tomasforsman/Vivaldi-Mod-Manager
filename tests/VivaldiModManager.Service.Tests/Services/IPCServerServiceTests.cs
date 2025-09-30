using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.Configuration;
using VivaldiModManager.Service.IPC;
using VivaldiModManager.Service.Models;
using VivaldiModManager.Service.Services;

namespace VivaldiModManager.Service.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="IPCServerService"/> class.
/// </summary>
public class IPCServerServiceTests
{
    private readonly Mock<ILogger<IPCServerService>> _loggerMock;
    private readonly Mock<IManifestService> _manifestServiceMock;
    private readonly Mock<IVivaldiService> _vivaldiServiceMock;
    private readonly ServiceConfiguration _config;

    public IPCServerServiceTests()
    {
        _loggerMock = new Mock<ILogger<IPCServerService>>();
        _manifestServiceMock = new Mock<IManifestService>();
        _vivaldiServiceMock = new Mock<IVivaldiService>();
        _config = new ServiceConfiguration
        {
            IPCPipeName = $"TestPipe_{Guid.NewGuid()}",
            IPCTimeoutSeconds = 5,
            MaxConcurrentOperations = 3,
            ManifestPath = Path.Combine(Path.GetTempPath(), "test-manifest.json")
        };
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IPCServerService(null!, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IPCServerService(_loggerMock.Object, null!, _manifestServiceMock.Object, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullManifestService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IPCServerService(_loggerMock.Object, _config, null!, _vivaldiServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullVivaldiService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_StartsServiceSuccessfully()
    {
        // Arrange
        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Wait a moment for the service to initialize
        await Task.Delay(100);

        // Assert - service should start without throwing
        // We can't easily verify the server is listening without connecting to it

        // Cleanup
        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    [Fact]
    public async Task StartAsync_WhenPipeAlreadyInUse_ThrowsInvalidOperationException()
    {
        // Arrange
        var service1 = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service1.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Give it time to start

        var loggerMock2 = new Mock<ILogger<IPCServerService>>();
        var service2 = new IPCServerService(loggerMock2.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service2.StartAsync(CancellationToken.None));

        // Cleanup
        await service1.StopAsync(CancellationToken.None);
        service1.Dispose();
        service2.Dispose();
    }

    [Fact]
    public async Task StopAsync_StopsServiceGracefully()
    {
        // Arrange
        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - service should stop without throwing
        service.Dispose();
    }

    [Fact]
    public async Task GetServiceStatus_ReturnsValidStatus()
    {
        // Arrange
        _vivaldiServiceMock.Setup(v => v.DetectInstallationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Core.Models.VivaldiInstallation>());

        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Give server time to start

        // Act
        var response = await SendCommandAsync(_config.IPCPipeName, IPCCommand.GetServiceStatus);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();

        var statusJson = JsonSerializer.Serialize(response.Data);
        var status = JsonSerializer.Deserialize<ServiceStatus>(statusJson);
        status.Should().NotBeNull();
        status!.IsRunning.Should().BeTrue();
        status.MonitoringEnabled.Should().BeFalse();
        status.AutoHealEnabled.Should().BeFalse();
        status.SafeModeActive.Should().BeFalse();

        // Cleanup
        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    [Fact]
    public async Task GetHealthCheck_ReturnsValidHealthCheck()
    {
        // Arrange
        _manifestServiceMock.Setup(m => m.ManifestExists(It.IsAny<string>()))
            .Returns(false);

        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        // Act
        var response = await SendCommandAsync(_config.IPCPipeName, IPCCommand.GetHealthCheck);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();

        var healthJson = JsonSerializer.Serialize(response.Data);
        var health = JsonSerializer.Deserialize<HealthCheck>(healthJson);
        health.Should().NotBeNull();
        health!.ServiceRunning.Should().BeTrue();
        health.IPCServerRunning.Should().BeTrue();
        health.ManifestLoaded.Should().BeFalse();
        health.MonitoringActive.Should().BeFalse();
        health.IntegrityCheckActive.Should().BeFalse();

        // Cleanup
        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    [Fact]
    public async Task PlaceholderCommands_ReturnNotImplemented()
    {
        // Arrange
        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        var placeholderCommands = new[]
        {
            IPCCommand.TriggerAutoHeal,
            IPCCommand.EnableSafeMode,
            IPCCommand.DisableSafeMode,
            IPCCommand.ReloadManifest,
            IPCCommand.PauseMonitoring,
            IPCCommand.ResumeMonitoring,
            IPCCommand.GetMonitoringStatus
        };

        // Act & Assert
        foreach (var command in placeholderCommands)
        {
            var response = await SendCommandAsync(_config.IPCPipeName, command);
            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Error.Should().Contain("not yet implemented");
        }

        // Cleanup
        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    [Fact]
    public async Task InvalidMessage_ReturnsError()
    {
        // Arrange
        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        // Act
        var response = await SendInvalidMessageAsync(_config.IPCPipeName, "{ invalid json");

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Invalid JSON format");

        // Cleanup
        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    [Fact]
    public async Task MultipleClients_CanConnectSimultaneously()
    {
        // Arrange
        _vivaldiServiceMock.Setup(v => v.DetectInstallationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Core.Models.VivaldiInstallation>());

        var service = new IPCServerService(_loggerMock.Object, _config, _manifestServiceMock.Object, _vivaldiServiceMock.Object);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        // Act - Send multiple commands concurrently
        var tasks = new[]
        {
            SendCommandAsync(_config.IPCPipeName, IPCCommand.GetServiceStatus),
            SendCommandAsync(_config.IPCPipeName, IPCCommand.GetHealthCheck),
            SendCommandAsync(_config.IPCPipeName, IPCCommand.GetServiceStatus)
        };

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(3);
        responses.Should().AllSatisfy(r => r.Success.Should().BeTrue());

        // Cleanup
        await service.StopAsync(CancellationToken.None);
        service.Dispose();
    }

    private static async Task<IPCResponseMessage> SendCommandAsync(string pipeName, IPCCommand command)
    {
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);

        using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
        using var reader = new StreamReader(client, Encoding.UTF8);

        var commandMessage = new IPCCommandMessage
        {
            Command = command,
            MessageId = Guid.NewGuid().ToString()
        };

        var commandJson = JsonSerializer.Serialize(commandMessage);
        await writer.WriteLineAsync(commandJson);

        var responseJson = await reader.ReadLineAsync();
        return JsonSerializer.Deserialize<IPCResponseMessage>(responseJson!)!;
    }

    private static async Task<IPCResponseMessage> SendInvalidMessageAsync(string pipeName, string invalidMessage)
    {
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);

        using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
        using var reader = new StreamReader(client, Encoding.UTF8);

        await writer.WriteLineAsync(invalidMessage);

        var responseJson = await reader.ReadLineAsync();
        return JsonSerializer.Deserialize<IPCResponseMessage>(responseJson!)!;
    }
}

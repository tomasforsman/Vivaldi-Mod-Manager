using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.BackgroundServices;
using VivaldiModManager.Service.Configuration;
using VivaldiModManager.Service.IPC;
using VivaldiModManager.Service.Models;

namespace VivaldiModManager.Service.Services;

/// <summary>
/// Hosted service that provides IPC communication via named pipes.
/// </summary>
public class IPCServerService : IHostedService, IDisposable
{
    private readonly ILogger<IPCServerService> _logger;
    private readonly ServiceConfiguration _config;
    private readonly IManifestService _manifestService;
    private readonly IVivaldiService _vivaldiService;
    private readonly FileSystemMonitorService _fileSystemMonitorService;
    private readonly IntegrityCheckService _integrityCheckService;
    private readonly DateTimeOffset _startTime;
    private readonly List<Task> _clientTasks;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _serverTask;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IPCServerService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="config">The service configuration.</param>
    /// <param name="manifestService">The manifest service.</param>
    /// <param name="vivaldiService">The Vivaldi service.</param>
    /// <param name="fileSystemMonitorService">The file system monitor service.</param>
    /// <param name="integrityCheckService">The integrity check service.</param>
    public IPCServerService(
        ILogger<IPCServerService> logger,
        ServiceConfiguration config,
        IManifestService manifestService,
        IVivaldiService vivaldiService,
        FileSystemMonitorService fileSystemMonitorService,
        IntegrityCheckService integrityCheckService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _manifestService = manifestService ?? throw new ArgumentNullException(nameof(manifestService));
        _vivaldiService = vivaldiService ?? throw new ArgumentNullException(nameof(vivaldiService));
        _fileSystemMonitorService = fileSystemMonitorService ?? throw new ArgumentNullException(nameof(fileSystemMonitorService));
        _integrityCheckService = integrityCheckService ?? throw new ArgumentNullException(nameof(integrityCheckService));
        _startTime = DateTimeOffset.UtcNow;
        _clientTasks = new List<Task>();
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting IPC Server on pipe: {PipeName}", _config.IPCPipeName);

        // Check if pipe already exists (another instance running)
        if (IsPipeInUse(_config.IPCPipeName))
        {
            _logger.LogError("Named pipe {PipeName} is already in use. Another service instance may be running.", _config.IPCPipeName);
            throw new InvalidOperationException($"Named pipe '{_config.IPCPipeName}' is already in use.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = true;
        _serverTask = Task.Run(() => RunServerAsync(_cancellationTokenSource.Token), cancellationToken);

        _logger.LogInformation("IPC Server started successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping IPC Server");

        _isRunning = false;
        _cancellationTokenSource?.Cancel();

        if (_serverTask != null)
        {
            await _serverTask.ConfigureAwait(false);
        }

        // Wait for all client tasks to complete
        await Task.WhenAll(_clientTasks).ConfigureAwait(false);

        _logger.LogInformation("IPC Server stopped");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }

    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                var pipeSecurity = CreatePipeSecurity();
                using var pipeServer = NamedPipeServerStreamAcl.Create(
                    _config.IPCPipeName,
                    PipeDirection.InOut,
                    10, // Max 10 concurrent connections
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    4096,
                    4096,
                    pipeSecurity);

                _logger.LogDebug("Waiting for client connection on pipe: {PipeName}", _config.IPCPipeName);
                await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Client connected to pipe");

                // Handle client in a separate task
                var clientTask = Task.Run(async () =>
                {
                    try
                    {
                        await HandleClientAsync(pipeServer, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling client connection");
                    }
                }, cancellationToken);

                lock (_clientTasks)
                {
                    _clientTasks.Add(clientTask);
                    // Clean up completed tasks
                    _clientTasks.RemoveAll(t => t.IsCompleted);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("IPC Server operation cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IPC Server main loop");
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(pipeServer, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(pipeServer, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            var requestJson = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(requestJson))
            {
                _logger.LogWarning("Received empty request from client");
                return;
            }

            _logger.LogDebug("Received IPC request: {Request}", requestJson);

            IPCCommandMessage? command;
            try
            {
                command = JsonSerializer.Deserialize<IPCCommandMessage>(requestJson);
                if (command == null)
                {
                    await SendErrorResponseAsync(writer, string.Empty, "Invalid command format", cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize IPC command");
                await SendErrorResponseAsync(writer, string.Empty, "Invalid JSON format", cancellationToken).ConfigureAwait(false);
                return;
            }

            var response = await ProcessCommandAsync(command, cancellationToken).ConfigureAwait(false);
            var responseJson = JsonSerializer.Serialize(response);
            _logger.LogDebug("Sending IPC response: {Response}", responseJson);
            await writer.WriteLineAsync(responseJson).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Client disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client request");
        }
    }

    private async Task<IPCResponseMessage> ProcessCommandAsync(IPCCommandMessage command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing IPC command: {Command}", command.Command);

        try
        {
            switch (command.Command)
            {
                case IPCCommand.GetServiceStatus:
                    return await HandleGetServiceStatusAsync(command, cancellationToken).ConfigureAwait(false);

                case IPCCommand.GetHealthCheck:
                    return await HandleGetHealthCheckAsync(command, cancellationToken).ConfigureAwait(false);

                case IPCCommand.GetMonitoringStatus:
                    return HandleGetMonitoringStatus(command);

                case IPCCommand.PauseMonitoring:
                    return HandlePauseMonitoring(command);

                case IPCCommand.ResumeMonitoring:
                    return await HandleResumeMonitoringAsync(command).ConfigureAwait(false);

                case IPCCommand.TriggerAutoHeal:
                case IPCCommand.EnableSafeMode:
                case IPCCommand.DisableSafeMode:
                case IPCCommand.ReloadManifest:
                    return CreateNotImplementedResponse(command, $"{command.Command} is not yet implemented. This will be available in future issues (#38).");

                default:
                    return CreateErrorResponse(command, $"Unknown command: {command.Command}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command: {Command}", command.Command);
            return CreateErrorResponse(command, $"Error processing command: {ex.Message}");
        }
    }

    private async Task<IPCResponseMessage> HandleGetServiceStatusAsync(IPCCommandMessage command, CancellationToken cancellationToken)
    {
        var status = new ServiceStatus
        {
            IsRunning = _isRunning,
            StartTime = _startTime,
            Uptime = DateTimeOffset.UtcNow - _startTime,
            MonitoringEnabled = false,
            AutoHealEnabled = false,
            SafeModeActive = false,
            ManagedInstallations = 0,
            LastOperation = null,
            LastOperationTime = null,
            TotalHealsAttempted = 0,
            TotalHealsSucceeded = 0,
            TotalHealsFailed = 0
        };

        // Try to get managed installations count
        try
        {
            var installations = await _vivaldiService.DetectInstallationsAsync(cancellationToken).ConfigureAwait(false);
            status.ManagedInstallations = installations.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect Vivaldi installations for status");
        }

        return CreateSuccessResponse(command, status);
    }

    private Task<IPCResponseMessage> HandleGetHealthCheckAsync(IPCCommandMessage command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        bool manifestLoaded = false;

        // Check if manifest exists and is loadable
        try
        {
            manifestLoaded = _manifestService.ManifestExists(_config.ManifestPath);
            if (!manifestLoaded)
            {
                errors.Add($"Manifest file not found at: {_config.ManifestPath}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error checking manifest: {ex.Message}");
        }

        // Check monitoring and integrity check status
        bool monitoringActive = !_fileSystemMonitorService.IsPaused && _fileSystemMonitorService.ActiveWatcherCount > 0;
        bool integrityCheckActive = _integrityCheckService.LastCheckTime != null;

        var healthCheck = new HealthCheck
        {
            ServiceRunning = _isRunning,
            ManifestLoaded = manifestLoaded,
            IPCServerRunning = _isRunning,
            MonitoringActive = monitoringActive,
            IntegrityCheckActive = integrityCheckActive,
            LastHealthCheckTime = DateTimeOffset.UtcNow,
            Errors = errors
        };

        return Task.FromResult(CreateSuccessResponse(command, healthCheck));
    }

    private IPCResponseMessage HandleGetMonitoringStatus(IPCCommandMessage command)
    {
        var status = new MonitoringStatus
        {
            MonitoringEnabled = true, // Will be determined from manifest in actual implementation
            ActiveWatcherCount = _fileSystemMonitorService.ActiveWatcherCount,
            TotalFileChanges = _fileSystemMonitorService.TotalFileChanges,
            TotalVivaldiChanges = _fileSystemMonitorService.TotalVivaldiChanges,
            LastChangeTime = _fileSystemMonitorService.LastChangeTime,
            TotalChecksRun = _integrityCheckService.TotalChecksRun,
            TotalViolationsDetected = _integrityCheckService.TotalViolationsDetected,
            LastCheckTime = _integrityCheckService.LastCheckTime,
            InstallationsWithViolations = _integrityCheckService.InstallationsWithViolations
        };

        return CreateSuccessResponse(command, status);
    }

    private IPCResponseMessage HandlePauseMonitoring(IPCCommandMessage command)
    {
        try
        {
            _fileSystemMonitorService.PauseMonitoring();
            _logger.LogInformation("Monitoring paused via IPC command");
            return CreateSuccessResponse(command, new { Message = "Monitoring paused successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing monitoring");
            return CreateErrorResponse(command, $"Error pausing monitoring: {ex.Message}");
        }
    }

    private async Task<IPCResponseMessage> HandleResumeMonitoringAsync(IPCCommandMessage command)
    {
        try
        {
            await _fileSystemMonitorService.ResumeMonitoringAsync().ConfigureAwait(false);
            _logger.LogInformation("Monitoring resumed via IPC command");
            return CreateSuccessResponse(command, new { Message = "Monitoring resumed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming monitoring");
            return CreateErrorResponse(command, $"Error resuming monitoring: {ex.Message}");
        }
    }

    private static IPCResponseMessage CreateSuccessResponse(IPCCommandMessage command, object data)
    {
        return new IPCResponseMessage
        {
            MessageId = command.MessageId,
            Success = true,
            Data = data
        };
    }

    private static IPCResponseMessage CreateErrorResponse(IPCCommandMessage command, string error)
    {
        return new IPCResponseMessage
        {
            MessageId = command.MessageId,
            Success = false,
            Error = error
        };
    }

    private static IPCResponseMessage CreateNotImplementedResponse(IPCCommandMessage command, string message)
    {
        return new IPCResponseMessage
        {
            MessageId = command.MessageId,
            Success = false,
            Error = message
        };
    }

    private async Task SendErrorResponseAsync(StreamWriter writer, string messageId, string error, CancellationToken cancellationToken)
    {
        var response = new IPCResponseMessage
        {
            MessageId = messageId,
            Success = false,
            Error = error
        };
        var responseJson = JsonSerializer.Serialize(response);
        await writer.WriteLineAsync(responseJson).ConfigureAwait(false);
    }

    private static PipeSecurity CreatePipeSecurity()
    {
        var pipeSecurity = new PipeSecurity();

        // Allow current user
        var currentUser = WindowsIdentity.GetCurrent().User;
        if (currentUser != null)
        {
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                currentUser,
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
        }

        // Allow Administrators group
        var adminsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        pipeSecurity.AddAccessRule(new PipeAccessRule(
            adminsSid,
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));

        return pipeSecurity;
    }

    private bool IsPipeInUse(string pipeName)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None);
            client.Connect(100); // Try to connect with short timeout
            return true; // If we can connect, the pipe is in use
        }
        catch (TimeoutException)
        {
            return false; // Pipe doesn't exist or not responding
        }
        catch (IOException)
        {
            return false; // Pipe doesn't exist
        }
    }
}

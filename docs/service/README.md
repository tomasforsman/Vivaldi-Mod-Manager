# Vivaldi Mod Manager Service Documentation

## Overview

The Vivaldi Mod Manager Service is a Windows background service that provides continuous monitoring, auto-healing capabilities, and IPC (Inter-Process Communication) for the Vivaldi Mod Manager system. It runs independently of the UI application and ensures that modifications remain intact across Vivaldi updates.

## Features

- **Continuous Background Operation**: Runs as a Windows Service, starting automatically on system boot
- **IPC Communication**: Named pipe-based communication for UI and CLI integration
- **Health Monitoring**: Provides health check endpoints for diagnostics
- **Service Status**: Real-time service metrics and operational status
- **Configuration-Based**: Flexible configuration via `appsettings.json`
- **Secure**: Named pipe access restricted to current user and Administrators group

## Installation

### Prerequisites

- Windows operating system
- .NET 8.0 Runtime installed
- Administrator privileges

### Installation Steps

1. **Build the Service** (if not already built):
   ```powershell
   cd Vivaldi-Mod-Manager
   dotnet build --configuration Release
   ```

2. **Run the Installation Script** (as Administrator):
   ```powershell
   # Right-click PowerShell and select "Run as Administrator"
   cd Vivaldi-Mod-Manager/scripts
   .\install-service.ps1
   ```

   The script will:
   - Check for administrator privileges
   - Verify the service binary exists
   - Check if service is already installed
   - Create and configure the service
   - Set up automatic startup
   - Configure recovery options (restart on failure)
   - Start the service
   - Verify the service is running

3. **Custom Binary Path** (optional):
   ```powershell
   .\install-service.ps1 -BinaryPath "C:\Custom\Path\VivaldiModManager.Service.exe"
   ```

### Verification

Check that the service is running:
```powershell
Get-Service VivaldiModManagerService
```

Expected output:
```
Status   Name                     DisplayName
------   ----                     -----------
Running  VivaldiModManagerService Vivaldi Mod Manager Service
```

## Uninstallation

To uninstall the service (as Administrator):
```powershell
cd Vivaldi-Mod-Manager/scripts
.\uninstall-service.ps1
```

The script will:
- Stop the service if running
- Remove the service registration
- Verify successful deletion

**Note**: Configuration files in `%APPDATA%\VivaldiModManager` are not deleted and must be removed manually if desired.

## Configuration

The service is configured via `appsettings.json` located in the service directory:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "EventLog": {
      "LogLevel": {
        "Default": "Warning",
        "VivaldiModManager": "Information"
      }
    },
    "Console": {
      "LogLevel": {
        "Default": "Debug",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  },
  "ServiceConfiguration": {
    "ManifestPath": "%APPDATA%\\VivaldiModManager\\manifest.json",
    "LogDirectory": "%APPDATA%\\VivaldiModManager\\Logs",
    "IPCPipeName": "VivaldiModManagerPipe",
    "IPCTimeoutSeconds": 30,
    "MaxConcurrentOperations": 5,
    "ServiceStartupTimeoutSeconds": 10
  }
}
```

### Configuration Options

#### ManifestPath
- **Type**: String
- **Default**: `%APPDATA%\VivaldiModManager\manifest.json`
- **Description**: Path to the manifest file. Environment variables are automatically expanded.
- **Example**: `C:\ProgramData\VivaldiModManager\manifest.json`

#### LogDirectory
- **Type**: String
- **Default**: `%APPDATA%\VivaldiModManager\Logs`
- **Description**: Directory for file-based logs. Environment variables are automatically expanded.

#### IPCPipeName
- **Type**: String
- **Default**: `VivaldiModManagerPipe`
- **Description**: Name of the named pipe for IPC communication. Must be unique on the system.
- **Note**: If you run multiple instances (not recommended), each must have a unique pipe name.

#### IPCTimeoutSeconds
- **Type**: Integer
- **Default**: `30`
- **Description**: Timeout in seconds for IPC operations.

#### MaxConcurrentOperations
- **Type**: Integer
- **Default**: `5`
- **Description**: Maximum number of concurrent operations the service can perform.

#### ServiceStartupTimeoutSeconds
- **Type**: Integer
- **Default**: `10`
- **Description**: Maximum time in seconds to wait for the service to reach Running state during installation.

## IPC Communication

The service uses Windows Named Pipes for IPC, providing a request-response pattern with JSON serialization.

### Security Model

- **Local Machine Only**: Named pipes are restricted to the local machine
- **Access Control**: Only the current user and Administrators group can connect
- **No Network Access**: IPC is not accessible over the network

### Message Format

#### Command Message (Client → Service)
```json
{
  "command": "GetServiceStatus",
  "parameters": {},
  "messageId": "unique-guid"
}
```

#### Response Message (Service → Client)
```json
{
  "messageId": "same-guid-as-request",
  "success": true,
  "data": { /* command-specific data */ },
  "error": null
}
```

### Available Commands

#### GetServiceStatus
Returns the current service status and metrics.

**Request**:
```json
{
  "command": "GetServiceStatus",
  "messageId": "12345"
}
```

**Response**:
```json
{
  "messageId": "12345",
  "success": true,
  "data": {
    "isRunning": true,
    "startTime": "2025-01-01T12:00:00Z",
    "uptime": "01:30:00",
    "monitoringEnabled": false,
    "autoHealEnabled": false,
    "safeModeActive": false,
    "managedInstallations": 1,
    "lastOperation": null,
    "lastOperationTime": null,
    "totalHealsAttempted": 0,
    "totalHealsSucceeded": 0,
    "totalHealsFailed": 0
  }
}
```

#### GetHealthCheck
Returns detailed health information about the service.

**Request**:
```json
{
  "command": "GetHealthCheck",
  "messageId": "67890"
}
```

**Response**:
```json
{
  "messageId": "67890",
  "success": true,
  "data": {
    "serviceRunning": true,
    "manifestLoaded": true,
    "ipcServerRunning": true,
    "monitoringActive": false,
    "integrityCheckActive": false,
    "lastHealthCheckTime": "2025-01-01T13:30:00Z",
    "errors": []
  }
}
```

#### Placeholder Commands (Not Yet Implemented)

The following commands return "not yet implemented" responses and will be available in future releases:

- **TriggerAutoHeal** - Trigger manual auto-heal operation (Issue #38)
- **EnableSafeMode** - Enable safe mode to disable all mods (Issue #38)
- **DisableSafeMode** - Disable safe mode (Issue #38)
- **ReloadManifest** - Reload the manifest from disk (Issue #37)
- **PauseMonitoring** - Pause file system monitoring (Issue #37)
- **ResumeMonitoring** - Resume file system monitoring (Issue #37)
- **GetMonitoringStatus** - Get monitoring status (Issue #37)

### Example: Connecting to the Service (C#)

```csharp
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

async Task<object> SendCommandAsync(string command)
{
    using var client = new NamedPipeClientStream(".", "VivaldiModManagerPipe", 
        PipeDirection.InOut, PipeOptions.Asynchronous);
    
    await client.ConnectAsync(5000);
    
    using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
    using var reader = new StreamReader(client, Encoding.UTF8);
    
    var request = new
    {
        command = command,
        messageId = Guid.NewGuid().ToString()
    };
    
    await writer.WriteLineAsync(JsonSerializer.Serialize(request));
    
    var responseJson = await reader.ReadLineAsync();
    return JsonSerializer.Deserialize<object>(responseJson);
}

// Usage
var status = await SendCommandAsync("GetServiceStatus");
```

## Service Management

### Checking Service Status
```powershell
Get-Service VivaldiModManagerService
```

### Starting the Service
```powershell
Start-Service VivaldiModManagerService
```

### Stopping the Service
```powershell
Stop-Service VivaldiModManagerService
```

### Restarting the Service
```powershell
Restart-Service VivaldiModManagerService
```

### Viewing Service Properties
```powershell
Get-Service VivaldiModManagerService | Format-List *
```

## Logging

The service logs to multiple destinations:

### Windows Event Log
- **Log Name**: Application
- **Source**: VivaldiModManagerService
- **Levels**: Warning and above

View logs:
```powershell
Get-EventLog -LogName Application -Source VivaldiModManagerService -Newest 10
```

### Console Output
- Used during debugging when running the service interactively
- **Levels**: Debug and above

### File Logs (Future)
- Will be implemented in the LogDirectory specified in configuration

## Resource Usage

### Expected Resource Usage
- **Memory**: ~50-100 MB (depending on number of managed installations)
- **CPU**: Minimal when idle; brief spikes during monitoring checks
- **Disk I/O**: Low; primarily reads during health checks
- **Network**: None (no network communication)

### Service Behavior
- **Startup Time**: < 2 seconds typical
- **Shutdown Time**: < 1 second typical
- **Recovery**: Automatically restarts on failure (up to 3 times per day)

## Troubleshooting

### Service Won't Start

**Symptom**: Service fails to start or immediately stops after starting.

**Solutions**:
1. **Check Event Viewer**:
   ```powershell
   Get-EventLog -LogName Application -Source VivaldiModManagerService -Newest 10
   ```
   Look for error messages indicating the cause.

2. **Verify Manifest Path**:
   - Ensure the path in `appsettings.json` is correct
   - Create the directory if it doesn't exist:
     ```powershell
     New-Item -ItemType Directory -Force -Path "$env:APPDATA\VivaldiModManager"
     ```

3. **Check .NET Runtime**:
   ```powershell
   dotnet --version
   ```
   Ensure .NET 8.0 or higher is installed.

4. **Verify Binary Exists**:
   ```powershell
   Test-Path ".\src\VivaldiModManager.Service\bin\Release\net8.0-windows\VivaldiModManager.Service.exe"
   ```

5. **Review Permissions**:
   - Ensure the service account has read access to the binary path
   - Ensure write access to the LogDirectory and ManifestPath

### IPC Connection Fails

**Symptom**: UI or CLI cannot connect to the service.

**Solutions**:
1. **Verify Service is Running**:
   ```powershell
   Get-Service VivaldiModManagerService
   ```
   If not running, start it:
   ```powershell
   Start-Service VivaldiModManagerService
   ```

2. **Check Pipe Name**:
   - Ensure both client and service use the same IPCPipeName
   - Default is `VivaldiModManagerPipe`

3. **Verify Permissions**:
   - Ensure your user account has permission to access the pipe
   - The service restricts access to current user + Administrators

4. **Firewall Not the Issue**:
   - Named pipes are local-only and don't go through the firewall
   - No firewall rules needed

### Service Already Running

**Symptom**: Installation script reports service already exists.

**Solutions**:
1. **Intentional Protection**: This prevents duplicate installations
2. **Uninstall First**:
   ```powershell
   .\uninstall-service.ps1
   ```
3. **Then Reinstall**:
   ```powershell
   .\install-service.ps1
   ```

### Service Consuming Too Many Resources

**Symptom**: High CPU or memory usage.

**Solutions**:
1. **Check for Errors**: Review Event Log for repeated errors causing retry loops
2. **Monitor Managed Installations**: Verify the count is as expected
3. **Restart Service**:
   ```powershell
   Restart-Service VivaldiModManagerService
   ```

### Manifest Not Found

**Symptom**: Health check reports manifest not loaded.

**Solutions**:
1. **Create Default Manifest**: The service should create one automatically, but you can create it manually:
   ```powershell
   New-Item -ItemType Directory -Force -Path "$env:APPDATA\VivaldiModManager"
   # Let the service create the default manifest on next run
   Restart-Service VivaldiModManagerService
   ```

2. **Verify Path**: Check `appsettings.json` ManifestPath setting

## FAQ

### Q: Can I run multiple instances of the service?
**A**: No, by design. The service detects if another instance is using the named pipe and fails to start. This prevents conflicts and ensures only one service manages Vivaldi modifications.

### Q: Does the service require internet access?
**A**: No, the service operates entirely locally and does not require network access.

### Q: Can I access the service remotely?
**A**: No, named pipes are local-machine only. For remote management, you would need to implement a separate remote access solution.

### Q: What happens if Vivaldi updates while the service is running?
**A**: This functionality will be implemented in issue #37 (Monitoring) and #38 (Auto-Healing). Currently, the service provides the foundation but doesn't actively monitor for updates.

### Q: How do I know if the service is working correctly?
**A**: Use the GetHealthCheck command via IPC, or check the Windows Event Log for service activity.

### Q: Can I customize log levels?
**A**: Yes, edit the `Logging` section in `appsettings.json` and restart the service.

### Q: What if I need to move the manifest to a different location?
**A**: 
1. Stop the service
2. Update the `ManifestPath` in `appsettings.json`
3. Move or copy the manifest file to the new location
4. Start the service

## Next Steps

- **Issue #37**: File system monitoring and manifest reloading
- **Issue #38**: Auto-healing capabilities and safe mode
- **Future**: Enhanced health monitoring and diagnostics
- **Future**: Multi-installation support with per-installation status

## Support

For issues, feature requests, or contributions:
- GitHub Issues: https://github.com/tomasforsman/Vivaldi-Mod-Manager/issues
- See CONTRIBUTING.md for contribution guidelines
- See SECURITY.md for security vulnerability reporting

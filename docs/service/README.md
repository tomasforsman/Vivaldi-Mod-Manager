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
    "ServiceStartupTimeoutSeconds": 10,
    "MonitoringDebounceMs": 2000,
    "IntegrityCheckIntervalSeconds": 60,
    "IntegrityCheckStaggeringEnabled": true,
    "MaxConsecutiveFailures": 5
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

#### MonitoringDebounceMs
- **Type**: Integer
- **Default**: `2000`
- **Description**: Debounce time in milliseconds for file system changes. Prevents multiple rapid changes from triggering multiple events.
- **Recommended**: 2000ms (2 seconds) for most users. Increase if working with slow storage or remote filesystems.

#### IntegrityCheckIntervalSeconds
- **Type**: Integer
- **Default**: `60`
- **Description**: Interval in seconds between integrity checks of Vivaldi installations.
- **Recommended**: 60-300 seconds. Lower values detect problems faster but use more resources.

#### IntegrityCheckStaggeringEnabled
- **Type**: Boolean
- **Default**: `true`
- **Description**: When true, distributes integrity checks evenly across the interval for multiple installations to avoid CPU spikes.
- **Note**: Only applies when 3 or more installations are managed.

#### MaxConsecutiveFailures
- **Type**: Integer
- **Default**: `5`
- **Description**: Number of consecutive integrity check failures before escalating alert level to Error.

## Monitoring and Integrity Checks

The service continuously monitors file system changes and periodically verifies the integrity of mod injections.

### File System Monitoring

The file system monitor watches for changes to:
- **Mod files**: All `.js` files in the mods directory (recursive)
- **Vivaldi installations**: All files in `resources/vivaldi` folder for each managed installation

**Key Features**:
- **Debouncing**: Rapid file changes are consolidated into single events (configurable via `MonitoringDebounceMs`)
- **Filtering**: Temporary files (`.tmp`, `.bak`, `.swp`, files ending with `~`) are automatically ignored
- **Event Publishing**: File change events are logged and available for auto-heal consumption
- **Pause/Resume**: Monitoring can be paused and resumed via IPC commands

**Behavior**:
- Monitoring starts automatically if `MonitoringEnabled` is true in the manifest
- If the mods directory doesn't exist, a warning is logged but the service continues
- Each Vivaldi installation gets its own file system watcher
- Statistics are tracked: total file changes, total Vivaldi changes, last change time

**Resource Considerations**:
- Each installation adds one `FileSystemWatcher`
- Most users will have 1-3 installations (minimal impact)
- 5+ installations may slightly impact performance but is fully supported
- Watchers use kernel-level notifications (very efficient)

**Limitations**:
- FileSystemWatcher may not work reliably on network drives or unusual filesystems
- Some antivirus software may interfere with file system notifications
- Very rapid changes (1000+ files/second) may overwhelm the watcher

### Integrity Checks

The integrity check service periodically verifies:
1. **Injection Stubs**: Checks that each target file (window.html, browser.html) contains the injection stub comment
2. **Fingerprint Matching**: Verifies the injection fingerprint matches the expected value
3. **Loader Presence**: Confirms loader.js exists in the vivaldi-mods folder
4. **Mod File Existence**: Validates all enabled mod files exist

**Check Behavior**:
- Runs every `IntegrityCheckIntervalSeconds` (default 60 seconds)
- First check runs 5 seconds after service starts
- Checks are skipped if:
  - Safe Mode is active
  - Auto-heal is disabled
  - No manifest found

**Staggered Checking**:
When `IntegrityCheckStaggeringEnabled` is true and 3+ installations are managed:
- Checks are distributed evenly across the interval
- Example: 3 installations with 60s interval = one check every 20s
- Prevents CPU spikes when checking many installations simultaneously
- Stagger timing is logged at service startup

**Violation Tracking**:
- Consecutive failures are tracked per installation
- Logging escalates based on failure count:
  - First violation: Warning
  - 2-3 violations: Warning with count
  - 4+ violations: Error with count
- Failure counter resets when a check passes
- Events include violation details and consecutive failure count

**Statistics**:
- Total checks run
- Total violations detected
- Last check timestamp
- Count of installations currently with violations

### Events

The monitoring services publish events that can be consumed by other components (such as the auto-heal service):

#### FileChanged Event
```csharp
public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
```

**When**: Triggered after debounce period when a mod file changes
**Example**: User edits a mod file and saves - event fires 2 seconds later

#### VivaldiChanged Event
```csharp
public class VivaldiChangedEventArgs : EventArgs
{
    public string FilePath { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string InstallationId { get; init; }
}
```

**When**: Triggered after debounce period when a file in a Vivaldi installation changes
**Example**: Vivaldi updates itself - events fire for each changed file

#### IntegrityViolation Event
```csharp
public class IntegrityViolationEventArgs : EventArgs
{
    public VivaldiInstallation Installation { get; init; }
    public List<string> Violations { get; init; }
    public int ConsecutiveFailures { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
```

**When**: Triggered when an integrity check detects violations
**Example**: User manually removes injection stub - violation detected within 60s

### Monitoring Statistics

View current monitoring statistics using the `GetMonitoringStatus` IPC command:

```json
{
  "monitoringEnabled": true,
  "activeWatcherCount": 3,
  "totalFileChanges": 42,
  "totalVivaldiChanges": 15,
  "lastChangeTime": "2024-01-15T10:30:00Z",
  "totalChecksRun": 120,
  "totalViolationsDetected": 2,
  "lastCheckTime": "2024-01-15T10:31:00Z",
  "installationsWithViolations": 0
}
```

## Auto-Heal Service

The Auto-Heal Service automatically restores mod injections when Vivaldi updates or integrity violations are detected, ensuring mods persist across browser updates without manual intervention.

### Overview

Auto-heal responds to two types of events:
- **Vivaldi Updates**: Detected when new version folders appear
- **Integrity Violations**: Detected when injection stubs are missing or corrupted

### Key Features

**Folder Stabilization**: Waits for Vivaldi files to be accessible before injecting (up to 30 seconds)

**Cooldown Period**: 30-second cooldown between heals for the same installation prevents thrashing

**Retry Logic**: Failed heals are automatically retried with exponential backoff:
  - First retry: After 5 seconds
  - Second retry: After 30 seconds
  - Third retry: After 2 minutes
  - After 3 failures, stops retrying and logs error

**Rollback on Failure**: If a heal fails partway through, the service attempts to roll back changes

**Heal History**: Maintains a circular buffer of the last 50 heal attempts with full details

### Configuration

In `appsettings.json`:

```json
{
  "ServiceConfiguration": {
    "AutoHealRetryDelays": [5, 30, 120],
    "AutoHealMaxRetries": 3,
    "AutoHealCooldownSeconds": 30,
    "VivaldiFolderStabilizationMaxWaitSeconds": 30,
    "HealHistoryMaxEntries": 50,
    "HealHistoryFilePath": "%APPDATA%\\VivaldiModManager\\heal-history.json"
  }
}
```

### Metrics

The service tracks:
- `TotalHealsAttempted`: Total number of heal attempts
- `TotalHealsSucceeded`: Number of successful heals
- `TotalHealsFailed`: Number of failed heals after max retries

Access metrics via `GetServiceStatus` IPC command.

### Detailed Documentation

See [Auto-Heal Documentation](auto-heal.md) for complete details on:
- Heal process steps
- Folder stabilization logic
- Cooldown behavior
- Retry logic
- Rollback mechanism
- Heal history
- Troubleshooting

## Safe Mode

Safe Mode is an emergency feature that immediately disables all mod injections across all Vivaldi installations.

### When to Use

Use Safe Mode when:
- A mod is causing Vivaldi to crash or behave incorrectly
- You need to troubleshoot whether mods are causing an issue
- You want to temporarily disable all mods without losing your configuration

### How It Works

**Activation**:
1. Sets `SafeModeActive` flag in manifest
2. Cancels any pending heal operations
3. Removes injection stubs from all installations
4. Blocks auto-heal from running
5. Persists state across service restarts

**Deactivation**:
1. Clears `SafeModeActive` flag
2. Queues heal requests for all installations
3. Resumes auto-heal operation
4. Mods are automatically restored

### IPC Commands

Enable Safe Mode:
```json
{
  "command": "EnableSafeMode",
  "messageId": "12345"
}
```

Disable Safe Mode:
```json
{
  "command": "DisableSafeMode",
  "messageId": "12345"
}
```

### Persistence

Safe Mode state persists across:
- Service restarts
- System reboots
- Vivaldi updates

### Detailed Documentation

See [Safe Mode Documentation](safe-mode.md) for complete details on:
- Activation/deactivation process
- Impact on other features
- Error handling
- Use cases
- Best practices
- Troubleshooting

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
    "monitoringActive": true,
    "integrityCheckActive": true,
    "lastHealthCheckTime": "2025-01-01T13:30:00Z",
    "errors": []
  }
}
```

**Fields**:
- `monitoringActive`: True if file system monitoring is running and not paused
- `integrityCheckActive`: True if integrity checks are running (at least one check has completed)

#### GetMonitoringStatus
Returns detailed monitoring statistics and status.

**Request**:
```json
{
  "command": "GetMonitoringStatus",
  "messageId": "abc123"
}
```

**Response**:
```json
{
  "messageId": "abc123",
  "success": true,
  "data": {
    "monitoringEnabled": true,
    "activeWatcherCount": 3,
    "totalFileChanges": 42,
    "totalVivaldiChanges": 15,
    "lastChangeTime": "2024-01-15T10:30:00Z",
    "totalChecksRun": 120,
    "totalViolationsDetected": 2,
    "lastCheckTime": "2024-01-15T10:31:00Z",
    "installationsWithViolations": 0
  }
}
```

**Fields**:
- `monitoringEnabled`: Whether monitoring is enabled in manifest
- `activeWatcherCount`: Number of active FileSystemWatcher instances
- `totalFileChanges`: Cumulative count of mod file changes detected
- `totalVivaldiChanges`: Cumulative count of Vivaldi installation changes
- `lastChangeTime`: Timestamp of most recent change (null if no changes yet)
- `totalChecksRun`: Cumulative count of integrity checks performed
- `totalViolationsDetected`: Cumulative count of violations found
- `lastCheckTime`: Timestamp of most recent integrity check (null if no checks yet)
- `installationsWithViolations`: Current count of installations with active violations

#### PauseMonitoring
Pauses file system monitoring. Integrity checks continue to run.

**Request**:
```json
{
  "command": "PauseMonitoring",
  "messageId": "pause1"
}
```

**Response**:
```json
{
  "messageId": "pause1",
  "success": true,
  "data": {
    "message": "Monitoring paused successfully"
  }
}
```

**Use Cases**:
- During bulk mod file operations
- When making manual changes that shouldn't trigger events
- Troubleshooting performance issues

#### ResumeMonitoring
Resumes file system monitoring after it was paused.

**Request**:
```json
{
  "command": "ResumeMonitoring",
  "messageId": "resume1"
}
```

**Response**:
```json
{
  "messageId": "resume1",
  "success": true,
  "data": {
    "message": "Monitoring resumed successfully"
  }
}
```

**Note**: Watchers are recreated when resuming, so there may be a brief delay before changes are detected.

#### TriggerAutoHeal

Manually triggers auto-heal for all managed installations.

**Request**:
```json
{
  "command": "TriggerAutoHeal",
  "messageId": "guid"
}
```

**Response**:
```json
{
  "messageId": "same-guid",
  "success": true,
  "data": {
    "message": "Heal requested for 2 installation(s)",
    "count": 2
  }
}
```

#### EnableSafeMode

Activates Safe Mode, disabling all mod injections.

**Request**:
```json
{
  "command": "EnableSafeMode",
  "messageId": "guid"
}
```

**Response**:
```json
{
  "messageId": "same-guid",
  "success": true,
  "data": {
    "message": "Safe Mode activated, processed 2 installation(s)",
    "count": 2
  }
}
```

#### DisableSafeMode

Deactivates Safe Mode and queues heal requests to restore mods.

**Request**:
```json
{
  "command": "DisableSafeMode",
  "messageId": "guid"
}
```

**Response**:
```json
{
  "messageId": "same-guid",
  "success": true,
  "data": {
    "message": "Safe Mode deactivated, 2 heal request(s) queued",
    "count": 2
  }
}
```

#### ReloadManifest

Reloads the manifest from disk.

**Request**:
```json
{
  "command": "ReloadManifest",
  "messageId": "guid"
}
```

**Response**:
```json
{
  "messageId": "same-guid",
  "success": true,
  "data": {
    "message": "Manifest reloaded successfully",
    "monitoringEnabled": true,
    "autoHealEnabled": true,
    "safeModeActive": false
  }
}
```

#### GetHealHistory

Retrieves the heal history (last 50 attempts by default).

**Request**:
```json
{
  "command": "GetHealHistory",
  "messageId": "guid"
}
```

**Response**:
```json
{
  "messageId": "same-guid",
  "success": true,
  "data": [
    {
      "installationId": "vivaldi-stable",
      "timestamp": "2024-01-15T10:30:00Z",
      "triggerReason": "VivaldiUpdate",
      "success": true,
      "errorMessage": null,
      "retryCount": 0,
      "duration": "00:00:02.5"
    }
  ]
}
```

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

### Monitoring Not Working

**Symptom**: File changes not detected, or integrity checks not running.

**Solutions**:
1. **Check Monitoring Status**:
   Send `GetMonitoringStatus` command via IPC to see current state. Look at `activeWatcherCount` (should be 1+ per installation) and `monitoringEnabled`.

2. **Verify Manifest Settings**:
   Check that `monitoringEnabled` is `true` in manifest.

3. **Check Event Log**:
   ```powershell
   Get-EventLog -LogName Application -Source VivaldiModManagerService -After (Get-Date).AddHours(-1) | 
     Where-Object { $_.Message -like "*monitor*" -or $_.Message -like "*integrity*" }
   ```
   Look for "Starting File System Monitor Service" and "Starting Integrity Check Service" messages.

4. **Test File Changes**:
   - Create a test `.js` file in the mods directory
   - Wait 5 seconds (debounce period + processing)
   - Check Event Log for file change messages

5. **Verify Paths Exist**:
   Ensure the mods directory from manifest exists.

6. **Check for FileSystemWatcher Limitations**:
   - Network drives: May not work reliably
   - Antivirus: Some AV software blocks file system notifications
   - Very deep paths: FileSystemWatcher has path length limits

7. **Performance with Many Installations**:
   - 1-3 installations: Negligible impact
   - 5+ installations: ~1-2 MB memory per watcher, minimal CPU
   - To reduce load: Disable monitoring in manifest

### Integrity Checks Reporting False Violations

**Symptom**: Integrity violations detected but mods are working correctly.

**Solutions**:
1. **Check Fingerprint**:
   - Fingerprint may be outdated after manual re-injection
   - Re-inject mods via UI/CLI to update fingerprint

2. **Verify Injection Stub Format**:
   - Check that target files contain exact text: `<!-- Vivaldi Mod Manager - Injection Stub -->`
   - Case-sensitive, exact spacing required

3. **Safe Mode Active**:
   - Integrity checks are skipped when safe mode is active
   - Verify safe mode status in manifest

4. **Review Violation Details**:
   - Check Event Log for specific violation messages
   - Each violation includes detailed reason

### Auto-Heal Not Triggering

**Symptom**: Vivaldi updates or integrity violations detected, but mods are not automatically restored.

**Solutions**:
1. **Check Auto-Heal Enabled**:
   Use `GetServiceStatus` IPC command to verify `autoHealEnabled` is `true`

2. **Check Safe Mode**:
   Verify `safeModeActive` is `false` - auto-heal is disabled in Safe Mode

3. **Review Heal History**:
   Use `GetHealHistory` IPC command to see if heals were attempted and why they failed

4. **Check Service Logs**:
   Look for auto-heal related messages in Event Viewer

5. **Verify Monitoring Running**:
   Ensure File System Monitor and Integrity Check services are active

### Auto-Heal Failing Repeatedly

**Symptom**: Heal attempts fail consistently, possibly after max retries.

**Solutions**:
1. **Check Heal History for Errors**:
   ```json
   { "command": "GetHealHistory", "messageId": "guid" }
   ```
   Review error messages for specific failure reasons

2. **Verify Vivaldi Installation**:
   - Ensure Vivaldi is installed and accessible
   - Check that injection target files exist
   - Verify file permissions

3. **Check Disk Space**:
   Mod files need to be copied during heal

4. **Close Vivaldi**:
   Files may be locked if Vivaldi is running - close browser and trigger manual heal

5. **Check Mod Files**:
   Verify enabled mod files exist in the mods directory

### Cooldown Preventing Heals

**Symptom**: Heal attempts seem delayed or don't happen immediately.

**This is normal behavior**:
- 30-second cooldown between heals for the same installation
- Prevents rapid repeated heals from causing system thrashing
- Wait for cooldown period to expire

**To verify**:
- Check service logs for "cooldown active" messages
- Use `GetHealthCheck` to see active cooldowns

## FAQ

### Q: Can I run multiple instances of the service?
**A**: No, by design. The service detects if another instance is using the named pipe and fails to start. This prevents conflicts and ensures only one service manages Vivaldi modifications.

### Q: Does the service require internet access?
**A**: No, the service operates entirely locally and does not require network access.

### Q: Can I access the service remotely?
**A**: No, named pipes are local-machine only. For remote management, you would need to implement a separate remote access solution.

### Q: What happens if Vivaldi updates while the service is running?
**A**: The service automatically detects Vivaldi updates and re-injects mods without user intervention:
1. File System Monitor detects new version folder
2. Auto-Heal Service waits for folder to stabilize (files accessible)
3. Mods are automatically re-injected into the new version
4. Process typically completes within seconds

If auto-heal fails, it retries with exponential backoff. You can view heal history via the `GetHealHistory` IPC command.

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

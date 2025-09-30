#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs the Vivaldi Mod Manager Windows Service.

.DESCRIPTION
    This script installs the Vivaldi Mod Manager service, configures it to start automatically,
    sets up recovery options, and starts the service. Requires Administrator privileges.

.PARAMETER BinaryPath
    Path to the service executable. Defaults to the Release build output.

.EXAMPLE
    .\install-service.ps1
    Installs the service using the default binary path.

.EXAMPLE
    .\install-service.ps1 -BinaryPath "C:\Custom\Path\VivaldiModManager.Service.exe"
    Installs the service using a custom binary path.
#>

param(
    [string]$BinaryPath = "$PSScriptRoot\..\src\VivaldiModManager.Service\bin\Release\net8.0-windows\VivaldiModManager.Service.exe"
)

$ServiceName = "VivaldiModManagerService"
$ServiceDisplayName = "Vivaldi Mod Manager Service"
$ServiceDescription = "Background service for managing Vivaldi browser modifications, monitoring for updates, and providing auto-healing capabilities."
$StartupTimeoutSeconds = 10

Write-Host "Vivaldi Mod Manager Service Installation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator', then run this script again." -ForegroundColor Yellow
    exit 1
}

# Resolve binary path
$BinaryPath = [System.IO.Path]::GetFullPath($BinaryPath)

# Check if binary exists
if (-not (Test-Path $BinaryPath)) {
    Write-Host "ERROR: Service binary not found at: $BinaryPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the service first using:" -ForegroundColor Yellow
    Write-Host "  dotnet build --configuration Release" -ForegroundColor White
    Write-Host ""
    Write-Host "Or specify a custom path using the -BinaryPath parameter." -ForegroundColor Yellow
    exit 1
}

Write-Host "Binary path: $BinaryPath" -ForegroundColor Gray
Write-Host ""

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "WARNING: Service '$ServiceName' already exists." -ForegroundColor Yellow
    Write-Host "Current status: $($existingService.Status)" -ForegroundColor Gray
    Write-Host ""
    
    $response = Read-Host "Do you want to remove the existing service and reinstall? (yes/no)"
    if ($response -ne "yes") {
        Write-Host "Installation cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Removing existing service..." -ForegroundColor Yellow
    
    # Stop service if running
    if ($existingService.Status -eq "Running") {
        Write-Host "Stopping service..." -ForegroundColor Gray
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
    
    # Delete service
    sc.exe delete $ServiceName | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to delete existing service." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Existing service removed." -ForegroundColor Green
    Start-Sleep -Seconds 1
}

# Create service
Write-Host "Creating service..." -ForegroundColor Cyan
sc.exe create $ServiceName binPath= "`"$BinaryPath`"" start= auto DisplayName= "$ServiceDisplayName" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create service." -ForegroundColor Red
    exit 1
}

# Set service description
sc.exe description $ServiceName "$ServiceDescription" | Out-Null

# Configure recovery options (restart on failure)
Write-Host "Configuring recovery options..." -ForegroundColor Cyan
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Host "Service created successfully." -ForegroundColor Green
Write-Host ""

# Start service
Write-Host "Starting service..." -ForegroundColor Cyan
sc.exe start $ServiceName | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to start service." -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Check Windows Event Viewer (Application log) for error details" -ForegroundColor White
    Write-Host "2. Verify the manifest path exists and is accessible" -ForegroundColor White
    Write-Host "3. Ensure .NET 8.0 runtime is installed" -ForegroundColor White
    Write-Host "4. Run: Get-EventLog -LogName Application -Source VivaldiModManagerService -Newest 10" -ForegroundColor White
    exit 1
}

# Wait for service to start
Write-Host "Waiting for service to reach Running state (timeout: ${StartupTimeoutSeconds}s)..." -ForegroundColor Gray
$timeout = [DateTime]::Now.AddSeconds($StartupTimeoutSeconds)
$serviceStarted = $false

while ([DateTime]::Now -lt $timeout) {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service.Status -eq "Running") {
        $serviceStarted = $true
        break
    }
    Start-Sleep -Milliseconds 500
}

Write-Host ""

# Verify service is running
if ($serviceStarted) {
    Write-Host "SUCCESS: Service is running!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Service details:" -ForegroundColor Cyan
    Get-Service -Name $ServiceName | Format-List Name, DisplayName, Status, StartType
    
    Write-Host "Installation complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "- The service will start automatically on system boot" -ForegroundColor White
    Write-Host "- Check service status: Get-Service $ServiceName" -ForegroundColor White
    Write-Host "- View logs in Windows Event Viewer (Application log)" -ForegroundColor White
    Write-Host "- To uninstall: .\uninstall-service.ps1" -ForegroundColor White
} else {
    Write-Host "WARNING: Service did not reach Running state within ${StartupTimeoutSeconds}s" -ForegroundColor Yellow
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    Write-Host "Current status: $($service.Status)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Check Windows Event Viewer (Application log) for error details" -ForegroundColor White
    Write-Host "2. Check service status: Get-Service $ServiceName" -ForegroundColor White
    Write-Host "3. Try starting manually: Start-Service $ServiceName" -ForegroundColor White
    exit 1
}

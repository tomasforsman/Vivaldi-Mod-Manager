#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Uninstalls the Vivaldi Mod Manager Windows Service.

.DESCRIPTION
    This script stops and removes the Vivaldi Mod Manager service.
    Requires Administrator privileges.

.EXAMPLE
    .\uninstall-service.ps1
    Uninstalls the service.
#>

$ServiceName = "VivaldiModManagerService"
$StopTimeoutSeconds = 30

Write-Host "Vivaldi Mod Manager Service Uninstallation" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator', then run this script again." -ForegroundColor Yellow
    exit 1
}

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
    Write-Host "Nothing to uninstall." -ForegroundColor Gray
    exit 0
}

Write-Host "Found service: $($service.DisplayName)" -ForegroundColor Gray
Write-Host "Current status: $($service.Status)" -ForegroundColor Gray
Write-Host ""

# Stop service if running
if ($service.Status -eq "Running") {
    Write-Host "Stopping service..." -ForegroundColor Cyan
    
    try {
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        
        # Wait for service to stop
        $timeout = [DateTime]::Now.AddSeconds($StopTimeoutSeconds)
        $serviceStopped = $false
        
        while ([DateTime]::Now -lt $timeout) {
            $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
            if ($service.Status -eq "Stopped") {
                $serviceStopped = $true
                break
            }
            Start-Sleep -Milliseconds 500
        }
        
        if ($serviceStopped) {
            Write-Host "Service stopped successfully." -ForegroundColor Green
        } else {
            Write-Host "WARNING: Service did not stop within ${StopTimeoutSeconds}s" -ForegroundColor Yellow
            Write-Host "Attempting to delete anyway..." -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "WARNING: Error stopping service: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "Attempting to delete anyway..." -ForegroundColor Gray
    }
} else {
    Write-Host "Service is already stopped." -ForegroundColor Gray
}

Write-Host ""

# Delete service
Write-Host "Deleting service..." -ForegroundColor Cyan
sc.exe delete $ServiceName | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to delete service." -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Ensure no applications are connected to the service" -ForegroundColor White
    Write-Host "2. Try restarting your computer and running this script again" -ForegroundColor White
    Write-Host "3. Check Services Manager (services.msc) for the service status" -ForegroundColor White
    exit 1
}

# Verify deletion
Start-Sleep -Seconds 1
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "WARNING: Service still exists after deletion attempt." -ForegroundColor Yellow
    Write-Host "You may need to restart your computer to complete the uninstallation." -ForegroundColor Yellow
    exit 1
}

Write-Host "Service deleted successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Uninstallation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Note: Configuration files and logs in %APPDATA%\VivaldiModManager have not been removed." -ForegroundColor Gray
Write-Host "Delete them manually if you wish to remove all service data." -ForegroundColor Gray

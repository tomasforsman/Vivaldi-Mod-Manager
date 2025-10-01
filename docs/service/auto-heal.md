# Auto-Heal Service Documentation

## Overview

The Auto-Heal Service automatically restores mod injections when Vivaldi updates or integrity violations are detected. This ensures mods persist across browser updates without manual intervention.

## How It Works

### Detection

Auto-heal responds to two types of events:

1. **Vivaldi Updates**: Detected by the File System Monitor when new version folders appear
2. **Integrity Violations**: Detected by the Integrity Check Service when injection stubs are missing or corrupted

### Folder Stabilization

When a Vivaldi update is detected, auto-heal doesn't immediately inject. Instead, it waits for the new version folder to stabilize:

- Attempts to open target files exclusively to verify they're not being written
- Retries every 1 second for up to 30 seconds (configurable)
- If files are still locked after max wait time, proceeds anyway with a warning
- This prevents injection failures due to incomplete browser updates

### Cooldown Period

To prevent thrashing from rapid repeated heals:

- Each installation has a 30-second cooldown (configurable)
- If a heal is requested within the cooldown period, it's deferred
- Cooldowns are tracked per-installation (different installations can heal independently)
- Logged when cooldown is active

### Retry Logic

Failed heal attempts are automatically retried with exponential backoff:

1. **First retry**: After 5 seconds
2. **Second retry**: After 30 seconds
3. **Third retry**: After 2 minutes (120 seconds)

After 3 failed attempts, the service stops retrying and logs an error. The retry counter is reset on success.

## Heal Process

When a heal request is processed, the service performs these steps:

1. **Check Cooldown**: If a heal was attempted for this installation within 30 seconds, defer
2. **Load Manifest**: Read current configuration
3. **Validate Settings**: Check `AutoHealEnabled` and `SafeModeActive` flags
4. **Wait for Stability**: Ensure Vivaldi files are accessible (see Folder Stabilization)
5. **Find Injection Targets**: Locate `window.html` and `browser.html` files
6. **Generate Loader**: Create `loader.js` file with enabled mods
7. **Copy Mod Files**: Copy enabled mods to `vivaldi-mods/mods/` folder
8. **Inject Stub**: Insert loader import into HTML files
9. **Verify Success**: Read files back and confirm stub presence
10. **Update Manifest**: Record injection time, status, and fingerprint
11. **Save Manifest**: Persist changes to disk
12. **Update Metrics**: Increment success/failure counters
13. **Record History**: Add entry to heal history

## Rollback on Failure

If a heal operation fails partway through, the service attempts to roll back changes:

- If stub was injected but `loader.js` creation failed, removes the stub
- If `loader.js` was created but mod files failed to copy, removes `loader.js`
- Rollback failures are logged but don't stop the service
- Helps prevent partially-injected state that could cause issues

## Heal History

The service maintains a circular buffer of the last 50 heal attempts:

### What's Tracked

Each history entry includes:
- `InstallationId`: Which Vivaldi installation was healed
- `Timestamp`: When the heal occurred
- `TriggerReason`: Why the heal was triggered (`VivaldiUpdate`, `IntegrityViolation`, `ManualTrigger`, `SafeModeDisabled`)
- `Success`: Whether the heal succeeded
- `ErrorMessage`: Error details if failed
- `RetryCount`: Number of retries attempted
- `Duration`: How long the heal took

### Persistence

Heal history is persisted to a JSON file:
- Location: `%APPDATA%\VivaldiModManager\heal-history.json` (configurable)
- Saved after each heal attempt
- Loaded when service starts
- Human-readable format (pretty-printed JSON)

### Viewing History

Via IPC command:
```json
{
  "command": "GetHealHistory",
  "messageId": "12345"
}
```

Response includes all history entries (default 50, configurable).

## Manual Triggering

You can manually trigger auto-heal for all installations via IPC:

```json
{
  "command": "TriggerAutoHeal",
  "messageId": "12345"
}
```

This queues heal requests for all managed installations with trigger reason `ManualTrigger`.

## Metrics

The service tracks these metrics:

- `TotalHealsAttempted`: Total number of heal attempts (includes retries)
- `TotalHealsSucceeded`: Number of successful heals
- `TotalHealsFailed`: Number of failed heals (after max retries exceeded)

Metrics are exposed via `GetServiceStatus` IPC command and persist across service restarts.

## Configuration

Settings in `appsettings.json`:

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

### Configuration Options

- **AutoHealRetryDelays**: Array of retry delays in seconds (default: `[5, 30, 120]`)
- **AutoHealMaxRetries**: Maximum number of retries before giving up (default: `3`)
- **AutoHealCooldownSeconds**: Cooldown period between heals for same installation (default: `30`)
- **VivaldiFolderStabilizationMaxWaitSeconds**: Max time to wait for Vivaldi folder stability (default: `30`)
- **HealHistoryMaxEntries**: Max number of history entries to retain (default: `50`)
- **HealHistoryFilePath**: Path to heal history JSON file (supports environment variables)

## Safe Mode Integration

Auto-heal respects Safe Mode:

- **When Safe Mode is Active**: Auto-heal is completely disabled, no healing occurs
- **When Safe Mode is Deactivated**: Automatically queues heal requests for all installations

See [Safe Mode Documentation](safe-mode.md) for details.

## Limitations and Best Practices

### When Vivaldi is Running

- Files may be locked by the running browser
- Folder stabilization handles most cases by waiting for file access
- If files remain locked after max wait time, heal proceeds anyway
- **Best Practice**: Close Vivaldi before updates for cleanest auto-heal

### Rapid Updates

- Cooldown prevents rapid repeated heals
- If Vivaldi updates multiple times quickly, heals are deferred appropriately
- This is normal behavior and prevents system thrashing

### Manual File Modifications

- If user manually modifies Vivaldi files during a heal, unexpected behavior may occur
- Files locked by other tools (editors, backup software) may cause heal failures
- Failed heals will retry automatically with exponential backoff

### Performance

- Auto-heal operations complete in seconds under normal conditions
- Folder stabilization adds up to 30 seconds wait time if files are locked
- Processing is sequential (one heal at a time) to prevent conflicts
- Multiple installations heal independently (separate cooldowns)

## Troubleshooting

### Heal Not Triggering

**Check:**
1. `AutoHealEnabled` flag in manifest (via `GetServiceStatus`)
2. `SafeModeActive` flag (auto-heal disabled when true)
3. Service logs for detection events
4. File System Monitor is running and watching correct paths

### Heal Failing Repeatedly

**Check:**
1. Heal history for error messages: `GetHealHistory` command
2. Verify Vivaldi installation is valid and accessible
3. Check disk space (mod files need to be copied)
4. Verify mod files exist in mods directory
5. Look for file permission issues in logs

### Cooldown Preventing Heals

This is normal behavior when:
- Multiple events trigger rapid heals
- Manual trigger attempted soon after automatic heal
- Wait 30 seconds or check service logs for cooldown status

### Vivaldi Locked During Heal

**Symptoms:**
- Heal fails with "file in use" or "access denied" errors
- Folder stabilization times out after 30 seconds

**Solutions:**
- Close Vivaldi browser before heal
- Wait for folder stabilization to complete
- Check for other processes locking Vivaldi files (antivirus, backup tools)

## Logging

Auto-heal operations are logged with these levels:

- **Debug**: Folder stabilization attempts, file access checks
- **Information**: Heal requests queued, heals started/completed, cooldown active
- **Warning**: Folder stabilization timeout, rollback attempts
- **Error**: Heal failures, max retries exceeded, unexpected errors

Check logs in:
- Windows Event Viewer: Application log, source "VivaldiModManagerService"
- Console output (when running in console mode)
- Log files in `%APPDATA%\VivaldiModManager\Logs\` (if configured)

## Related Documentation

- [Service README](README.md) - Overall service documentation
- [Safe Mode Documentation](safe-mode.md) - Safe Mode feature details
- [Monitoring Documentation](README.md#monitoring-and-integrity-checks) - Detection mechanisms

# Safe Mode Documentation

## Overview

Safe Mode is an emergency feature that immediately disables all mod injections across all Vivaldi installations. Use it when mods are causing problems and you need to quickly return to a vanilla Vivaldi state.

## When to Use Safe Mode

Use Safe Mode when:

- A mod is causing Vivaldi to crash or behave incorrectly
- You need to troubleshoot whether mods are causing an issue
- You want to temporarily disable all mods without losing your configuration
- You need to ensure no mods run during a critical task

## How Safe Mode Works

### Activation

When Safe Mode is activated:

1. **Sets Flag**: `SafeModeActive` is set to `true` in the manifest
2. **Stops Pending Heals**: Any queued auto-heal operations are cancelled
3. **Removes All Injections**: For each managed Vivaldi installation:
   - Removes injection stubs from `window.html` and `browser.html`
   - Updates installation status to "Safe Mode - Disabled"
   - Logs success/failure per installation
4. **Blocks Auto-Heal**: Auto-heal service stops processing heal requests
5. **Persists State**: Flag saved to manifest and persists across service restarts
6. **Broadcasts Event**: `SafeModeChanged` event sent to connected IPC clients

### Deactivation

When Safe Mode is deactivated:

1. **Clears Flag**: `SafeModeActive` is set to `false` in the manifest
2. **Queues Heal Requests**: Auto-heal requests queued for all installations (trigger reason: `SafeModeDisabled`)
3. **Resumes Auto-Heal**: Auto-heal service resumes normal operation
4. **Persists State**: Changes saved to manifest
5. **Broadcasts Event**: `SafeModeChanged` event sent to connected IPC clients
6. **Restores Mods**: Mods are automatically reinjected by auto-heal service

## Activation Methods

### Via IPC Command

```json
{
  "command": "EnableSafeMode",
  "messageId": "12345"
}
```

Response:
```json
{
  "messageId": "12345",
  "success": true,
  "data": {
    "message": "Safe Mode activated, processed 2 installation(s)",
    "count": 2
  }
}
```

### Via UI (Future)

The UI will provide:
- Safe Mode toggle in settings or toolbar
- One-click activation/deactivation
- Visual indicator when Safe Mode is active
- Toast notifications for state changes

## Persistence

Safe Mode state persists across:

- Service restarts
- System reboots
- Vivaldi updates

If the service restarts while in Safe Mode:
- Remains in Safe Mode
- Auto-heal stays disabled
- Injections remain removed
- Must be manually deactivated

## Impact on Other Features

### When Safe Mode is Active

**Disabled:**
- Auto-heal service (completely inactive)
- All mod injections (removed from all installations)
- Integrity checks continue but don't trigger heals

**Still Active:**
- File system monitoring (continues watching for changes)
- Integrity checks (violations detected but not auto-fixed)
- IPC server (continues accepting commands)
- Service status reporting

### When Safe Mode is Inactive (Normal Mode)

All features operate normally:
- Auto-heal responds to updates and violations
- Mods are injected and maintained
- Full functionality available

## Checking Safe Mode Status

### Via IPC Command

```json
{
  "command": "GetServiceStatus",
  "messageId": "12345"
}
```

Response includes:
```json
{
  "messageId": "12345",
  "success": true,
  "data": {
    "safeModeActive": true,
    "...": "..."
  }
}
```

### Via UI (Future)

The UI will show:
- Safe Mode status badge/indicator
- Last Safe Mode activation time
- Number of installations affected

## Deactivation and Mod Restoration

When you deactivate Safe Mode:

1. **Immediate**: Flag cleared and auto-heal resumes
2. **Healing Queued**: Heal requests added for all installations
3. **Processing**: Auto-heal processes queue one installation at a time
4. **Cooldowns Apply**: Standard 30-second cooldown between heals per installation
5. **Completion**: Depending on number of installations, full restoration may take a few minutes

**Timeline example** (3 installations):
- t=0s: Safe Mode deactivated, 3 heal requests queued
- t=0-30s: First installation healed
- t=30-60s: Second installation healed (cooldown respected)
- t=60-90s: Third installation healed
- t=90s: All mods fully restored

## Error Handling

### Activation Failures

If Safe Mode activation fails:
- **Partial Success**: Some installations may have injections removed
- **Status Updated**: Failed installations marked in manifest
- **Service Continues**: Service doesn't crash, error logged
- **Retry Available**: Can attempt activation again

### Deactivation Failures

If Safe Mode deactivation fails:
- **Flag Cleared**: Safe Mode flag is cleared regardless
- **Heal Queued**: Heal requests are queued
- **Auto-Heal Handles**: Individual heal failures handled by auto-heal retry logic

### Per-Installation Failures

During activation, if removing injection from one installation fails:
- **Other Installations**: Continue to be processed
- **Status Recorded**: Failed installation status includes error message
- **Service Continues**: Doesn't stop processing remaining installations

## Use Cases

### Troubleshooting Mod Issues

1. Activate Safe Mode
2. Restart Vivaldi
3. Test if issue persists
4. If issue resolved: One of your mods was the problem
5. Deactivate Safe Mode
6. Disable mods one-by-one to identify culprit

### Emergency Mod Disable

1. Vivaldi crashes or becomes unusable
2. Activate Safe Mode immediately
3. Restart Vivaldi (now running vanilla)
4. Fix or remove problematic mod
5. Deactivate Safe Mode when ready

### Clean Browser Testing

1. Need to test Vivaldi without any mods
2. Activate Safe Mode
3. Perform tests with vanilla Vivaldi
4. Deactivate Safe Mode to restore normal setup

## Limitations

### Cannot Disable Individual Mods

Safe Mode is all-or-nothing:
- Disables **all** mods across **all** installations
- Cannot selectively disable specific mods
- To disable individual mods, use the main UI or edit manifest

### Requires Service Access

Safe Mode requires:
- Service must be running
- IPC communication functional
- If service isn't running, Safe Mode unavailable

**Workaround**: Manually remove injection stubs from Vivaldi HTML files.

### Doesn't Remove Mod Files

Safe Mode only:
- Removes injection stubs from HTML files
- Removes references to mods
- **Does not** delete mod files from disk

Your mod files remain in the mods directory and are re-injected when Safe Mode is deactivated.

## Best Practices

### When to Use

- **Do**: Use for troubleshooting mod-related issues
- **Do**: Use when you need guaranteed vanilla Vivaldi
- **Do**: Use as emergency disable when mods malfunction
- **Don't**: Use as regular way to disable mods (use the UI instead)
- **Don't**: Leave enabled long-term (defeats purpose of mod manager)

### After Activation

1. Restart Vivaldi to fully load without mods
2. Test your scenario with vanilla browser
3. When done, deactivate to restore functionality

### Before Updates

Not necessary:
- Auto-heal automatically handles Vivaldi updates
- Safe Mode isn't needed for normal update process
- Only use if you suspect mods will cause update issues

## Monitoring Safe Mode

### In Logs

Safe Mode operations are logged:

**Activation:**
```
[Information] Activating Safe Mode
[Information] Removing injection for Vivaldi Stable (Safe Mode)
[Information] Safe Mode activated, processed 2 installations
```

**Deactivation:**
```
[Information] Deactivating Safe Mode
[Information] Safe Mode deactivated, 2 heal request(s) queued
```

**Errors:**
```
[Error] Failed to remove injection for Vivaldi Snapshot: Access denied
```

### Via IPC Events

When subscribed to IPC events:

```json
{
  "event": "SafeModeChanged",
  "data": {
    "enabled": true,
    "installationsAffected": 2
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Related Documentation

- [Service README](README.md) - Overall service documentation
- [Auto-Heal Documentation](auto-heal.md) - Auto-heal behavior during Safe Mode
- [IPC Commands](README.md#ipc-commands) - Command reference

## FAQ

**Q: Will Safe Mode remove my mod files?**  
A: No. It only removes injection stubs. Your mod files remain on disk and are restored when you deactivate Safe Mode.

**Q: Can I use Safe Mode from the UI?**  
A: Future versions will include UI support. Currently requires IPC commands or direct service access.

**Q: What happens if service restarts in Safe Mode?**  
A: Service remains in Safe Mode until you manually deactivate it. State persists across restarts.

**Q: Can I disable Safe Mode if service isn't running?**  
A: No. You need service access to toggle Safe Mode. Alternatively, manually remove the `SafeModeActive` flag from the manifest JSON file.

**Q: How long does activation/deactivation take?**  
A: Activation is fast (< 2 seconds typically). Deactivation queues heals which process over 30-90 seconds depending on installation count.

**Q: Will integrity checks run in Safe Mode?**  
A: Yes, integrity checks continue but don't trigger auto-heals. Violations are detected and logged but not automatically fixed.

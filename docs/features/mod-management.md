# Mod Management Features

The Vivaldi Mod Manager desktop app now supports managing the lifecycle and load order of installed mods directly from the UI.

## Deleting Mods

- Select a mod in the *Mod Management* list and click `Remove` (or press the `Delete` key).
- The manager removes the entry from the manifest, deletes the corresponding file from the managed mods folder, and normalises the remaining load order.
- A toast notification and the status bar confirm the outcome. Any failure to delete the underlying file is logged and surfaced through an error dialog.

## Changing Load Order

- Use the `Move Up` / `Move Down` buttons (or `Ctrl` + `↑/↓`) to adjust the selected mod.
- The manager swaps the mod ordering in the manifest, reassigns sequential order numbers, and refreshes the list without losing the current selection.
- Updated ordering is persisted immediately so future loader generations honour the new sequence.
- After each change the installation list refreshes to show the latest injection timestamp; any missing scripts surface as a warning so you know a reinjection is required.
- Each mod row now displays `Loaded`, `Unloaded`, or `Missing` so you can see which scripts are active for the selected installation at a glance.

## Keyboard Shortcuts

| Action          | Shortcut      |
|-----------------|---------------|
| Remove mod      | `Delete`      |
| Move mod up     | `Ctrl + Up`   |
| Move mod down   | `Ctrl + Down` |

These controls work in tandem with the existing `Add Mod`, `Import`, and `Export` options, giving contributors a complete mod-management workflow from within the desktop UI.

The toolbar button formerly labelled **Inject Mods** is now **Inject Mod Loader**, highlighting that the action regenerates the loader bundle before writing to Vivaldi’s resources.

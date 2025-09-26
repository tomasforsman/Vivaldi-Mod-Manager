# Issue: Persist Manifest and Mod State

## Background
Mod lists and manifest data are rebuilt in memory each run. Users lose ordering, notes, and enabled flags after restarting, and services have no reliable source of truth.

## Tasks
- Design a storage format (JSON file or similar) for manifests and mod metadata, including upgrade hooks for future schema changes.
- Implement read/write logic in `ManifestService` that commits changes atomically and validates data before saving.
- Add safeguards to recover from corrupt manifests (e.g., backup copies, repair prompts).
- Update UI flows to rely on the persisted data instead of sample collections.

## Acceptance Criteria
- Restarting the application preserves mod settings, ordering, and notes for each installation.
- Manifest persistence handles concurrent operations safely and logs clear errors when writes fail.
- Automated tests cover serialization/deserialization paths and corrupted-file recovery.
- Contributor documentation explains where the manifest lives and how to reset it during development.

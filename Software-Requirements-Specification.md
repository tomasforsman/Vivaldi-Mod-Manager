# Software Requirements Specification (SRS)

**Project:** Vivaldi Mod Manager  
**Version:** 1.3  
**Date:** 2025-09-24  
**Status:** Draft

## 1. Introduction

### 1.1 Purpose

The Vivaldi Mod Manager provides a Windows solution for managing custom JavaScript modifications in the Vivaldi browser. The system consists of two components:

**Service component:** Runs in the background, monitors Vivaldi for updates, performs stub injection, and auto-heals if modifications are removed.

**Application component:** Desktop interface for end users to manage mods, review logs, and control the service.

This separation ensures persistence across browser updates with minimal user intervention, while giving users a simple and reliable control panel.

### 1.2 Scope

#### In Scope (v1.3):

- Centralized mod directory with metadata persistence
- Manifest schema with versioning, checksums, notes, and compatibility fields
- Dynamic loader generation and stub injection with fingerprints
- Automated detection and recovery after Vivaldi updates
- File system monitoring via background service
- Safe Mode (disable all mods, restore clean state)
- GUI mod management (enable/disable, order, notes, drag-and-drop)
- System tray integration
- Backup and restore of modified files
- Log viewer with search, export, and rotation
- CLI tool for headless operations

#### Out of Scope (v1.3):

- Developer tools (live reload, debugger)
- Network-based mod sync or sharing
- Cross-platform support (Linux/macOS)
- Advanced JavaScript security analysis
- Marketplace integration

#### Future Roadmap (not in v1.3):

- Multiple profiles and mod collections
- Domain/URL scoping and conditional loading
- Dependency management between mods
- Portable mode for USB/multi-machine setups
- Cloud sync and sharing of profiles
- Developer mode with debugging and rapid reload
- Mod marketplace

### 1.3 Definitions

| Term | Definition |
|------|------------|
| Stub | Minimal script injected into Vivaldi's user_files that imports the loader. |
| Loader | Generated file (loader.js) that dynamically imports enabled mods in order. |
| Mods Root | External directory containing mods, manifest, loader, and backups. |
| Manifest | JSON file storing metadata: enabled state, order, notes, version, checksum, last-known-compatible Vivaldi build. |
| Injection Point | Target HTML file (window.html or browser.html) modified to include the stub. |
| Auto-Heal | Automatic restoration of stub and loader after Vivaldi update or removal. |
| Injection Fingerprint | Unique marker to detect duplicate or missing injections. |
| Safe Mode | Mode that disables all mods and restores original Vivaldi files for a clean launch. |

## 2. Overall Description

### 2.1 Product Perspective

The product consists of:

- **Service:** Continuous background process for file monitoring, injection, and healing.
- **Application:** WPF desktop UI with tray integration, managing mods, logs, and service controls.
- **Data Layer:** Shared manifest, backups, logs, and configs.

### 2.2 Product Functions

#### Core Functions (v1.3):

- Add, remove, enable/disable, reorder mods
- Maintain manifest with schema and metadata
- Generate loader.js from manifest
- Inject stub into Vivaldi and verify with fingerprint
- Detect Vivaldi updates and auto-heal
- Provide Safe Mode (disable all mods, restore backups)
- Backup and restore modified Vivaldi files
- Log errors, injections, and mod load failures
- Expose CLI for headless control

#### Extended Functions (roadmap):

- Multi-profile support
- Domain scoping per mod
- Mod dependency management
- Cloud sync and sharing

### 2.3 User Classes

- **Casual Users:** Want simple toggles and backup/restore.
- **Power Users:** Need ordering, notes, Safe Mode, logs, and CLI.
- **Developers (future):** Want debugging, live reload, conditional loading.

### 2.4 Operating Environment

- Windows 10 (1903+) or Windows 11
- .NET 8 runtime or later
- ~50 MB disk space
- Admin rights required for injection tasks

### 2.5 Constraints

- Service runs with least privileges; elevation occurs only when writing to Program Files.
- Must gracefully handle changes in Vivaldi folder structure.
- If Content Security Policy (CSP) blocks injection, fail gracefully and notify user.

### 2.6 Assumptions and Dependencies

- Vivaldi folder structure remains `Application\<version>\`.
- User mods are valid ES6 modules.
- File system notifications available via Windows APIs.

## 3. Functional Requirements (Summary)

### FR-1: Mod Management
Maintain centralized folder; enable/disable/reorder mods; metadata (state, order, notes, checksum, compatibility).

### FR-2: Manifest Management
Store in JSON with schemaVersion. Include per-mod id, version, enabled, order, notes, hash, lastKnownCompatibleVivaldi, scopes (URL patterns).

### FR-3: Loader Generation
Generate loader.js based on manifest; embed loader version; maintain last-known-good rollback copy.

### FR-4: Vivaldi Integration
Detect installations; inject stub with fingerprint; backup modified files; verify injection.

### FR-5: Update Handling
Detect new version folders; reinject stub; rollback if verification fails.

### FR-6: Monitoring
Watch mods root and Vivaldi paths; debounce rapid changes; show monitoring health indicators.

### FR-7: User Interface
UI with list of mods, checkboxes, ordering, notes; drag-and-drop support; Safe Mode toggle; log viewer; tray quick actions.

### FR-8: Safety & Recovery
Maintain backups; allow restore; validate file integrity; offer Safe Mode on repeated failures.

### FR-9: CLI
Commands: list, enable, disable, order, inject, restore, simulate-update.

## 4. Non-Functional Requirements

### Performance
- Injection < 2s
- Startup < 3s
- Monitoring < 1% CPU
- Memory < 100 MB

### Reliability
- 99.5% service uptime
- >95% auto-heal success

### Usability
- Enable/disable mods within 3 clicks
- Mod install within 2 minutes

### Security
- Local-only
- Mod integrity checks
- Warn on hash changes

### Maintainability
- Human-readable configs
- Logging with rotation
- Schema versioning

### Compatibility
- Support Vivaldi 3.0+
- Handle multiple install paths

## 5. Use Cases

### UC-1 First-time Setup
Detect Vivaldi, prompt mods root, create structure, inject stub.

### UC-2 Add Mod
User adds JS; system validates, updates manifest, regenerates loader.

### UC-3 Vivaldi Update
Service detects update, reinjects stub, logs result.

### UC-4 Error Recovery
Service detects failure, rolls back, Safe Mode offered.

### UC-5 Safe Mode Toggle
User disables all mods, restores clean Vivaldi launch.

### UC-6 Simulated Update
User/dev runs test update to validate auto-heal.

## 6. System Architecture Overview

- **Application Layer:** WPF UI, tray integration, CLI front end.
- **Service Layer:** File monitoring, injection, healing, rollback.
- **Data Layer:** Manifest, backups, logs, configuration.
- **External Interfaces:** Windows File APIs, Registry detection, Tray APIs, FileSystemWatcher.

## 7. Quality Assurance Requirements

### Unit Testing
Manifest, loader generation, hash verification.

### Integration Testing
Injection, update detection, rollback.

### UAT
Validate workflows (Safe Mode, update recovery).

### Documentation
User guide, troubleshooting, CLI reference.

## 8. Future Enhancements

- Profiles and mod collections
- Domain/URL scoping and conditional loading
- Dependency management
- Cloud sync and sharing
- CSS mod support
- Portable mode
- Mod marketplace

## 9. Appendices

- **Appendix A:** Example file structures
- **Appendix B:** Manifest schema (JSON with schemaVersion)
- **Appendix C:** Error codes and remedies
- **Appendix D:** Log levels and rotation policy
- **Appendix E:** Injection state machine
- **Appendix F:** CLI usage examples

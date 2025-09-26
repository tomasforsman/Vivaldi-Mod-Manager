# Issue: Add Windows Service and CLI Projects

## Background
The README promises background service and command-line tooling, but only Core and UI projects exist. Automating mod management requires headless options.

## Tasks
- Create a Windows service project that hosts the core mod management features (e.g., monitoring, auto-injection, safe mode toggles).
- Add a CLI project that wraps common operations such as detecting installs, enabling mods, and running injections.
- Share the existing Core library via dependency injection to avoid duplicating logic.
- Provide documentation and examples for installing the service and using the CLI.

## Acceptance Criteria
- Solution contains service and CLI projects that build alongside Core and UI.
- Both new entry points rely on shared services, with configuration handled via appsettings or command arguments.
- Automated smoke tests confirm the CLI commands work against sample directories.
- README and docs describe how to install/run the service, and how to script tasks with the CLI.

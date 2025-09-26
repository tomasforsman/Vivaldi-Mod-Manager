# Issue: Connect WPF UI to Live Services

## Background
The main window currently seeds demo installations and mods, so the app never hits `IVivaldiService`, `IManifestService`, or loader logic. Contributors need real detection, manifest loading, and injection feedback to validate work and debug issues.

## Tasks
- Replace the sample data blocks in `MainWindowViewModel` with calls to `IVivaldiService.DetectInstallationsAsync` and real manifest loading.
- Surface progress and error states from service calls in the status bar and dialogs.
- Ensure commands (`RefreshInstallations`, `LoadMods`, `InjectMods`) update view models based on actual service responses.
- Document the updated flow in `docs/` so new contributors can follow the happy path.

## Acceptance Criteria
- Running the UI against a local Vivaldi install populates the installations list without hard-coded data.
- Enabling/disabling mods persists through view-model refreshes because data now flows from the manifest service.
- Injection feedback in the UI reflects success or failure messages coming from `IInjectionService`.
- Documentation describes the new live path and no longer mentions placeholder/demo data.

## Completed Work
- Replaced the stubbed collections in `src/VivaldiModManager.UI/ViewModels/MainWindowViewModel.cs` with manifest-backed data, real Vivaldi detection, and injection logic that generates loaders and copies mods into each installation.
- Added persistence helpers so manifests are created under `%APPDATA%/VivaldiModManager`, mods are imported into a managed library, and Safe Mode toggles update stored state.
- Updated `tests/VivaldiModManager.UI.Tests/ViewModels/MainWindowViewModelTests.cs` to work against the new constructor signature, isolate test data in a temp directory, and validate the refreshed add-mod flow.

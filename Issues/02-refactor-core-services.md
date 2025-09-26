# Issue: Break Down Core Injection & Loader Services

## Background
`InjectionService` and `LoaderService` currently mix validation, file operations, fingerprinting, and logging. The large methods make reasoning and testing difficult.

## Tasks
- Extract file backup/write logic into dedicated helpers or utility classes that can be mocked.
- Separate validation steps (e.g., loader content checks) from command logic so they can be unit tested in isolation.
- Review logging usage and ensure consistent levels/messages after the split.
- Update existing unit tests to cover the new helper components and keep current scenarios passing.

## Acceptance Criteria
- Core services focus on orchestration while helpers handle file and validation details.
- Unit tests exist for the extracted helpers with meaningful coverage of success and failure paths.
- Public APIs of `IInjectionService` and `ILoaderService` remain stable, and all current tests continue to pass.
- Code readability improves (smaller methods/files) without losing functionality.

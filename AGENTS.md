# Repository Guidelines

## Project Structure & Module Organization
The solution `VivaldiModManager.sln` centralizes the build, with shared MSBuild settings in `Directory.Build.props`. Core logic lives in `src/VivaldiModManager.Core`, while the WPF desktop client is in `src/VivaldiModManager.UI` alongside XAML assets. Tests mirror production layout in `tests/VivaldiModManager.Core.Tests` and `tests/VivaldiModManager.UI.Tests`, and shared documentation sits under `docs/` with security and contribution policies at the repo root. `global.json` pins tooling to .NET 8.0 for consistent builds.

## Build, Test, and Development Commands
Run these from the repository root:
- `dotnet restore` – restores dependencies via the pinned SDK.
- `dotnet build --configuration Release` – builds all projects with shared analyzers.
- `dotnet run --project src/VivaldiModManager.UI` – launches the WPF client for smoke tests.
- `dotnet test` – executes the full xUnit suite.
- `dotnet test --collect:"XPlat Code Coverage"` – creates coverage artifacts for CI.

## Coding Style & Naming Conventions
Follow `.editorconfig`: UTF-8 encoding, CRLF endings, and 4-space indentation for C#. Builds enforce analyzers and `stylecop.ruleset`; treat warnings as actionable. Use PascalCase for public types and methods, camelCase for locals, and suffix asynchronous APIs with `Async`. Keep namespaces aligned with folder structure and maintain XML doc comments for any new public surface.

## Testing Guidelines
Write tests beside the code they validate inside the matching `*.Tests` project, naming files `<Subject>Tests.cs` and methods `MethodUnderTest_Scenario_ExpectedResult`. Prefer xUnit facts/theories with FluentAssertions for clarity and Moq for collaborators. Validate critical paths, including failure cases and serialization or file I/O with deterministic fixtures. Before opening a PR, run `dotnet test` locally and review coverage artifacts in `TestResults/`.

## Commit & Pull Request Guidelines
Adopt the conventional `type(scope): summary` commit format (for example `feat(service): add mod recovery queue`) and reference issues with `Fixes #123` when applicable. Keep commits scoped; separate refactors from behavior changes. Pull requests should use the template, summarize motivation, list test evidence, and note the Vivaldi, Windows, and .NET versions exercised. Attach UI screenshots for WPF changes, tick the style and testing checklist, and respond promptly to reviewer comments.

## Security & Configuration Tips
Respect the steps in `SECURITY.md` when reporting vulnerabilities and never include sensitive paths or crash dumps in logs. Running the manager requires Windows with administrator rights; avoid storing credentials in the repository and prefer environment variables for local secrets. When contributing automation or scripts, assume restricted environments and document any elevated permissions they require.

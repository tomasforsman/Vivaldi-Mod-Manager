# Issue: Speed Up GitHub Workflows

## Background
CI, Release, and CodeQL workflows restore packages from scratch and analyze languages we do not use. Artifacts upload on every run, which increases time and storage costs.

## Tasks
- Add `actions/cache` for NuGet packages and consider caching `~/.dotnet/tools` when useful.
- Reuse build outputs across steps (`dotnet build --no-restore`, `dotnet test --no-build`) and confirm job dependencies are minimal.
- Narrow the CodeQL workflow to C# only unless JavaScript code is added.
- Limit artifact uploads to tagged releases or PRs that need them; skip for routine CI runs.

## Acceptance Criteria
- CI and Release jobs run noticeably faster thanks to caching and fewer redundant steps.
- CodeQL jobs analyze only the necessary language(s) and complete more quickly.
- Artifact uploads happen only when they provide value (e.g., release tags), reducing storage overhead.
- Workflow documentation notes the caching strategy and artifact policy for future maintainers.

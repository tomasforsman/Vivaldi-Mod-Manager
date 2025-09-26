# Issue: Strengthen Automated Testing

## Background
Testing focuses on individual services with limited coverage of the full injection flow or UI command behavior. Platform guards in UI tests add noise, and CI does not enforce coverage goals.

## Tasks
- Add integration tests that exercise loader generation and injection against sample HTML directories.
- Remove unnecessary OS checks in UI tests and ensure they run on Windows in CI.
- Introduce coverage reporting (e.g., `dotnet test --collect:"XPlat Code Coverage"`) and fail builds when coverage drops below an agreed threshold.
- Document how to run the new suites locally and interpret coverage reports.

## Acceptance Criteria
- Integration tests fail when core injection/inversion logic regresses.
- UI tests run by default on Windows and no longer skip major scenarios.
- CI surfaces coverage metrics and blocks merges below the target percentage.
- Contributors have clear instructions for running and troubleshooting the expanded test suite.

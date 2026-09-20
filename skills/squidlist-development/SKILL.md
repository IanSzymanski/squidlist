---
name: squidlist-development
description: Guide backlog-driven .NET 10 and MAUI development in the Squidlist repository.
---

# Squidlist development

Use this skill for implementation, backlog review, architecture decisions, validation, commits, and pull requests in the Squidlist repository.

## Skill freshness and change control

- Treat the repository's skills as a shared project contract that keeps every developer up to speed.
- Before substantial work, compare the current repository conventions and backlog with the applicable skill guidance.
- If the work reveals that a skill is stale, incomplete, or missing a rule, tell the developer exactly what should change and why. Do not silently edit a skill, its metadata, or a local installed copy.
- A skill update requires the developer's explicit verbal confirmation in the conversation. Once confirmed, update the repository copy first, validate it, and report which local copies may need synchronization.
- Raise skill drift proactively even when the current feature can proceed without the update.

## Project baseline

- Use the repository's `global.json` and target `.NET 10` for new shared projects.
- Keep shared projects platform-neutral. Do not add Windows, WinUI, or MAUI references to Core or Storage.
- Treat the GitHub backlog as the source of truth for feature scope and acceptance criteria. Read the issue before coding and keep out-of-scope work out of the change.

## Architecture boundary

The dependency direction is:

```text
UI / platform implementations ──┬──> Squidlist.Storage ──> Squidlist.Core
                                └──> Squidlist.Core
```

- `Squidlist.Core` owns platform-neutral domain models and rules and has no project references.
- `Squidlist.Storage` owns storage contracts and storage-facing logic and may reference Core.
- Future MAUI and Windows projects may reference Core and Storage abstractions; platform-specific APIs belong there.

## Media identity rules

When implementing media models:

- Use a persisted, opaque `MediaId` backed by a `Guid`; never derive identity from the current path.
- Use a platform-neutral `MediaKind` with `Audio` and `Video` values.
- Prefer immutable records/value objects so equality is value-based and serialization is predictable.
- Playlists should store `MediaId`, not an absolute filesystem path. Storage/catalog code resolves the ID to the current relative path.
- A move or rename updates the catalog location while preserving the ID. An unresolved ID remains representable for later repair.
- Keep root and relative-path behavior in the media-root/path work, not in the identity model unless the issue explicitly requires it.

## Backlog and GitHub workflow

- Assign issues only when the user asks for assignment.
- For implementation work, create a focused feature branch using the `codex/` prefix unless the user specifies another name.
- Keep commits scoped to the issue. Do not add a co-author trailer unless requested.
- Open pull requests into `main` with a concise summary, validation details, and `Closes #N` only when the PR fully satisfies that issue.
- Do not close issues manually when a PR can close them on merge.
- External GitHub mutations such as assignment, comments, pushes, and PR creation require explicit user intent. If local push authentication fails, preserve the local commit and report the exact recovery point; use an authenticated GitHub path only when the user has requested the PR or remote update.

## Validation

For solution or project changes, run:

```text
dotnet build Squidlist.sln --configuration Release
dotnet build src\Squidlist.Core\Squidlist.Core.csproj --configuration Release --no-restore
dotnet build src\Squidlist.Storage\Squidlist.Storage.csproj --configuration Release --no-restore
```

Confirm the project references with `dotnet list src\Squidlist.Storage\Squidlist.Storage.csproj reference`. Add focused tests for new Core behavior, including equality and `System.Text.Json` round-tripping for identity-bearing models.

## Repository hygiene

The root `.gitignore` should exclude Visual Studio/.NET output, test artifacts, MAUI app packages, and signing secrets. Keep source assets and platform configuration trackable, including `Platforms/`, `Resources/`, manifests, XAML, fonts, images, `global.json`, and documentation.

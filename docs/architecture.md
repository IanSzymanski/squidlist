# Squidlist architecture boundaries

Squidlist is organized so that the platform-neutral application rules can be
built and tested without Windows APIs or MAUI.

## Projects

- `Squidlist.Core` contains platform-neutral domain models and application
  rules. It has no project references and must not reference MAUI, Windows, or
  storage implementations.
- `Squidlist.Storage` contains platform-neutral persistence contracts and
  storage-facing logic. It may reference `Squidlist.Core` models, but Core does
  not reference Storage.
- Future UI and platform projects may reference Core and Storage abstractions.
  Windows APIs and MAUI types belong only in those platform/UI projects.

The dependency direction is:

```text
UI / platform implementations ──┬──> Squidlist.Storage ──> Squidlist.Core
                                └──> Squidlist.Core
```

This keeps Core and Storage independently buildable on any supported .NET
runtime and leaves platform-specific implementations at the outer edge of the
solution.

## Current target

The shared projects target `net10.0` and intentionally have no external package
or platform dependencies. The MAUI Windows application and concrete Windows
storage implementations can be added later without changing this boundary.

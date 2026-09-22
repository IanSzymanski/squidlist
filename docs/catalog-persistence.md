# Local JSON catalog persistence

`JsonCatalogStore` implements `ICatalogStore` with local .NET filesystem APIs and
no Windows SDK or database dependency. It is intended for local Windows storage;
the shared library remains platform-neutral. The catalog lives at
`<Squidlist root>/.squidlist/catalog.json`, using the version 1 schema from
[catalog-schema.md](catalog-schema.md), as indented UTF-8 JSON without a BOM.
Catalog data is separate from media and playlist files and survives store/app restart.

## Root policy

Construction requires `Func<MediaRoot, CancellationToken, Task> validateRoot`.
The platform layer must verify the persisted marker identity for the supplied root
and throw a classified StorageException on failure. This keeps marker format and
root discovery in #17 rather than inventing a second identity system here. A no-op
callback is only appropriate for isolated tests. No production composition is
included until that root policy exists.

The store checks directory existence itself, rejects unavailable root states, and
requires a path absolute on the executing platform. It rejects reparse points in
the root ancestor chain, metadata directory, or catalog path. It does not create
missing roots. The root validator runs again immediately before the commit point.
This is not protection against a malicious process racing directory replacement;
callers must use a trusted local root and serialize writes per root, as required by
[the storage contracts](storage-contracts.md).

## Load and recovery

A missing file returns null; a saved empty catalog returns an empty snapshot.
Unreadable files yield AccessDenied or IoError. Invalid JSON, missing constructor
fields, duplicate properties/IDs, unknown properties, and invalid model values
yield CorruptData. An older or newer integer schema version yields UnsupportedVersion.
The adapter is deliberately stricter than default model serialization: ignoring
unknown properties could silently erase data on the next save.

Save validates an existing catalog before replacing it. Corrupt or unsupported
data is never automatically overwritten, migrated, renamed, or reset. Recovery is
explicit: back up the original, then use a compatible application/migration or
move the damaged catalog aside before rebuilding. This store never changes media
or playlists. Diagnostic inner exceptions are retained; UI should translate the
StorageFailure code rather than show raw exception text.

## Atomic save and cancellation

1. Validate root and read/validate any existing catalog.
2. Write a unique `catalog.<guid>.tmp` file in the catalog directory.
3. Flush asynchronous buffers and request a durable disk flush; close the file.
4. Revalidate root and check cancellation.
5. Commit with File.Replace for an existing catalog or a non-overwriting File.Move
   for the first save. No delete-then-write fallback is used.

Reads allow delete sharing so replacement does not invalidate already-open readers.
With one writer on a local filesystem supporting atomic replacement, readers see
an old or new complete snapshot. If replacement is unsupported or fails, the
operation surfaces an error; it never falls back to truncating the destination.
File contents are flushed before replacement, but sudden power-loss guarantees
for directory metadata and storage hardware are filesystem-dependent. Network
shares and cross-process writers are outside the supported guarantee.

Cancellation before commit preserves the previous catalog. After commit the save
returns success even if cancellation arrives. Blocking path checks and durable
flushes execute on a worker thread. The synchronous commit is not cancellable.
Failures clean up only the current operation's temporary file, best effort.
Process termination may leave orphan temporary files; loads ignore them, and they
may be removed manually while the app is stopped. No automatic orphan promotion
or deletion occurs. IO failures can have uncertain outcomes; reload before retrying.

Tests use isolated real directories to cover restart/readback, replacement,
concurrent readers, staged-write cancellation, root validation failure, malformed
and versioned input preservation, and interrupted temporary files. They do not
simulate physical power loss or claim to validate every filesystem.

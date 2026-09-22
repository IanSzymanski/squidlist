# Local catalog schema (version 1)

`Squidlist.Storage.Catalog` defines the platform-neutral logical schema for
issue #7. A `MediaCatalog` snapshot belongs to one Squidlist root. The persistence
layer will associate the snapshot with that root; this schema does not choose a
database, file location, or atomic-write strategy (issue #8).

## Envelope and identity

- `SchemaVersion`: required integer, currently 1.
- `Entries`: required collection of immutable `CatalogEntry` records. Media IDs
  must be unique within a snapshot. Input collections are defensively copied.
- Entry order has no semantic meaning. Playlist ordering belongs to playlists.
- `MediaId` is the primary key, never a path or fingerprint. Multiple IDs can
  have identical metadata or content; a fingerprint match is not proof of identity.

## Entry fields

| Field | Meaning |
| --- | --- |
| `MediaId` | Nonempty opaque Core media ID, preserved on moves and renames. |
| `Kind` | Core `MediaKind`: audio or video. |
| `Location` | Core normalized `MediaLocation`; relative to the owning root where possible. Absolute locations remain supported for external files. Null is allowed only for unresolved entries. |
| `State` | Available (0), Missing (1), or Unresolved (2). |
| `SizeBytes` | Nullable nonnegative byte count; null means unknown, zero means an observed empty file. |
| `ModifiedAt` | Nullable `DateTimeOffset` describing the last observed file modification instant, not scan time. Null means unknown. |
| `Duration` | Nullable nonnegative `TimeSpan`; null means unknown or extraction failed. |
| `FingerprintStatus` | NotComputed (0), Ready (1), Stale (2), or Failed (3). |

Missing means the last-known location was checked and the file was absent.
Unresolved means a location cannot be confidently assigned, including ambiguous
recovery candidates; an optional location is only a hint. Available means the
last observation found the file, not a guarantee that it still exists.
Both missing and unresolved records retain their ID and any last-known metadata.
Neither state removes a playlist entry or authorizes deletion of files.

Fingerprint status is independent of availability: a missing file may have a
previously computed usable fingerprint. Ready means fingerprint data is usable
for the recorded observation; Stale means it needs recomputation after change;
Failed means the last attempt failed; NotComputed means no attempt has succeeded.
Issue #10 will define algorithm, fingerprint payload, storage relationship, and
ambiguous match results. This schema supplies status only and performs no hashing
or matching. Size and modification time alone must never trigger automatic relinking.

## Serialization and compatibility

The records round-trip with default `System.Text.Json` options (PascalCase field
names and numeric enums). This is a reference representation of the logical
schema, not a commitment to JSON as the catalog storage engine. Core IDs and
locations keep their existing serialization. Dates carry an offset and durations
use the standard `TimeSpan` JSON representation. Unknown metadata stays null.

Only version 1 is accepted. Missing, older, and future versions are rejected with
`NotSupportedException`; malformed records fail constructor validation. The
persistence layer must surface this as a recoverable load failure and preserve
the original data, rather than replace it with an empty catalog. Unknown optional
JSON properties are ignored by the default serializer; unknown enum values fail.

Future required fields, changed meanings, or incompatible representations require
a version increment and an explicit migration before construction. Migrations
must preserve media IDs, unresolved records, and original data until the new
snapshot has been safely written. No historical migration exists for version 1.
Adding fingerprint payloads in #10 must explicitly revisit this compatibility
policy. Root rebinding changes the root association, not stored relative paths
or IDs; reconciliation and recovery are implemented in issues #11 and #12.

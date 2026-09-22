# Playlist file format, version 1

Issue #9 uses indented UTF-8 JSON, without a byte-order mark, in files ending in
`.squidlist.json`. Files are ordinary text: they can be inspected with a text
editor and copied for backup. See [the complete example](examples/evening.squidlist.json).

`Squidlist.Storage.Playlists.PlaylistFileFormat` serializes and parses the format.
It works entirely in memory; file placement, atomic writes, and UI editing belong
to later storage and application work. Encode its output as UTF-8 when saving.
Back up the catalog and media as well as playlists to retain identity resolution;
a playlist contains references, not embedded media or a replacement catalog.

## Fields

| Field | Requirement and meaning |
| --- | --- |
| `version` | Required integer, exactly `1` for this format. |
| `name` | Required nonblank string; Unicode and original whitespace are preserved. This is a display name, not a filesystem path or unique identifier. |
| `entries` | Required array, possibly empty. Array order is playback order. |
| `entries[].mediaId` | Required nonempty GUID string. Writers use the standard hyphenated representation. |
| `entries[].pathHint` | Optional string or null; a nonempty path relative to the Squidlist root. |
| `metadata` | Optional object or null, mapping nonblank case-sensitive keys to string values. Empty string values are valid. |

Metadata is playlist-level descriptive information, for example `description`.
All metadata keys are preserved, including keys unknown to the application.
Version 1 assigns no behavioral meaning to any metadata key. Writers omit empty
metadata and null path hints; explicit null and omission have the same meaning.

## Identity and paths

IDs are persisted opaque identities, never hashes of filenames or paths. Repeated
IDs are allowed and retain their positions. Loading requires no catalog lookup;
missing media and references without hints remain in the playlist for repair.
The ID is authoritative, while a hint may be stale. Removing a reference never
deletes the underlying media file.

Hints follow [Core path normalization](media-paths.md): separators become `/`,
`.` segments are removed, and contained `..` segments are collapsed. Absolute,
drive-relative, empty, and root-escaping paths are rejected. These are lexical
rules, not filesystem or symlink validation. External media can be represented by
ID with no hint; absolute paths must not be exported as relative hints.

`PlaylistDocument` adds the name and metadata around the existing Core `Playlist`.
Core models remain unchanged. Constructing the document validates and normalizes
hints and copies metadata so subsequent caller changes cannot affect the document.
Use `PlaylistFileFormat`, rather than serializing Core objects directly, for files.

## Compatibility and errors

Property names are case-sensitive. Comments, trailing commas, duplicate object
properties, unknown structural fields, null entries, malformed IDs, and invalid
field types are rejected with `JsonException`. Missing or noninteger versions are
malformed; other integer versions raise `NotSupportedException`. Validation never
drops individual entries or silently substitutes an empty playlist.

Readers reject unknown structural fields so a read/edit/write cycle cannot
silently erase unsupported data. Optional descriptive additions fit in metadata;
new structural fields or changed semantics require a new version. Future readers
must explicitly migrate supported older versions, preserving IDs, order, repeats,
unresolved entries, and metadata. No pre-version-1 migration is defined.

Callers must leave the original file untouched on failure and offer recovery or
an application update for unsupported versions. Formatting, property order,
omitted nulls, and normalized path spelling may change on a successful round trip;
playlist meaning is preserved. Names need separate filename validation when a
future persistence implementation chooses storage paths.

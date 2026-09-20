# Media roots and locations

`Squidlist.Core.Media` contains the portable models used to describe the
Squidlist media root and the paths of media items. The models do not inspect the
filesystem. A platform or storage layer reports the root state after it has
performed discovery.

## Root state

`MediaRoot.State` is one of:

- `Configured`: a valid absolute root path is available.
- `Missing`: no usable root is currently available. The path may be `null` when
  no root has ever been configured, or may retain the last configured path for
  later repair.
- `Moved`: a valid root path is known, but discovery determined that the root
  moved from its previous location.
- `Invalid`: the configured value is not a valid absolute path. `Path` is
  `null` unless the supplied value could still be normalized.

`Configured`, `Missing`, and `Moved` do not imply that the directory exists;
filesystem checks belong outside Core.

## Normalization

The models use these deterministic, platform-neutral rules:

- Both `/` and `\\` are accepted as separators. Stored paths use `/`.
- Repeated separators are collapsed, `.` segments are removed, and `..`
  segments remove the preceding segment. Traversing above the root is invalid.
- Absolute paths may use `/`, a drive root such as `C:/`, or a UNC-style prefix
  such as `//server/share`. Trailing separators are removed except for the root
  itself.
- Relative paths must contain at least one segment and may not be rooted or
  drive-relative. No filesystem-specific character blacklist is applied beyond
  rejecting the null character.
- Root containment uses an ordinal, case-sensitive lexical comparison and a
  path-segment boundary. Core does not apply platform-specific case folding.

`MediaRoot.Locate` converts an absolute input below the root into a
`MediaLocation` with `Kind == Relative`. Inputs outside the root remain absolute;
relative inputs are normalized and remain relative. This means a media catalog
can preserve a root-independent location without deriving media identity from a
current filesystem path.

Folder-picker behavior and filesystem existence checks are intentionally outside
the scope of these models.

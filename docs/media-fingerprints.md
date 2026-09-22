# Media fingerprints for recovery

Issue #10 adds `Squidlist.Storage.Fingerprints`. These types use only .NET and Core;
there is no filesystem search, catalog mutation, or Windows-specific dependency.

## Version 1 algorithm and inputs

`MediaFingerprinter.ComputeAsync` hashes every byte of a supplied readable stream
using SHA-256 and records the number of bytes actually read. It produces an
immutable `MediaFingerprint` with Version 1, Algorithm `sha256`, nonnegative
SizeBytes, and a lowercase 64-character hexadecimal Digest. Equality compares all
four fields. Uppercase hex is accepted at construction and normalized.

Paths, file names, timestamps, media kind, and extracted metadata are not hash
inputs. Unchanged bytes produce the same fingerprint after moving or renaming a
file. Equal lengths and modification times do not imply equal fingerprints.
Changing tags or any other embedded bytes changes the fingerprint. Empty files
have the standard SHA-256 empty digest and size zero; several empty files are
ambiguous matches, not a shared media identity.

Full-content hashing avoids assumptions about which parts of a file distinguish
it. Cost is O(file size) IO/CPU, with one pooled 64 KiB read buffer and constant
hash state. It never reads the entire file into memory, does not require Length
or seeking, and honors cancellation between reads and through ReadAsync.
The rented buffer is cleared and returned on success, failure, and cancellation.

The caller owns the stream. It must supply the complete file at position zero;
seekable streams positioned elsewhere are rejected. A nonseekable stream's
initial position cannot be verified, so the caller must guarantee it. Successful
hashing consumes to EOF; failures may partially consume the stream. The stream
remains open. IO errors and OperationCanceledException propagate and no partial
fingerprint is returned. Filesystem adapters own locking and snapshot stability:
reject or retry files that change during hashing rather than store mixed-content
evidence. This stream-only API cannot detect external concurrent modifications.

## Matching and ambiguity

`FingerprintCandidate` associates an existing nonempty MediaId with a fingerprint.
`FingerprintMatcher.Match` returns all distinct matching IDs in encounter order:

- None: no equal content evidence.
- Unique: exactly one candidate identity matches.
- Ambiguous: two or more distinct identities have equal content; never pick the first.

Repeated identical candidates for the same ID are deduplicated; contradictory
fingerprints for one ID are rejected. The result owns a read-only snapshot and
never changes a catalog, playlist, file, or MediaId. A copy is indistinguishable
from a move by content alone. A Unique result only describes the supplied candidate
set, not proof of identity or permission to relink. #11 must account for candidate
completeness, availability, duplicate content, and user repair before relinking.

## Catalog relationship and compatibility

Catalog version 1 remains unchanged: its FingerprintStatus is a lifecycle flag,
not a fingerprint payload. `FingerprintCandidate` is a serializable companion
record keyed by MediaId for an eventual fingerprint index. No index file or write
path is introduced here. Index persistence/atomic coordination with catalog changes
must be defined by the integrating storage work; do not claim Ready without its
corresponding usable fingerprint. Pass only Ready evidence to the matcher; exclude
NotComputed, Stale, and Failed candidates. File-change observations invalidate
previous evidence until recomputation succeeds.

Records round-trip through System.Text.Json with PascalCase fields, the existing
Core MediaId representation, and the fields above. A future persistence reader
must require all constructor parameters (RespectRequiredConstructorParameters),
validate its envelope, and map invalid payloads to recoverable errors. Constructors
reject invalid IDs, negative sizes, malformed digests, and unknown algorithms or
versions. Future algorithms or input definitions need a new fingerprint version;
old evidence must be recomputed or explicitly migrated, never compared as though
it used the current algorithm. Embedding payloads in catalog records would also
require an explicit catalog schema/compatibility change. This PR makes neither
that change nor a dependency on catalog persistence PR #47.

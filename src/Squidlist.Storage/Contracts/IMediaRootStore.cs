using Squidlist.Core.Media;

namespace Squidlist.Storage.Contracts;

/// <summary>Owns local root configuration and validation; never displays a folder picker.</summary>
/// <remarks>See docs/storage-contracts.md for shared failure and cancellation guarantees.</remarks>
public interface IMediaRootStore
{
    /// <summary>Returns validated root state, or Missing(null) when setup has not occurred.</summary>
    Task<MediaRoot> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an existing root marker, or initializes a new root when createNew is true,
    /// then persists configuration. Never overwrites a conflicting marker or deletes content.
    /// The path must be absolute. An unsuccessful operation preserves prior configuration.
    /// </summary>
    Task<MediaRoot> ConfigureAsync(string absolutePath, bool createNew, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebinds the configured root after verifying its persisted marker identity at the new
    /// absolute path. Preserves media IDs, playlist files, and relative paths. An absent prior
    /// configuration or mismatched marker is a Conflict; this never creates a new root.
    /// </summary>
    Task<MediaRoot> RebindAsync(string absolutePath, CancellationToken cancellationToken = default);
}

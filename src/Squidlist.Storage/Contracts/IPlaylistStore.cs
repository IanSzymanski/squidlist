using Squidlist.Core.Media;
using Squidlist.Storage.Playlists;

namespace Squidlist.Storage.Contracts;

/// <summary>Owns playlist files and opaque root-relative handles; names are display names, not paths.</summary>
public interface IPlaylistStore
{
    /// <summary>
    /// Returns an immutable snapshot of file handles, including unreadable playlist files.
    /// Empty means no files; listing errors fail the operation. No playlist contents are parsed.
    /// </summary>
    Task<IReadOnlyList<string>> ListAsync(MediaRoot root, CancellationToken cancellationToken = default);

    /// <summary>Loads one document; null means the file is absent, not corrupt or unsupported.</summary>
    Task<PlaylistDocument?> LoadAsync(MediaRoot root, string handle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new file without overwriting any existing file and returns its opaque handle.
    /// Duplicate display names are allowed; naming policy belongs to the caller.
    /// </summary>
    Task<string> CreateAsync(MediaRoot root, PlaylistDocument document, CancellationToken cancellationToken = default);

    /// <summary>Atomically replaces an existing playlist file; an absent handle is NotFound.</summary>
    Task SaveAsync(MediaRoot root, string handle, PlaylistDocument document, CancellationToken cancellationToken = default);

    /// <summary>Deletes only the playlist file; an already absent file is a successful no-op.</summary>
    Task DeleteAsync(MediaRoot root, string handle, CancellationToken cancellationToken = default);
}

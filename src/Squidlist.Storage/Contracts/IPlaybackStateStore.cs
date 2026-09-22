using Squidlist.Core.Media;

namespace Squidlist.Storage.Contracts;

/// <summary>Persists playback position and queue together, without resolving or playing media.</summary>
public interface IPlaybackStateStore
{
    /// <summary>Returns null only when no saved session exists; preserves unresolved media IDs.</summary>
    Task<PlaybackSession?> LoadAsync(MediaRoot root, CancellationToken cancellationToken = default);

    /// <summary>Atomically replaces state and queue as one session snapshot.</summary>
    Task SaveAsync(MediaRoot root, PlaybackSession session, CancellationToken cancellationToken = default);
}

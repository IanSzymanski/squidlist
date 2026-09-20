using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Squidlist.Core.Models;

/// <summary>
/// An immutable ordered queue of media references and its shuffle setting.
/// </summary>
public sealed class PlaybackQueue : IEquatable<PlaybackQueue>
{
    [JsonConstructor]
    public PlaybackQueue(IReadOnlyList<MediaId> mediaIds, bool isShuffled)
    {
        ArgumentNullException.ThrowIfNull(mediaIds);

        MediaIds = new ReadOnlyCollection<MediaId>(mediaIds.ToArray());
        IsShuffled = isShuffled;
    }

    public IReadOnlyList<MediaId> MediaIds { get; }

    public bool IsShuffled { get; }

    public static PlaybackQueue Empty => new(Array.Empty<MediaId>(), false);

    public PlaybackQueue Enqueue(MediaId mediaId)
    {
        return new PlaybackQueue(MediaIds.Append(mediaId).ToArray(), IsShuffled);
    }

    public PlaybackQueue RemoveAt(int index)
    {
        ValidateIndex(index);

        return new PlaybackQueue(
            MediaIds.Where((_, mediaIndex) => mediaIndex != index).ToArray(),
            IsShuffled);
    }

    /// <summary>
    /// Returns a queue with one item moved while preserving all other items.
    /// </summary>
    public PlaybackQueue Move(int fromIndex, int toIndex)
    {
        ValidateIndex(fromIndex);
        ValidateIndex(toIndex);

        if (fromIndex == toIndex)
        {
            return this;
        }

        var reorderedMediaIds = MediaIds.ToList();
        var mediaId = reorderedMediaIds[fromIndex];
        reorderedMediaIds.RemoveAt(fromIndex);
        reorderedMediaIds.Insert(toIndex, mediaId);

        return new PlaybackQueue(reorderedMediaIds, IsShuffled);
    }

    public PlaybackQueue WithShuffle(bool isShuffled) =>
        new(MediaIds, isShuffled);

    public bool Equals(PlaybackQueue? other) =>
        other is not null &&
        IsShuffled == other.IsShuffled &&
        MediaIds.SequenceEqual(other.MediaIds);

    public override bool Equals(object? obj) => Equals(obj as PlaybackQueue);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsShuffled);

        foreach (var mediaId in MediaIds)
        {
            hash.Add(mediaId);
        }

        return hash.ToHashCode();
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)MediaIds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}

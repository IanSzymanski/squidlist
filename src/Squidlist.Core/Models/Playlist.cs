using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Squidlist.Core.Models;

/// <summary>
/// An immutable, ordered collection of playlist entries.
/// </summary>
public sealed class Playlist : IEquatable<Playlist>
{
    [JsonConstructor]
    public Playlist(IReadOnlyList<PlaylistEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Entries = new ReadOnlyCollection<PlaylistEntry>(entries.ToArray());
    }

    public IReadOnlyList<PlaylistEntry> Entries { get; }

    /// <summary>
    /// Returns a playlist with an entry appended, preserving existing order.
    /// </summary>
    public Playlist Add(PlaylistEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new Playlist(Entries.Append(entry).ToArray());
    }

    /// <summary>
    /// Returns a playlist without the entry at <paramref name="index"/>.
    /// This only removes the reference from the playlist; it does not delete media.
    /// </summary>
    public Playlist RemoveAt(int index)
    {
        if ((uint)index >= (uint)Entries.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new Playlist(Entries.Where((_, entryIndex) => entryIndex != index).ToArray());
    }

    public bool Equals(Playlist? other) =>
        other is not null && Entries.SequenceEqual(other.Entries);

    public override bool Equals(object? obj) => Equals(obj as Playlist);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var entry in Entries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }
}

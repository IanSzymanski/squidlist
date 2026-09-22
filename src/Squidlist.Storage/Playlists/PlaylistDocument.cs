using System.Collections.ObjectModel;
using Squidlist.Core.Media;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Playlists;

/// <summary>A named playlist with portable path hints and optional descriptive metadata.</summary>
public sealed class PlaylistDocument
{
    public PlaylistDocument(string name, Playlist playlist, IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(playlist);

        // Enforce the file format's relative-only hints without restricting Core's general model.
        Playlist = new Playlist(playlist.Entries.Select(entry => new PlaylistEntry(
            entry.MediaId,
            entry.PathHint is null ? null : MediaLocation.Relative(entry.PathHint).Path)).ToArray());

        var snapshot = new Dictionary<string, string>(StringComparer.Ordinal);
        if (metadata is not null)
        {
            foreach (var (key, value) in metadata)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                ArgumentNullException.ThrowIfNull(value);
                snapshot.Add(key, value);
            }
        }

        Name = name;
        Metadata = new ReadOnlyDictionary<string, string>(snapshot);
    }

    public string Name { get; }
    public Playlist Playlist { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

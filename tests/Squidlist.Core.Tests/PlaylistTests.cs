using System.Text.Json;
using Squidlist.Core.Models;
using Xunit;

namespace Squidlist.Core.Tests;

public sealed class PlaylistTests
{
    [Fact]
    public void Add_preserves_entry_order()
    {
        var first = new PlaylistEntry(MediaId.New(), "audio/first.mp3");
        var second = new PlaylistEntry(MediaId.New(), "audio/second.mp3");

        var playlist = new Playlist(new[] { first }).Add(second);

        Assert.Equal(new[] { first, second }, playlist.Entries);
    }

    [Fact]
    public void RemoveAt_removes_only_the_playlist_reference()
    {
        var removed = new PlaylistEntry(MediaId.New(), "audio/removed.mp3");
        var retained = new PlaylistEntry(MediaId.New(), "audio/retained.mp3");
        var playlist = new Playlist(new[] { removed, retained });

        var updated = playlist.RemoveAt(0);

        Assert.Single(updated.Entries);
        Assert.Equal(retained, updated.Entries[0]);
        Assert.Equal(2, playlist.Entries.Count);
        Assert.Equal(removed.MediaId, playlist.Entries[0].MediaId);
    }

    [Fact]
    public void An_entry_can_represent_media_that_is_not_currently_resolved()
    {
        var entry = new PlaylistEntry(MediaId.New(), "audio/missing.mp3");

        var playlist = new Playlist(new[] { entry });

        Assert.Equal(entry.MediaId, playlist.Entries[0].MediaId);
        Assert.Equal("audio/missing.mp3", playlist.Entries[0].PathHint);
    }

    [Fact]
    public void Playlist_entry_rejects_a_default_media_id()
    {
        Assert.Throws<ArgumentException>(() => new PlaylistEntry(default));
    }

    [Fact]
    public void Playlist_rejects_null_entries()
    {
        var entries = new PlaylistEntry[] { null! };

        Assert.Throws<ArgumentException>(() => new Playlist(entries));
    }

    [Fact]
    public void Json_round_trip_preserves_identity_path_hint_order_and_value_equality()
    {
        var playlist = new Playlist(new[]
        {
            new PlaylistEntry(MediaId.New(), "audio/first.mp3"),
            new PlaylistEntry(MediaId.New(), "video/second.mp4")
        });

        var json = JsonSerializer.Serialize(playlist);
        var roundTripped = JsonSerializer.Deserialize<Playlist>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(playlist, roundTripped);
        Assert.Equal(playlist.Entries[0].MediaId, roundTripped.Entries[0].MediaId);
        Assert.Equal(playlist.Entries[1].PathHint, roundTripped.Entries[1].PathHint);
    }
}

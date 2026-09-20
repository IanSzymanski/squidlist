using System.Text.Json;
using Squidlist.Core.Media;
using Squidlist.Core.Models;
using Xunit;

namespace Squidlist.Core.Tests;

public sealed class MediaItemTests
{
    [Fact]
    public void Supports_audio_and_video_items()
    {
        var audio = new MediaItem(MediaId.New(), MediaKind.Audio);
        var video = new MediaItem(MediaId.New(), MediaKind.Video);

        Assert.Equal(MediaKind.Audio, audio.Kind);
        Assert.Equal(MediaKind.Video, video.Kind);
    }

    [Fact]
    public void Identity_is_preserved_when_location_changes()
    {
        var mediaId = MediaId.New();
        var beforeMove = new MediaItem(
            mediaId,
            MediaKind.Audio,
            MediaLocation.Relative("old/track.mp3"));
        var afterMove = new MediaItem(
            mediaId,
            MediaKind.Audio,
            MediaLocation.Relative("new/track.mp3"));

        Assert.Equal(beforeMove.MediaId, afterMove.MediaId);
        Assert.NotEqual(beforeMove.Location, afterMove.Location);
    }

    [Fact]
    public void An_item_can_be_unresolved_without_a_location()
    {
        var item = new MediaItem(MediaId.New(), MediaKind.Video);

        Assert.Null(item.Location);
    }

    [Fact]
    public void Equality_is_value_based()
    {
        var mediaId = MediaId.New();
        var first = new MediaItem(
            mediaId,
            MediaKind.Audio,
            MediaLocation.Relative("audio/track.mp3"));
        var equivalent = new MediaItem(
            mediaId,
            MediaKind.Audio,
            MediaLocation.Relative("audio/track.mp3"));
        var differentKind = new MediaItem(
            mediaId,
            MediaKind.Video,
            MediaLocation.Relative("audio/track.mp3"));

        Assert.Equal(first, equivalent);
        Assert.NotEqual(first, differentKind);
    }

    [Fact]
    public void Json_round_trip_preserves_a_resolved_item()
    {
        var item = new MediaItem(
            MediaId.New(),
            MediaKind.Video,
            MediaLocation.Relative("video/movie.mp4"));

        var json = JsonSerializer.Serialize(item);
        var roundTripped = JsonSerializer.Deserialize<MediaItem>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(item, roundTripped);
    }

    [Fact]
    public void Json_round_trip_preserves_an_unresolved_item()
    {
        var item = new MediaItem(MediaId.New(), MediaKind.Audio);

        var json = JsonSerializer.Serialize(item);
        var roundTripped = JsonSerializer.Deserialize<MediaItem>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(item, roundTripped);
        Assert.Null(roundTripped.Location);
    }

    [Fact]
    public void Rejects_a_default_media_id()
    {
        Assert.Throws<ArgumentException>(() => new MediaItem(default, MediaKind.Audio));
    }

    [Fact]
    public void Rejects_an_unknown_media_kind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MediaItem(MediaId.New(), (MediaKind)999));
    }
}

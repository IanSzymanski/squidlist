using System.Text.Json;
using Squidlist.Core.Models;
using Xunit;

namespace Squidlist.Core.Tests;

public sealed class PlaybackTests
{
    [Fact]
    public void Playback_state_round_trips_persisted_values()
    {
        var mediaId = MediaId.New();
        var timestamp = new DateTimeOffset(2026, 9, 20, 12, 30, 0, TimeSpan.Zero);
        var state = new PlaybackState(mediaId, TimeSpan.FromSeconds(42), 0.65, timestamp);

        var restored = JsonSerializer.Deserialize<PlaybackState>(JsonSerializer.Serialize(state));

        Assert.NotNull(restored);
        Assert.Equal(state, restored);
    }

    [Fact]
    public void Playback_state_allows_no_current_media()
    {
        var state = PlaybackState.Default;

        Assert.Null(state.CurrentMediaId);
        Assert.Equal(TimeSpan.Zero, state.Position);
        Assert.Equal(1, state.Volume);
        Assert.Null(state.LastPlayedAt);
    }

    [Fact]
    public void Playback_state_rejects_invalid_position_and_volume()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PlaybackState(null, TimeSpan.FromSeconds(-1), 0.5, null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PlaybackState(null, TimeSpan.Zero, 1.1, null));
    }

    [Fact]
    public void Playback_queue_preserves_order_and_shuffle_state()
    {
        var first = MediaId.New();
        var second = MediaId.New();
        var third = MediaId.New();

        var queue = PlaybackQueue.Empty
            .Enqueue(first)
            .Enqueue(second)
            .Enqueue(third)
            .Move(2, 0)
            .WithShuffle(true);

        Assert.Equal(new[] { third, first, second }, queue.MediaIds);
        Assert.True(queue.IsShuffled);
    }

    [Fact]
    public void Playback_queue_round_trips_order_and_shuffle_state()
    {
        var queue = new PlaybackQueue(new[] { MediaId.New(), MediaId.New() }, true);

        var restored = JsonSerializer.Deserialize<PlaybackQueue>(JsonSerializer.Serialize(queue));

        Assert.NotNull(restored);
        Assert.Equal(queue, restored);
    }

}

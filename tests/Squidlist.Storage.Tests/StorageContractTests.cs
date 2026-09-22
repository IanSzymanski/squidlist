using System.Text.Json;
using Squidlist.Core.Media;
using Squidlist.Core.Models;
using Squidlist.Storage.Contracts;
using Xunit;

namespace Squidlist.Storage.Tests;

public sealed class StorageContractTests
{
    [Fact]
    public void Session_round_trip_preserves_unresolved_current_media_and_repeated_queue_entries()
    {
        var current = MediaId.New();
        var queued = MediaId.New();
        var session = new PlaybackSession(
            new PlaybackState(current, TimeSpan.FromSeconds(42), 0.5,
                DateTimeOffset.Parse("2026-09-21T12:00:00Z")),
            new PlaybackQueue([queued, queued], true));

        var restored = JsonSerializer.Deserialize<PlaybackSession>(JsonSerializer.Serialize(session));

        Assert.Equal(session, restored);
        Assert.NotNull(restored);
        Assert.Equal(current, restored.State.CurrentMediaId);
        Assert.Equal(new[] { queued, queued }, restored.Queue.MediaIds);
        Assert.True(restored.Queue.IsShuffled);
    }

    [Fact]
    public void Empty_session_is_valid_and_distinct_from_no_saved_session()
    {
        var session = new PlaybackSession(PlaybackState.Default, PlaybackQueue.Empty);
        var restored = JsonSerializer.Deserialize<PlaybackSession>(JsonSerializer.Serialize(session));
        Assert.NotNull(restored);
        Assert.Null(restored.State.CurrentMediaId);
        Assert.Empty(restored.Queue.MediaIds);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"State\":null,\"Queue\":null}")]
    public void Incomplete_saved_sessions_are_rejected(string json)
    {
        Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<PlaybackSession>(json));
    }

    [Fact]
    public void Session_rejects_empty_current_identity_without_changing_core()
    {
        var state = new PlaybackState(default(MediaId), TimeSpan.Zero, 1, null);
        Assert.Throws<ArgumentException>(() => new PlaybackSession(state, PlaybackQueue.Empty));
        var json = JsonSerializer.Serialize(new { State = state, Queue = PlaybackQueue.Empty });
        Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<PlaybackSession>(json));
    }

    [Fact]
    public void Discovery_has_relative_location_and_media_kind_without_assigning_identity()
    {
        var location = MediaLocation.Relative(@"audio\track.mp3");
        var discovered = new MediaScanEvent.Discovered(location, MediaKind.Audio);
        Assert.Equal("audio/track.mp3", discovered.Location.Path);
        Assert.Equal(MediaKind.Audio, discovered.Kind);
        Assert.Equal(discovered, new MediaScanEvent.Discovered(location, MediaKind.Audio));
        Assert.Throws<ArgumentException>(() => new MediaScanEvent.Discovered(
            MediaLocation.Absolute("C:/external.mp3"), MediaKind.Audio));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MediaScanEvent.Discovered(location, (MediaKind)99));
    }

    [Fact]
    public void Scan_failures_represent_inaccessible_subtrees_and_root_failures()
    {
        var subtree = new MediaScanEvent.Failed(MediaLocation.Relative("private"), StorageFailure.AccessDenied);
        var root = new MediaScanEvent.Failed(null, StorageFailure.IoError);
        Assert.Equal("private", subtree.Location!.Path);
        Assert.Equal(StorageFailure.AccessDenied, subtree.Failure);
        Assert.Null(root.Location);
        Assert.Throws<ArgumentException>(() => new MediaScanEvent.Failed(
            MediaLocation.Absolute("C:/private"), StorageFailure.AccessDenied));
    }

    [Fact]
    public void Storage_failure_keeps_diagnostics_and_rejects_unknown_codes()
    {
        var cause = new IOException("Diagnostic detail");
        var error = new StorageException(StorageFailure.IoError, "Read failed", cause);
        Assert.Same(cause, error.InnerException);
        Assert.Equal(StorageFailure.IoError, error.Failure);
        Assert.Throws<ArgumentOutOfRangeException>(() => new StorageException((StorageFailure)99, "Invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MediaScanEvent.Failed(null, (StorageFailure)99));
    }
}

using System.Text.Json;
using Squidlist.Core.Media;
using Squidlist.Core.Models;
using Squidlist.Storage.Catalog;
using Xunit;

namespace Squidlist.Storage.Tests;

public sealed class CatalogTests
{
    private static CatalogEntry Entry(MediaId? id = null,
        CatalogMediaState state = CatalogMediaState.Available,
        string? path = "audio/track.mp3", long? size = 1024,
        TimeSpan? duration = null, FingerprintStatus fingerprint = FingerprintStatus.NotComputed) =>
        new(id ?? MediaId.New(), MediaKind.Audio,
            path is null ? null : MediaLocation.Relative(path), state, size,
            DateTimeOffset.Parse("2026-09-20T12:00:00Z"), duration, fingerprint);

    [Theory]
    [InlineData(CatalogMediaState.Available, "audio/track.mp3")]
    [InlineData(CatalogMediaState.Missing, "audio/old.mp3")]
    [InlineData(CatalogMediaState.Unresolved, null)]
    [InlineData(CatalogMediaState.Unresolved, "audio/ambiguous.mp3")]
    public void Catalog_round_trip_preserves_recovery_records(CatalogMediaState state, string? path)
    {
        var entry = Entry(state: state, path: path, duration: TimeSpan.FromSeconds(90));
        var catalog = new MediaCatalog(1, [entry]);
        var restored = JsonSerializer.Deserialize<MediaCatalog>(JsonSerializer.Serialize(catalog));
        Assert.NotNull(restored);
        Assert.Equal(1, restored.SchemaVersion);
        Assert.Equal(entry, Assert.Single(restored.Entries));
    }

    [Fact]
    public void Move_and_missing_state_preserve_identity_and_last_observed_metadata()
    {
        var original = Entry();
        var moved = Entry(original.MediaId, path: "renamed/track.mp3");
        var missing = Entry(original.MediaId, CatalogMediaState.Missing, "renamed/track.mp3");
        Assert.Equal(original.MediaId, moved.MediaId);
        Assert.Equal(moved.MediaId, missing.MediaId);
        Assert.Equal(moved.Location, missing.Location);
        Assert.Equal(original.SizeBytes, missing.SizeBytes);
        Assert.Equal(original.ModifiedAt, missing.ModifiedAt);
    }

    [Fact]
    public void Unknown_metadata_is_distinct_from_zero()
    {
        var unknown = Entry(size: null);
        var zero = Entry(unknown.MediaId, size: 0, duration: TimeSpan.Zero);
        Assert.Null(unknown.SizeBytes);
        Assert.Null(unknown.Duration);
        Assert.NotEqual(unknown, zero);
    }

    [Fact]
    public void Catalog_copies_input_and_rejects_duplicate_identities()
    {
        var entry = Entry();
        var entries = new List<CatalogEntry> { entry };
        var catalog = new MediaCatalog(1, entries);
        entries.Clear();
        Assert.Equal(entry, Assert.Single(catalog.Entries));
        Assert.Throws<ArgumentException>(() => new MediaCatalog(1, [entry, Entry(entry.MediaId)]));
        Assert.Throws<ArgumentNullException>(() => new MediaCatalog(1, [null!]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    public void Unsupported_versions_are_rejected_on_read(int version)
    {
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MediaCatalog>(
            $$"""{"SchemaVersion":{{version}},"Entries":[]}"""));
    }

    [Fact]
    public void Malformed_records_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => Entry(default(MediaId)));
        Assert.Throws<ArgumentException>(() => Entry(path: null));
        Assert.Throws<ArgumentException>(() => Entry(state: CatalogMediaState.Missing, path: null));
        Assert.Throws<ArgumentOutOfRangeException>(() => Entry(size: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Entry(duration: TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Entry(state: (CatalogMediaState)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Entry(fingerprint: (FingerprintStatus)99));
    }

    [Theory]
    [InlineData(FingerprintStatus.NotComputed)]
    [InlineData(FingerprintStatus.Ready)]
    [InlineData(FingerprintStatus.Stale)]
    [InlineData(FingerprintStatus.Failed)]
    public void Fingerprint_status_round_trips(FingerprintStatus status)
    {
        var entry = Entry(fingerprint: status);
        Assert.Equal(entry, JsonSerializer.Deserialize<CatalogEntry>(JsonSerializer.Serialize(entry)));
    }

    [Fact]
    public void External_video_and_unknown_metadata_round_trip()
    {
        var entry = new CatalogEntry(MediaId.New(), MediaKind.Video,
            MediaLocation.Absolute("C:/external/movie.mp4"), CatalogMediaState.Available,
            null, null, null, FingerprintStatus.NotComputed);
        Assert.Equal(entry, JsonSerializer.Deserialize<CatalogEntry>(JsonSerializer.Serialize(entry)));
    }

    [Fact]
    public void Json_read_rejects_duplicate_ids_and_invalid_metadata()
    {
        var entry = Entry();
        var duplicateJson = JsonSerializer.Serialize(new { SchemaVersion = 1, Entries = new[] { entry, entry } });
        Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<MediaCatalog>(duplicateJson));
        var invalidJson = JsonSerializer.Serialize(entry).Replace("\"SizeBytes\":1024", "\"SizeBytes\":-1");
        Assert.Throws<ArgumentOutOfRangeException>(() => JsonSerializer.Deserialize<CatalogEntry>(invalidJson));
    }
}

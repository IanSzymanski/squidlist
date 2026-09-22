using System.Text.Json;
using System.Text.Json.Nodes;
using Squidlist.Core.Models;
using Squidlist.Storage.Playlists;
using Xunit;

namespace Squidlist.Storage.Tests;

public sealed class PlaylistFileFormatTests
{
    private const string ExampleId = "64061c81-cd34-44b4-bb38-ab45f415829b";

    private static string Example => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "evening.squidlist.json"));

    [Fact]
    public void Documented_example_preserves_order_repeats_and_unresolved_references()
    {
        var document = PlaylistFileFormat.Deserialize(Example);
        Assert.Equal("Evening listening", document.Name);
        Assert.Equal(3, document.Playlist.Entries.Count);
        Assert.Equal(Guid.Parse(ExampleId), document.Playlist.Entries[0].MediaId.Value);
        Assert.Equal(document.Playlist.Entries[0], document.Playlist.Entries[2]);
        Assert.Null(document.Playlist.Entries[1].PathHint);

        var restored = PlaylistFileFormat.Deserialize(PlaylistFileFormat.Serialize(document));
        Assert.Equal(document.Name, restored.Name);
        Assert.Equal(document.Playlist, restored.Playlist);
        Assert.Equal(document.Metadata["description"], restored.Metadata["description"]);
    }

    [Fact]
    public void Writer_uses_readable_versioned_json_and_plain_guid_strings()
    {
        var json = PlaylistFileFormat.Serialize(PlaylistFileFormat.Deserialize(Example));
        Assert.Contains("\n", json);
        using var parsed = JsonDocument.Parse(json);
        Assert.Equal(1, parsed.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(ExampleId, parsed.RootElement.GetProperty("entries")[0].GetProperty("mediaId").GetString());
    }

    [Fact]
    public void Empty_playlist_and_unicode_name_round_trip()
    {
        var document = new PlaylistDocument("  音楽 🎵  ", new Playlist([]));
        var json = PlaylistFileFormat.Serialize(document);
        Assert.DoesNotContain("metadata", json);
        var restored = PlaylistFileFormat.Deserialize(json);
        Assert.Equal(document.Name, restored.Name);
        Assert.Empty(restored.Playlist.Entries);
    }

    [Fact]
    public void Metadata_is_copied_and_unknown_keys_and_empty_values_survive()
    {
        var metadata = new Dictionary<string, string> { ["custom.note"] = "keep me", ["empty"] = "" };
        var document = new PlaylistDocument("Notes", new Playlist([]), metadata);
        metadata["custom.note"] = "changed";
        var restored = PlaylistFileFormat.Deserialize(PlaylistFileFormat.Serialize(document));
        Assert.Equal("keep me", restored.Metadata["custom.note"]);
        Assert.Equal("", restored.Metadata["empty"]);
    }

    [Fact]
    public void Hints_are_normalized_without_changing_core_playlist()
    {
        var playlist = new Playlist([new PlaylistEntry(MediaId.New(), @"audio\old\..\track.mp3")]);
        var document = new PlaylistDocument("Normalized", playlist);
        Assert.Equal("audio/track.mp3", document.Playlist.Entries[0].PathHint);
        Assert.Equal(@"audio\old\..\track.mp3", playlist.Entries[0].PathHint);
    }

    [Theory]
    [InlineData("C:/music/track.mp3")]
    [InlineData("/music/track.mp3")]
    [InlineData("../track.mp3")]
    [InlineData("C:track.mp3")]
    [InlineData("")]
    public void Invalid_hints_are_rejected_on_read_and_write(string hint)
    {
        var root = JsonNode.Parse(Example)!;
        root["entries"]![0]!["pathHint"] = hint;
        Assert.Throws<JsonException>(() => PlaylistFileFormat.Deserialize(root.ToJsonString()));
        Assert.ThrowsAny<ArgumentException>(() => new PlaylistDocument("Invalid",
            new Playlist([new PlaylistEntry(MediaId.New(), hint)])));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    public void Unsupported_versions_are_distinct_from_malformed_files(int version)
    {
        var root = JsonNode.Parse(Example)!;
        root["version"] = version;
        Assert.Throws<NotSupportedException>(() => PlaylistFileFormat.Deserialize(root.ToJsonString()));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"version\":\"1\"}")]
    [InlineData("{\"version\":1.5}")]
    [InlineData("{\"version\":1,\"entries\":[]}")]
    [InlineData("{\"version\":1,\"name\":\"x\"}")]
    [InlineData("{\"version\":1,\"name\":null,\"entries\":[]}")]
    [InlineData("{\"version\":1,\"name\":\"  \",\"entries\":[]}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":null}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[null]}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[{}]}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[{\"mediaId\":\"invalid\"}]}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[{\"mediaId\":\"00000000-0000-0000-0000-000000000000\"}]}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[],\"metadata\":{\"x\":null}}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[],\"metadata\":{\"\":\"x\"}}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[],\"futureField\":true}")]
    [InlineData("{\"version\":1,\"version\":1,\"name\":\"x\",\"entries\":[]}")]
    [InlineData("{\"version\":1,\"name\":\"x\",\"entries\":[],\"metadata\":{\"x\":\"a\",\"x\":\"b\"}}")]
    public void Malformed_files_fail_without_dropping_data(string json)
    {
        Assert.Throws<JsonException>(() => PlaylistFileFormat.Deserialize(json));
    }

    [Fact]
    public void Unknown_entry_fields_are_rejected()
    {
        var root = JsonNode.Parse(Example)!;
        root["entries"]![0]!["futureField"] = true;
        Assert.Throws<JsonException>(() => PlaylistFileFormat.Deserialize(root.ToJsonString()));
    }

    [Fact]
    public void Explicit_null_optional_fields_are_accepted()
    {
        var root = JsonNode.Parse(Example)!;
        root["metadata"] = null;
        root["entries"]![0]!["pathHint"] = null;
        var document = PlaylistFileFormat.Deserialize(root.ToJsonString());
        Assert.Empty(document.Metadata);
        Assert.Null(document.Playlist.Entries[0].PathHint);
    }
}

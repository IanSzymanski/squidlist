using System.Text.Json;
using System.Text.Json.Serialization;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Playlists;

/// <summary>Encodes version 1 playlist JSON without accessing files or resolving media.</summary>
public static class PlaylistFileFormat
{
    public const int CurrentVersion = 1;
    public const string FileExtension = ".squidlist.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static string Serialize(PlaylistDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(new FileData
        {
            Version = CurrentVersion,
            Name = document.Name,
            Entries = document.Playlist.Entries.Select(entry => new EntryData
            {
                MediaId = entry.MediaId.Value,
                PathHint = entry.PathHint
            }).ToArray(),
            Metadata = document.Metadata.Count == 0 ? null : new Dictionary<string, string>(document.Metadata)
        }, Options);
    }

    /// <exception cref="JsonException">The document is malformed or violates version 1 rules.</exception>
    /// <exception cref="NotSupportedException">The document declares another format version.</exception>
    public static PlaylistDocument Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var parsed = JsonDocument.Parse(json);
        ValidateUniqueProperties(parsed.RootElement);
        if (parsed.RootElement.ValueKind != JsonValueKind.Object ||
            !parsed.RootElement.TryGetProperty("version", out var versionElement) ||
            versionElement.ValueKind != JsonValueKind.Number ||
            !versionElement.TryGetInt32(out var version))
        {
            throw new JsonException("A playlist must declare an integer version.");
        }

        if (version != CurrentVersion)
        {
            throw new NotSupportedException($"Playlist format version {version} is not supported.");
        }

        var data = JsonSerializer.Deserialize<FileData>(json, Options)!;
        if (data.Entries is null || data.Entries.Any(entry => entry is null))
        {
            throw new JsonException("Playlist entries must be an array of non-null objects.");
        }

        try
        {
            return new PlaylistDocument(data.Name,
                new Playlist(data.Entries.Select(entry =>
                    new PlaylistEntry(new MediaId(entry.MediaId), entry.PathHint)).ToArray()),
                data.Metadata);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("Invalid playlist name, media identity, relative path hint, or metadata.", exception);
        }
    }

    private static void ValidateUniqueProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException($"Duplicate playlist property '{property.Name}'.");
                }
                ValidateUniqueProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                ValidateUniqueProperties(child);
            }
        }
    }

    private sealed class FileData
    {
        public required int Version { get; set; }
        public required string Name { get; set; }
        public required EntryData[] Entries { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    private sealed class EntryData
    {
        public required Guid MediaId { get; set; }
        public string? PathHint { get; set; }
    }
}

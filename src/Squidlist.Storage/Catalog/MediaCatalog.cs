using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Catalog;

/// <summary>A versioned catalog snapshot belonging to one media root.</summary>
public sealed class MediaCatalog
{
    public const int CurrentSchemaVersion = 1;

    [JsonConstructor]
    public MediaCatalog(int schemaVersion, IReadOnlyList<CatalogEntry> entries)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new NotSupportedException($"Catalog schema version {schemaVersion} is not supported.");
        ArgumentNullException.ThrowIfNull(entries);
        var snapshot = entries.ToArray();
        var ids = new HashSet<MediaId>();
        foreach (var entry in snapshot)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!ids.Add(entry.MediaId))
                throw new ArgumentException("Catalog media IDs must be unique.", nameof(entries));
        }

        SchemaVersion = schemaVersion;
        Entries = new ReadOnlyCollection<CatalogEntry>(snapshot);
    }

    public int SchemaVersion { get; }
    public IReadOnlyList<CatalogEntry> Entries { get; }
}

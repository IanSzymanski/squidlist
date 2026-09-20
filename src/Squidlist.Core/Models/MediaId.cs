using System.Text.Json.Serialization;

namespace Squidlist.Core.Models;

/// <summary>
/// The persisted identity of a media item.
/// </summary>
public readonly record struct MediaId
{
    [JsonConstructor]
    public MediaId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A media ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static MediaId New() => new(Guid.NewGuid());
}

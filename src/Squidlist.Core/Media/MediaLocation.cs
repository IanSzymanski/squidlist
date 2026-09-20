using System.Text.Json.Serialization;

namespace Squidlist.Core.Media;

/// <summary>
/// A normalized media path stored relative to the media root where possible.
/// </summary>
public sealed record MediaLocation
{
    /// <summary>
    /// Creates a media location and normalizes its path according to its kind.
    /// </summary>
    [JsonConstructor]
    public MediaLocation(MediaLocationKind kind, string path)
    {
        Kind = kind;
        Path = kind switch
        {
            MediaLocationKind.Relative => MediaPathNormalizer.NormalizeRelative(path),
            MediaLocationKind.Absolute => MediaPathNormalizer.NormalizeAbsolute(path),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown media location kind.")
        };
    }

    /// <summary>
    /// Gets whether the location is relative or absolute.
    /// </summary>
    public MediaLocationKind Kind { get; }

    /// <summary>
    /// Gets the normalized path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets whether this location is relative to a media root.
    /// </summary>
    [JsonIgnore]
    public bool IsRelative => Kind == MediaLocationKind.Relative;

    /// <summary>
    /// Creates a normalized relative media location.
    /// </summary>
    public static MediaLocation Relative(string path) => new(MediaLocationKind.Relative, path);

    /// <summary>
    /// Creates a normalized absolute media location.
    /// </summary>
    public static MediaLocation Absolute(string path) => new(MediaLocationKind.Absolute, path);
}

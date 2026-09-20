using System.Text.Json.Serialization;

namespace Squidlist.Core.Media;

/// <summary>
/// A portable description of the configured Squidlist media root.
/// </summary>
public sealed record MediaRoot
{
    /// <summary>
    /// Creates a media root. Non-invalid states require an absolute path, except
    /// for a missing root, which may have no path at all.
    /// </summary>
    [JsonConstructor]
    public MediaRoot(MediaRootState state, string? path)
    {
        State = state;
        Path = state switch
        {
            MediaRootState.Missing when path is null => null,
            MediaRootState.Invalid => TryNormalizeAbsolute(path),
            MediaRootState.Configured or MediaRootState.Missing or MediaRootState.Moved
                => MediaPathNormalizer.NormalizeAbsolute(
                    path ?? throw new ArgumentNullException(nameof(path))),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown media root state.")
        };
    }

    /// <summary>
    /// Gets the state reported for this root.
    /// </summary>
    public MediaRootState State { get; }

    /// <summary>
    /// Gets the normalized absolute root path, when one is known.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Creates a root that is configured and available to the caller.
    /// </summary>
    public static MediaRoot Configured(string path) => new(MediaRootState.Configured, path);

    /// <summary>
    /// Creates a root that is not currently available. A configured path may be
    /// retained for later repair, or omitted when no path was configured.
    /// </summary>
    public static MediaRoot Missing(string? path = null) => new(MediaRootState.Missing, path);

    /// <summary>
    /// Creates a root whose known location has moved.
    /// </summary>
    public static MediaRoot Moved(string path) => new(MediaRootState.Moved, path);

    /// <summary>
    /// Creates a root that cannot be represented as a valid absolute path.
    /// </summary>
    public static MediaRoot Invalid(string? path = null) => new(MediaRootState.Invalid, path);

    /// <summary>
    /// Converts an input location to a relative location when it is below this
    /// root; otherwise it retains a normalized absolute location.
    /// </summary>
    public MediaLocation Locate(string path)
    {
        if (!MediaPathNormalizer.IsAbsolute(path))
        {
            return MediaLocation.Relative(path);
        }

        var absolutePath = MediaPathNormalizer.NormalizeAbsolute(path);
        if (Path is not null
            && State is not MediaRootState.Invalid
            && MediaPathNormalizer.TryGetRelative(Path, absolutePath, out var relativePath))
        {
            return MediaLocation.Relative(relativePath!);
        }

        return MediaLocation.Absolute(absolutePath);
    }

    private static string? TryNormalizeAbsolute(string? path)
    {
        if (path is null)
        {
            return null;
        }

        try
        {
            return MediaPathNormalizer.NormalizeAbsolute(path);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

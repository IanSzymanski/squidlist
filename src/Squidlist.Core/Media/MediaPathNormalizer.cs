namespace Squidlist.Core.Media;

internal static class MediaPathNormalizer
{
    public static bool IsAbsolute(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var normalized = path.Replace('\\', '/');
        return normalized.StartsWith("/", StringComparison.Ordinal)
            || IsDriveAbsolute(normalized);
    }

    public static string NormalizeRelative(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        RejectNullCharacter(path);

        var normalized = path.Replace('\\', '/');
        if (IsAbsolute(normalized))
        {
            throw new ArgumentException("A relative media path cannot be rooted.", nameof(path));
        }

        if (IsDriveRelative(normalized))
        {
            throw new ArgumentException("Drive-relative paths are not supported.", nameof(path));
        }

        return NormalizeSegments(normalized, prefix: null, nameof(path));
    }

    public static string NormalizeAbsolute(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        RejectNullCharacter(path);

        var normalized = path.Replace('\\', '/');
        string prefix;
        string remainder;

        if (normalized.StartsWith("//", StringComparison.Ordinal))
        {
            prefix = "//";
            remainder = normalized.TrimStart('/');
        }
        else if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            prefix = "/";
            remainder = normalized.TrimStart('/');
        }
        else if (IsDriveAbsolute(normalized))
        {
            prefix = normalized[..3];
            remainder = normalized[3..];
        }
        else
        {
            throw new ArgumentException("A media root must be an absolute path.", nameof(path));
        }

        return NormalizeSegments(remainder, prefix, nameof(path));
    }

    public static bool TryGetRelative(string root, string absolutePath, out string? relativePath)
    {
        if (root == absolutePath)
        {
            relativePath = null;
            return false;
        }

        // A double-slash path is treated as a UNC-style path, not as a child of
        // the POSIX root. This keeps the lexical comparison deterministic.
        if (root == "/" && absolutePath.StartsWith("//", StringComparison.Ordinal))
        {
            relativePath = null;
            return false;
        }

        var separator = root.EndsWith("/", StringComparison.Ordinal) ? root : root + "/";
        if (!absolutePath.StartsWith(separator, StringComparison.Ordinal))
        {
            relativePath = null;
            return false;
        }

        var candidate = absolutePath[separator.Length..];
        if (candidate.Length == 0)
        {
            relativePath = null;
            return false;
        }

        relativePath = NormalizeRelative(candidate);
        return true;
    }

    private static string NormalizeSegments(string path, string? prefix, string parameterName)
    {
        var segments = new List<string>();
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    throw new ArgumentException("A path cannot traverse above its root.", parameterName);
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        var joined = string.Join('/', segments);
        if (prefix is null)
        {
            if (joined.Length == 0)
            {
                throw new ArgumentException("A relative media path cannot be empty.", parameterName);
            }

            return joined;
        }

        if (prefix == "/")
        {
            return joined.Length == 0 ? "/" : "/" + joined;
        }

        if (prefix == "//")
        {
            if (joined.Length == 0)
            {
                throw new ArgumentException("A UNC-style path must contain a location.", parameterName);
            }

            return "//" + joined;
        }

        return joined.Length == 0 ? prefix : prefix + joined;
    }

    private static bool IsDriveAbsolute(string path) =>
        path.Length >= 3
        && IsAsciiLetter(path[0])
        && path[1] == ':'
        && path[2] == '/';

    private static bool IsDriveRelative(string path) =>
        path.Length >= 2
        && IsAsciiLetter(path[0])
        && path[1] == ':'
        && !IsDriveAbsolute(path);

    private static bool IsAsciiLetter(char value) =>
        (value is >= 'A' and <= 'Z') || (value is >= 'a' and <= 'z');

    private static void RejectNullCharacter(string path)
    {
        if (path.IndexOf('\0') >= 0)
        {
            throw new ArgumentException("A media path cannot contain a null character.", nameof(path));
        }
    }
}

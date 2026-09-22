using System.Text.Json;
using System.Text.Json.Serialization;
using Squidlist.Core.Media;
using Squidlist.Storage.Contracts;

namespace Squidlist.Storage.Catalog;

/// <summary>Local JSON catalog persistence using same-directory atomic replacement.</summary>
public sealed class JsonCatalogStore : ICatalogStore
{
    public const string DirectoryName = ".squidlist";
    public const string FileName = "catalog.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly Func<MediaRoot, CancellationToken, Task> validateRoot;

    /// <param name="validateRoot">
    /// Required platform policy that verifies the supplied root's persisted marker identity.
    /// It must throw StorageException on ownership/availability failure and honor cancellation.
    /// The store separately checks local paths. A no-op validator is not suitable for production.
    /// </param>
    public JsonCatalogStore(Func<MediaRoot, CancellationToken, Task> validateRoot)
    {
        ArgumentNullException.ThrowIfNull(validateRoot);
        this.validateRoot = validateRoot;
    }

    public Task<MediaCatalog?> LoadAsync(MediaRoot root, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(root);
        // Directory checks and durable flushes can block; keep them off the caller's UI thread.
        return Task.Run(async () =>
        {
            try
            {
                var path = await ValidatePathAsync(root, cancellationToken).ConfigureAwait(false);
                var catalog = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
                if (catalog is null)
                {
                    // A disappearing root must not be reported as an ordinary missing catalog.
                    await ValidatePathAsync(root, cancellationToken).ConfigureAwait(false);
                }
                return catalog;
            }
            catch (Exception exception) when (IsStorageFailure(exception))
            {
                throw Translate(exception);
            }
        }, cancellationToken);
    }

    public Task SaveAsync(MediaRoot root, MediaCatalog catalog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(catalog);
        return Task.Run(async () =>
        {
            string? temporaryPath = null;
            try
            {
                var path = await ValidatePathAsync(root, cancellationToken).ConfigureAwait(false);
                // Never silently replace damaged data or a schema this application cannot read.
                var previous = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
                var directory = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(directory);
                CheckDirectory(directory);
                temporaryPath = Path.Combine(directory, $"catalog.{Guid.NewGuid():N}.tmp");
                await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(stream, catalog, Options, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }

                await ValidatePathAsync(root, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                // Commit point: never check cancellation or perform fallible work after this rename.
                if (previous is null)
                {
                    File.Move(temporaryPath, path); // No overwrite if another writer created a file.
                }
                else
                {
                    File.Replace(temporaryPath, path, destinationBackupFileName: null);
                }
                temporaryPath = null;
            }
            catch (Exception exception) when (IsStorageFailure(exception))
            {
                throw Translate(exception);
            }
            finally
            {
                if (temporaryPath is not null)
                {
                    try { File.Delete(temporaryPath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }, cancellationToken);
    }

    private async Task<string> ValidatePathAsync(MediaRoot root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (root.State is not (MediaRootState.Configured or MediaRootState.Moved) || root.Path is null)
        {
            throw new StorageException(StorageFailure.RootUnavailable, "The media root is not available.");
        }
        if (!Path.IsPathFullyQualified(root.Path))
        {
            throw new ArgumentException("The media root must be absolute on the current platform.", nameof(root));
        }
        await validateRoot(root, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var rootPath = Path.GetFullPath(root.Path);
        // Reject reparse points on the entire root chain, not just the final directory.
        try
        {
            for (var current = new DirectoryInfo(rootPath); current is not null; current = current.Parent)
            {
                CheckDirectory(current.FullName);
            }
        }
        catch (IOException exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
            throw new StorageException(StorageFailure.RootUnavailable, "The media root no longer exists.", exception);
        }

        var directory = Path.Combine(rootPath, DirectoryName);
        try { CheckDirectory(directory); }
        catch (DirectoryNotFoundException) { }
        catch (FileNotFoundException) { }
        var path = Path.Combine(directory, FileName);
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            {
                throw new StorageException(StorageFailure.Conflict, "The catalog path must be a regular file.");
            }
        }
        catch (DirectoryNotFoundException) { }
        catch (FileNotFoundException) { }
        return path;
    }

    private static void CheckDirectory(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0 || (attributes & FileAttributes.Directory) == 0)
        {
            throw new StorageException(StorageFailure.Conflict, "Catalog directories must be ordinary local directories.");
        }
    }

    private static async Task<MediaCatalog?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.Read | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }

        await using (stream)
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var element = document.RootElement;
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty("SchemaVersion", out var versionElement) ||
                versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var version))
            {
                throw new JsonException("Catalog schema version is missing or invalid.");
            }
            if (version != MediaCatalog.CurrentSchemaVersion)
            {
                throw new StorageException(StorageFailure.UnsupportedVersion, $"Unsupported catalog version {version}.");
            }
            ValidateUniqueProperties(element, cancellationToken);
            try
            {
                var catalog = element.Deserialize<MediaCatalog>(Options)
                    ?? throw new JsonException("A catalog cannot be null.");
                cancellationToken.ThrowIfCancellationRequested();
                return catalog;
            }
            catch (ArgumentException exception)
            {
                throw new JsonException("Catalog model data is invalid.", exception);
            }
        }
    }

    private static void ValidateUniqueProperties(JsonElement element, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException($"Duplicate catalog property '{property.Name}'.");
                }
                ValidateUniqueProperties(property.Value, cancellationToken);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                ValidateUniqueProperties(child, cancellationToken);
            }
        }
    }

    private static bool IsStorageFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or JsonException;

    private static StorageException Translate(Exception exception) => new(
        exception switch
        {
            UnauthorizedAccessException => StorageFailure.AccessDenied,
            JsonException => StorageFailure.CorruptData,
            _ => StorageFailure.IoError
        }, "Catalog storage operation failed.", exception);
}

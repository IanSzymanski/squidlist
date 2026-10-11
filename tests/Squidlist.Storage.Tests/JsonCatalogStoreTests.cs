using System.Text.Json;
using Squidlist.Core.Media;
using Squidlist.Core.Models;
using Squidlist.Storage.Catalog;
using Squidlist.Storage.Contracts;
using Xunit;

namespace Squidlist.Storage.Tests;

public sealed class JsonCatalogStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "squidlist-catalog-tests", Guid.NewGuid().ToString("N"));
    private MediaRoot Root => MediaRoot.Configured(directory);
    private string CatalogPath => Path.Combine(directory, JsonCatalogStore.DirectoryName, JsonCatalogStore.FileName);
    private static JsonCatalogStore Store => new((_, token) => { token.ThrowIfCancellationRequested(); return Task.CompletedTask; });

    public JsonCatalogStoreTests() => Directory.CreateDirectory(directory);

    private static MediaCatalog Catalog() => new(1,
    [
        new CatalogEntry(MediaId.New(), MediaKind.Audio, MediaLocation.Relative("audio/a.mp3"),
            CatalogMediaState.Missing, 123, DateTimeOffset.Parse("2026-09-21T12:00:00Z"),
            TimeSpan.FromSeconds(42), FingerprintStatus.Ready),
        new CatalogEntry(MediaId.New(), MediaKind.Video, null, CatalogMediaState.Unresolved,
            null, null, null, FingerprintStatus.NotComputed)
    ]);

    [Fact]
    public async Task Catalog_survives_a_new_store_instance_and_replacement()
    {
        var original = Catalog();
        await Store.SaveAsync(Root, original);
        var loaded = await Store.LoadAsync(Root);
        Assert.NotNull(loaded);
        Assert.Equal(original.Entries, loaded.Entries);
        var replacement = Catalog();
        await Store.SaveAsync(Root, replacement);
        Assert.Equal(replacement.Entries, (await Store.LoadAsync(Root))!.Entries);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(CatalogPath)!, "*.tmp"));
    }

    [Fact]
    public async Task Absent_catalog_is_distinct_from_a_saved_empty_catalog()
    {
        Assert.Null(await Store.LoadAsync(Root));
        Assert.False(Directory.Exists(Path.GetDirectoryName(CatalogPath)));
        await Store.SaveAsync(Root, new MediaCatalog(1, []));
        Assert.Empty((await Store.LoadAsync(Root))!.Entries);
    }

    [Theory]
    [InlineData("{", StorageFailure.CorruptData)]
    [InlineData("null", StorageFailure.CorruptData)]
    [InlineData("{}", StorageFailure.CorruptData)]
    [InlineData("{\"SchemaVersion\":1,\"Entries\":null}", StorageFailure.CorruptData)]
    [InlineData("{\"SchemaVersion\":1,\"Entries\":[{}]}", StorageFailure.CorruptData)]
    [InlineData("{\"SchemaVersion\":1,\"Entries\":[],\"Extra\":true}", StorageFailure.CorruptData)]
    [InlineData("{\"SchemaVersion\":1,\"SchemaVersion\":1,\"Entries\":[]}", StorageFailure.CorruptData)]
    [InlineData("{\"SchemaVersion\":0,\"Entries\":[]}", StorageFailure.UnsupportedVersion)]
    [InlineData("{\"SchemaVersion\":2,\"Entries\":[]}", StorageFailure.UnsupportedVersion)]
    public async Task Invalid_existing_data_is_recoverable_and_never_overwritten(string json, StorageFailure expected)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
        await File.WriteAllTextAsync(CatalogPath, json);
        var readError = await Assert.ThrowsAsync<StorageException>(() => Store.LoadAsync(Root));
        var saveError = await Assert.ThrowsAsync<StorageException>(() => Store.SaveAsync(Root, Catalog()));
        Assert.Equal(expected, readError.Failure);
        Assert.Equal(expected, saveError.Failure);
        Assert.Equal(json, await File.ReadAllTextAsync(CatalogPath));
    }

    [Fact]
    public async Task Duplicate_media_ids_are_rejected_at_the_storage_boundary()
    {
        var entry = Catalog().Entries[0];
        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
        await File.WriteAllTextAsync(CatalogPath, JsonSerializer.Serialize(new { SchemaVersion = 1, Entries = new[] { entry, entry } }));
        var error = await Assert.ThrowsAsync<StorageException>(() => Store.LoadAsync(Root));
        Assert.Equal(StorageFailure.CorruptData, error.Failure);
    }

    [Fact]
    public async Task Pre_cancelled_operations_leave_existing_data_unchanged()
    {
        await Store.SaveAsync(Root, Catalog());
        var before = await File.ReadAllBytesAsync(CatalogPath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Store.LoadAsync(Root, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Store.SaveAsync(Root, Catalog(), cancellation.Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(CatalogPath));
    }

    [Fact]
    public async Task Cancellation_after_staging_preserves_old_catalog_and_cleans_temporary_file()
    {
        await Store.SaveAsync(Root, Catalog());
        var before = await File.ReadAllBytesAsync(CatalogPath);
        using var cancellation = new CancellationTokenSource();
        var validations = 0;
        var store = new JsonCatalogStore((_, _) =>
        {
            if (++validations == 2)
            {
                Assert.Single(Directory.GetFiles(Path.GetDirectoryName(CatalogPath)!, "*.tmp"));
                cancellation.Cancel();
            }
            return Task.CompletedTask;
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(Root, Catalog(), cancellation.Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(CatalogPath));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(CatalogPath)!, "*.tmp"));
    }

    [Fact]
    public async Task Root_ownership_is_checked_before_read_and_before_commit()
    {
        await Store.SaveAsync(Root, Catalog());
        var before = await File.ReadAllBytesAsync(CatalogPath);
        var validations = 0;
        var store = new JsonCatalogStore((_, _) =>
        {
            if (++validations == 2) throw new StorageException(StorageFailure.Conflict, "Marker changed");
            return Task.CompletedTask;
        });
        var error = await Assert.ThrowsAsync<StorageException>(() => store.SaveAsync(Root, Catalog()));
        Assert.Equal(StorageFailure.Conflict, error.Failure);
        Assert.Equal(before, await File.ReadAllBytesAsync(CatalogPath));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(CatalogPath)!, "*.tmp"));
    }

    [Fact]
    public async Task Missing_root_is_not_treated_as_an_absent_catalog_or_created_on_save()
    {
        var missing = MediaRoot.Configured(Path.Combine(directory, "missing"));
        var readError = await Assert.ThrowsAsync<StorageException>(() => Store.LoadAsync(missing));
        var writeError = await Assert.ThrowsAsync<StorageException>(() => Store.SaveAsync(missing, Catalog()));
        Assert.Equal(StorageFailure.RootUnavailable, readError.Failure);
        Assert.Equal(StorageFailure.RootUnavailable, writeError.Failure);
        Assert.False(Directory.Exists(missing.Path));
    }

    [Fact]
    public async Task Directory_at_catalog_path_is_a_conflict()
    {
        Directory.CreateDirectory(CatalogPath);
        var error = await Assert.ThrowsAsync<StorageException>(() => Store.LoadAsync(Root));
        Assert.Equal(StorageFailure.Conflict, error.Failure);
    }

    [Fact]
    public async Task Interrupted_staging_files_are_ignored_and_preserved_for_manual_cleanup()
    {
        await Store.SaveAsync(Root, Catalog());
        var orphan = Path.Combine(Path.GetDirectoryName(CatalogPath)!, "catalog.interrupted.tmp");
        await File.WriteAllTextAsync(orphan, "partial");
        Assert.NotNull(await Store.LoadAsync(Root));
        await Store.SaveAsync(Root, new MediaCatalog(1, []));
        Assert.Empty((await Store.LoadAsync(Root))!.Entries);
        Assert.Equal("partial", await File.ReadAllTextAsync(orphan));
    }

    [Fact]
    public async Task Concurrent_readers_observe_complete_snapshots()
    {
        var original = Catalog();
        var updated = Catalog();
        await Store.SaveAsync(Root, original);
        var reads = Task.Run(async () =>
        {
            for (var i = 0; i < 25; i++)
            {
                var loaded = (await Store.LoadAsync(Root))!;
                Assert.True(loaded.Entries.SequenceEqual(original.Entries) || loaded.Entries.SequenceEqual(updated.Entries));
            }
        });
        for (var i = 0; i < 10; i++) await Store.SaveAsync(Root, i % 2 == 0 ? updated : original);
        await reads;
    }

    [Fact]
    public async Task Changed_catalog_path_aborts_before_commit_and_cleans_staging_file()
    {
        await Store.SaveAsync(Root, Catalog());
        var before = await File.ReadAllBytesAsync(CatalogPath);
        var validations = 0;
        // A changed target is rejected by the last path validation on every platform.
        var preserved = Path.Combine(directory, "preserved-catalog.json");
        var store = new JsonCatalogStore((_, _) =>
        {
            if (++validations == 2)
            {
                File.Move(CatalogPath, preserved);
                Directory.CreateDirectory(CatalogPath);
            }
            return Task.CompletedTask;
        });
        var error = await Assert.ThrowsAsync<StorageException>(() => store.SaveAsync(Root, Catalog()));
        Assert.Equal(StorageFailure.Conflict, error.Failure);
        Assert.Equal(before, await File.ReadAllBytesAsync(preserved));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(CatalogPath)!, "*.tmp"));
    }

    [Fact]
    public async Task Root_marker_failure_is_not_treated_as_absent_data()
    {
        var store = new JsonCatalogStore((_, _) => throw new StorageException(StorageFailure.Conflict, "Wrong marker"));
        var error = await Assert.ThrowsAsync<StorageException>(() => store.LoadAsync(Root));
        Assert.Equal(StorageFailure.Conflict, error.Failure);
        Assert.False(Directory.Exists(Path.GetDirectoryName(CatalogPath)));
    }

    [Fact]
    public async Task Windows_sharing_violation_at_commit_keeps_the_previous_file()
    {
        if (!OperatingSystem.IsWindows()) return;
        await Store.SaveAsync(Root, Catalog());
        var before = await File.ReadAllBytesAsync(CatalogPath);
        FileStream? blocker = null;
        var validations = 0;
        var store = new JsonCatalogStore((_, _) =>
        {
            if (++validations == 2)
            {
                blocker = new FileStream(CatalogPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            return Task.CompletedTask;
        });
        try
        {
            var error = await Assert.ThrowsAsync<StorageException>(() => store.SaveAsync(Root, Catalog()));
            Assert.Equal(StorageFailure.IoError, error.Failure);
        }
        finally { blocker?.Dispose(); }
        Assert.Equal(before, await File.ReadAllBytesAsync(CatalogPath));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(CatalogPath)!, "*.tmp"));
    }

    public void Dispose()
    {
        // This fixture owns only its uniquely generated directory beneath the temp test root.
        Directory.Delete(directory, recursive: true);
    }
}

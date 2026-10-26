using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bing.Net.FTP;
using FluentFTP;
using AsyncFtpClient = Bing.Net.FTP.AsyncFtpClient;

namespace Bing.Utils.Net.Tests;

/// <summary>
/// 验证异步 FTP 目录能力的路径保护、策略映射、递归和取消行为。
/// </summary>
public sealed class FtpAsyncDirectoryTest
{
    /// <summary>
    /// 上传按本地目录树遍历，进度直传，跳过 reparse point 并映射冲突策略。
    /// </summary>
    [Fact]
    public async Task UploadWalksDirectoryAndMapsPolicies()
    {
        var root = CreateTempDirectory();
        try
        {
            var nested = Path.Combine(root, "nested");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(nested, "data.txt"), "payload", Encoding.UTF8);
            var backend = new FakeBackend();
            using var client = CreateClient(backend);
            var progress = new Progress<FtpProgress>();

            var result = await client.UploadDirectoryAsync(root, "/upload", FtpConflictPolicy.Overwrite, progress);

            Assert.Contains("/upload/nested/data.txt", result.TransferredPaths);
            Assert.Contains("/upload/nested", backend.CreatedDirectories);
            Assert.Equal(FtpRemoteExists.Overwrite, backend.LastUploadExistsMode);
            Assert.Same(progress, backend.LastProgress);

            backend.ExistingFiles.Add("/upload/nested/data.txt");
            var skipped = await client.UploadDirectoryAsync(root, "/upload", FtpConflictPolicy.Skip);
            Assert.Contains("/upload/nested/data.txt", skipped.SkippedPaths);
            await Assert.ThrowsAsync<IOException>(() =>
                client.UploadDirectoryAsync(root, "/upload", FtpConflictPolicy.Error));

            await client.UploadDirectoryAsync(root, "/upload", FtpConflictPolicy.Resume);
            Assert.Equal(FtpRemoteExists.Resume, backend.LastUploadExistsMode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// 下载拒绝越界目录项，不跟随链接，并将本地冲突策略传给后端。
    /// </summary>
    [Fact]
    public async Task DownloadRejectsTraversalSkipsLinksAndMapsLocalPolicies()
    {
        var root = CreateTempDirectory();
        try
        {
            var backend = new FakeBackend();
            backend.AddDirectory("/remote", new FtpDirectoryEntry("link", "/remote/link", FtpDirectoryEntryType.Link),
                new FtpDirectoryEntry("sub", "/remote/sub", FtpDirectoryEntryType.Directory),
                new FtpDirectoryEntry("data.txt", "/remote/data.txt", FtpDirectoryEntryType.File, 7,
                    DateTime.SpecifyKind(new DateTime(2025, 1, 2), DateTimeKind.Utc)));
            backend.AddDirectory("/remote/sub", new FtpDirectoryEntry("nested.txt", "/remote/sub/nested.txt",
                FtpDirectoryEntryType.File));
            using var client = CreateClient(backend);
            var local = Path.Combine(root, "target");
            var progress = new Progress<FtpProgress>();

            var result = await client.DownloadDirectoryAsync(local, "/remote", FtpConflictPolicy.Overwrite, progress);

            Assert.Contains("/remote/link", result.SkippedPaths);
            Assert.Equal(2, result.TransferredPaths.Count);
            Assert.True(File.Exists(Path.Combine(local, "data.txt")));
            Assert.True(File.Exists(Path.Combine(local, "sub", "nested.txt")));
            Assert.Equal((long?)7, backend.EntriesByDirectory["/remote"][2].Size);
            Assert.Equal((DateTime?)new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                backend.EntriesByDirectory["/remote"][2].ModifiedTime);
            Assert.Same(progress, backend.LastProgress);

            File.WriteAllText(Path.Combine(local, "data.txt"), "existing", Encoding.UTF8);
            await Assert.ThrowsAsync<IOException>(() => client.DownloadDirectoryAsync(local, "/remote",
                FtpConflictPolicy.Error));
            var skipped = await client.DownloadDirectoryAsync(local, "/remote", FtpConflictPolicy.Skip);
            Assert.Contains("/remote/data.txt", skipped.SkippedPaths);
            await client.DownloadDirectoryAsync(local, "/remote", FtpConflictPolicy.Resume);
            Assert.Equal(FtpLocalExists.Resume, backend.LastDownloadExistsMode);

            backend.AddDirectory("/malicious", new FtpDirectoryEntry("../escape.txt", "/malicious/../escape.txt",
                FtpDirectoryEntryType.File));
            await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadDirectoryAsync(local, "/malicious",
                FtpConflictPolicy.Overwrite));
            Assert.False(File.Exists(Path.Combine(root, "escape.txt")));

            backend.AddDirectory("/malicious-trailing-dot", new FtpDirectoryEntry(".. ",
                "/malicious-trailing-dot/.. ", FtpDirectoryEntryType.Directory));
            await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadDirectoryAsync(local,
                "/malicious-trailing-dot", FtpConflictPolicy.Overwrite));

            backend.AddDirectory("/malicious-absolute", new FtpDirectoryEntry(Path.GetPathRoot(root),
                "/malicious-absolute/absolute", FtpDirectoryEntryType.File));
            await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadDirectoryAsync(local,
                "/malicious-absolute", FtpConflictPolicy.Overwrite));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// 元数据和校验值使用可用的服务器结果，目录移动及删除经过异步后端。
    /// </summary>
    [Fact]
    public async Task MetadataChecksumAndDirectoryManagementUseBackend()
    {
        var backend = new FakeBackend();
        backend.AddDirectory("/remote", new FtpDirectoryEntry("file.txt", "/remote/file.txt",
            FtpDirectoryEntryType.File, 42, new DateTime(2025, 1, 2)));
        backend.Checksum = new FtpRemoteChecksum(FtpHashAlgorithm.SHA256, "abc123");
        backend.ExistingDirectories.Add("/source");
        using var client = CreateClient(backend);

        var entries = await ((IFtpAsyncClient)client).GetDirectoryEntriesAsync("/remote");
        Assert.Equal((long?)42, Assert.Single(entries).Size);
        Assert.Equal("abc123", (await client.GetFileChecksumAsync("/remote/file.txt", FtpHashAlgorithm.SHA256)).Value);
        Assert.True(await client.MoveDirectoryAsync("/source", "/destination", FtpConflictPolicy.Overwrite));
        Assert.Equal(FtpRemoteExists.Overwrite, backend.LastMoveExistsMode);
        await client.DeleteDirectoryAsync("/destination");
        Assert.Contains("/destination", backend.DeletedDirectories);
    }

    /// <summary>
    /// 目录枚举取消传到后端，并释放客户端 gate 以便后续操作继续。
    /// </summary>
    [Fact]
    public async Task CancellationIsForwardedAndReleasesClientGate()
    {
        var root = CreateTempDirectory();
        try
        {
            var backend = new FakeBackend();
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            backend.ListingHandler = async (_, token) =>
            {
                started.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
                return Array.Empty<FtpDirectoryEntry>();
            };
            using var client = CreateClient(backend);
            using var cancellation = new CancellationTokenSource();
            var operation = client.DownloadDirectoryAsync(Path.Combine(root, "target"), "/remote",
                cancellationToken: cancellation.Token);
            await started.Task;
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

            backend.ListingHandler = (_, _) => Task.FromResult<IReadOnlyList<FtpDirectoryEntry>>(
                Array.Empty<FtpDirectoryEntry>());
            Assert.Empty(await client.GetDirectoryEntriesAsync("/remote"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// 本地源和下载目标中的目录链接不会被遍历或写入。
    /// </summary>
    [Fact]
    public async Task LocalDirectoryLinksAreSkipped()
    {
        var root = CreateTempDirectory();
        var source = Path.Combine(root, "source");
        var realDirectory = Path.Combine(source, "real");
        var sourceLink = Path.Combine(source, "alias");
        var target = Path.Combine(root, "target");
        var outside = Path.Combine(root, "outside");
        var targetLink = Path.Combine(target, "alias");
        Directory.CreateDirectory(realDirectory);
        Directory.CreateDirectory(target);
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(realDirectory, "source.txt"), "source", Encoding.UTF8);

        try
        {
            try
            {
                Directory.CreateSymbolicLink(sourceLink, realDirectory);
                Directory.CreateSymbolicLink(targetLink, outside);
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException ||
                                              exception is PlatformNotSupportedException || exception is IOException)
            {
                return;
            }

            var backend = new FakeBackend();
            backend.AddDirectory("/remote", new FtpDirectoryEntry("alias", "/remote/alias",
                FtpDirectoryEntryType.Directory));
            backend.AddDirectory("/remote/alias", new FtpDirectoryEntry("unexpected.txt",
                "/remote/alias/unexpected.txt", FtpDirectoryEntryType.File));
            using var client = CreateClient(backend);

            var upload = await client.UploadDirectoryAsync(source, "/upload", FtpConflictPolicy.Overwrite);
            Assert.Contains("/upload/alias", upload.SkippedPaths);
            Assert.DoesNotContain("/upload/alias/source.txt", upload.TransferredPaths);

            var download = await client.DownloadDirectoryAsync(target, "/remote", FtpConflictPolicy.Overwrite);
            Assert.Contains("/remote/alias", download.SkippedPaths);
            Assert.False(File.Exists(Path.Combine(outside, "unexpected.txt")));
        }
        finally
        {
            if (Directory.Exists(sourceLink)) Directory.Delete(sourceLink);
            if (Directory.Exists(targetLink)) Directory.Delete(targetLink);
            Directory.Delete(root, recursive: true);
        }
    }
#endif

    [Fact]
    public async Task FileCountLimitStopsBeforeStartingNextFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var backend = new FakeBackend();
            backend.AddDirectory("/data", new FtpDirectoryEntry("a", "/data/a", FtpDirectoryEntryType.File, 1),
                new FtpDirectoryEntry("b", "/data/b", FtpDirectoryEntryType.File, 1));
            using var client = CreateClient(backend);
            await Assert.ThrowsAsync<InvalidDataException>(() => ((IFtpAsyncClient)client).DownloadDirectoryWithLimitsAsync(root,
                "/data", new FtpDirectoryTransferLimits { MaxFiles = 1 }));
            Assert.True(File.Exists(Path.Combine(root, "a"))); Assert.False(File.Exists(Path.Combine(root, "b")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task DepthLimitRejectsChildBeforeCreatingIt()
    {
        var root = CreateTempDirectory();
        try
        {
            var backend = new FakeBackend();
            backend.AddDirectory("/data", new FtpDirectoryEntry("child", "/data/child", FtpDirectoryEntryType.Directory));
            using var client = CreateClient(backend);
            await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadDirectoryWithLimitsAsync(root,
                "/data", new FtpDirectoryTransferLimits { MaxDepth = 0 }));
            Assert.False(Directory.Exists(Path.Combine(root, "child")));
            Directory.CreateDirectory(Path.Combine(root, "source", "child"));
            await Assert.ThrowsAsync<InvalidDataException>(() => client.UploadDirectoryWithLimitsAsync(
                Path.Combine(root, "source"), "/up", new FtpDirectoryTransferLimits { MaxDepth = 0 }));
            Assert.DoesNotContain("/up/child", backend.CreatedDirectories);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1L)]
    [InlineData(3L)]
    public async Task UnknownOrExcessiveDeclaredSizeIsRejectedBeforeDownload(long? size)
    {
        var root = CreateTempDirectory();
        try
        {
            var backend = new FakeBackend();
            backend.AddDirectory("/data", new FtpDirectoryEntry("item", "/data/item", FtpDirectoryEntryType.File, size));
            using var client = CreateClient(backend);
            await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadDirectoryWithLimitsAsync(root,
                "/data", new FtpDirectoryTransferLimits { MaxTotalFileBytes = 2 }));
            Assert.Empty(Directory.GetFiles(root));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ExactSizeBudgetAndResumeUseFullSizeWhileSkipUsesNoBytes()
    {
        var root = CreateTempDirectory();
        try
        {
            var backend = new FakeBackend();
            backend.AddDirectory("/data", new FtpDirectoryEntry("item", "/data/item", FtpDirectoryEntryType.File, 2));
            using var client = CreateClient(backend);
            var result = await client.DownloadDirectoryWithLimitsAsync(root, "/data",
                new FtpDirectoryTransferLimits { MaxFiles = 1, MaxDepth = 0, MaxTotalFileBytes = 2 });
            Assert.Single(result.TransferredPaths);
            result = await client.DownloadDirectoryWithLimitsAsync(root, "/data",
                new FtpDirectoryTransferLimits { MaxTotalFileBytes = 0 }, FtpConflictPolicy.Skip);
            Assert.Single(result.SkippedPaths);
            await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadDirectoryWithLimitsAsync(root, "/data",
                new FtpDirectoryTransferLimits { MaxTotalFileBytes = 1 }, FtpConflictPolicy.Resume));
            await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadDirectoryWithLimitsAsync(root, "/data",
                new FtpDirectoryTransferLimits { MaxFiles = 0 }, FtpConflictPolicy.Skip));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task UploadBudgetUsesLocalSizeAndCancellationDoesNotStartTransfer()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllBytes(Path.Combine(root, "file"), new byte[] { 1, 2, 3 });
            var backend = new FakeBackend(); using var client = CreateClient(backend);
            await Assert.ThrowsAsync<InvalidDataException>(() => client.UploadDirectoryWithLimitsAsync(root, "/up",
                new FtpDirectoryTransferLimits { MaxTotalFileBytes = 2 }));
            Assert.Empty(backend.ExistingFiles);
            var result = await client.UploadDirectoryWithLimitsAsync(root, "/up",
                new FtpDirectoryTransferLimits { MaxTotalFileBytes = 3, MaxFiles = 1, MaxDepth = 0 });
            Assert.Single(result.TransferredPaths);
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.UploadDirectoryWithLimitsAsync(root, "/cancel",
                new FtpDirectoryTransferLimits(), cancellationToken: cancellation.Token));
            Assert.DoesNotContain("/cancel", backend.CreatedDirectories);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.DownloadDirectoryWithLimitsAsync(root, "/data", new FtpDirectoryTransferLimits { MaxDepth = -1 }));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.DownloadDirectoryWithLimitsAsync(root, "/data", new FtpDirectoryTransferLimits { MaxFiles = -1 }));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.DownloadDirectoryWithLimitsAsync(root, "/data", new FtpDirectoryTransferLimits { MaxTotalFileBytes = -1 }));
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.DownloadDirectoryWithLimitsAsync(root, "/data", null));
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>
    /// 下载预算跨目录累计；恰好达到预算成功，超额文件在调用传输后端之前被拒绝。
    /// </summary>
    [Theory]
    [InlineData(5L, false)]
    [InlineData(6L, true)]
    public async Task DownloadByteBudgetAccumulatesAcrossDirectories(long maxBytes, bool succeeds)
    {
        var root = CreateTempDirectory();
        try
        {
            var backend = new FakeBackend();
            backend.AddDirectory("/data",
                new FtpDirectoryEntry("first", "/data/first", FtpDirectoryEntryType.File, 3),
                new FtpDirectoryEntry("child", "/data/child", FtpDirectoryEntryType.Directory));
            backend.AddDirectory("/data/child",
                new FtpDirectoryEntry("second", "/data/child/second", FtpDirectoryEntryType.File, 3));
            using var client = CreateClient(backend);
            Func<Task<FtpDirectoryTransferResult>> transfer = () => ((IFtpAsyncClient)client)
                .DownloadDirectoryWithLimitsAsync(root, "/data",
                    new FtpDirectoryTransferLimits { MaxTotalFileBytes = maxBytes });

            if (succeeds)
            {
                var result = await transfer();
                Assert.Equal(new[] { "/data/first", "/data/child/second" }, result.TransferredPaths);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidDataException>(transfer);
            }

            var expected = succeeds
                ? new[] { "/data/first", "/data/child/second" }
                : new[] { "/data/first" };
            Assert.Equal(expected, backend.DownloadedPaths);
            Assert.True(File.Exists(Path.Combine(root, "first")));
            Assert.Equal(succeeds, File.Exists(Path.Combine(root, "child", "second")));
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>
    /// 上传预算从本地文件长度累计，不能在进入子目录时重新获得完整额度。
    /// </summary>
    [Theory]
    [InlineData(5L, false)]
    [InlineData(6L, true)]
    public async Task UploadByteBudgetAccumulatesAcrossDirectories(long maxBytes, bool succeeds)
    {
        var root = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "child"));
            File.WriteAllBytes(Path.Combine(root, "first"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(root, "child", "second"), new byte[] { 4, 5, 6 });
            var backend = new FakeBackend();
            using var client = CreateClient(backend);
            Func<Task<FtpDirectoryTransferResult>> transfer = () => ((IFtpAsyncClient)client)
                .UploadDirectoryWithLimitsAsync(root, "/up",
                    new FtpDirectoryTransferLimits { MaxTotalFileBytes = maxBytes });

            if (succeeds)
            {
                var result = await transfer();
                Assert.Equal(new[] { "/up/first", "/up/child/second" }, result.TransferredPaths);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidDataException>(transfer);
            }

            var expected = succeeds
                ? new[] { "/up/first", "/up/child/second" }
                : new[] { "/up/first" };
            Assert.Equal(expected, backend.UploadedPaths);
            Assert.Contains("/up/first", backend.ExistingFiles);
            Assert.Equal(succeeds, backend.ExistingFiles.Contains("/up/child/second"));
        }
        finally { Directory.Delete(root, true); }
    }

    private static AsyncFtpClient CreateClient(FakeBackend backend) =>
        new(new AsyncFtpClientOptions { Host = "localhost" }, _ => backend);

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "BingFtpDirectory-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeBackend : IAsyncFtpBackend, IAsyncFtpDirectoryBackend
    {
        public readonly HashSet<string> ExistingFiles = new(StringComparer.Ordinal);
        public readonly HashSet<string> ExistingDirectories = new(StringComparer.Ordinal);
        public readonly List<string> CreatedDirectories = new();
        public readonly List<string> DeletedDirectories = new();
        public readonly List<string> UploadedPaths = new();
        public readonly List<string> DownloadedPaths = new();
        public readonly Dictionary<string, IReadOnlyList<FtpDirectoryEntry>> EntriesByDirectory = new(StringComparer.Ordinal);

        public bool IsConnected { get; private set; }
        public FtpRemoteExists LastUploadExistsMode { get; private set; }
        public FtpLocalExists LastDownloadExistsMode { get; private set; }
        public FtpRemoteExists LastMoveExistsMode { get; private set; }
        public IProgress<FtpProgress> LastProgress { get; private set; }
        public FtpRemoteChecksum Checksum { get; set; } = new(FtpHashAlgorithm.SHA256, "hash");
        public Func<string, CancellationToken, Task<IReadOnlyList<FtpDirectoryEntry>>> ListingHandler { get; set; } =
            (path, _) => Task.FromResult<IReadOnlyList<FtpDirectoryEntry>>(
                Array.Empty<FtpDirectoryEntry>());

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingFiles.Contains(remotePath));

        public Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingDirectories.Contains(remotePath));

        public Task<IReadOnlyList<string>> GetFilesAsync(string remoteDirectory, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<IReadOnlyList<string>> GetDirectoriesAsync(string remoteDirectory, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<bool> UploadFileAsync(string localPath, string remotePath, IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<bool> DownloadFileAsync(string localPath, string remotePath, IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<bool> UploadStreamAsync(Stream source, string remotePath, IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<bool> DownloadStreamAsync(Stream destination, string remotePath, IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken)
        {
            ExistingDirectories.Add(remotePath);
            CreatedDirectories.Add(remotePath);
            return Task.FromResult(true);
        }

        public Task<bool> MoveFileAsync(string remotePath, string destinationPath, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<FtpDirectoryEntry>> GetDirectoryEntriesAsync(string remoteDirectory,
            CancellationToken cancellationToken) => ListingHandler(remoteDirectory, cancellationToken);

        public Task<FtpStatus> UploadFileAsync(string localPath, string remotePath, FtpRemoteExists existsMode,
            IProgress<FtpProgress> progress, CancellationToken cancellationToken)
        {
            UploadedPaths.Add(remotePath);
            LastUploadExistsMode = existsMode;
            LastProgress = progress;
            if (ExistingFiles.Contains(remotePath) && existsMode == FtpRemoteExists.Skip)
                return Task.FromResult(FtpStatus.Skipped);
            ExistingFiles.Add(remotePath);
            return Task.FromResult(FtpStatus.Success);
        }

        public Task<FtpStatus> DownloadFileAsync(string localPath, string remotePath, FtpLocalExists existsMode,
            IProgress<FtpProgress> progress, CancellationToken cancellationToken)
        {
            DownloadedPaths.Add(remotePath);
            LastDownloadExistsMode = existsMode;
            LastProgress = progress;
            if (File.Exists(localPath) && existsMode == FtpLocalExists.Skip)
                return Task.FromResult(FtpStatus.Skipped);
            File.WriteAllText(localPath, "download", Encoding.UTF8);
            return Task.FromResult(FtpStatus.Success);
        }

        public Task<bool> MoveDirectoryAsync(string remotePath, string destinationPath, FtpRemoteExists existsMode,
            CancellationToken cancellationToken)
        {
            LastMoveExistsMode = existsMode;
            if (ExistingDirectories.Contains(destinationPath) && existsMode == FtpRemoteExists.Skip)
                return Task.FromResult(false);
            ExistingDirectories.Remove(remotePath);
            ExistingDirectories.Add(destinationPath);
            return Task.FromResult(true);
        }

        public Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken)
        {
            DeletedDirectories.Add(remotePath);
            ExistingDirectories.Remove(remotePath);
            return Task.CompletedTask;
        }

        public Task<FtpRemoteChecksum> GetFileChecksumAsync(string remotePath, FtpHashAlgorithm algorithm,
            CancellationToken cancellationToken) => Task.FromResult(Checksum);

        public void AddDirectory(string path, params FtpDirectoryEntry[] entries)
        {
            ExistingDirectories.Add(path);
            EntriesByDirectory[path] = entries;
            ListingHandler = (directory, _) =>
            {
                IReadOnlyList<FtpDirectoryEntry> values = EntriesByDirectory.TryGetValue(directory, out var found)
                    ? found
                    : Array.Empty<FtpDirectoryEntry>();
                return Task.FromResult(values);
            };
        }

        public void Dispose()
        {
        }
    }
}

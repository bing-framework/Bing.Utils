using System.Text;
using System.Threading;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// 断点续传侧车清理的生命周期和并发安全测试。

/// </summary>
public class HttpResumableDownloadMaintenanceTest
{
    /// <summary>
    /// 验证移除陈旧状态文件并保留锁文件。
    /// </summary>
    [Fact]
    public async Task CleanupAsync_RemovesStaleSidecarsAndKeepsLockFile()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "download.bin");
            var partialPath = target + ".binghttp.part";
            var statePath = target + ".binghttp.resume.json";
            var lockPath = target + ".binghttp.lock";
            var tempPath = statePath + ".abc.tmp";
            File.WriteAllBytes(partialPath, new byte[] { 1, 2 });
            File.WriteAllText(statePath, "{}");
            File.WriteAllText(tempPath, "{}");
            File.WriteAllText(lockPath, string.Empty, Encoding.UTF8);
            var staleAt = DateTime.UtcNow.AddMinutes(-1);
            File.SetLastWriteTimeUtc(partialPath, staleAt);
            File.SetLastWriteTimeUtc(statePath, staleAt);
            File.SetLastWriteTimeUtc(tempPath, staleAt);
            File.SetLastWriteTimeUtc(lockPath, staleAt);

            var result = await HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.FromSeconds(1));

            result.PartialFilesRemoved.ShouldBe(1);
            result.StateFilesRemoved.ShouldBe(1);
            result.TemporaryFilesRemoved.ShouldBe(1);
            result.TotalRemoved.ShouldBe(3);
            result.LockFilesRetained.ShouldBe(1);
            File.Exists(partialPath).ShouldBeFalse();
            File.Exists(statePath).ShouldBeFalse();
            File.Exists(tempPath).ShouldBeFalse();
            File.Exists(lockPath).ShouldBeTrue();
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证跳过活动的下载锁。

    /// </summary>
    [Fact]
    public async Task CleanupAsync_SkipsActiveDownloadLock()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "active.bin");
            var partialPath = target + ".binghttp.part";
            var statePath = target + ".binghttp.resume.json";
            var lockPath = target + ".binghttp.lock";
            File.WriteAllBytes(partialPath, new byte[] { 1 });
            File.WriteAllText(statePath, "{}");
            File.SetLastWriteTimeUtc(partialPath, DateTime.UtcNow.AddMinutes(-1));
            File.SetLastWriteTimeUtc(statePath, DateTime.UtcNow.AddMinutes(-1));
            using (var activeLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var result = await HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.Zero);
                result.TotalRemoved.ShouldBe(0);
                result.LockFilesRetained.ShouldBe(1);
                File.Exists(partialPath).ShouldBeTrue();
                File.Exists(statePath).ShouldBeTrue();
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证清理嵌套状态文件时默认只清理顶层目录。

    /// </summary>
    [Fact]
    public async Task CleanupRecursiveAsync_RemovesNestedSidecars_WhileDefaultCleanupStaysTopLevel()
    {
        var directory = CreateDirectory();
        try
        {
            var nested = Path.Combine(directory, "nested", "deeper");
            Directory.CreateDirectory(nested);
            var target = Path.Combine(nested, "download.bin");
            var partialPath = target + ".binghttp.part";
            var statePath = target + ".binghttp.resume.json";
            var lockPath = target + ".binghttp.lock";
            File.WriteAllBytes(partialPath, new byte[] { 1 });
            File.WriteAllText(statePath, "{}");
            File.WriteAllText(lockPath, string.Empty, Encoding.UTF8);
            var staleAt = DateTime.UtcNow.AddMinutes(-1);
            File.SetLastWriteTimeUtc(partialPath, staleAt);
            File.SetLastWriteTimeUtc(statePath, staleAt);
            File.SetLastWriteTimeUtc(lockPath, staleAt);

            var topLevel = await HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.Zero);
            topLevel.TotalRemoved.ShouldBe(0);
            File.Exists(partialPath).ShouldBeTrue();

            var recursive = await HttpResumableDownloadMaintenance.CleanupRecursiveAsync(directory, TimeSpan.Zero);

            recursive.PartialFilesRemoved.ShouldBe(1);
            recursive.StateFilesRemoved.ShouldBe(1);
            recursive.TotalRemoved.ShouldBe(2);
            recursive.LockFilesRetained.ShouldBe(1);
            File.Exists(partialPath).ShouldBeFalse();
            File.Exists(statePath).ShouldBeFalse();
            File.Exists(lockPath).ShouldBeTrue();
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证使用注入的文件系统，且删除失败不计入移除数量。

    /// </summary>
    [Fact]
    public async Task CleanupAsync_UsesInjectedFileSystemAndKeepsFailedDeleteUncounted()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "injected.bin");
            var partialPath = target + ".binghttp.part";
            File.WriteAllBytes(partialPath, new byte[] { 1 });
            File.SetLastWriteTimeUtc(partialPath, DateTime.UtcNow.AddMinutes(-1));
            var fileSystem = new TestFileSystem { FailDeletes = true };

            var result = await HttpResumableDownloadMaintenance.CleanupAsync(
                directory, TimeSpan.Zero, CancellationToken.None, fileSystem);

            result.PartialFilesRemoved.ShouldBe(0);
            File.Exists(partialPath).ShouldBeTrue();
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证枚举操作异常会向上传播。

    /// </summary>
    [Fact]
    public async Task CleanupAsync_PropagatesInjectedEnumerationFailure()
    {
        var directory = CreateDirectory();
        try
        {
            var fileSystem = new TestFileSystem { FailEnumeration = true };
            await Should.ThrowAsync<IOException>(() => HttpResumableDownloadMaintenance.CleanupAsync(
                directory, TimeSpan.Zero, CancellationToken.None, fileSystem));
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证文件存在性检查失败时向上传播异常。

    /// </summary>
    [Fact]
    public async Task CleanupAsync_PropagatesInjectedFileExistenceFailure()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "injected-existence.bin");
            var partialPath = target + ".binghttp.part";
            File.WriteAllBytes(partialPath, new byte[] { 1 });
            File.SetLastWriteTimeUtc(partialPath, DateTime.UtcNow.AddMinutes(-1));
            var fileSystem = new TestFileSystem { FailFileExists = true };

            await Should.ThrowAsync<IOException>(() => HttpResumableDownloadMaintenance.CleanupAsync(
                directory, TimeSpan.Zero, CancellationToken.None, fileSystem));
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证保留被锁定的临时文件且不计入删除失败数。

    /// </summary>
    [Fact]
    public async Task CleanupAsync_KeepsLockedTemporaryFileAndDoesNotCountFailedDelete()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "occupied.bin");
            var temporaryPath = target + ".binghttp.resume.json.abc.tmp";
            File.WriteAllText(temporaryPath, "temporary");
            File.SetLastWriteTimeUtc(temporaryPath, DateTime.UtcNow.AddMinutes(-1));
            using (var held = new FileStream(temporaryPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var result = await HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.Zero);
                result.TemporaryFilesRemoved.ShouldBe(0);
                File.Exists(temporaryPath).ShouldBeTrue();
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证取得目标锁后重新读取临时文件状态。

    /// </summary>
    [Fact]
    public async Task CleanupAsync_RereadsTemporaryFilesAfterTakingTargetLock()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "fresh.bin");
            var partialPath = target + ".binghttp.part";
            var statePath = target + ".binghttp.resume.json";
            var lockPath = target + ".binghttp.lock";
            var temporaryPath = statePath + ".abc.tmp";
            File.WriteAllBytes(partialPath, new byte[] { 1 });
            File.WriteAllText(statePath, "{}");
            File.WriteAllText(temporaryPath, "{}");
            var staleAt = DateTime.UtcNow.AddMinutes(-1);
            File.SetLastWriteTimeUtc(partialPath, staleAt);
            File.SetLastWriteTimeUtc(statePath, staleAt);
            File.SetLastWriteTimeUtc(temporaryPath, staleAt);
            using (var activeLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var result = await HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.Zero);
                result.TotalRemoved.ShouldBe(0);
            }

            File.SetLastWriteTimeUtc(temporaryPath, DateTime.UtcNow.AddMinutes(1));
            var resultAfterRelease = await HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.Zero);
            resultAfterRelease.PartialFilesRemoved.ShouldBe(1);
            resultAfterRelease.StateFilesRemoved.ShouldBe(1);
            resultAfterRelease.TemporaryFilesRemoved.ShouldBe(0);
            File.Exists(temporaryPath).ShouldBeTrue();
        }
        finally { Directory.Delete(directory, true); }
    }

    /// <summary>

    /// 验证遵循取消请求并处理目录缺失。

    /// </summary>
    [Fact]
    public async Task CleanupAsync_HonorsCancellationAndMissingDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bing-resume-missing-" + Guid.NewGuid().ToString("N"));
        var empty = await HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.Zero);
        empty.TotalRemoved.ShouldBe(0);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() =>
            HttpResumableDownloadMaintenance.CleanupAsync(directory, TimeSpan.Zero, cancellation.Token));
    }

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-resume-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>

    /// 为续传下载维护测试提供可控的文件系统实现。

    /// </summary>
    private sealed class TestFileSystem : IResumableDownloadFileSystem
    {
        /// <summary>
        /// 控制测试文件系统的删除结果。
        /// </summary>
        public bool FailDeletes { get; set; }
        /// <summary>
        /// 控制测试文件系统的枚举结果。
        /// </summary>
        public bool FailEnumeration { get; set; }
        /// <summary>
        /// 控制测试文件系统的存在性检查结果。
        /// </summary>
        public bool FailFileExists { get; set; }

        /// <inheritdoc />
        public bool DirectoryExists(string path) => Directory.Exists(path);

        /// <inheritdoc />
        public IEnumerable<string> GetDirectories(string path) => Directory.GetDirectories(path);

        /// <inheritdoc />
        public IEnumerable<string> GetFiles(string directory, string pattern)
        {
            if (FailEnumeration) throw new IOException("injected enumeration failure");
            return Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public bool FileExists(string path)
        {
            if (FailFileExists) throw new IOException("injected file existence failure");
            return File.Exists(path);
        }
        /// <inheritdoc />
        public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
        /// <inheritdoc />
        public FileStream OpenLock(string path) => new FileStream(path, FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);

        /// <inheritdoc />
        public bool Delete(string path)
        {
            if (FailDeletes) return false;
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
    }
}

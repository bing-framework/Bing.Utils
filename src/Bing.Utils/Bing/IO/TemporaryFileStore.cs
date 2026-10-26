using System.Globalization;
using System.Text;

namespace Bing.IO;

/// <summary>专用临时目录的容量统计；预留字节包含已关闭但尚未删除的文件。</summary>
public sealed class TemporaryFileStoreStatistics
{
    /// <summary>可识别的文件租约数量。</summary>
    public int FileCount { get; internal set; }
    /// <summary>已预留的正文最大字节数。</summary>
    public long ReservedBytes { get; internal set; }
    /// <summary>正文文件的逻辑长度总和，不代表文件系统实际分配的块数量。</summary>
    public long ActualBytes { get; internal set; }
}

/// <summary>临时文件清理结果。</summary>
public sealed class TemporaryFileCleanupResult
{
    internal readonly List<string> ErrorList = new();
    /// <summary>删除或预览可删除的正文数量。</summary>
    public int FileCount { get; internal set; }
    /// <summary>删除或预览可删除的正文长度。</summary>
    public long Bytes { get; internal set; }
    /// <summary>释放或预览可释放的预留额度。</summary>
    public long ReservedBytes { get; internal set; }
    /// <summary>活动、含外部文件或无法处理的条目数。</summary>
    public int SkippedFiles { get; internal set; }
    /// <summary>处理失败的路径和原因。</summary>
    public IReadOnlyList<string> Errors => ErrorList.AsReadOnly();
}

/// <summary>在显式指定的专用目录中管理有配额的临时文件。</summary>
/// <remarks>配额限制本存储预留的正文逻辑长度，不是全磁盘配额，不含元数据。
/// 同一实例的管理操作串行执行；跨实例/进程使用磁盘独占锁，锁冲突抛出 IOException，可由调用方稍后重试。
/// 所有管理者应使用相同配额。根目录及其祖先须由可信调用方管理，不得绕过租约流写入文件。</remarks>
public sealed partial class TemporaryFileStore
{
    private const string Prefix = "bing-temp-";
    private const string Marker = "BING-TEMP-1";
    private readonly object _gate = new();

    /// <summary>创建存储；目录不存在时创建。零配额只允许空文件。</summary>
    public TemporaryFileStore(string directory, long maxReservedBytes)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A directory is required.", nameof(directory));
        if (maxReservedBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxReservedBytes));
        DirectoryPath = Path.GetFullPath(directory); MaxReservedBytes = maxReservedBytes;
        Directory.CreateDirectory(DirectoryPath); RejectLink(DirectoryPath);
    }

    /// <summary>获取专用根目录完整路径。</summary>
    public string DirectoryPath { get; }
    /// <summary>获取允许的预留正文总字节数。</summary>
    public long MaxReservedBytes { get; }

    /// <summary>预留容量并创建可读写租约。默认释放租约时保留文件，直至显式过期清理。</summary>
    /// <param name="maxBytes">此文件允许的最大逻辑长度，包含边界。</param>
    /// <param name="deleteOnDispose">释放租约时删除正文和元数据；删除失败会抛出并保留可再次清理的记录。</param>
    public TemporaryFileLease CreateFile(long maxBytes, bool deleteOnDispose = false)
    {
        if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        lock (_gate)
        {
            using var rootLock = AcquireRoot();
            var statistics = ReadStatistics();
            if (maxBytes > MaxReservedBytes || statistics.ReservedBytes > MaxReservedBytes - maxBytes)
                throw new IOException("The temporary file reservation quota would be exceeded.");
            return CreateFileLocked(maxBytes, deleteOnDispose);
        }
    }

    /// <summary>
    /// 在根目录管理锁内创建正文与租约。
    /// </summary>
    private TemporaryFileLease CreateFileLocked(long maxBytes, bool deleteOnDispose)
    {
        var entry = Path.Combine(DirectoryPath, Prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(entry);
        FileStream leaseLock = null; FileStream body = null;
        try
        {
            leaseLock = new FileStream(Path.Combine(entry, "lease.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            File.WriteAllText(Path.Combine(entry, "reservation.txt"), Marker + "\n" + maxBytes.ToString(CultureInfo.InvariantCulture), Encoding.UTF8);
            var filePath = Path.Combine(entry, "data.bin");
            body = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.Asynchronous);
            return new TemporaryFileLease(this, entry, filePath, maxBytes, body, leaseLock, deleteOnDispose);
        }
        catch
        {
            body?.Dispose(); leaseLock?.Dispose();
            // 只删除本次刚创建的固定文件；失败时保留标记，供管理者后续诊断。
            foreach (var name in new[] { "data.bin", "lease.lock", "reservation.txt" })
                try { File.Delete(Path.Combine(entry, name)); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try { Directory.Delete(entry); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    /// <summary>读取磁盘预留与实际长度；无法识别的保留命名条目会报错，避免低估额度。</summary>
    public TemporaryFileStoreStatistics GetStatistics()
    {
        lock (_gate) { using var held = AcquireRoot(); return ReadStatistics(); }
    }

    /// <summary>清理最后写入时间早于 UTC 截止时间的闲置文件。默认预览；不递归删除，不处理活动租约或外部内容。</summary>
    public TemporaryFileCleanupResult CleanupExpired(DateTime olderThanUtc, bool dryRun = true)
        => CleanupExpired(olderThanUtc, dryRun, CancellationToken.None);

    /// <summary>
    /// 按条目观察取消并清理过期文件；取消不回滚已经删除的条目。
    /// </summary>
    public TemporaryFileCleanupResult CleanupExpired(DateTime olderThanUtc, bool dryRun, CancellationToken cancellationToken)
    {
        if (olderThanUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("The cutoff must be UTC.", nameof(olderThanUtc));
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            using var held = AcquireRoot();
            var result = new TemporaryFileCleanupResult();
            foreach (var entry in Entries())
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var reserved = ReadReservation(entry);
                    if (File.Exists(Path.Combine(entry, "retention.txt"))) { result.SkippedFiles++; continue; }
                    if (Directory.EnumerateFileSystemEntries(entry).Any(x => !IsOwnedName(Path.GetFileName(x)))) { result.SkippedFiles++; continue; }
                    foreach (var file in Directory.EnumerateFileSystemEntries(entry)) RejectLink(file);
                    using (var lease = OpenLease(entry))
                    {
                        var body = Path.Combine(entry, "data.bin");
                        var marker = Path.Combine(entry, "reservation.txt");
                        var updated = File.GetLastWriteTimeUtc(marker);
                        if (File.Exists(body) && File.GetLastWriteTimeUtc(body) > updated) updated = File.GetLastWriteTimeUtc(body);
                        if (updated >= olderThanUtc) continue;
                        var bytes = File.Exists(body) ? new FileInfo(body).Length : 0;
                        if (!dryRun) { DeletePendingReservation(entry); File.Delete(body); File.Delete(marker); }
                        result.FileCount++; result.Bytes += bytes; result.ReservedBytes += reserved;
                    }
                    if (!dryRun) { File.Delete(Path.Combine(entry, "lease.lock")); Directory.Delete(entry); }
                }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException)
                {
                    result.SkippedFiles++; result.ErrorList.Add(entry + ": " + ex.Message);
                }
            }
            return result;
        }
    }

    internal void DeleteLease(string entry)
    {
        lock (_gate)
        {
            using var held = AcquireRoot();
            if (!Directory.Exists(entry)) return;
            ReadReservation(entry);
            if (Directory.EnumerateFileSystemEntries(entry).Any(x => !IsOwnedName(Path.GetFileName(x))))
                throw new IOException("The lease directory contains unmanaged entries.");
            foreach (var file in Directory.EnumerateFileSystemEntries(entry)) RejectLink(file);
            using (var lease = OpenLease(entry))
            {
                DeletePendingReservation(entry);
                File.Delete(Path.Combine(entry, "data.bin")); File.Delete(Path.Combine(entry, "retention.txt")); File.Delete(Path.Combine(entry, "reservation.txt"));
            }
            File.Delete(Path.Combine(entry, "lease.lock")); Directory.Delete(entry);
        }
    }

    private TemporaryFileStoreStatistics ReadStatistics()
    {
        var result = new TemporaryFileStoreStatistics();
        foreach (var entry in Entries())
        {
            var reserved = ReadReservation(entry);
            var body = Path.Combine(entry, "data.bin");
            if (File.Exists(body)) RejectLink(body);
            result.ReservedBytes = checked(result.ReservedBytes + reserved);
            result.ActualBytes = checked(result.ActualBytes + (File.Exists(body) ? new FileInfo(body).Length : 0));
            result.FileCount++;
        }
        return result;
    }

    private IEnumerable<string> Entries() => Directory.EnumerateDirectories(DirectoryPath, Prefix + "*")
        .Where(path => Guid.TryParseExact(Path.GetFileName(path).Substring(Prefix.Length), "N", out _));
    private static bool IsOwnedName(string name) => name == "data.bin" || name == "lease.lock" || name == "reservation.txt" || name == "retention.txt" || name == "reservation.pending";

    /// <summary>
    /// 清理中断的额度更新，仅能在根锁及活动租约锁内执行。
    /// </summary>
    private static void DeletePendingReservation(string entry)
    {
        var pending = Path.Combine(entry, "reservation.pending");
        if (File.Exists(pending)) { RejectLink(pending); File.Delete(pending); }
    }
    private static long ReadReservation(string entry)
    {
        RejectLink(entry); var path = Path.Combine(entry, "reservation.txt"); RejectLink(path);
        if (new FileInfo(path).Length > 128) throw new InvalidDataException("Invalid temporary reservation marker.");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length != 2 || lines[0] != Marker || !long.TryParse(lines[1], NumberStyles.None, CultureInfo.InvariantCulture, out var bytes))
            throw new InvalidDataException("Invalid temporary reservation marker.");
        return bytes;
    }
    private FileStream AcquireRoot()
    {
        RejectLink(DirectoryPath);
        var path = Path.Combine(DirectoryPath, ".bing-temp.lock");
        if (File.Exists(path)) RejectLink(path);
        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    private static FileStream OpenLease(string entry)
    {
        var path = Path.Combine(entry, "lease.lock"); RejectLink(path);
        return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }
    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked paths are not managed: " + path);
    }
}

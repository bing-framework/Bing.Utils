using System.Globalization;
using System.Text;

namespace Bing.IO;

/// <summary>
/// 临时条目的只读磁盘快照。
/// </summary>
public sealed class TemporaryFileEntry
{
    /// <summary>
    /// 获取可传给 OpenFile 的条目标识，不是任意路径。
    /// </summary>
    public string Id { get; internal set; }
    /// <summary>
    /// 获取正文完整路径。
    /// </summary>
    public string FilePath { get; internal set; }
    /// <summary>
    /// 获取预留的逻辑字节数。
    /// </summary>
    public long ReservedBytes { get; internal set; }
    /// <summary>
    /// 获取正文逻辑长度。
    /// </summary>
    public long Length { get; internal set; }
    /// <summary>
    /// 获取正文和元数据中较新的 UTC 修改时间。
    /// </summary>
    public DateTime LastWriteTimeUtc { get; internal set; }
    /// <summary>
    /// 获取明确保留的原因；非 null 条目不会被过期清理自动删除。
    /// </summary>
    public string RetentionReason { get; internal set; }
}

public sealed partial class TemporaryFileStore
{
    /// <summary>
    /// 一次扫描和管理锁内批量预留多个文件，适合已知的临时任务清单。
    /// </summary>
    /// <remarks>额度先整体验证。取消或创建失败会关闭本批租约并尝试回滚；磁盘清理失败的条目仍计入额度，可通过清理器诊断。
    /// 不缓存跨进程配额，不改变逐次 CreateFile 的磁盘校验行为。</remarks>
    /// <returns>与输入顺序对应的活动租约，调用方逐一负责释放。</returns>
    public IReadOnlyList<TemporaryFileLease> CreateFiles(IReadOnlyList<long> maxBytes, bool deleteOnDispose = false,
        CancellationToken cancellationToken = default)
    {
        if (maxBytes == null) throw new ArgumentNullException(nameof(maxBytes));
        cancellationToken.ThrowIfCancellationRequested();
        var sizes = maxBytes.ToArray(); long requested = 0;
        foreach (var size in sizes)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            requested = checked(requested + size);
        }
        lock (_gate)
        {
            using var held = AcquireRoot();
            var reserved = ReadStatistics().ReservedBytes;
            if (requested > MaxReservedBytes || reserved > MaxReservedBytes - requested)
                throw new IOException("The temporary file reservation quota would be exceeded.");
            var result = new List<TemporaryFileLease>(sizes.Length);
            try
            {
                foreach (var size in sizes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.Add(CreateFileLocked(size, deleteOnDispose));
                }
                return result.AsReadOnly();
            }
            catch
            {
                foreach (var lease in result)
                {
                    try
                    {
                        lease.ReleaseHandles();
                        var entry = ResolveEntry(lease.Id);
                        File.Delete(Path.Combine(entry, "data.bin")); File.Delete(Path.Combine(entry, "lease.lock"));
                        File.Delete(Path.Combine(entry, "reservation.txt")); Directory.Delete(entry);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                throw;
            }
        }
    }

    /// <summary>
    /// 显式删除一个闲置条目，包括有保留保护或正文已经提交的条目；活动租约会拒绝删除。
    /// </summary>
    /// <remarks>外部文件依赖此条目的预留时，应先清理外部内容，再删除条目。</remarks>
    public void DeleteFile(string id) => DeleteLease(ResolveEntry(id));

    /// <summary>
    /// 列出可识别条目；快照不保留租约，后续 OpenFile 仍可能因占用或清理失败。
    /// </summary>
    /// <returns>按标识排序的只读条目集合。</returns>
    public IReadOnlyList<TemporaryFileEntry> ListFiles(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            using var held = AcquireRoot();
            var result = new List<TemporaryFileEntry>();
            foreach (var entry in Entries())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var reserved = ReadReservation(entry);
                var body = Path.Combine(entry, "data.bin");
                var markerTime = File.GetLastWriteTimeUtc(Path.Combine(entry, "reservation.txt"));
                if (File.Exists(body)) RejectLink(body);
                var bodyTime = File.GetLastWriteTimeUtc(body);
                result.Add(new TemporaryFileEntry { Id = Path.GetFileName(entry), FilePath = body,
                    ReservedBytes = reserved, Length = File.Exists(body) ? new FileInfo(body).Length : 0,
                    LastWriteTimeUtc = bodyTime > markerTime ? bodyTime : markerTime, RetentionReason = ReadRetention(entry) });
            }
            return result.OrderBy(x => x.Id, StringComparer.Ordinal).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// 独占重开已保留条目，正文位置从零开始；占用、链接或无效标记直接失败。
    /// </summary>
    /// <returns>调用方负责释放的租约。</returns>
    public TemporaryFileLease OpenFile(string id, bool deleteOnDispose = false)
    {
        var entry = ResolveEntry(id);
        lock (_gate)
        {
            using var held = AcquireRoot();
            var maximum = ReadReservation(entry);
            var lease = OpenLease(entry);
            FileStream body = null;
            try
            {
                var path = Path.Combine(entry, "data.bin"); RejectLink(path);
                body = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.Asynchronous);
                if (body.Length > maximum) throw new InvalidDataException("The body exceeds its reservation.");
                return new TemporaryFileLease(this, entry, path, maximum, body, lease, deleteOnDispose);
            }
            catch { body?.Dispose(); lease.Dispose(); throw; }
        }
    }

    /// <summary>
    /// 在线程池执行可取消的过期清理；取消不撤销已完成的删除。
    /// </summary>
    /// <returns>实际处理的清理结果。</returns>
    public Task<TemporaryFileCleanupResult> CleanupExpiredAsync(DateTime olderThanUtc, bool dryRun = true, CancellationToken cancellationToken = default)
        => Task.Run(() => CleanupExpired(olderThanUtc, dryRun, cancellationToken), cancellationToken);

    /// <summary>
    /// 在活动租约锁保护下更新额度；原子替换标记成功后才更新内存限制。
    /// </summary>
    internal void ResizeReservation(string entry, long bytes, Action apply)
    {
        lock (_gate)
        {
            using var held = AcquireRoot();
            var previous = ReadReservation(entry);
            var total = ReadStatistics().ReservedBytes;
            if (bytes > MaxReservedBytes || total - previous > MaxReservedBytes - bytes)
                throw new IOException("The temporary file reservation quota would be exceeded.");
            var marker = Path.Combine(entry, "reservation.txt");
            var pending = Path.Combine(entry, "reservation.pending");
            // 根锁与活动租约均已持有，遗留 pending 未提交，不改变 reservation.txt 的有效额度。
            DeletePendingReservation(entry);
            try
            {
                using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var buffer = Encoding.UTF8.GetBytes(Marker + "\n" + bytes.ToString(CultureInfo.InvariantCulture));
                    file.Write(buffer, 0, buffer.Length); file.Flush(true);
                }
                File.Replace(pending, marker, null);
                apply();
            }
            finally { if (File.Exists(pending)) File.Delete(pending); }
        }
    }

    /// <summary>
    /// 仅接受由本存储产生的单段标识。
    /// </summary>
    private string ResolveEntry(string id)
    {
        if (id == null || !id.StartsWith(Prefix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(id.Substring(Prefix.Length), "N", out _))
            throw new ArgumentException("A temporary entry ID is required.", nameof(id));
        return Path.Combine(DirectoryPath, id);
    }

    /// <summary>
    /// 读取保护条目的用途标记，拒绝链接及异常大小。
    /// </summary>
    internal static string ReadRetention(string entry)
    {
        var path = Path.Combine(entry, "retention.txt");
        if (!File.Exists(path)) return null;
        RejectLink(path);
        if (new FileInfo(path).Length > 16384) throw new InvalidDataException("Invalid retention marker.");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    /// <summary>
    /// 在管理锁内更新过期清理保护；条目仍可通过显式删除释放。
    /// </summary>
    internal void SetRetention(string entry, string reason)
    {
        if (reason != null && Encoding.UTF8.GetByteCount(reason) > 16384) throw new ArgumentOutOfRangeException(nameof(reason));
        lock (_gate)
        {
            using var held = AcquireRoot(); ReadReservation(entry);
            var path = Path.Combine(entry, "retention.txt");
            if (File.Exists(path)) RejectLink(path);
            if (reason == null) File.Delete(path);
            else File.WriteAllText(path, reason, new UTF8Encoding(false));
        }
    }
}

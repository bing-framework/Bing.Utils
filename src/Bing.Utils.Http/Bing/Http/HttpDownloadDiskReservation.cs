using System.Text;
using System.Text.Json;
using Bing.IO;

namespace Bing.Http;

/// <summary>
/// 为外部分片及合并临时文件持久化预留额度，必须在下载目标锁内使用。
/// </summary>
internal sealed class HttpDownloadDiskReservation : IDisposable
{
    private const string Suffix = ".binghttp.reservation.json";
    private readonly TemporaryFileLease _lease;
    private readonly string _path;
    private bool _released;
    private HttpDownloadDiskReservation(string path, TemporaryFileLease lease) { _path = path; _lease = lease; }
    private static string Identity(string path) => "BING-HTTP-DISK-1\n" + Path.GetFullPath(path);

    /// <summary>
    /// 获取、验证或创建持久预留；零长度文件不消耗正文配额。
    /// </summary>
    internal static async Task<HttpDownloadDiskReservation> AcquireAsync(string path, TemporaryFileStore store,
        long totalLength, CancellationToken token, long existingBytes = 0)
    {
        token.ThrowIfCancellationRequested();
        var marker = path + Suffix;
        if (store == null)
        {
            if (File.Exists(marker)) throw new InvalidOperationException("Resume this download with its original TemporaryStore.");
            return null;
        }
        var maximum = totalLength < 0 ? -1 : Math.Max(checked(totalLength * 2), existingBytes);
        TemporaryFileLease lease = null; var created = false;
        try
        {
            if (File.Exists(marker))
            {
                RejectLink(marker);
                if (new FileInfo(marker).Length > 32768) throw new InvalidDataException("Invalid disk reservation reference.");
                var reference = JsonSerializer.Deserialize<ReservationReference>(File.ReadAllText(marker, Encoding.UTF8));
                if (reference == null || reference.StorePath != store.DirectoryPath)
                    throw new InvalidDataException("The disk reservation belongs to another store.");
                lease = store.OpenFile(reference.Id);
                if (lease.RetentionReason != Identity(path)) throw new InvalidDataException("The reservation belongs to another download.");
                // 表示变短时仍保留旧额度，直到旧分片清理完成，避免清理失败造成低估。
                if (maximum > lease.MaxBytes) lease.Resize(maximum);
            }
            else
            {
                if (maximum < 0) return null;
                lease = store.CreateFile(maximum); created = true;
                lease.SetRetention(Identity(path));
                var json = JsonSerializer.Serialize(new ReservationReference { Id = lease.Id, StorePath = store.DirectoryPath });
                await AtomicFile.WriteTextAsync(marker, json, token).ConfigureAwait(false);
            }
            return new HttpDownloadDiskReservation(marker, lease);
        }
        catch
        {
            if (created && lease != null) lease.Delete(); else lease?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 只重开已有预留，不创建或改变额度。
    /// </summary>
    internal static Task<HttpDownloadDiskReservation> OpenExistingAsync(string path, TemporaryFileStore store, CancellationToken token)
        => AcquireAsync(path, store, -1, token);

    /// <summary>
    /// 在所有临时正文清理后释放额度；先删除引用，再删除租约，意外中断只会保守保留额度。
    /// </summary>
    internal void Release()
    {
        File.Delete(_path); _lease.Delete(); _released = true;
    }
    /// <inheritdoc />
    public void Dispose() { if (!_released) _lease.Dispose(); }
    private static void RejectLink(string path)
    { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked reservation files are not supported."); }
    private sealed class ReservationReference
    {
        public ReservationReference() { }
        public string Id { get; set; }
        public string StorePath { get; set; }
    }
}

using System.Text.RegularExpressions;

namespace Bing.Http;

/// <summary>独立读取的分片检查点快照；修改它不会写回磁盘。</summary>
public sealed class HttpSegmentedDownloadSnapshot
{
    /// <summary>最终目标完整路径。</summary>
    public string FilePath { get; internal set; }
    /// <summary>检查点内容；不存在时为 null。</summary>
    public HttpDownloadSegmentsState State { get; internal set; }
    /// <summary>所属分片及临时分片的实际字节总量。</summary>
    public long StagedBytes { get; internal set; }
    /// <summary>检查点最后修改的 UTC 时间；不存在时为 null。</summary>
    public DateTime? LastUpdatedUtc { get; internal set; }
}

/// <summary>检查点清理结果，不包含最终目标文件。</summary>
public sealed class HttpSegmentedDownloadCleanupResult
{
    internal readonly List<string> ErrorList = new();
    /// <summary>已删除的文件数；预览模式下表示可删除数。</summary>
    public int FileCount { get; internal set; }
    /// <summary>已删除字节数；预览模式下表示可删除字节数。</summary>
    public long Bytes { get; internal set; }
    /// <summary>因锁冲突或其他 I/O 问题而未检查的任务数，详情在 Errors。</summary>
    public int SkippedTasks { get; internal set; }
    /// <summary>未匹配本库文件规则或为链接而保留的文件数量。</summary>
    public int SkippedFiles { get; internal set; }
    /// <summary>检查或删除失败的路径及错误，不将失败伪装成删除成功。</summary>
    public IReadOnlyList<string> Errors => ErrorList.AsReadOnly();
}

/// <summary>查询、丢弃和清理分片检查点；与运行中的下载共享独占目标锁。</summary>
/// <remarks>不删除最终目标、续传锁或非本库命名文件。不跟随分片目录/文件链接；根目录须由调用方管理。
/// 过期清理只扫描指定目录的检查点文件，不递归。没有检查点的孤立任务可用 Discard 显式清理。</remarks>
public static partial class HttpSegmentedDownloadFiles
{
    private const string Suffix = ".binghttp.segments.json";
    private static readonly Regex OwnedPart = new(@"^segment-[0-9]+-[0-9]+\.part(?:\.[0-9a-f]{32}\.tmp)?$",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>读取一致的磁盘快照；活动任务或 I/O 错误抛出 IOException，不等待任务结束。</summary>
    public static HttpSegmentedDownloadSnapshot Inspect(string filePath)
    {
        var path = Normalize(filePath);
        var result = new HttpSegmentedDownloadSnapshot { FilePath = path };
        if (!Directory.Exists(Path.GetDirectoryName(path))) return result;
        using var held = Acquire(path);
        var checkpoint = path + Suffix;
        if (File.Exists(checkpoint))
        {
            RejectLink(checkpoint);
            result.State = HttpDownloadSegmentStateStore.Load(checkpoint);
            result.LastUpdatedUtc = File.GetLastWriteTimeUtc(checkpoint);
        }
        var staging = path + ".binghttp.segments";
        if (Directory.Exists(staging))
        {
            RejectLink(staging);
            foreach (var file in Directory.EnumerateFiles(staging))
                if (OwnedPart.IsMatch(Path.GetFileName(file)) && !IsLink(file)) result.StagedBytes += new FileInfo(file).Length;
        }
        return result;
    }

    /// <summary>显式删除所属检查点及分片；运行中的锁冲突正常抛出，最终目标不受影响。</summary>
    public static HttpSegmentedDownloadCleanupResult Discard(string filePath)
    {
        var path = Normalize(filePath); var result = new HttpSegmentedDownloadCleanupResult();
        if (!Directory.Exists(Path.GetDirectoryName(path))) return result;
        using var held = Acquire(path);
        Clean(path, false, result);
        return result;
    }

    /// <summary>
    /// 清理分片后释放关联临时存储额度；清理失败时保留额度，最终目标不受影响。
    /// </summary>
    public static async Task<HttpSegmentedDownloadCleanupResult> DiscardAsync(string filePath, Bing.IO.TemporaryFileStore temporaryStore,
        CancellationToken cancellationToken = default)
    {
        if (temporaryStore == null) throw new ArgumentNullException(nameof(temporaryStore));
        var path = Normalize(filePath); cancellationToken.ThrowIfCancellationRequested();
        var result = new HttpSegmentedDownloadCleanupResult();
        if (!Directory.Exists(Path.GetDirectoryName(path))) return result;
        using var held = Acquire(path);
        // 先验证额度引用，避免清理后才发现存储不匹配。零长度只用于已有额度重开，不在此阶段缩容。
        using var reservation = await HttpDownloadDiskReservation.OpenExistingAsync(path, temporaryStore, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested(); Clean(path, false, result);
        var staging = path + ".binghttp.segments";
        if (reservation != null)
            foreach (var residue in new HttpDownloadSegmentFileStore(staging, path).GetMergeResidues())
            {
                cancellationToken.ThrowIfCancellationRequested();
                RejectLink(residue);
                Delete(residue, false, result);
            }
        if (result.Errors.Count == 0 && (!Directory.Exists(staging) || !Directory.EnumerateFileSystemEntries(staging).Any()))
            reservation?.Release();
        return result;
    }

    /// <summary>检查指定目录在 UTC 截止时间之前的检查点；默认仅预览，活动任务或错误会保留并报告。</summary>
    public static HttpSegmentedDownloadCleanupResult CleanupExpired(string directory, DateTime olderThanUtc, bool dryRun = true)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A directory is required.", nameof(directory));
        if (olderThanUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("The cutoff must be UTC.", nameof(olderThanUtc));
        var root = Path.GetFullPath(directory); var result = new HttpSegmentedDownloadCleanupResult();
        if (!Directory.Exists(root)) return result;
        RejectLink(root);
        foreach (var checkpoint in Directory.EnumerateFiles(root, "*" + Suffix))
        {
            var path = checkpoint.Substring(0, checkpoint.Length - Suffix.Length);
            try
            {
                using var held = Acquire(path);
                RejectLink(checkpoint);
                if (!File.Exists(checkpoint) || File.GetLastWriteTimeUtc(checkpoint) >= olderThanUtc) continue;
                // 只清理可识别的真实清单；损坏状态由调用方 Inspect/Discard 显式处理。
                HttpDownloadSegmentStateStore.Load(checkpoint);
                Clean(path, dryRun, result);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is System.Text.Json.JsonException || ex is NotSupportedException)
            {
                result.SkippedTasks++; result.ErrorList.Add(checkpoint + ": " + ex.Message);
            }
        }
        return result;
    }

    private static void Clean(string path, bool dryRun, HttpSegmentedDownloadCleanupResult result)
    {
        var errorsBefore = result.ErrorList.Count;
        var staging = path + ".binghttp.segments";
        if (Directory.Exists(staging))
        {
            RejectLink(staging);
            foreach (var entry in Directory.EnumerateFileSystemEntries(staging))
            {
                if (Directory.Exists(entry) || !OwnedPart.IsMatch(Path.GetFileName(entry)) || IsLink(entry)) { result.SkippedFiles++; continue; }
                Delete(entry, dryRun, result);
            }
            if (!dryRun && !Directory.EnumerateFileSystemEntries(staging).Any()) Directory.Delete(staging);
        }
        var checkpoint = path + Suffix;
        if (result.ErrorList.Count == errorsBefore && File.Exists(checkpoint))
        {
            if (IsLink(checkpoint)) result.SkippedFiles++;
            else Delete(checkpoint, dryRun, result);
        }
    }

    private static void Delete(string path, bool dryRun, HttpSegmentedDownloadCleanupResult result)
    {
        try
        {
            var bytes = new FileInfo(path).Length;
            if (!dryRun) File.Delete(path);
            result.FileCount++; result.Bytes += bytes;
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException) { result.ErrorList.Add(path + ": " + ex.Message); }
    }
    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A target path is required.", nameof(path));
        return Path.GetFullPath(path);
    }
    private static FileStream Acquire(string path)
    {
        var name = path + ".binghttp.lock";
        if (File.Exists(name)) RejectLink(name);
        return new FileStream(name, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private static void RejectLink(string path) { if (IsLink(path)) throw new IOException("Linked paths are not managed: " + path); }
}

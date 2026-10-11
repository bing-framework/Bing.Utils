using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bing.Http;

/// <summary>

/// 一个 HTTP 分片及其本地完成状态。

/// </summary>
public sealed class HttpDownloadSegmentState
{
    /// <summary>
    /// 分片起始偏移。
    /// </summary>
    public long Start { get; set; }

    /// <summary>

    /// 分片包含式结束偏移。

    /// </summary>
    public long End { get; set; }

    /// <summary>

    /// 当前已写入的字节数。

    /// </summary>
    public long BytesWritten { get; set; }

    /// <summary>

    /// 分片是否已完整写入。

    /// </summary>
    public bool Completed { get; set; }

    /// <summary>

    /// 可选的分片 SHA-256 十六进制摘要。

    /// </summary>
    public string Sha256 { get; set; }

    /// <summary>

    /// 获取此状态对应的范围。

    /// </summary>
    [JsonIgnore]
    public HttpDownloadRange Range => new HttpDownloadRange(Start, End);
}

/// <summary>

/// 可恢复的 HTTP 分片下载状态。

/// </summary>
public sealed class HttpDownloadSegmentsState
{
    /// <summary>
    /// 当前状态格式版本。
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>

    /// 正文总长度。

    /// </summary>
    public long TotalLength { get; set; }

    /// <summary>

    /// 用于确认正文身份的可选验证器。

    /// </summary>
    public string Validator { get; set; }

    /// <summary>

    /// 服务端响应的内容编码。

    /// </summary>
    public string ContentEncoding { get; set; }

    /// <summary>

    /// 正文的完整分片状态。

    /// </summary>
    public List<HttpDownloadSegmentState> Segments { get; set; }
}

/// <summary>

/// 读写和验证 HTTP 分片下载状态文件。

/// </summary>
public static class HttpDownloadSegmentStateStore
{
    /// <summary>
    /// 当前状态格式版本。
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>

    /// 从 JSON 文件读取并验证分片状态。

    /// </summary>
    /// <param name="path">状态文件路径。</param>
    /// <returns>已验证的分片状态。</returns>
    public static HttpDownloadSegmentsState Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A state path is required.", nameof(path));
        using (var stream = File.OpenRead(path))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            var state = JsonSerializer.Deserialize<HttpDownloadSegmentsState>(reader.ReadToEnd());
            Validate(state);
            return state;
        }
    }

    /// <summary>

    /// 验证分片状态的版本、范围、覆盖关系和校验字段。

    /// </summary>
    /// <param name="state">待验证的状态。</param>
    public static void Validate(HttpDownloadSegmentsState state)
    {
        if (state == null) throw new InvalidDataException("The segment state is missing.");
        if (state.SchemaVersion > CurrentSchemaVersion)
            throw new NotSupportedException("The segment state version is newer than this library supports.");
        if (state.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException("The segment state version is invalid.");
        if (state.TotalLength < 0) throw new InvalidDataException("The total length is invalid.");
        if (state.Segments == null) throw new InvalidDataException("The segment list is missing.");

        if (state.TotalLength == 0)
        {
            if (state.Segments.Count != 0) throw new InvalidDataException("An empty body cannot have segments.");
            return;
        }

        long expectedStart = 0;
        foreach (var segment in state.Segments)
        {
            if (segment == null) throw new InvalidDataException("A segment entry is missing.");
            if (segment.Start != expectedStart || segment.End < segment.Start || segment.End >= state.TotalLength)
                throw new InvalidDataException("Segment ranges must be contiguous and within the body.");
            var length = segment.End - segment.Start + 1;
            if (segment.BytesWritten < 0 || segment.BytesWritten > length ||
                (segment.Completed && segment.BytesWritten != length))
                throw new InvalidDataException("Segment completion length is invalid.");
            ValidateSha256(segment.Sha256);
            expectedStart = segment.End + 1;
        }

        if (expectedStart != state.TotalLength)
            throw new InvalidDataException("Segment ranges do not cover the complete body.");
    }

    /// <summary>

    /// 验证状态中的范围与请求的分片大小完全一致。

    /// </summary>
    /// <param name="state">待验证的分片状态。</param>
    /// <param name="segmentSize">请求的最大分片大小。</param>
    internal static void ValidatePlan(HttpDownloadSegmentsState state, long segmentSize)
    {
        if (segmentSize <= 0) throw new ArgumentOutOfRangeException(nameof(segmentSize));
        Validate(state);
        var expected = HttpDownloadRangePlanner.Create(state.TotalLength, segmentSize);
        if (!expected.SequenceEqual(state.Segments.Select(segment => segment.Range)))
            throw new InvalidDataException("The segment state does not match the requested segment size.");
    }

    /// <summary>

    /// 验证后以临时文件原子替换状态文件。

    /// </summary>
    /// <param name="path">目标状态文件路径。</param>
    /// <param name="state">要保存的状态。</param>
    /// <param name="cancellationToken">取消序列化或写入。</param>
    public static async Task SaveAsync(string path, HttpDownloadSegmentsState state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A state path is required.", nameof(path));
        Validate(state);
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory)) throw new ArgumentException("A state directory is required.", nameof(path));
        Directory.CreateDirectory(directory);
        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(state);
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 4096, FileOptions.SequentialScan))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                stream.Flush(true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(fullPath)) File.Replace(tempPath, fullPath, null);
            else File.Move(tempPath, fullPath);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch { }
        }
    }

    /// <summary>

    /// 校验 SHA-256 值的格式。

    /// </summary>
    private static void ValidateSha256(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException("The segment SHA-256 value is invalid.");
    }
}

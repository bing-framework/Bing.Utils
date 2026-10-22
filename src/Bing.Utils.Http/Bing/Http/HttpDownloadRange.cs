using System.Net.Http;
using System.Net.Http.Headers;

namespace Bing.Http;

/// <summary>
/// 表示一个包含首尾位置的 HTTP 字节范围。
/// </summary>
public readonly struct HttpDownloadRange : IEquatable<HttpDownloadRange>
{
    /// <summary>
    /// 初始化一个 <see cref="HttpDownloadRange"/> 类型的实例。
    /// </summary>
    /// <param name="start">起始偏移。</param>
    /// <param name="end">包含的结束偏移。</param>
    public HttpDownloadRange(long start, long end)
    {
        if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end));
        Start = start;
        End = end;
    }

    /// <summary>
    /// 获取范围的起始偏移。
    /// </summary>
    public long Start { get; }

    /// <summary>
    /// 获取范围的包含式结束偏移。
    /// </summary>
    public long End { get; }

    /// <summary>
    /// 获取范围包含的字节数。
    /// </summary>
    public long Length => End - Start + 1;

    /// <summary>
    /// 生成 Range 请求头的值。
    /// </summary>
    public string ToHeaderValue() => "bytes=" + Start + "-" + End;

    /// <inheritdoc />
    public bool Equals(HttpDownloadRange other) => Start == other.Start && End == other.End;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is HttpDownloadRange && Equals((HttpDownloadRange)obj);

    /// <inheritdoc />
    public override int GetHashCode() => Start.GetHashCode() ^ End.GetHashCode();

    /// <summary>
    /// 比较两个字节范围。
    /// </summary>
    public static bool operator ==(HttpDownloadRange left, HttpDownloadRange right) => left.Equals(right);

    /// <summary>
    /// 比较两个字节范围是否不同。
    /// </summary>
    public static bool operator !=(HttpDownloadRange left, HttpDownloadRange right) => !left.Equals(right);
}

/// <summary>
/// 创建和验证显式选择的 HTTP 分片范围。
/// </summary>
public static class HttpDownloadRangePlanner
{
    /// <summary>
    /// 按固定大小创建覆盖整个正文且不重叠的字节范围。
    /// </summary>
    /// <param name="totalLength">服务端报告的正文总长度。</param>
    /// <param name="segmentSize">每个分片的最大字节数。</param>
    /// <returns>按起始偏移排序的范围列表；零长度正文返回空列表。</returns>
    public static IReadOnlyList<HttpDownloadRange> Create(long totalLength, long segmentSize)
    {
        if (totalLength < 0) throw new ArgumentOutOfRangeException(nameof(totalLength));
        if (segmentSize <= 0) throw new ArgumentOutOfRangeException(nameof(segmentSize));
        if (totalLength == 0) return new List<HttpDownloadRange>();

        var count = (totalLength - 1) / segmentSize + 1;
        if (count > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(segmentSize), "The range count is too large.");

        var ranges = new List<HttpDownloadRange>((int)count);
        for (var start = 0L; start < totalLength;)
        {
            var remaining = totalLength - start;
            var length = Math.Min(segmentSize, remaining);
            var range = new HttpDownloadRange(start, start + length - 1);
            ranges.Add(range);
            start += length;
        }
        return ranges;
    }

    /// <summary>
    /// 严格验证一个分片响应是否匹配预期范围和正文总长度。
    /// </summary>
    /// <param name="expected">请求的范围。</param>
    /// <param name="contentRange">响应的 Content-Range 值。</param>
    /// <param name="totalLength">预期正文总长度。</param>
    /// <exception cref="HttpRequestException">响应缺少范围、范围不匹配或总长度不匹配时抛出。</exception>
    public static void ValidateResponse(HttpDownloadRange expected,
        ContentRangeHeaderValue contentRange, long totalLength)
    {
        if (totalLength <= 0) throw new ArgumentOutOfRangeException(nameof(totalLength));
        if (contentRange == null || !contentRange.From.HasValue || !contentRange.To.HasValue ||
            !string.Equals(contentRange.Unit, "bytes", StringComparison.OrdinalIgnoreCase) ||
            !contentRange.Length.HasValue || contentRange.From.Value != expected.Start ||
            contentRange.To.Value != expected.End || contentRange.Length.Value != totalLength)
        {
            throw new HttpRequestException("The Content-Range response does not match the requested download range.");
        }
    }
}

using System.Net;
using System.Net.Http.Headers;

namespace Bing.Http;

/// <summary>
/// HTTP 分片能力探测的结果状态。
/// </summary>
public enum HttpDownloadRangeSupportStatus
{
    /// <summary>
    /// 响应明确支持请求的字节范围。
    /// </summary>
    Supported,

    /// <summary>
    /// 服务器没有提供范围能力或拒绝了范围请求。
    /// </summary>
    Unsupported,

    /// <summary>
    /// 服务器响应违反了范围协议。
    /// </summary>
    Invalid
}

/// <summary>
/// 与具体 HTTP 客户端无关的响应元数据。
/// </summary>
public sealed class HttpDownloadRangeResponseMetadata
{
    /// <summary>
    /// 获取或设置响应状态码。
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// 获取或设置 Accept-Ranges 头值。
    /// </summary>
    public string AcceptRanges { get; set; }

    /// <summary>
    /// 获取或设置 Content-Length 值。
    /// </summary>
    public long? ContentLength { get; set; }

    /// <summary>
    /// 获取或设置解析后的 Content-Range 值。
    /// </summary>
    public ContentRangeHeaderValue ContentRange { get; set; }

    /// <summary>
    /// 获取或设置响应的 ETag。
    /// </summary>
    public string ETag { get; set; }

    /// <summary>
    /// 获取或设置响应的 Last-Modified HTTP 日期。
    /// </summary>
    public string LastModified { get; set; }

    /// <summary>
    /// 获取或设置规范化后的 Content-Encoding 值。
    /// </summary>
    public string ContentEncoding { get; set; }

    /// <summary>
    /// 获取或设置响应对应的 HTTP 请求身份。
    /// </summary>
    public string RequestIdentity { get; set; }

    /// <summary>
    /// 获取或设置响应对应的 HTTP 请求头指纹。
    /// </summary>
    public string RequestFingerprint { get; set; }
}

/// <summary>
/// 描述 HTTP 字节范围能力探测结果。
/// </summary>
public sealed class HttpDownloadRangeProbeResult
{
    /// <summary>
    /// 初始化一个 <see cref="HttpDownloadRangeProbeResult"/> 类型的实例。
    /// </summary>
    internal HttpDownloadRangeProbeResult(HttpDownloadRangeSupportStatus status, long? totalLength, string reason)
    {
        Status = status;
        TotalLength = totalLength;
        Reason = reason;
    }

    /// <summary>
    /// 获取探测状态。
    /// </summary>
    public HttpDownloadRangeSupportStatus Status { get; }

    /// <summary>
    /// 获取响应报告的正文总长度。
    /// </summary>
    public long? TotalLength { get; }

    /// <summary>
    /// 获取状态说明。
    /// </summary>
    public string Reason { get; }
}

/// <summary>
/// 探测和适配 HTTP 字节范围响应。
/// </summary>
public static class HttpDownloadRangeProbe
{
    /// <summary>
    /// 根据响应元数据判断初始响应或范围响应的能力。
    /// </summary>
    /// <param name="metadata">与客户端无关的响应元数据。</param>
    /// <param name="expectedRange">范围请求；为空表示初始能力探测。</param>
    /// <param name="expectedTotalLength">此前已确认的正文总长度；为空表示不比较历史长度。</param>
    /// <returns>支持、拒绝或协议无效结果。</returns>
    public static HttpDownloadRangeProbeResult Probe(HttpDownloadRangeResponseMetadata metadata,
        HttpDownloadRange? expectedRange = null, long? expectedTotalLength = null)
    {
        if (metadata == null) return Invalid("Response metadata is missing.");
        var totalLength = metadata.ContentRange?.Length ?? metadata.ContentLength;

        if (!expectedRange.HasValue)
        {
            if (metadata.StatusCode != HttpStatusCode.OK)
                return Invalid("An initial range probe requires a 200 response.");
            if (!totalLength.HasValue || totalLength.Value < 0)
                return Unsupported("The response did not provide a content length.");
            return IsBytes(metadata.AcceptRanges)
                ? Supported(totalLength, "The server advertises byte ranges.")
                : Unsupported("The server did not advertise byte ranges.");
        }

        if (metadata.StatusCode == HttpStatusCode.OK)
            return Unsupported("The server returned a full response instead of the requested range.");
        if (metadata.StatusCode != HttpStatusCode.PartialContent)
            return Invalid("A range response must use status 206.");
        if (metadata.ContentRange == null)
            return Invalid("A 206 response must include Content-Range.");
        if (!string.IsNullOrEmpty(metadata.AcceptRanges) && !IsBytes(metadata.AcceptRanges))
            return Invalid("A 206 response cannot advertise a non-byte range unit.");

        try
        {
            var contentRange = metadata.ContentRange;
            var length = contentRange.Length;
            if (!length.HasValue) return Invalid("Content-Range must include the total length.");
            if (expectedTotalLength.HasValue && length.Value != expectedTotalLength.Value)
                return Invalid("The Content-Range total length changed during the download.");
            HttpDownloadRangePlanner.ValidateResponse(expectedRange.Value, contentRange, length.Value);
            if (metadata.ContentLength.HasValue && metadata.ContentLength.Value != expectedRange.Value.Length)
                return Invalid("Content-Length does not match the requested range length.");
            return Supported(length, "The response matches the requested byte range.");
        }
        catch (Exception error) when (error is HttpRequestException || error is ArgumentOutOfRangeException)
        {
            return Invalid(error.Message);
        }
    }

    /// <summary>
    /// 通过调用方提供的响应读取委托执行可取消的能力探测。
    /// </summary>
    /// <param name="readMetadata">读取响应元数据的委托。</param>
    /// <param name="expectedRange">范围请求；为空表示初始能力探测。</param>
    /// <param name="expectedTotalLength">此前已确认的正文总长度。</param>
    /// <param name="cancellationToken">取消元数据读取。</param>
    public static async Task<HttpDownloadRangeProbeResult> ProbeAsync(
        Func<CancellationToken, Task<HttpDownloadRangeResponseMetadata>> readMetadata,
        HttpDownloadRange? expectedRange = null, long? expectedTotalLength = null,
        CancellationToken cancellationToken = default)
    {
        if (readMetadata == null) throw new ArgumentNullException(nameof(readMetadata));
        var metadata = await readMetadata(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return Probe(metadata, expectedRange, expectedTotalLength);
    }

    /// <summary>
    /// 判断 Content-Range 单位是否为 bytes。
    /// </summary>
    private static bool IsBytes(string value)
        => string.Equals(value?.Trim(), "bytes", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 创建表示服务器支持范围请求的探测结果。
    /// </summary>
    private static HttpDownloadRangeProbeResult Supported(long? totalLength, string reason)
        => new HttpDownloadRangeProbeResult(HttpDownloadRangeSupportStatus.Supported, totalLength, reason);

    /// <summary>
    /// 创建表示服务器不支持范围请求的探测结果。
    /// </summary>
    private static HttpDownloadRangeProbeResult Unsupported(string reason)
        => new HttpDownloadRangeProbeResult(HttpDownloadRangeSupportStatus.Unsupported, null, reason);

    /// <summary>
    /// 创建表示范围探测响应无效的结果。
    /// </summary>
    private static HttpDownloadRangeProbeResult Invalid(string reason)
        => new HttpDownloadRangeProbeResult(HttpDownloadRangeSupportStatus.Invalid, null, reason);
}

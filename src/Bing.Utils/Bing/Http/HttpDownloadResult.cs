using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Bing.Http;

/// <summary>

/// 流式下载结束后可独立使用的响应元数据。

/// </summary>
public sealed class HttpDownloadResult
{
    /// <summary>
    /// 初始化一个<see cref="HttpDownloadResult"/>类型的实例。
    /// </summary>
    /// <param name="statusCode">响应状态码。</param>
    /// <param name="headers">响应头；为空时使用空快照。</param>
    /// <param name="contentHeaders">内容头；为空时使用空快照。</param>
    /// <param name="bytesWritten">实际写入目标的非负字节数。</param>
    /// <param name="isFileSaved">文件是否已经成功保存。</param>
    public HttpDownloadResult(HttpStatusCode statusCode, IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        IReadOnlyDictionary<string, IReadOnlyList<string>> contentHeaders, long bytesWritten, bool isFileSaved)
    {
        if (bytesWritten < 0) throw new ArgumentOutOfRangeException(nameof(bytesWritten));
        StatusCode = statusCode;
        Headers = Copy(headers);
        ContentHeaders = Copy(contentHeaders);
        BytesWritten = bytesWritten;
        IsFileSaved = isFileSaved;
    }

    /// <summary>

    /// 初始化一个<see cref="HttpDownloadResult"/>类型的实例。

    /// </summary>
    /// <param name="response">用于创建结果快照的响应消息。</param>
    /// <param name="bytesWritten">实际写入目标的字节数。</param>
    /// <param name="isFileSaved">文件是否已经成功保存。</param>
    internal HttpDownloadResult(HttpResponseMessage response, long bytesWritten, bool isFileSaved)
    {
        StatusCode = response.StatusCode;
        Headers = Copy(response.Headers);
        ContentHeaders = Copy(response.Content?.Headers);
        BytesWritten = bytesWritten;
        IsFileSaved = isFileSaved;
    }

    /// <summary>

    /// 响应状态码。

    /// </summary>
    public HttpStatusCode StatusCode { get; }
    /// <summary>
    /// 响应状态码是否为 2xx。
    /// </summary>
    public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
    /// <summary>
    /// 响应状态码是否为 304（未修改）。
    /// </summary>
    public bool IsNotModified => StatusCode == HttpStatusCode.NotModified;
    /// <summary>
    /// 响应头快照。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }
    /// <summary>
    /// 内容头快照。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ContentHeaders { get; }
    /// <summary>
    /// 实际写入的字节数。
    /// </summary>
    /// <remarks>续传失败时包含仍保留的临时片段长度。</remarks>
    public long BytesWritten { get; }
    /// <summary>
    /// 文件是否已成功保存。
    /// </summary>
    /// <remarks>下载到流时始终为 <see langword="false"/>。</remarks>
    public bool IsFileSaved { get; }

    /// <summary>

    /// 返回标记为已保存文件的下载结果副本。

    /// </summary>
    /// <returns>文件保存状态为 <see langword="true"/> 的结果副本。</returns>
    internal HttpDownloadResult WithFileSaved() => new HttpDownloadResult(StatusCode, Headers, ContentHeaders, BytesWritten);

    /// <summary>

    /// 初始化一个<see cref="HttpDownloadResult"/>类型的实例。

    /// </summary>
    /// <param name="statusCode">响应状态码。</param>
    /// <param name="headers">已复制的响应头快照。</param>
    /// <param name="contentHeaders">已复制的内容头快照。</param>
    /// <param name="bytesWritten">实际写入目标的字节数。</param>
    private HttpDownloadResult(HttpStatusCode statusCode, IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        IReadOnlyDictionary<string, IReadOnlyList<string>> contentHeaders, long bytesWritten)
    {
        StatusCode = statusCode;
        Headers = headers;
        ContentHeaders = contentHeaders;
        BytesWritten = bytesWritten;
        IsFileSaved = true;
    }

    /// <summary>

    /// 复制 HTTP 头集合为独立只读快照。

    /// </summary>
    /// <param name="source">待复制的头集合；可以为空。</param>
    /// <returns>名称不区分大小写且保留多值的快照。</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Copy(HttpHeaders source)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (source != null)
            foreach (var item in source)
                result[item.Key] = Array.AsReadOnly(item.Value.ToArray());
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(result);
    }

    /// <summary>

    /// 复制头字典及其多值集合为独立只读快照。

    /// </summary>
    /// <param name="source">待复制的头字典；可以为空。</param>
    /// <returns>名称不区分大小写且保留多值的快照。</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Copy(IReadOnlyDictionary<string, IReadOnlyList<string>> source)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (source != null)
            foreach (var item in source)
                result[item.Key] = Array.AsReadOnly(item.Value.ToArray());
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(result);
    }
}

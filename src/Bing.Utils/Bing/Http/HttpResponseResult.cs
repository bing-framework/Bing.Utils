using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Bing.Http;

/// <summary>

/// 保存 HTTP 响应状态、头信息及转换结果，供请求结束后继续读取。

/// </summary>
/// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
/// <remarks>头信息和多值集合均为独立只读快照，不依赖已释放的响应对象。</remarks>
public sealed class HttpResponseResult<TResult> where TResult : class
{
    /// <summary>
    /// 初始化一个<see cref="HttpResponseResult{TResult}"/>类型的实例。
    /// </summary>
    /// <param name="statusCode">HTTP 响应状态码。</param>
    /// <param name="headers">响应头；构造时会复制为大小写不敏感的只读快照。</param>
    /// <param name="contentHeaders">正文头；构造时会复制为大小写不敏感的只读快照。</param>
    /// <param name="data">转换后的成功响应数据；可为 <see langword="null"/>。</param>
    /// <param name="rawContent">按需保留的解码正文；未请求保留时为 <see langword="null"/>。</param>
    public HttpResponseResult(HttpStatusCode statusCode, IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        IReadOnlyDictionary<string, IReadOnlyList<string>> contentHeaders, TResult data, string rawContent)
    {
        StatusCode = statusCode;
        Headers = Copy(headers);
        ContentHeaders = Copy(contentHeaders);
        Data = data;
        RawContent = rawContent;
    }

    /// <summary>

    /// 初始化一个<see cref="HttpResponseResult{TResult}"/>类型的实例。

    /// </summary>
    /// <param name="response">尚未释放的响应消息。</param>
    /// <param name="data">转换后的成功响应数据。</param>
    /// <param name="rawContent">按需保留的解码正文。</param>
    internal HttpResponseResult(HttpResponseMessage response, TResult data, string rawContent)
    {
        StatusCode = response.StatusCode;
        Headers = Copy(response.Headers);
        ContentHeaders = Copy(response.Content?.Headers);
        Data = data;
        RawContent = rawContent;
    }

    /// <summary>

    /// 获取 HTTP 响应状态码。

    /// </summary>
    public HttpStatusCode StatusCode { get; }
    /// <summary>
    /// 获取状态码是否位于成功范围（200 至 299）。
    /// </summary>
    public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
    /// <summary>
    /// 获取响应是否为 304 Not Modified。
    /// </summary>
    public bool IsNotModified => StatusCode == HttpStatusCode.NotModified;
    /// <summary>
    /// 获取响应头快照。
    /// </summary>
    /// <remarks>头名称比较不区分大小写，值集合保留多值。</remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }
    /// <summary>
    /// 获取正文头快照。
    /// </summary>
    /// <remarks>头名称比较不区分大小写，值集合保留多值。</remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ContentHeaders { get; }
    /// <summary>
    /// 获取成功响应转换后的数据；响应非成功或正文为空时为 <see langword="null"/>。
    /// </summary>
    public TResult Data { get; }
    /// <summary>
    /// 获取解码后的正文文本。
    /// </summary>
    /// <remarks>仅在请求保留正文时提供；未启用时为 <see langword="null"/>，空正文返回 <see cref="string.Empty"/>。</remarks>
    public string RawContent { get; }

    /// <summary>

    /// 复制头字典及其多值集合为独立只读快照。

    /// </summary>
    /// <param name="source">待复制的头字典。</param>
    /// <returns>名称不区分大小写且保留多值的快照。</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Copy(IReadOnlyDictionary<string, IReadOnlyList<string>> source)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
            result[item.Key] = Array.AsReadOnly(item.Value.ToArray());
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(result);
    }

    /// <summary>

    /// 将响应消息的头信息复制为独立只读快照。

    /// </summary>
    /// <param name="source">尚未释放的响应头；可为空。</param>
    /// <returns>名称不区分大小写且保留多值的头信息快照。</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Copy(HttpHeaders source)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (source != null)
            foreach (var item in source)
                result[item.Key] = Array.AsReadOnly(item.Value.ToArray());
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(result);
    }
}

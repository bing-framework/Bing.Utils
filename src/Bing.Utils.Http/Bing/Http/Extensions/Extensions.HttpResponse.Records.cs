using System.Text.Json;
using Bing.Serialization.SystemTextJson;
using Microsoft.AspNetCore.Http;

namespace Bing.Http;

public static partial class HttpResponseExtensions
{
    /// <summary>
    /// 将异步序列逐项写为 UTF-8 JSON 数组，保留响应体；失败可能已输出部分内容。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="response">HTTP 响应。</param>
    /// <param name="source">逐步枚举的数据源。</param>
    /// <param name="options">JSON 选项，null 使用系统默认语义。</param>
    /// <param name="flushEveryItems">按条目刷新间隔，零使用内部缓冲策略。</param>
    /// <param name="cancellationToken">调用方取消，同时遵守 RequestAborted。</param>
    public static Task WriteJsonArrayStreamAsync<T>(this HttpResponse response, IAsyncEnumerable<T> source,
        JsonSerializerOptions options = null, int flushEveryItems = 0, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (flushEveryItems < 0) throw new ArgumentOutOfRangeException(nameof(flushEveryItems));
        return WriteRecordsResponseAsync(response, "application/json; charset=utf-8",
            token => JsonStream.WriteArrayAsync(response.Body, source, options, flushEveryItems, true, token), cancellationToken);
    }

    /// <summary>
    /// 将异步序列写为 UTF-8 NDJSON，支持单记录限额，保留响应体。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="response">HTTP 响应。</param>
    /// <param name="source">异步记录源。</param>
    /// <param name="recordOptions">记录字节预算和刷新策略。</param>
    /// <param name="jsonOptions">JSON 选项，不能启用缩进。</param>
    /// <param name="cancellationToken">调用方取消，同时遵守 RequestAborted。</param>
    public static Task WriteNdjsonStreamAsync<T>(this HttpResponse response, IAsyncEnumerable<T> source,
        NdjsonOptions recordOptions = null, JsonSerializerOptions jsonOptions = null, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return WriteRecordsResponseAsync(response, "application/x-ndjson; charset=utf-8",
            token => JsonStream.WriteNdjsonAsync(response.Body, source, recordOptions, jsonOptions, true, token), cancellationToken);
    }

    /// <summary>
    /// 组合请求中止和显式取消，不关闭调用方响应流。
    /// </summary>
    private static async Task WriteRecordsResponseAsync(HttpResponse response, string contentType,
        Func<CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        if (response == null) throw new ArgumentNullException(nameof(response));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, response.HttpContext.RequestAborted);
        linked.Token.ThrowIfCancellationRequested();
        response.ContentType = contentType;
        await write(linked.Token).ConfigureAwait(false);
    }
}

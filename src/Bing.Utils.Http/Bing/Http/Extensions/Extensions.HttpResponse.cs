using System.Text;
using System.Text.Json;
using Bing.Utils.Json;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Bing.Http;

/// <summary>
/// Http响应(<see cref="HttpResponse"/>) 扩展
/// </summary>
public static partial class HttpResponseExtensions
{
    /// <summary>
    /// 将对象以 UTF-8 JSON 格式直接序列化到响应正文流。
    /// </summary>
    /// <typeparam name="T">待序列化对象的类型。</typeparam>
    /// <param name="response">HTTP 响应。</param>
    /// <param name="value">待序列化对象。</param>
    /// <param name="options">JSON 序列化选项；为空时使用默认选项。</param>
    /// <param name="cancellationToken">调用方提供的取消令牌；同时遵守请求的 RequestAborted 令牌。</param>
    /// <returns>表示异步序列化操作的任务。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> 为 null。</exception>
    /// <exception cref="OperationCanceledException">请求或调用方取消了序列化。</exception>
    public static async Task WriteJsonStreamAsync<T>(this HttpResponse response, T value,
        JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
    {
        if (response == null) throw new ArgumentNullException(nameof(response));

        var requestAborted = response.HttpContext?.RequestAborted ?? CancellationToken.None;
        CancellationTokenSource linkedCancellation = null;
        var effectiveCancellationToken = cancellationToken;
        if (cancellationToken.CanBeCanceled && requestAborted.CanBeCanceled)
        {
            linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, requestAborted);
            effectiveCancellationToken = linkedCancellation.Token;
        }
        else if (!effectiveCancellationToken.CanBeCanceled)
        {
            effectiveCancellationToken = requestAborted;
        }

        try
        {
            effectiveCancellationToken.ThrowIfCancellationRequested();
            response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(response.Body, value, options, effectiveCancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            linkedCancellation?.Dispose();
        }
    }

    #region WriteJsonAsync(写入Json)

    /// <summary>
    /// 写入Json
    /// </summary>
    /// <param name="response">Http响应</param>
    /// <param name="obj">对象</param>
    public static async Task WriteJsonAsync(this HttpResponse response, object obj)
    {
        var json = JsonHelper.ToJson(obj);
        await response.WriteJsonAsync(json);
    }

    /// <summary>
    /// 写入Json
    /// </summary>
    /// <param name="response">Http响应</param>
    /// <param name="json">Json字符串</param>
    public static async Task WriteJsonAsync(this HttpResponse response, string json)
    {
        response.ContentType = "application/json; charset=utf-8";
        await response.WriteAsync(json);
    }

    #endregion

    #region SetCache(设置缓存头)

    /// <summary>
    /// 设置缓存头
    /// </summary>
    /// <param name="response">Http响应</param>
    /// <param name="maxAge">最大有效期</param>
    public static void SetCache(this HttpResponse response, int maxAge)
    {
        if (maxAge == 0)
        {
            SetNoCache(response);
        }
        else if (maxAge > 0)
        {
            if (!response.Headers.ContainsKey("Cache-Control"))
            {
                response.Headers["Cache-Control"] = $"max-age={maxAge}";
            }
        }
    }

    #endregion

    #region SetNoCache(设置无缓存)

    /// <summary>
    /// 设置无缓存
    /// </summary>
    /// <param name="response">Http响应</param>
    public static void SetNoCache(this HttpResponse response)
    {
        if (!response.Headers.ContainsKey("Cache-Control"))
            response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        if (!response.Headers.ContainsKey("Pragma"))
            response.Headers["Pragma"] = "no-cache";
    }

    #endregion

    #region WriteHtmlAsync(写入Html)

    /// <summary>
    /// 写入Html
    /// </summary>
    /// <param name="response">Http响应</param>
    /// <param name="html">Html字符串</param>
    public static async Task WriteHtmlAsync(this HttpResponse response, string html)
    {
        response.ContentType = "text/html; charset=utf-8";
        await response.WriteAsync(html, Encoding.UTF8);
    }

    #endregion
}

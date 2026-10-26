namespace Bing.Http;

/// <summary>
/// 提供最终下载正文总量限制的可选能力，旧请求实现无需增加成员。
/// </summary>
/// <typeparam name="TResult">响应转换类型。</typeparam>
public interface IHttpRequestDownloadLimitPolicy<TResult> where TResult : class
{
    /// <summary>
    /// 设置逻辑下载字节上限，null 取消限制；重试不重复累计，续传包含已有字节。
    /// </summary>
    /// <param name="maxBytes">非负字节上限，零仅接受空文件。</param>
    void SetMaxDownloadBytes(long? maxBytes);
}

/// <summary>
/// 下载总量限额的链式配置。
/// </summary>
public static class HttpDownloadLimitExtensions
{
    /// <summary>
    /// 限制最终下载大小；与每次 HTTP 响应限额独立生效。
    /// </summary>
    /// <typeparam name="TResult">响应类型。</typeparam>
    /// <param name="request">支持下载总量限额的请求。</param>
    /// <param name="maxBytes">非负总字节数，包含续传已有内容。</param>
    /// <returns>原请求。</returns>
    public static IHttpRequest<TResult> MaxDownloadBytes<TResult>(this IHttpRequest<TResult> request, long maxBytes) where TResult : class
    {
        if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        GetPolicy(request).SetMaxDownloadBytes(maxBytes);
        return request;
    }

    /// <summary>
    /// 取消最终下载大小限制，不改变每次响应限额。
    /// </summary>
    /// <typeparam name="TResult">响应类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <returns>原请求。</returns>
    public static IHttpRequest<TResult> WithoutMaxDownloadLimit<TResult>(this IHttpRequest<TResult> request) where TResult : class
    {
        GetPolicy(request).SetMaxDownloadBytes(null);
        return request;
    }

    /// <summary>
    /// 获取可选能力，未支持时明确失败。
    /// </summary>
    private static IHttpRequestDownloadLimitPolicy<TResult> GetPolicy<TResult>(IHttpRequest<TResult> request) where TResult : class
        => request == null ? throw new ArgumentNullException(nameof(request)) :
            request as IHttpRequestDownloadLimitPolicy<TResult> ?? throw new NotSupportedException("请求未实现下载总量限制能力。");
}

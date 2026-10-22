using System.Net.Http;

namespace Bing.Http;

/// <summary>
/// 通过可选能力接口提供原始正文、下载进度和流式 JSON 入口。
/// </summary>
public static class HttpTransferCapabilityExtensions
{
    /// <summary>
    /// 设置原始二进制正文；字节数组会防御复制。
    /// </summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="content">待发送字节。</param>
    /// <param name="contentType">媒体类型，默认 application/octet-stream。</param>
    /// <returns>原请求对象。</returns>
    public static IHttpRequest<TResult> BinaryContent<TResult>(this IHttpRequest<TResult> request, byte[] content,
        string contentType = "application/octet-stream") where TResult : class
    {
        Capability<IHttpRequestRawContentRegistration<TResult>>(request).SetBinaryContent(content, contentType);
        return request;
    }

    /// <summary>
    /// 设置从当前位置发送的流正文，允许保留调用方流。
    /// </summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="content">可读流。</param>
    /// <param name="contentType">媒体类型，默认 application/octet-stream。</param>
    /// <param name="leaveOpen">执行结束后是否保持流打开，默认由模块释放。</param>
    /// <returns>原请求对象。</returns>
    public static IHttpRequest<TResult> BinaryContent<TResult>(this IHttpRequest<TResult> request, Stream content,
        string contentType = "application/octet-stream", bool leaveOpen = false) where TResult : class
    {
        Capability<IHttpRequestRawContentRegistration<TResult>>(request).SetBinaryContent(content, contentType, leaveOpen);
        return request;
    }

    /// <summary>
    /// 为每次发送尝试创建全新的、由模块释放的 HttpContent。
    /// </summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="factory">正文工厂；不能返回空或在同一执行内返回重复实例。</param>
    /// <returns>原请求对象。</returns>
    public static IHttpRequest<TResult> ContentFactory<TResult>(this IHttpRequest<TResult> request, Func<HttpContent> factory)
        where TResult : class
    {
        Capability<IHttpRequestRawContentRegistration<TResult>>(request).SetContentFactory(factory);
        return request;
    }

    /// <summary>
    /// 注册成功下载的同步进度通知，中间通知同时遵守字节和时间阈值。
    /// </summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="observer">通知处理；异常会终止下载并传播。</param>
    /// <param name="minimumBytes">最少新增字节数，默认 65536。</param>
    /// <param name="minimumInterval">最短间隔，默认 100 毫秒；零表示只按字节限频。</param>
    /// <returns>原请求对象；重复注册覆盖之前配置。</returns>
    public static IHttpRequest<TResult> ObserveDownloadProgress<TResult>(this IHttpRequest<TResult> request,
        Action<HttpDownloadProgress> observer, long minimumBytes = 65536, TimeSpan? minimumInterval = null)
        where TResult : class
    {
        Capability<IHttpRequestDownloadProgressRegistration<TResult>>(request)
            .SetDownloadProgressObserver(observer, minimumBytes, minimumInterval ?? TimeSpan.FromMilliseconds(100));
        return request;
    }

    /// <summary>
    /// 注册上传正文的同步进度通知，中间通知同时遵守字节和时间阈值。
    /// </summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="observer">通知处理；异常会终止发送并传播。</param>
    /// <param name="minimumBytes">最少新增字节数，默认 65536。</param>
    /// <param name="minimumInterval">最短间隔，默认 100 毫秒；零表示只按字节限频。</param>
    /// <returns>原请求对象；重复注册覆盖之前配置。</returns>
    public static IHttpRequest<TResult> ObserveUploadProgress<TResult>(this IHttpRequest<TResult> request,
        Action<HttpUploadProgress> observer, long minimumBytes = 65536, TimeSpan? minimumInterval = null)
        where TResult : class
    {
        Capability<IHttpRequestUploadProgressRegistration<TResult>>(request)
            .SetUploadProgressObserver(observer, minimumBytes, minimumInterval ?? TimeSpan.FromMilliseconds(100));
        return request;
    }

    /// <summary>
    /// 逐项读取 UTF-8 顶层 JSON 数组；消费期间不会自动重试。
    /// </summary>
    /// <typeparam name="TResult">数组元素类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">发送、读取和枚举取消令牌。</param>
    /// <returns>按正文顺序产生数组元素的异步枚举。</returns>
    public static IAsyncEnumerable<TResult> GetJsonArrayStreamAsync<TResult>(
        this IHttpRequest<TResult> request, CancellationToken cancellationToken = default)
        where TResult : class
        => Capability<IHttpRequestJsonArrayStreaming<TResult>>(request)
            .GetJsonArrayStreamAsync(cancellationToken);

    /// <summary>
    /// 显式使用流式 JSON；需要完整正文语义时自动回退现有处理流程。
    /// </summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">请求执行取消令牌。</param>
    /// <returns>JSON 转换结果；无成功数据时为空。</returns>
    public static Task<TResult> GetJsonStreamAsync<TResult>(this IHttpRequest<TResult> request,
        CancellationToken cancellationToken = default) where TResult : class
        => Capability<IHttpRequestStreamingJson<TResult>>(request).GetJsonStreamAsync(cancellationToken);

    /// <summary>
    /// 直接流式解析 JSON，并返回响应元数据快照。
    /// </summary>
    /// <typeparam name="TResult">响应转换类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">请求执行取消令牌。</param>
    /// <returns>状态码、响应头、转换结果和空的 RawContent 快照。</returns>
    public static Task<HttpResponseResult<TResult>> GetJsonStreamResponseAsync<TResult>(
        this IHttpRequest<TResult> request, CancellationToken cancellationToken = default) where TResult : class
        => Capability<IHttpRequestStreamingResponse<TResult>>(request)
            .GetJsonStreamResponseAsync(cancellationToken);
    /// <summary>
    /// 获取请求实现的指定可选能力。
    /// </summary>
    /// <typeparam name="TCapability">要获取的能力接口类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <returns>请求实现的能力接口。</returns>
    private static TCapability Capability<TCapability>(object request) where TCapability : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        return request as TCapability ?? throw new NotSupportedException(
            $"This HTTP request does not support {typeof(TCapability).Name}.");
    }
}

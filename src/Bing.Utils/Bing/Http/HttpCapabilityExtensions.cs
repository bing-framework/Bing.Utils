using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Bing.Http;

/// <summary>
/// 为既有 HTTP 客户端和请求对象提供基于可选接口的扩展能力。
/// </summary>
/// <remarks>扩展会在运行时检查相应能力接口；旧实现缺少能力时以 <see cref="NotSupportedException"/> 明确失败。</remarks>
public static class HttpCapabilityExtensions
{
    /// <summary>
    /// 以字节数组形式获取响应正文。
    /// </summary>
    /// <typeparam name="TResult">响应类型。</typeparam>
    /// <param name="request">HTTP 请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>响应正文的字节数组。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 null。</exception>
    public static Task<byte[]> GetBytesAsync<TResult>(this IHttpRequest<TResult> request,
        CancellationToken cancellationToken = default) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        return request.GetStreamAsync(cancellationToken);
    }

    /// <summary>
    /// 设置 If-None-Match 条件请求头。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要配置的请求。</param>
    /// <param name="entityTag">有效的实体标签，例如 <c>"v1"</c> 或 <c>W/"v1"</c>。</param>
    /// <returns>已配置的请求对象。</returns>
    public static IHttpRequest<TResult> IfNoneMatch<TResult>(this IHttpRequest<TResult> request, string entityTag)
        where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(entityTag))
            throw new ArgumentException("Entity tag is required.", nameof(entityTag));
        if (!EntityTagHeaderValue.TryParse(entityTag, out var parsed))
            throw new ArgumentException("Entity tag is invalid.", nameof(entityTag));
        return request.Header("If-None-Match", parsed.ToString());
    }

    /// <summary>
    /// 设置 If-Modified-Since 条件请求头。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要配置的请求。</param>
    /// <param name="lastModified">资源的最后修改时间。</param>
    /// <returns>已配置的请求对象。</returns>
    public static IHttpRequest<TResult> IfModifiedSince<TResult>(this IHttpRequest<TResult> request,
        DateTimeOffset lastModified) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        return request.Header("If-Modified-Since",
            lastModified.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 发送一次请求并按校验器确认服务端已接受上传。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <param name="confirmation">服务端响应确认校验器；返回 <see langword="false"/> 时抛出带响应快照的异常。</param>
    /// <param name="includeRawContent">是否在响应快照中保留解码后的正文。</param>
    /// <param name="cancellationToken">用于发送和读取响应的取消令牌。</param>
    /// <returns>通过确认校验的响应快照；发送前拒绝时返回 <see langword="null"/>。</returns>
    /// <exception cref="HttpUploadConfirmationException{TResult}">服务端响应未通过确认校验。</exception>
    public static async Task<HttpResponseResult<TResult>> GetUploadConfirmedResponseAsync<TResult>(
        this IHttpRequest<TResult> request, Func<HttpResponseResult<TResult>, bool> confirmation,
        bool includeRawContent = false, CancellationToken cancellationToken = default) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (confirmation == null) throw new ArgumentNullException(nameof(confirmation));
        var response = await request.GetResponseAsync(includeRawContent, cancellationToken).ConfigureAwait(false);
        if (response == null || confirmation(response))
            return response;
        throw new HttpUploadConfirmationException<TResult>(response);
    }

    /// <summary>
    /// 流式下载到调用方拥有的目标流，并返回状态及写入字节数。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <param name="destination">调用方持有的可写目标流。</param>
    /// <param name="cancellationToken">取消发送和正文复制的令牌。</param>
    /// <returns>表示异步操作的任务；收到响应时返回下载元数据，发送前拒绝时返回 <see langword="null"/>。</returns>
    /// <exception cref="NotSupportedException">请求未实现带结果的下载能力。</exception>
    public static Task<HttpDownloadResult> DownloadToWithResultAsync<TResult>(this IHttpRequest<TResult> request,
        Stream destination, CancellationToken cancellationToken = default) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request is not IHttpRequestDownloadResult<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support download results. Implement IHttpRequestDownloadResult<TResult>.");
        return capability.DownloadToWithResultAsync(destination, cancellationToken);
    }

    /// <summary>
    /// 流式保存文件，并返回状态及写入字节数。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <param name="filePath">目标文件路径。</param>
    /// <param name="cancellationToken">取消发送、正文复制和文件操作的令牌。</param>
    /// <returns>表示异步操作的任务；收到响应时返回下载元数据，发送前拒绝时返回 <see langword="null"/>。</returns>
    /// <exception cref="NotSupportedException">请求未实现带结果的下载能力。</exception>
    public static Task<HttpDownloadResult> DownloadFileWithResultAsync<TResult>(this IHttpRequest<TResult> request,
        string filePath, CancellationToken cancellationToken = default) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request is not IHttpRequestDownloadResult<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support download results. Implement IHttpRequestDownloadResult<TResult>.");
        return capability.DownloadFileWithResultAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// 使用持久化 partial 和验证器续传文件，并返回下载元数据。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <param name="filePath">目标文件路径。</param>
    /// <param name="cancellationToken">取消发送、读取、文件操作和提交的令牌。</param>
    /// <returns>表示异步操作的任务；收到响应时返回下载元数据，发送前拒绝时返回 <see langword="null"/>。</returns>
    /// <exception cref="NotSupportedException">请求未实现断点续传能力。</exception>
    public static Task<HttpDownloadResult> DownloadFileResumableWithResultAsync<TResult>(
        this IHttpRequest<TResult> request, string filePath, CancellationToken cancellationToken = default)
        where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request is not IHttpRequestResumableDownload<TResult> capability)
            throw new NotSupportedException(
                "This HTTP request does not support resumable downloads. Implement IHttpRequestResumableDownload<TResult>.");
        return capability.DownloadFileResumableWithResultAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// 设置断点续传提交前的异步内容校验器。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要配置的请求。</param>
    /// <param name="validator">从位置零读取完整临时文件的校验器；校验失败应抛出异常。</param>
    /// <returns>已配置的请求对象。</returns>
    public static IHttpRequest<TResult> ValidateResumableDownload<TResult>(this IHttpRequest<TResult> request,
        Func<Stream, Task> validator) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (validator == null) throw new ArgumentNullException(nameof(validator));
        if (request is not IHttpRequestResumableDownloadValidation<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support resumable download validation.");
        capability.SetResumableDownloadValidator(validator);
        return request;
    }

    /// <summary>
    /// 设置断点续传提交前的 SHA-256 内容校验。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要配置的请求。</param>
    /// <param name="expectedSha256">期望的 SHA-256 十六进制摘要，可包含连字符或空白。</param>
    /// <returns>已配置的请求对象。</returns>
    public static IHttpRequest<TResult> ValidateResumableDownloadSha256<TResult>(this IHttpRequest<TResult> request,
        string expectedSha256) where TResult : class
    {
        if (string.IsNullOrWhiteSpace(expectedSha256))
            throw new ArgumentException("Expected SHA-256 is required.", nameof(expectedSha256));
        var normalized = expectedSha256.Where(Uri.IsHexDigit).Aggregate(string.Empty, (value, character) => value + character);
        if (normalized.Length != 64 || expectedSha256.Any(character => !Uri.IsHexDigit(character) && !char.IsWhiteSpace(character) && character != '-'))
            throw new ArgumentException("Expected SHA-256 must contain exactly 64 hexadecimal characters.", nameof(expectedSha256));
        return request.ValidateResumableDownload(stream => ValidateSha256Async(stream, normalized));
    }

    /// <summary>
    /// 计算流内容的 SHA-256 摘要并与期望值比较。
    /// </summary>
    /// <param name="stream">从当前位置读取的内容流。</param>
    /// <param name="expectedSha256">期望的十六进制摘要。</param>
    /// <returns>表示摘要校验操作的已完成或失败任务。</returns>
    private static Task ValidateSha256Async(Stream stream, string expectedSha256)
    {
        using var sha256 = SHA256.Create();
        var actualSha256 = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new IOException("The resumable download SHA-256 does not match the expected value.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 使用持久化 partial 和验证器续传文件；成功后返回。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要发送的请求。</param>
    /// <param name="filePath">目标文件路径。</param>
    /// <param name="cancellationToken">取消发送、读取和文件操作的令牌。</param>
    public static async Task DownloadFileResumableAsync<TResult>(
        this IHttpRequest<TResult> request, string filePath, CancellationToken cancellationToken = default)
        where TResult : class
    {
        var result = await request.DownloadFileResumableWithResultAsync(filePath, cancellationToken)
            .ConfigureAwait(false);
        if (result != null && result.IsSuccessStatusCode && !result.IsFileSaved)
            throw new IOException("The resumable download response succeeded but the file was not saved.");
    }

    /// <summary>
    /// 注册整次请求完成时的观测回调。
    /// </summary>
    /// <typeparam name="TResult">成功响应结果类型。</typeparam>
    /// <param name="request">要配置的请求。</param>
    /// <param name="observer">请求完成时执行的观测回调。</param>
    /// <param name="onObserverError">观测回调异常处理器；可为空。</param>
    /// <returns>原请求对象，供继续配置或发送。</returns>
    public static IHttpRequest<TResult> ObserveCompletion<TResult>(this IHttpRequest<TResult> request,
        Action<HttpRequestCompletion> observer, Action<Exception> onObserverError = null) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        if (request is not IHttpRequestObservationRegistration<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support completion observation. Implement IHttpRequestObservationRegistration<TResult>.");
        capability.SetCompletionObserver(observer, onObserverError);
        return request;
    }

    /// <summary>
    /// 通过客户端的请求工厂创建指定 HTTP 方法的请求。
    /// </summary>
    /// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
    /// <param name="client">提供请求创建能力的 HTTP 客户端。</param>
    /// <param name="method">要发送的 HTTP 方法。</param>
    /// <param name="url">请求地址。</param>
    /// <returns>可继续配置并发送的请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException"><paramref name="client"/> 未实现 <see cref="IHttpRequestFactory"/>。</exception>
    public static IHttpRequest<TResult> Request<TResult>(this IHttpClient client, HttpMethod method, string url) where TResult : class
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (client is not IHttpRequestFactory factory)
            throw new NotSupportedException("This HTTP client does not support arbitrary HTTP methods. Implement IHttpRequestFactory.");
        return factory.CreateRequest<TResult>(method, url);
    }

    /// <summary>
    /// 创建指定成功响应类型的 PATCH 请求。
    /// </summary>
    /// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
    /// <param name="client">提供 PATCH 请求创建能力的 HTTP 客户端。</param>
    /// <param name="url">请求地址。</param>
    /// <returns>可继续配置并发送的 PATCH 请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException">客户端未实现 <see cref="IHttpRequestFactory"/>。</exception>
    public static IHttpRequest<TResult> Patch<TResult>(this IHttpClient client, string url) where TResult : class =>
        client.Request<TResult>(new HttpMethod("PATCH"), url);

    /// <summary>
    /// 创建 PATCH 请求并设置可选正文。
    /// </summary>
    /// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
    /// <param name="client">提供 PATCH 请求创建能力的 HTTP 客户端。</param>
    /// <param name="url">请求地址。</param>
    /// <param name="content">要发送的请求正文对象；为 <see langword="null"/> 时不设置正文。</param>
    /// <returns>已设置正文（若提供）的 PATCH 请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException">客户端未实现 <see cref="IHttpRequestFactory"/>。</exception>
    public static IHttpRequest<TResult> Patch<TResult>(this IHttpClient client, string url, object content) where TResult : class =>
        content == null ? client.Patch<TResult>(url) : client.Patch<TResult>(url).Content(content);

    /// <summary>
    /// 创建 PATCH 请求并将响应正文按字符串读取。
    /// </summary>
    /// <param name="client">提供 PATCH 请求创建能力的 HTTP 客户端。</param>
    /// <param name="url">请求地址。</param>
    /// <returns>可继续配置并发送的 PATCH 请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException">客户端未实现 <see cref="IHttpRequestFactory"/>。</exception>
    public static IHttpRequest<string> Patch(this IHttpClient client, string url) => client.Patch<string>(url);

    /// <summary>
    /// 创建 PATCH 请求、设置可选正文并将响应正文按字符串读取。
    /// </summary>
    /// <param name="client">提供 PATCH 请求创建能力的 HTTP 客户端。</param>
    /// <param name="url">请求地址。</param>
    /// <param name="content">要发送的请求正文对象；为 <see langword="null"/> 时不设置正文。</param>
    /// <returns>已设置正文（若提供）的 PATCH 请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException">客户端未实现 <see cref="IHttpRequestFactory"/>。</exception>
    public static IHttpRequest<string> Patch(this IHttpClient client, string url, object content) => client.Patch<string>(url, content);

    /// <summary>
    /// 创建 HEAD 请求并将结果类型设为字符串。
    /// </summary>
    /// <param name="client">提供 HEAD 请求创建能力的 HTTP 客户端。</param>
    /// <param name="url">请求地址。</param>
    /// <returns>可继续配置并发送的 HEAD 请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException">客户端未实现 <see cref="IHttpRequestFactory"/>。</exception>
    public static IHttpRequest<string> Head(this IHttpClient client, string url) => client.Request<string>(HttpMethod.Head, url);

    /// <summary>
    /// 创建 OPTIONS 请求并将结果类型设为字符串。
    /// </summary>
    /// <param name="client">提供 OPTIONS 请求创建能力的 HTTP 客户端。</param>
    /// <param name="url">请求地址。</param>
    /// <returns>可继续配置并发送的 OPTIONS 请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException">客户端未实现 <see cref="IHttpRequestFactory"/>。</exception>
    public static IHttpRequest<string> Options(this IHttpClient client, string url) => client.Request<string>(HttpMethod.Options, url);

    /// <summary>
    /// 发送请求并读取完整响应元数据和转换后的结果。
    /// </summary>
    /// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
    /// <param name="request">实现完整响应读取能力的 HTTP 请求。</param>
    /// <param name="includeRawContent">是否在结果中保留按响应字符集解码后的正文；默认不保留。</param>
    /// <param name="cancellationToken">用于取消发送和响应处理的令牌。</param>
    /// <returns>表示异步操作的任务；收到响应时返回状态码、头快照、转换数据和可选正文；发送前回调拒绝发送时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException">请求未实现 <see cref="IHttpRequestResponse{TResult}"/>，或不能与自定义发送后回调同时使用。</exception>
    /// <exception cref="OperationCanceledException">请求被取消或超时。</exception>
    public static Task<HttpResponseResult<TResult>> GetResponseAsync<TResult>(this IHttpRequest<TResult> request,
        bool includeRawContent = false, CancellationToken cancellationToken = default) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request is not IHttpRequestResponse<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support response metadata. Implement IHttpRequestResponse<TResult>.");
        return capability.GetResponseAsync(includeRawContent, cancellationToken);
    }

    /// <summary>
    /// 将异步拦截器追加到请求处理链，并返回原请求以继续链式配置。
    /// </summary>
    /// <typeparam name="TResult">请求响应结果类型。</typeparam>
    /// <param name="request">支持拦截器注册的 HTTP 请求。</param>
    /// <param name="interceptor">要追加的拦截器；每次发送尝试都会调用其前后处理。</param>
    /// <returns>同一个请求对象，供继续配置或发送。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 或 <paramref name="interceptor"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="NotSupportedException"><paramref name="request"/> 未实现 <see cref="IHttpRequestInterceptorRegistration{TResult}"/>。</exception>
    public static IHttpRequest<TResult> UseInterceptor<TResult>(this IHttpRequest<TResult> request, HttpRequestInterceptor interceptor) where TResult : class
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));
        if (request is not IHttpRequestInterceptorRegistration<TResult> capability)
            throw new NotSupportedException("This HTTP request does not support interceptors. Implement IHttpRequestInterceptorRegistration<TResult>.");
        capability.AddInterceptor(interceptor);
        return request;
    }
}

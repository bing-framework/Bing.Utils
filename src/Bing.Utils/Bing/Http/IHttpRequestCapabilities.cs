namespace Bing.Http;

/// <summary>

/// 为 HTTP 请求提供读取状态、头信息和转换结果的可选能力。

/// </summary>
/// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
public interface IHttpRequestResponse<TResult> where TResult : class
{
    /// <summary>
    /// 发送请求并返回独立的响应元数据快照及转换结果。
    /// </summary>
    /// <param name="includeRawContent">是否在结果中保留按响应字符集解码后的正文文本；默认不保留。</param>
    /// <param name="cancellationToken">用于取消发送、响应读取及请求处理的令牌。</param>
    /// <returns>表示异步操作的任务；收到响应时返回状态码、头快照、转换数据和可选正文；发送前回调拒绝发送时返回 <see langword="null"/>。</returns>
    /// <remarks>HTTP 非成功状态也返回结果，且数据为 <see langword="null"/>；网络、转换、响应大小限制、用户配置的失败/完成回调及取消错误通过任务传播。已有处理异常时仍执行完成回调，但保留原异常。发送前回调拒绝发送时不执行响应回调。HEAD、204 和空正文不会读取正文。返回结果不持有响应对象或正文流。</remarks>
    /// <exception cref="NotSupportedException">请求同时配置了自定义发送后回调，或实现不支持此能力。</exception>
    /// <exception cref="OperationCanceledException">请求被取消或达到请求超时时间。</exception>
    Task<HttpResponseResult<TResult>> GetResponseAsync(bool includeRawContent = false, CancellationToken cancellationToken = default);
}

/// <summary>

/// 为 HTTP 请求提供直接流式 JSON 解析及响应元数据快照的可选能力。

/// </summary>
/// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
public interface IHttpRequestStreamingResponse<TResult> where TResult : class
{
    /// <summary>
    /// 流式解析成功 JSON，并返回状态码、响应头和转换结果。
    /// </summary>
    /// <param name="cancellationToken">用于取消发送和响应读取的令牌。</param>
    /// <returns>响应元数据快照及转换结果；RawContent 默认为空。</returns>
    Task<HttpResponseResult<TResult>> GetJsonStreamResponseAsync(CancellationToken cancellationToken = default);
}

/// <summary>

/// 为 HTTP 请求提供追加异步拦截器的可选能力。

/// </summary>
/// <typeparam name="TResult">请求响应结果类型，用于与对应请求对象关联。</typeparam>
public interface IHttpRequestInterceptorRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 将拦截器追加到请求的发送前和发送后处理链。
    /// </summary>
    /// <param name="interceptor">要追加的拦截器实例；其前后回调在每次发送尝试中执行。</param>
    /// <remarks>一次请求执行开始后，新增注册不影响该次执行；重试沿用该次执行快照，新的执行会使用更新后的注册列表。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interceptor"/> 为 <see langword="null"/>。</exception>
    void AddInterceptor(HttpRequestInterceptor interceptor);
}

/// <summary>

/// 为请求提供带结果的流式下载能力。

/// </summary>
public interface IHttpRequestDownloadResult<TResult> where TResult : class
{
    /// <summary>
    /// 将成功响应写入调用方拥有的流，并返回下载元数据；拒绝发送时返回空。
    /// </summary>
    /// <returns>下载结果；发送被拒绝时返回 <see langword="null"/>。</returns>
    /// <param name="destination">调用方持有的可写目标流。</param>
    /// <param name="cancellationToken">请求和正文复制的取消令牌。</param>
    Task<HttpDownloadResult> DownloadToWithResultAsync(Stream destination, CancellationToken cancellationToken = default);

    /// <summary>

    /// 通过临时文件保存成功响应，并返回下载元数据；拒绝发送时返回空。

    /// </summary>
    /// <returns>下载结果；发送被拒绝时返回 <see langword="null"/>。</returns>
    /// <param name="filePath">目标文件路径。</param>
    /// <param name="cancellationToken">请求和正文复制的取消令牌。</param>
    Task<HttpDownloadResult> DownloadFileWithResultAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>

/// 为请求提供带验证器的文件断点续传能力。

/// </summary>
/// <typeparam name="TResult">成功响应结果类型。</typeparam>
public interface IHttpRequestResumableDownload<TResult> where TResult : class
{
    /// <summary>
    /// 通过断点续传下载目标文件。
    /// </summary>
    /// <remarks>状态文件与目标文件位于同一目录；此方法不改变接口中常规文件下载方法的既有行为。</remarks>
    /// <param name="filePath">目标文件路径。</param>
    /// <param name="cancellationToken">发送、读取、文件操作和提交取消令牌。</param>
    /// <returns>响应元数据、最终文件字节数和提交状态；不安全的 partial 状态会先清理并从头下载。</returns>
    Task<HttpDownloadResult> DownloadFileResumableWithResultAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>

/// 为请求提供断点续传完成后的内容校验能力。

/// </summary>
/// <typeparam name="TResult">成功响应结果类型。</typeparam>
public interface IHttpRequestResumableDownloadValidation<TResult> where TResult : class
{
    /// <summary>
    /// 设置提交目标文件前执行的内容校验器。
    /// </summary>
    /// <param name="validator">从位置零读取临时文件并在校验失败时抛出异常的异步委托。</param>
    /// <remarks>校验失败时目标文件不会替换；已有可验证片段和状态文件按续传失败语义保留。</remarks>
    void SetResumableDownloadValidator(Func<Stream, Task> validator);
}

/// <summary>

/// 为请求提供整次执行完成观测的可选能力。

/// </summary>
public interface IHttpRequestObservationRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 设置完成观测回调及可选的观测错误处理回调。
    /// </summary>
    /// <remarks>观测回调异常不会改变请求结果；错误处理回调自身异常也会被隔离。</remarks>
    /// <param name="observer">请求完成时调用的观测回调。</param>
    /// <param name="onObserverError">观测回调发生异常时调用的错误处理回调。</param>
    void SetCompletionObserver(Action<HttpRequestCompletion> observer, Action<Exception> onObserverError = null);
}

/// <summary>

/// 为请求提供重试等待观测的可选能力，不改变已有重试策略。

/// </summary>
/// <typeparam name="TResult">成功响应结果类型。</typeparam>
public interface IHttpRequestRetryObservationRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 设置重试等待观测回调及可选的观测错误处理回调。
    /// </summary>
    /// <param name="observer">响应成功释放后、等待前同步执行的回调；不保证调用线程。</param>
    /// <param name="onObserverError">观测回调异常处理器；自身异常也会被隔离。</param>
    /// <remarks>重复注册覆盖旧配置；一次执行开始时取快照，注册变更仅影响后续执行。回调耗时计入整次请求耗时，回调异常不改变请求结果。未注册时不创建事件。</remarks>
    /// <exception cref="ArgumentNullException">观测回调为空。</exception>
    void SetRetryObserver(Action<HttpRetryScheduled> observer, Action<Exception> onObserverError = null);
}

using System.Net.Http;

namespace Bing.Http;

/// <summary>

/// 提供原始二进制正文和按发送尝试创建正文的可选能力。

/// </summary>
/// <typeparam name="TResult">响应转换类型。</typeparam>
public interface IHttpRequestRawContentRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 设置防御复制的字节正文，替换此前的原始正文配置。
    /// </summary>
    /// <param name="content">原始字节；不能为 null。</param>
    /// <param name="contentType">合法的媒体类型及可选参数。</param>
    void SetBinaryContent(byte[] content, string contentType);

    /// <summary>

    /// 从注册时的位置发送流正文，可定位流重试时回到该位置。

    /// </summary>
    /// <param name="content">可读流；不能为 null。</param>
    /// <param name="contentType">合法的媒体类型及可选参数。</param>
    /// <param name="leaveOpen">是否由调用方保留流；否则执行结束或替换配置时释放。</param>
    /// <remarks>不可定位流不能用于允许自动重试的方法；原始正文不能与对象、参数或 multipart 正文混用。</remarks>
    void SetBinaryContent(Stream content, string contentType, bool leaveOpen);

    /// <summary>

    /// 设置每次发送尝试创建独立正文的工厂。

    /// </summary>
    /// <param name="factory">同步创建非空、全新 HttpContent 的工厂；正文由模块释放。</param>
    /// <remarks>同一执行内重复返回同一实例会失败；工厂应确保重试正文内容等价。工厂异常通过请求任务传播。</remarks>
    void SetContentFactory(Func<HttpContent> factory);
}

/// <summary>

/// 提供下载正文进度注册的可选能力。

/// </summary>
/// <typeparam name="TResult">响应转换类型。</typeparam>
public interface IHttpRequestDownloadProgressRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 设置同步进度回调；初始及最终通知不受限频约束。
    /// </summary>
    /// <param name="observer">进度处理；其异常终止下载并传播。</param>
    /// <param name="minimumBytes">中间通知最少新增字节数，必须大于零。</param>
    /// <param name="minimumInterval">中间通知最短时间间隔，不能为负。</param>
    /// <remarks>中间通知须同时满足两个阈值；每次执行固定配置快照。仅成功 HTTP 响应通知；文件提交状态请读取下载结果。</remarks>
    void SetDownloadProgressObserver(Action<HttpDownloadProgress> observer, long minimumBytes, TimeSpan minimumInterval);
}

/// <summary>

/// 提供上传正文进度注册的可选能力。

/// </summary>
/// <typeparam name="TResult">响应转换类型。</typeparam>
public interface IHttpRequestUploadProgressRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 设置同步上传进度回调；初始及最终通知不受限频约束。
    /// </summary>
    /// <param name="observer">进度处理；其异常终止发送并传播。</param>
    /// <param name="minimumBytes">中间通知最少新增字节数，必须大于零。</param>
    /// <param name="minimumInterval">中间通知最短时间间隔，不能为负。</param>
    void SetUploadProgressObserver(Action<HttpUploadProgress> observer, long minimumBytes, TimeSpan minimumInterval);
}

/// <summary>

/// 提供顶层 JSON 数组逐项异步消费的可选能力。

/// </summary>
/// <typeparam name="TResult">数组元素类型。</typeparam>
public interface IHttpRequestJsonArrayStreaming<TResult> where TResult : class
{
    /// <summary>
    /// 发送请求并逐项读取 UTF-8 顶层 JSON 数组。
    /// </summary>
    /// <param name="cancellationToken">发送、读取和枚举取消令牌。</param>
    /// <returns>按正文顺序产生数组元素的异步枚举。</returns>
    IAsyncEnumerable<TResult> GetJsonArrayStreamAsync(CancellationToken cancellationToken = default);
}

/// <summary>

/// 提供显式流式 JSON 解析的可选能力。

/// </summary>
/// <typeparam name="TResult">JSON 转换类型。</typeparam>
public interface IHttpRequestStreamingJson<TResult> where TResult : class
{
    /// <summary>
    /// 发送请求并以流式方式解析兼容的 UTF-8 JSON 响应。
    /// </summary>
    /// <param name="cancellationToken">发送、等待、读取和解析的取消令牌。</param>
    /// <returns>成功转换结果；直接解析时空正文返回 null，回退时沿用原转换语义；非成功状态及拒绝发送返回 null。</returns>
    /// <remarks>沿用现有 JSON 选项和成功回调。非 UTF-8、string 结果、自定义转换、正文回调或非 JSON 响应回退原有正文处理。原文保留仍使用 GetResponseAsync。不能与 OnSendAfter 同时配置；大小限额、取消和异常仍适用。</remarks>
    Task<TResult> GetJsonStreamAsync(CancellationToken cancellationToken = default);
}

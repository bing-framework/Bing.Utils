namespace Bing.Http;

/// <summary>

/// 表示服务端上传确认校验未通过，并保留服务端响应快照。

/// </summary>
/// <typeparam name="TResult">响应转换结果类型。</typeparam>
public sealed class HttpUploadConfirmationException<TResult> : InvalidOperationException where TResult : class
{
    /// <summary>
    /// 初始化一个<see cref="HttpUploadConfirmationException{TResult}"/>类型的实例。
    /// </summary>
    /// <param name="response">未通过确认校验的服务端响应。</param>
    public HttpUploadConfirmationException(HttpResponseResult<TResult> response)
        : base("The server upload confirmation was rejected.")
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    /// <summary>

    /// 获取未通过确认校验的服务端响应快照。

    /// </summary>
    public HttpResponseResult<TResult> Response { get; }
}

using System.Text.Json;

namespace Bing.Http;

/// <summary>
/// Http请求
/// </summary>
public interface IHttpRequest
{
}

/// <summary>
/// Http请求
/// </summary>
/// <typeparam name="TResult">结果类型</typeparam>
public interface IHttpRequest<TResult> : IHttpRequest where TResult : class
{
    /// <summary>
    /// 设置外部配置的HttpClient实例名称
    /// </summary>
    /// <param name="name">HttpClient名称</param>
    IHttpRequest<TResult> HttpClientName(string name);

    /// <summary>
    /// 设置基地址
    /// </summary>
    /// <param name="baseAddress">基地址</param>
    IHttpRequest<TResult> BaseAddress(string baseAddress);

    /// <summary>
    /// 设置字符编码
    /// </summary>
    /// <param name="encoding">字符编码，范例：gb2312</param>
    IHttpRequest<TResult> Encoding(string encoding);

    /// <summary>
    /// 设置字符编码
    /// </summary>
    /// <param name="encoding">字符编码</param>
    IHttpRequest<TResult> Encoding(Encoding encoding);

    /// <summary>
    /// 设置访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    IHttpRequest<TResult> BearerToken(string token);

    /// <summary>
    /// 设置内容类型
    /// </summary>
    /// <param name="contentType">内容类型</param>
    IHttpRequest<TResult> ContentType(HttpContentType contentType);

    /// <summary>
    /// 设置内容类型
    /// </summary>
    /// <param name="contentType">内容类型</param>
    IHttpRequest<TResult> ContentType(string contentType);

    /// <summary>
    /// 设置证书
    /// </summary>
    /// <param name="path">证书路径</param>
    /// <param name="password">证书密码</param>
    IHttpRequest<TResult> Certificate(string path, string password);

    /// <summary>
    /// 忽略SSL证书
    /// </summary>
    IHttpRequest<TResult> IgnoreSsl();

    /// <summary>
    /// 设置Json序列化配置
    /// </summary>
    /// <param name="options">Json序列化配置</param>
    IHttpRequest<TResult> JsonSerializerOptions(JsonSerializerOptions options);

    /// <summary>
    /// 设置超时时间
    /// </summary>
    /// <param name="timeout">超时时间。单位：秒</param>
    IHttpRequest<TResult> Timeout(int timeout);

    /// <summary>
    /// 设置超时时间
    /// </summary>
    /// <param name="timeout">超时时间</param>
    IHttpRequest<TResult> Timeout(TimeSpan timeout);

    /// <summary>
    /// 设置请求头
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IHttpRequest<TResult> Header(string key, string value);

    /// <summary>
    /// 设置请求头
    /// </summary>
    /// <param name="headers">请求头键值对集合</param>
    IHttpRequest<TResult> Header(IDictionary<string, string> headers);

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IHttpRequest<TResult> QueryString(string key, string value);

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="queryString">查询字符串键值对集合</param>
    IHttpRequest<TResult> QueryString(IDictionary<string, string> queryString);

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="queryString">查询字符串对象</param>
    IHttpRequest<TResult> QueryString(object queryString);

    /// <summary>
    /// 设置是否自动携带Cookie
    /// </summary>
    /// <param name="isUseCookies">是否自动携带Cookie</param>
    IHttpRequest<TResult> UseCookies(bool isUseCookies = true);

    /// <summary>
    /// 设置Cookie
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IHttpRequest<TResult> Cookie(string key, string value);

    /// <summary>
    /// 设置Cookie集合
    /// </summary>
    /// <param name="cookies">Cookie集合</param>
    IHttpRequest<TResult> Cookie(IDictionary<string, string> cookies);

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IHttpRequest<TResult> Content(string key, object value);

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="parameters">参数字典</param>
    IHttpRequest<TResult> Content(IDictionary<string, object> parameters);

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="value">值</param>
    IHttpRequest<TResult> Content(object value);

    /// <summary>
    /// 添加内容类型为 application/json 的参数
    /// </summary>
    /// <param name="value">值</param>
    IHttpRequest<TResult> JsonContent(object value);

    /// <summary>
    /// 添加内容类型为 text/xml 的参数
    /// </summary>
    /// <param name="value">值</param>
    IHttpRequest<TResult> XmlContent(string value);

    /// <summary>
    /// 添加内容类型为 multipart/form-data 的文件参数
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="name">参数名。默认值：file</param>
    IHttpRequest<TResult> FileContent(Stream stream, string fileName, string name = "file");

    /// <summary>
    /// 添加文件上传流。
    /// </summary>
    /// <remarks><paramref name="leaveOpen"/>为 true 时由调用方负责释放输入流；否则请求结束时释放。</remarks>
    /// <param name="stream">待上传的输入流</param>
    /// <param name="fileName">发送给服务端的文件名</param>
    /// <param name="name">表单参数名</param>
    /// <param name="leaveOpen">为 true 时由调用方继续持有输入流；否则请求结束时释放</param>
    /// <returns>当前请求配置对象</returns>
    IHttpRequest<TResult> FileContent(Stream stream, string fileName, string name, bool leaveOpen);

    /// <summary>
    /// 添加内容类型为 multipart/form-data 的文件参数
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="name">参数名。默认值：file</param>
    IHttpRequest<TResult> FileContent(string filePath, string name = "file");

    /// <summary>
    /// 文件上传参数是否添加双引号
    /// </summary>
    /// <param name="isQuote">是否添加双引号</param>
    IHttpRequest<TResult> FileParameterQuotes(bool isQuote = true);

    /// <summary>
    /// 配置请求级 HTTP 重试。
    /// </summary>
    /// <remarks>默认仅对安全方法及瞬时故障重试。</remarks>
    /// <param name="options">重试次数、退避及允许方法配置</param>
    /// <returns>当前请求配置对象</returns>
    /// <exception cref="ArgumentNullException">配置为空</exception>
    /// <exception cref="ArgumentOutOfRangeException">重试次数或延迟无效</exception>
    IHttpRequest<TResult> Retry(HttpRetryOptions options);

    /// <summary>
    /// 设置响应正文大小上限。
    /// </summary>
    /// <remarks>上限同时作用于正文读取和交付；零表示仅允许空正文。</remarks>
    /// <param name="maxBytes">允许的最大字节数，零表示仅允许空正文</param>
    /// <returns>当前请求配置对象</returns>
    /// <exception cref="ArgumentOutOfRangeException">上限为负数</exception>
    IHttpRequest<TResult> MaxResponseBytes(long maxBytes);

    /// <summary>
    /// 发送前事件
    /// </summary>
    /// <param name="action">发送前操作，返回false取消发送</param>
    IHttpRequest<TResult> OnSendBefore(Func<HttpRequestMessage, bool> action);

    /// <summary>
    /// 发送后事件
    /// </summary>
    /// <param name="action">发送后操作，自定义解析返回值</param>
    IHttpRequest<TResult> OnSendAfter(Func<HttpResponseMessage, Task<TResult>> action);

    /// <summary>
    /// 注册异步响应处理回调。
    /// </summary>
    /// <remarks>回调执行期间响应保持有效；回调应协作处理取消，且不得返回依赖响应存活的流。</remarks>
    /// <param name="action">在响应有效期间执行的操作；不得返回依赖响应存活的流</param>
    /// <returns>当前请求配置对象</returns>
    IHttpRequest<TResult> OnSendAfter(Func<HttpResponseMessage, CancellationToken, Task<TResult>> action);

    /// <summary>
    /// 结果转换事件
    /// </summary>
    /// <param name="action">结果转换操作，参数为响应内容</param>
    IHttpRequest<TResult> OnConvert(Func<string, TResult> action);

    /// <summary>
    /// 请求成功事件
    /// </summary>
    /// <param name="action">执行成功操作，参数为响应结果</param>
    IHttpRequest<TResult> OnSuccess(Action<TResult> action);

    /// <summary>
    /// 请求成功事件
    /// </summary>
    /// <param name="action">执行成功操作，参数为响应结果</param>
    IHttpRequest<TResult> OnSuccess(Func<TResult, Task> action);

    /// <summary>
    /// 请求失败事件
    /// </summary>
    /// <param name="action">执行失败操作，参数为响应消息和响应内容</param>
    IHttpRequest<TResult> OnFail(Action<HttpResponseMessage, object> action);

    /// <summary>
    /// 请求完成事件，不论成功失败都会执行
    /// </summary>
    /// <param name="action">执行完成操作，参数为响应消息和响应内容</param>
    IHttpRequest<TResult> OnComplete(Action<HttpResponseMessage, object> action);

    /// <summary>
    /// 获取结果
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TResult> GetResultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取流
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task<byte[]> GetStreamAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 将响应正文流式写入目标流。
    /// </summary>
    /// <remarks>目标流由调用方持有；失败时可能已有部分内容。</remarks>
    /// <param name="destination">调用方持有的可写目标流</param>
    /// <param name="cancellationToken">请求和正文复制的取消令牌</param>
    /// <exception cref="ArgumentNullException">目标流为空</exception>
    /// <exception cref="ArgumentException">目标流不可写</exception>
    Task DownloadToAsync(Stream destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将响应正文下载到目标文件。
    /// </summary>
    /// <remarks>使用同目录临时文件暂存，成功后替换目标文件；失败时保留原文件。</remarks>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="cancellationToken">请求和正文复制的取消令牌</param>
    /// <exception cref="ArgumentException">文件路径为空或空白</exception>
    Task DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteAsync(string filePath, CancellationToken cancellationToken = default);
}

namespace Bing.Http;

/// <summary>
/// Http客户端
/// </summary>
public interface IHttpClient
{
    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<string> Get(string url);

    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <param name="queryString">查询字符串对象</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<string> Get(string url, object queryString);

    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<TResult> Get<TResult>(string url) where TResult : class;

    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <param name="queryString">查询字符串对象</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<TResult> Get<TResult>(string url, object queryString) where TResult : class;

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<string> Post(string url);

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<string> Post(string url, object content);

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<TResult> Post<TResult>(string url) where TResult : class;

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<TResult> Post<TResult>(string url, object content) where TResult : class;

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<string> Put(string url);

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<string> Put(string url, object content);

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<TResult> Put<TResult>(string url) where TResult : class;

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<TResult> Put<TResult>(string url, object content) where TResult : class;

    /// <summary>
    /// 发送Delete请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<string> Delete(string url);

    /// <summary>
    /// 发送Delete请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <returns>用于配置和执行请求的请求对象。</returns>
    IHttpRequest<TResult> Delete<TResult>(string url) where TResult : class;
}

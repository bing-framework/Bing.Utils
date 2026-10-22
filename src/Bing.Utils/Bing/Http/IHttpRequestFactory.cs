using System.Net.Http;

namespace Bing.Http;

/// <summary>
/// 提供不限定于既有常用动词的 HTTP 请求创建能力。
/// </summary>
public interface IHttpRequestFactory
{
    /// <summary>
    /// 创建使用指定 HTTP 方法和地址的请求对象。
    /// </summary>
    /// <typeparam name="TResult">成功响应正文转换后的类型。</typeparam>
    /// <param name="method">要发送的 HTTP 方法。</param>
    /// <param name="url">请求地址，可结合实现提供的基地址解析。</param>
    /// <returns>可继续配置并发送的请求对象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> 为 <see langword="null"/>（由具体实现检查时）。</exception>
    IHttpRequest<TResult> CreateRequest<TResult>(HttpMethod method, string url) where TResult : class;
}

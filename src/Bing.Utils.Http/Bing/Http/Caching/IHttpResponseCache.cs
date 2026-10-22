using Bing.Http;

namespace Bing.Http.Caching;

/// <summary>
/// 显式管理 HTTP 响应快照的缓存。
/// </summary>
/// <typeparam name="TResult">响应转换后的数据类型。</typeparam>
public interface IHttpResponseCache<TResult> where TResult : class
{
    /// <summary>
    /// 按缓存键读取尚未过期的响应快照。
    /// </summary>
    /// <param name="key">调用方定义的稳定缓存键。</param>
    /// <returns>命中的响应快照；未命中时为 <see langword="null"/>。</returns>
    Task<HttpResponseResult<TResult>> GetAsync(string key);

    /// <summary>
    /// 按缓存键写入响应快照。
    /// </summary>
    /// <param name="key">调用方定义的稳定缓存键。</param>
    /// <param name="response">要缓存的响应快照。</param>
    /// <param name="ttl">缓存有效期，必须为正数且为有限值。</param>
    /// <param name="requestContainsAuthorization">请求是否带有认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存带认证信息的请求。</param>
    Task SetAsync(string key, HttpResponseResult<TResult> response, TimeSpan ttl,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false);

    /// <summary>
    /// 移除指定缓存键。
    /// </summary>
    /// <param name="key">要移除的缓存键。</param>
    /// <returns>表示移除操作的任务。</returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// 移除所有以指定前缀开头的缓存键。
    /// </summary>
    /// <param name="prefix">大小写敏感的序数前缀；空前缀不被允许。</param>
    /// <returns>实际移除的缓存条目数。</returns>
    int RemoveByPrefix(string prefix);

    /// <summary>
    /// 清空当前缓存实例中的所有已发布条目。
    /// </summary>
    /// <returns>实际移除的条目数。</returns>
    Task<int> ClearAsync();

    /// <summary>
    /// 读取缓存，未命中时由一个并发工厂请求回源并共享结果。
    /// </summary>
    /// <param name="key">调用方定义的稳定缓存键。</param>
    /// <param name="ttl">回源结果的缓存有效期，必须为正数且为有限值。</param>
    /// <param name="factory">缓存未命中时执行的回源委托。</param>
    /// <param name="requestContainsAuthorization">请求是否带有认证信息。</param>
    /// <param name="allowAuthenticated">是否明确允许缓存带认证信息的请求。</param>
    /// <returns>缓存或回源得到的响应快照。</returns>
    Task<HttpResponseResult<TResult>> GetOrCreateAsync(string key, TimeSpan ttl,
        Func<Task<HttpResponseResult<TResult>>> factory,
        bool requestContainsAuthorization = false, bool allowAuthenticated = false);
}

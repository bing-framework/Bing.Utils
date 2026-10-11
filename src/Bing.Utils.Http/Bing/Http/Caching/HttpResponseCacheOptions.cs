namespace Bing.Http.Caching;

/// <summary>

/// 配置进程内 HTTP 响应缓存的容量限制。

/// </summary>
public sealed class HttpResponseCacheOptions
{
    /// <summary>
    /// 最大缓存条目数；为空表示不限制。
    /// </summary>
    public int? MaxEntries { get; set; }

    /// <summary>

    /// 最大估算总字节数；为空表示不限制。

    /// </summary>
    public long? MaxBytes { get; set; }

    /// <summary>

    /// 响应数据的字节估算器；配置 <see cref="MaxBytes"/> 时必须提供。

    /// </summary>
    public Func<object, long> SizeEstimator { get; set; }
}

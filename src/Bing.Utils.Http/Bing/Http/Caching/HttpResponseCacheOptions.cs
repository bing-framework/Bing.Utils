namespace Bing.Http.Caching;

/// <summary>
/// 配置进程内 HTTP 响应缓存的容量限制。
/// </summary>
public sealed class HttpResponseCacheOptions
{
    /// <summary>
    /// 获取或设置最大缓存条目数；默认值为 null（不限制），指定时必须大于 0。
    /// </summary>
    public int? MaxEntries { get; set; }

    /// <summary>
    /// 获取或设置最大估算缓存字节数；默认值为 null（不限制），指定时必须大于 0。
    /// </summary>
    public long? MaxBytes { get; set; }

    /// <summary>
    /// 获取或设置响应数据的字节估算器；配置 <see cref="MaxBytes"/> 时必须提供，且返回值不得小于 0。
    /// </summary>
    public Func<object, long> SizeEstimator { get; set; }
}

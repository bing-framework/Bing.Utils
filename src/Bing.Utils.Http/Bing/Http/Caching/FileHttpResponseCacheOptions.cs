namespace Bing.Http.Caching;

/// <summary>
/// 配置本地文件 HTTP 响应缓存的容量限制。
/// </summary>
public sealed class FileHttpResponseCacheOptions
{
    /// <summary>
    /// 获取或设置最大缓存条目数；默认值为 null（不限制），指定时必须大于 0。
    /// </summary>
    public int? MaxEntries { get; set; }

    /// <summary>
    /// 获取或设置最大缓存文件总字节数；单位为字节，默认值为 null（不限制），指定时必须大于 0。
    /// </summary>
    public long? MaxBytes { get; set; }
}

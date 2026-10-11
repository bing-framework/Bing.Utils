namespace Bing.Http.Caching;

/// <summary>

/// 配置本地文件 HTTP 响应缓存的容量限制。

/// </summary>
public sealed class FileHttpResponseCacheOptions
{
    /// <summary>
    /// 最大缓存条目数；为空表示不限制。
    /// </summary>
    public int? MaxEntries { get; set; }

    /// <summary>

    /// 最大缓存文件总字节数；为空表示不限制。

    /// </summary>
    public long? MaxBytes { get; set; }
}

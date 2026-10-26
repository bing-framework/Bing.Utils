using System;

namespace Bing.Date.Clock;

/// <summary>
/// 提供可替换的 UTC 当前时间来源。
/// </summary>
public interface IClock
{
    /// <summary>
    /// 获取当前 UTC 时间点。
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

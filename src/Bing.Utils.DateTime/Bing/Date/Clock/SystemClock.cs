using System;

namespace Bing.Date.Clock;

/// <summary>
/// 从系统时钟读取 UTC 时间。
/// </summary>
public sealed class SystemClock : IClock
{
    /// <summary>
    /// 保存进程内共享的系统时钟实例。
    /// </summary>
    private static readonly SystemClock Singleton = new SystemClock();

    /// <summary>
    /// 获取系统时钟的单例实例。
    /// </summary>
    public static SystemClock Instance => Singleton;

    /// <summary>
    /// 初始化系统时钟实例。
    /// </summary>
    private SystemClock()
    {
    }

    /// <summary>
    /// 获取系统当前 UTC 时间点。
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

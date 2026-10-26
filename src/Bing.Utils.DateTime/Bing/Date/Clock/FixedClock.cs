using System;

namespace Bing.Date.Clock;

/// <summary>
/// 返回固定 UTC 时间点的时钟实现。
/// </summary>
public sealed class FixedClock : IClock
{
    /// <summary>
    /// 保存归一化为零偏移的固定时间点。
    /// </summary>
    private readonly DateTimeOffset _utcNow;

    /// <summary>
    /// 初始化固定时钟，并将输入时间点转换为 UTC 偏移。
    /// </summary>
    /// <param name="utcNow">固定时间点；允许任意有效偏移，内部按同一时间点归一为 UTC。</param>
    public FixedClock(DateTimeOffset utcNow)
    {
        _utcNow = utcNow.ToUniversalTime();
    }

    /// <summary>
    /// 获取固定 UTC 时间点。
    /// </summary>
    public DateTimeOffset UtcNow => _utcNow;
}

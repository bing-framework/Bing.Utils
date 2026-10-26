namespace Bing.IO;

/// <summary>
/// 文件监控选项。
/// </summary>
public sealed class FileWatcherOptions
{
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(int.MaxValue - 1d);

    /// <summary>
    /// 获取或设置检查已监控目录是否存在的间隔。
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 获取或设置重复事件合并间隔；为空时保持逐事件回调。
    /// </summary>
    public TimeSpan? DebounceInterval { get; set; }

    /// <summary>
    /// 获取或设置文件系统通知缓冲区大小。
    /// </summary>
    public int InternalBufferSize { get; set; } = 8192;

    /// <summary>
    /// 获取或设置待合并事件的最大数量。
    /// </summary>
    public int MaxPendingEvents { get; set; } = 4096;

    internal FileWatcherOptions CreateSnapshot()
    {
        if (ScanInterval <= TimeSpan.Zero || ScanInterval > MaxDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(ScanInterval), "轮询间隔必须大于零且不超过支持的定时器范围。");
        }

        if (DebounceInterval.HasValue &&
            (DebounceInterval.Value <= TimeSpan.Zero || DebounceInterval.Value > MaxDelay))
        {
            throw new ArgumentOutOfRangeException(nameof(DebounceInterval), "事件合并间隔必须大于零且不超过支持的定时器范围。");
        }

        if (InternalBufferSize < 4096 || InternalBufferSize > 65536)
        {
            throw new ArgumentOutOfRangeException(nameof(InternalBufferSize), "通知缓冲区大小必须位于 4096 到 65536 字节之间。");
        }

        if (MaxPendingEvents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxPendingEvents), "待处理事件上限必须大于零。");
        }

        return new FileWatcherOptions
        {
            ScanInterval = ScanInterval,
            DebounceInterval = DebounceInterval,
            InternalBufferSize = InternalBufferSize,
            MaxPendingEvents = MaxPendingEvents
        };
    }
}

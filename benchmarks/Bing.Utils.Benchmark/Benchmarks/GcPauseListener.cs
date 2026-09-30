using System.Diagnostics.Tracing;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 采集运行时线程完全暂停事件。
/// </summary>
internal sealed class GcPauseListener : EventListener
{
    /// <summary>
    /// 最近一次线程完全暂停的时间。
    /// </summary>
    private DateTime _suspendedAt;

    /// <summary>
    /// 是否正在采集测量窗口。
    /// </summary>
    private bool _active;

    /// <summary>
    /// 保护事件和测量线程共享状态的锁。
    /// </summary>
    private readonly object _sync = new();

    /// <summary>
    /// 测量窗口开始时间。
    /// </summary>
    private DateTime _start;

    /// <summary>
    /// 测量窗口结束时间。
    /// </summary>
    private DateTime _end = DateTime.MaxValue;

    /// <summary>
    /// 窗口后运行时事件到达信号。
    /// </summary>
    private readonly ManualResetEventSlim _drained = new(false);

    /// <summary>
    /// 当前暂停是否由 GC 引起。
    /// </summary>
    private bool _gcSuspension;

    /// <summary>
    /// 获取完整暂停事件的数量。
    /// </summary>
    public int PauseCount { get; private set; }

    /// <summary>
    /// 获取暂停总时长。
    /// </summary>
    public TimeSpan TotalPause { get; private set; }

    /// <summary>
    /// 获取单次最长暂停时长。
    /// </summary>
    public TimeSpan MaxPause { get; private set; }

    /// <summary>
    /// 开始采集暂停事件。
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            _start = DateTime.UtcNow;
            _active = true;
        }
    }

    /// <summary>
    /// 停止采集暂停事件。
    /// </summary>
    public void Stop()
    {
        lock (_sync)
            _end = DateTime.UtcNow;
    }

    /// <summary>
    /// 等待窗口后事件并读取同步快照。
    /// </summary>
    /// <returns>包含暂停次数、总时长、最长时长和事件排空状态的快照。</returns>
    public (int Count, TimeSpan Total, TimeSpan Maximum, bool Drained) Snapshot()
    {
        var drained = _drained.Wait(TimeSpan.FromSeconds(2));
        lock (_sync)
        {
            _active = false;
            return (PauseCount, TotalPause, MaxPause, drained);
        }
    }

    /// <inheritdoc />
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
            EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x1);
    }

    /// <inheritdoc />
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (_sync == null)
            return;
        lock (_sync)
        {
            if (!_active || eventData.TimeStamp < _start)
                return;
            if (eventData.TimeStamp > _end)
            {
                _drained.Set();
                return;
            }

            if (eventData.EventId == 9)
            {
                var reason = eventData.Payload?.Count > 0 ? Convert.ToInt32(eventData.Payload[0]) : -1;
                _gcSuspension = reason is 1 or 6;
                _suspendedAt = default;
            }

            // 8 = GCSuspendEEEnd，7 = GCRestartEEBegin；两者之间线程处于暂停状态。
            if (eventData.EventId == 8 && _gcSuspension)
            {
                _suspendedAt = eventData.TimeStamp;
            }
            else if (eventData.EventId == 7 && _suspendedAt != default)
            {
                var pause = eventData.TimeStamp - _suspendedAt;
                _suspendedAt = default;
                if (pause < TimeSpan.Zero)
                    return;
                PauseCount++;
                TotalPause += pause;
                if (pause > MaxPause)
                    MaxPause = pause;
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        _drained.Dispose();
    }
}

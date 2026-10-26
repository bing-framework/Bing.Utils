using System;

namespace Bing.Date.Clock;

/// <summary>
/// 可替换 UTC 时钟实现测试。
/// </summary>
public class ClockTest
{
    /// <summary>
    /// 验证系统时钟是共享单例且返回零偏移 UTC 时间。
    /// </summary>
    [Fact]
    public void SystemClock_UsesSingletonAndReturnsUtc()
    {
        var before = DateTimeOffset.UtcNow;
        IClock clock = SystemClock.Instance;
        var value = clock.UtcNow;
        var after = DateTimeOffset.UtcNow;

        SystemClock.Instance.ShouldBeSameAs(clock);
        value.Offset.ShouldBe(TimeSpan.Zero);
        (value >= before && value <= after).ShouldBeTrue();
    }

    /// <summary>
    /// 验证固定时钟将输入偏移归一到相同的 UTC 时间点。
    /// </summary>
    [Fact]
    public void FixedClock_NormalizesInputOffsetToUtc()
    {
        var input = new DateTimeOffset(2024, 5, 1, 12, 30, 0, TimeSpan.FromHours(8));
        var clock = new FixedClock(input);

        clock.UtcNow.ShouldBe(new DateTimeOffset(2024, 5, 1, 4, 30, 0, TimeSpan.Zero));
        clock.UtcNow.Offset.ShouldBe(TimeSpan.Zero);
    }
}

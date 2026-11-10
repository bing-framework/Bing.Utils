using Bing.Date.Clock;

namespace Bing.Date;

/// <summary>
/// 验证相对当前时间的计算使用输入所表示的时间点。
/// </summary>
public class DateTimeRelativeTimeRegressionTest
{
    /// <summary>
    /// 验证系统时钟入口按 UTC 输入表示的时间点计算。
    /// </summary>
    [Fact]
    public void SystemClock_UtcInput_UsesSameInstant()
    {
        var now = DateTime.UtcNow;

        now.ElapsedTime().Duration().ShouldBeLessThan(TimeSpan.FromSeconds(10));
        Math.Abs(now.ElapsedMilliseconds()).ShouldBeLessThan(10000);
        now.AddHours(1).IsInTheFuture().ShouldBeTrue();
        now.AddHours(-1).IsInThePast().ShouldBeTrue();
    }

    /// <summary>
    /// 验证固定时钟、不同 Kind 及相等边界的行为。
    /// </summary>
    /// <param name="kind">参照时间的种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void FixedClock_UsesInstantsAndHonorsBoundary(DateTimeKind kind)
    {
        var instant = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(new DateTimeOffset(instant));
        var local = instant.ToLocalTime();
        var value = kind == DateTimeKind.Utc ? instant : DateTime.SpecifyKind(local, kind);

        value.ElapsedTime(clock).ShouldBe(TimeSpan.Zero);
        value.IsInTheFuture(false, clock).ShouldBeFalse();
        value.IsInThePast(false, clock).ShouldBeFalse();
        value.IsInTheFuture(true, clock).ShouldBeTrue();
        value.IsInThePast(true, clock).ShouldBeTrue();
        value.AddTicks(-15001).ElapsedTime(clock).ShouldBe(TimeSpan.FromTicks(15001));
        value.AddTicks(-15001).ElapsedMilliseconds(clock).ShouldBe(1);
        value.AddTicks(15001).ElapsedMilliseconds(clock).ShouldBe(-1);
        value.AddHours(1).IsInTheFuture(false, clock).ShouldBeTrue();
        value.AddHours(-1).IsInThePast(false, clock).ShouldBeTrue();
    }

    /// <summary>
    /// 验证本地时间在夏令时回拨附近保留原时间点。
    /// </summary>
    /// <param name="utcHour">参照时间的 UTC 小时。</param>
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void FixedClock_LocalTimeDuringDstTransition_PreservesInstant(int utcHour)
    {
        var utc = new DateTime(2024, 11, 3, utcHour, 30, 0, DateTimeKind.Utc);
        var clock = new FixedClock(new DateTimeOffset(utc));
        var local = utc.ToLocalTime();

        local.ElapsedTime(clock).ShouldBe(TimeSpan.Zero);
        local.IsInTheFuture(true, clock).ShouldBeTrue();
        local.IsInThePast(true, clock).ShouldBeTrue();
    }

    /// <summary>
    /// 验证相对时间计算拒绝空时钟。
    /// </summary>
    [Fact]
    public void FixedClock_NullClock_Throws()
    {
        var value = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Should.Throw<ArgumentNullException>(() => value.ElapsedTime(null));
        Should.Throw<ArgumentNullException>(() => value.ElapsedMilliseconds(null));
        Should.Throw<ArgumentNullException>(() => value.IsInTheFuture(false, null));
        Should.Throw<ArgumentNullException>(() => value.IsInThePast(false, null));
    }

    /// <summary>
    /// 验证整毫秒超出 int 范围时明确抛出异常。
    /// </summary>
    [Fact]
    public void FixedClock_ElapsedMilliseconds_RejectsIntegerOverflow()
    {
        var instant = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(new DateTimeOffset(instant));

        instant.AddTicks(-21474836470000L).ElapsedMilliseconds(clock).ShouldBe(int.MaxValue);
        instant.AddTicks(21474836480000L).ElapsedMilliseconds(clock).ShouldBe(int.MinValue);
        Should.Throw<OverflowException>(() => instant.AddTicks(-21474836480000L).ElapsedMilliseconds(clock));
        Should.Throw<OverflowException>(() => instant.AddTicks(21474836490000L).ElapsedMilliseconds(clock));
    }
}

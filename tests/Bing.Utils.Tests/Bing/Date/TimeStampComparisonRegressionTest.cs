using System.Collections.Generic;

namespace Bing.Date;
/// <summary>
/// 验证不同时间戳类型的比较和时间间隔契约。
/// </summary>
public class TimeStampComparisonRegressionTest
{
    /// <summary>
    /// 验证 Unix 时间戳按秒差转换为 ticks 和时间间隔。
    /// </summary>
    [Fact]
    public void UnixTimeStamps_OneSecondApart_UseTickDifferences()
    {
        TimeStamp earlier = new UnixTimeStamp(0L);
        TimeStamp later = new UnixTimeStamp(1L);

        later.GetDifference(earlier).ShouldBe(TimeSpan.TicksPerSecond);
        earlier.GetDifference(later).ShouldBe(-TimeSpan.TicksPerSecond);
        later.GetTimeSpan(earlier).ShouldBe(TimeSpan.FromSeconds(1));
        earlier.GetTimeSpan(later).ShouldBe(TimeSpan.FromSeconds(1));
        (later - earlier).ShouldBe(TimeSpan.FromSeconds(1));
        (earlier - later).ShouldBe(TimeSpan.FromSeconds(-1));
        (earlier < later).ShouldBeTrue();
    }

    /// <summary>
    /// 验证不同时间戳类型按相同 ticks 比较并保持相等性契约。
    /// </summary>
    [Fact]
    public void MixedTimestampTypes_CompareUsingTheSameTickOrigin()
    {
        TimeStamp unix = new UnixTimeStamp(1L);
        TimeStamp unrelated = new TimeStamp(1L);
        TimeStamp equivalent = new TimeStamp(UnixTimeStamp.UnixEpoch.AddSeconds(1));

        unix.Equals(unrelated).ShouldBeFalse();
        unrelated.Equals(unix).ShouldBeFalse();
        (unrelated < unix).ShouldBeTrue();
        new HashSet<TimeStamp> { unix, unrelated }.Count.ShouldBe(2);
        new HashSet<TimeStamp> { unrelated, unix }.Count.ShouldBe(2);

        unix.Equals(equivalent).ShouldBeTrue();
        equivalent.Equals(unix).ShouldBeTrue();
        unix.CompareTo(equivalent).ShouldBe(0);
        equivalent.CompareTo(unix).ShouldBe(0);
        unix.GetDifference(equivalent).ShouldBe(0);
        unix.GetHashCode().ShouldBe(equivalent.GetHashCode());
        new HashSet<TimeStamp> { unix, equivalent }.Count.ShouldBe(1);
        new HashSet<TimeStamp> { equivalent, unix }.Count.ShouldBe(1);
        new SortedSet<TimeStamp> { unix, equivalent }.Count.ShouldBe(1);
    }

    /// <summary>
    /// 验证 Unix 时间戳比较使用量化后的整秒值。
    /// </summary>
    [Fact]
    public void UnixTimestamp_ComparisonsUseTheQuantizedSecond()
    {
        var origin = UnixTimeStamp.UnixEpoch.AddSeconds(5);
        TimeStamp unix = new UnixTimeStamp(origin.AddTicks(1234));
        TimeStamp second = new TimeStamp(origin);
        TimeStamp fractional = new TimeStamp(origin.AddTicks(1234));

        unix.ShouldBe(second);
        unix.GetDifference(fractional).ShouldBe(-1234);
        (unix < fractional).ShouldBeTrue();
        (fractional - unix).ShouldBe(TimeSpan.FromTicks(1234));
    }

    /// <summary>
    /// 验证最大 Unix 秒值加零后保持原值。
    /// </summary>
    [Fact]
    public void UnixTimestamp_Maximum_AddingZeroPreservesTheUtcSecond()
    {
        var timestamp = new UnixTimeStamp(253402300799L);

        timestamp.Add(TimeSpan.Zero).ToTimestamp().ShouldBe(253402300799L);
        timestamp.Subtract(TimeSpan.Zero).ToTimestamp().ShouldBe(253402300799L);
        Should.Throw<ArgumentOutOfRangeException>(() => timestamp.Add(TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// 验证秒数运算拒绝整数溢出和 Unix 时间范围溢出。
    /// </summary>
    [Fact]
    public void UnixTimestamp_AddSeconds_RejectsIntegerAndUnixRangeOverflow()
    {
        Should.Throw<OverflowException>(() => new UnixTimeStamp(1L).AddSeconds(long.MaxValue));
        Should.Throw<ArgumentOutOfRangeException>(() => new UnixTimeStamp(0L).AddSeconds(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => new UnixTimeStamp(253402300799L).AddSeconds(1));
    }

    /// <summary>
    /// 验证各 DateTime 种类下 Unix 纪元之前的时间均被拒绝。
    /// </summary>
    /// <param name="kind">测试日期时间的种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void UnixTimestamp_DateTimeKind_RejectsBeforeEpoch(DateTimeKind kind)
    {
        var epoch = kind == DateTimeKind.Local
            ? UnixTimeStamp.UnixEpoch.ToLocalTime()
            : DateTime.SpecifyKind(UnixTimeStamp.UnixEpoch, kind);

        Should.Throw<ArgumentOutOfRangeException>(() => new UnixTimeStamp(epoch.AddTicks(-1)))
            .ParamName.ShouldBe("dt");
        new UnixTimeStamp(epoch).ToTimestamp().ShouldBe(0);
    }

    /// <summary>
    /// 验证本地最大日期转换为不可表示的 UTC 时间时被拒绝。
    /// </summary>
    [Fact]
    public void UnixTimestamp_LocalMaximum_RejectsUnrepresentableUtc()
    {
        var localMaximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
        if (TimeZoneInfo.Local.GetUtcOffset(localMaximum) < TimeSpan.Zero)
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new UnixTimeStamp(localMaximum))
                .ParamName.ShouldBe("dt");
            return;
        }

        var utcTicks = new DateTimeOffset(localMaximum).UtcDateTime.Ticks;
        var expectedSeconds = (utcTicks - UnixTimeStamp.UnixEpoch.Ticks) / TimeSpan.TicksPerSecond;
        new UnixTimeStamp(localMaximum).ToTimestamp().ShouldBe(expectedSeconds);
    }

    /// <summary>
    /// 验证本地最大日期附近的运算在 UTC 可表示时保持结果。
    /// </summary>
    [Fact]
    public void UnixTimestamp_LocalArithmeticAcrossMaximum_UsesUtcWhenRepresentable()
    {
        var localMaximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
        if (TimeZoneInfo.Local.GetUtcOffset(localMaximum) <= TimeSpan.Zero)
            return;

        var lastWholeSecond = localMaximum.AddTicks(-(localMaximum.Ticks % TimeSpan.TicksPerSecond));
        var initial = new UnixTimeStamp(lastWholeSecond.AddSeconds(-1));

        var result = initial.Add(TimeSpan.FromSeconds(2));
        result.ToTimestamp().ShouldBe(initial.ToTimestamp() + 2);
        result.ToDateTime().Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// 验证 TimeSpan 运算先保留输入小数 ticks 再量化为秒。
    /// </summary>
    [Fact]
    public void UnixTimestamp_TimeSpanArithmetic_RetainsInputFractionBeforeQuantizing()
    {
        var input = UnixTimeStamp.UnixEpoch.AddSeconds(3).AddTicks(TimeSpan.TicksPerSecond - 1);
        var timestamp = new UnixTimeStamp(input);

        timestamp.ToTimestamp().ShouldBe(3);
        var result = timestamp.Add(TimeSpan.FromTicks(1));
        result.ToTimestamp().ShouldBe(4);
        result.ToDateTime().Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// 验证夏令时重叠期间的本地输入保持其时间点。
    /// </summary>
    [Fact]
    public void UnixTimestamp_LocalInputs_PreserveInstantDuringDstOverlap()
    {
        var first = new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc);
        foreach (var instant in new[] { first, first.AddHours(1) })
        {
            var expected = (instant.Ticks - UnixTimeStamp.UnixEpoch.Ticks) / TimeSpan.TicksPerSecond;
            var timestamp = new UnixTimeStamp(instant.ToLocalTime());
            timestamp.ToTimestamp().ShouldBe(expected);
            timestamp.Add(TimeSpan.Zero).ToTimestamp().ShouldBe(expected);
        }
    }
}

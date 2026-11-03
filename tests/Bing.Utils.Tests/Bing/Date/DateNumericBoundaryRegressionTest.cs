using Bing.Helpers;

namespace Bing.Date;

/// <summary>纪元数值和时间戳增量的边界回归。</summary>
public class DateNumericBoundaryRegressionTest
{
    [Theory]
    [InlineData(1844674407371L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void EpochSeconds_OverflowInput_IsRejected(long value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Time.OfEpochSecond(value)).ParamName.ShouldBe("epochSecond");
    }

    [Theory]
    [InlineData(1844674407371000L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void EpochMilliseconds_OverflowInput_IsRejected(long value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Time.OfEpochMilli(value)).ParamName.ShouldBe("epochMilli");
    }

    [Fact]
    public void Epoch_LegalNegativeAndLimits_MatchBcl()
    {
        foreach (var seconds in new[] { -62135596800L, -1L, 0L, 253402300799L })
            Time.OfEpochSecond(seconds).ShouldBe(DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime);
        foreach (var milliseconds in new[] { -62135596800000L, -1L, 0L, 253402300799999L })
            Time.OfEpochMilli(milliseconds).ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-999)]
    [InlineData(-1000)]
    public void UnixTimestamp_NegativeMilliseconds_AreRejected(long value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => UnixTimeStamp.FromUnixTimeMilliseconds(value))
            .ParamName.ShouldBe("unixTimestampMilliseconds");
    }

    [Theory]
    [InlineData(0.00001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    public void Increment_InvalidValue_DoesNotReplaceConfiguration(double value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new NoRepeatTimeStampFactory(value));
        var factory = new NoRepeatTimeStampFactory(1);
        Should.Throw<ArgumentOutOfRangeException>(() => factory.IncrementMs = value);
        factory.IncrementMs.ShouldBe(1);
        factory.GeneratedCount.ShouldBe(0);
    }

    [Fact]
    public void Increment_OneTick_IsIdenticalAcrossFrameworks()
    {
        var factory = new NoRepeatTimeStampFactory(0.0001);
        var origin = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        factory.GetTimeStamp(origin).Ticks.ShouldBe(origin.Ticks);
        factory.GetTimeStamp(origin).Ticks.ShouldBe(origin.Ticks + 1);
        factory.GetDiagnosticInfo().ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(999, 0)]
    [InlineData(1000, 1)]
    public void UnixTimestamp_PositiveMilliseconds_KeepSecondTruncation(long milliseconds, long seconds)
    {
        UnixTimeStamp.FromUnixTimeMilliseconds(milliseconds).ToTimestamp().ShouldBe(seconds);
    }
}

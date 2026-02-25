namespace Bing.Helpers;
/// <summary>
/// 测试类：覆盖 `UnixTimeHelper` 相关行为。
/// </summary>
[Trait("Bing.Helpers", "UnixTime")]
public class UnixTimeHelperTest
{
    /// <summary>
    /// 测试用例：验证 `EpochTime` 在 `StaticValue` 场景下，结果为 `IsUnixEpochUtc`。
    /// </summary>
    [Fact]
    public void EpochTime_StaticValue_IsUnixEpochUtc()
    {
        UnixTime.EpochTime.ShouldBe(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
    /// <summary>
    /// 测试用例：验证 `ToTimestamp` 在 `UtcDateTime` 场景下，结果为 `ReturnsExpectedMillisecondsAndSeconds`。
    /// </summary>
    [Fact]
    public void ToTimestamp_UtcDateTime_ReturnsExpectedMillisecondsAndSeconds()
    {
        var utc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        UnixTime.ToTimestamp(utc, true).ShouldBe(1704067200000L);
        UnixTime.ToTimestamp(utc, false).ShouldBe(1704067200L);
    }
    /// <summary>
    /// 测试用例：验证 `ToTimestamp` 在 `SecondPrecisionWithHalfSecond` 场景下，结果为 `CurrentlyRoundsUsingConvertToInt64`。
    /// </summary>
    [Fact]
    public void ToTimestamp_SecondPrecisionWithHalfSecond_CurrentlyRoundsUsingConvertToInt64()
    {
        var utc = new DateTime(1970, 1, 1, 0, 0, 1, 500, DateTimeKind.Utc);
        UnixTime.ToTimestamp(utc, false).ShouldBe(2L);
    }
    /// <summary>
    /// 测试用例：验证 `ToTimestamp` 在 `LocalDateTime` 场景下，结果为 `ConvertsToSameTimestampAsEquivalentUtc`。
    /// </summary>
    [Fact]
    public void ToTimestamp_LocalDateTime_ConvertsToSameTimestampAsEquivalentUtc()
    {
        var utc = new DateTime(2024, 1, 1, 8, 30, 45, DateTimeKind.Utc);
        var local = utc.ToLocalTime();
        var utcTimestamp = UnixTime.ToTimestamp(utc, false);
        var localTimestamp = UnixTime.ToTimestamp(local, false);
        localTimestamp.ShouldBe(utcTimestamp);
    }
    /// <summary>
    /// 测试用例：验证 `ToDateTime` 在 `ZeroTimestamp` 场景下，结果为 `ReturnsLocalEpoch`。
    /// </summary>
    [Fact]
    public void ToDateTime_ZeroTimestamp_ReturnsLocalEpoch()
    {
        var localEpoch = UnixTime.EpochTime.ToLocalTime();
        var result = UnixTime.ToDateTime(0);
        result.ShouldBe(localEpoch);
    }
    /// <summary>
    /// 测试用例：验证 `ToDateTime` 在 `RoundTripWithMilliseconds` 场景下，结果为 `ReturnsEquivalentLocalTime`。
    /// </summary>
    [Fact]
    public void ToDateTime_RoundTripWithMilliseconds_ReturnsEquivalentLocalTime()
    {
        var utc = new DateTime(2024, 5, 1, 8, 30, 45, 123, DateTimeKind.Utc);
        var timestamp = UnixTime.ToTimestamp(utc, true);
        var result = UnixTime.ToDateTime(timestamp);
        result.ShouldBe(utc.ToLocalTime());
    }
}


using Bing.Date;
namespace Bing.Helpers;
/// <summary>
/// 测试类：覆盖 `TimeAndUnixTimeBoundaryContract` 相关行为。
/// </summary>
[Trait("Bing.Helpers", "TimeAndUnixTime.Boundary")]
public class TimeAndUnixTimeBoundaryContractTest : IDisposable
{
    private readonly bool _originalIsUseUtc = TimeOptions.IsUseUtc;
    /// <summary>
    /// 测试辅助：提供 `Dispose` 的测试支撑逻辑。
    /// </summary>
    public void Dispose()
    {
        Time.Reset();
        TimeOptions.IsUseUtc = _originalIsUseUtc;
    }
    /// <summary>
    /// 测试用例：验证 `UseUtc` 在 `NullValue` 场景下，结果为 `UsesGlobalTimeOptionsSetting`。
    /// </summary>
    [Fact]
    public void UseUtc_NullValue_UsesGlobalTimeOptionsSetting()
    {
        TimeOptions.IsUseUtc = false;
        Time.UseUtc(null);
        var localNow = Time.Now;
        Math.Abs((localNow - DateTime.Now).TotalSeconds).ShouldBeLessThan(1);
        TimeOptions.IsUseUtc = true;
        Time.UseUtc(null);
        var utcNow = Time.Now;
        Math.Abs((utcNow - DateTime.UtcNow).TotalSeconds).ShouldBeLessThan(1);
    }
    /// <summary>
    /// 测试用例：验证 `SetTime` 在 `InvalidString` 场景下，结果为 `ThrowsArgumentExceptionWithParameterName`。
    /// </summary>
    [Fact]
    public void SetTime_InvalidString_ThrowsArgumentExceptionWithParameterName()
    {
        var exception = Should.Throw<ArgumentException>(() => Time.SetTime("not-a-datetime-value"));
        exception.ParamName.ShouldBe("dateTime");
    }
    /// <summary>
    /// 测试用例：验证 `OfEpochSecond` 在 `OutOfRange` 场景下，结果为 `ThrowsAndMessageContainsInput`。
    /// </summary>
    [Fact]
    public void OfEpochSecond_OutOfRange_ThrowsAndMessageContainsInput()
    {
        const long outOfRangeTimestamp = 253402300800;
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => Time.OfEpochSecond(outOfRangeTimestamp));
        exception.Message.ShouldContain(outOfRangeTimestamp.ToString());
    }
    /// <summary>
    /// 测试用例：验证 `ToEpochSecond` 在 `AndOfEpochSecond_BeforeUnixEpoch` 场景下，结果为 `RoundTrip`。
    /// </summary>
    [Fact]
    public void ToEpochSecond_AndOfEpochSecond_BeforeUnixEpoch_RoundTrip()
    {
        var utc = new DateTime(1969, 12, 31, 23, 59, 30, DateTimeKind.Utc);
        var local = utc.ToLocalTime();
        var timestamp = Time.ToEpochSecond(local);
        var restored = Time.OfEpochSecond(timestamp);
        timestamp.ShouldBe(-30);
        restored.ShouldBe(local);
    }
    /// <summary>
    /// 测试用例：验证 `ToTimestamp` 在 `UnspecifiedDateTime` 场景下，结果为 `TreatsInputAsLocalTime`。
    /// </summary>
    [Fact]
    public void ToTimestamp_UnspecifiedDateTime_TreatsInputAsLocalTime()
    {
        var unspecified = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        var expected = Convert.ToInt64((TimeZoneInfo.ConvertTimeToUtc(unspecified) - UnixTime.EpochTime).TotalMilliseconds / 1000);
        var actual = UnixTime.ToTimestamp(unspecified, false);
        actual.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `ToDateTime` 在 `NegativeTimestamp` 场景下，结果为 `ReturnsExpectedLocalTime`。
    /// </summary>
    [Fact]
    public void ToDateTime_NegativeTimestamp_ReturnsExpectedLocalTime()
    {
        const long timestamp = -1;
        var expected = UnixTime.EpochTime.AddMilliseconds(timestamp).ToLocalTime();
        var actual = UnixTime.ToDateTime(timestamp);
        actual.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `ToTimestamp` 在 `NoArg` 场景下，结果为 `ReturnsCurrentUnixMillisecondsRange`。
    /// </summary>
    [Fact]
    public void ToTimestamp_NoArg_ReturnsCurrentUnixMillisecondsRange()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var actual = UnixTime.ToTimestamp();
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        actual.ShouldBeInRange(before - 1000, after + 1000);
    }
}


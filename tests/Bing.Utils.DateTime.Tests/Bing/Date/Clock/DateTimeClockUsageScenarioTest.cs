using Bing.Date.Clock;

namespace Bing.Date;
/// <summary>
/// 验证固定时钟用于业务有效期判断的日期时间契约。
/// </summary>
public class DateTimeClockUsageScenarioTest
{
    /// <summary>
    /// 验证示例有效期包含创建时刻且不包含到期时刻。
    /// </summary>
    /// <param name="ticksFromExpiry">当前时间与到期时间的 tick 差。</param>
    /// <param name="expectedValid">示例有效期是否应当有效。</param>
    [Theory]
    [InlineData(-18000000001L, false)]
    [InlineData(-18000000000L, true)]
    [InlineData(-1L, true)]
    [InlineData(0L, false)]
    [InlineData(1L, false)]
    public void ValidityWindow_UsesFixedClockAtCreationAndExpiryBoundaries(long ticksFromExpiry, bool expectedValid)
    {
        var created = new DateTime(2024, 5, 1, 4, 0, 0, DateTimeKind.Utc);
        var expires = new DateTime(2024, 5, 1, 4, 30, 0, DateTimeKind.Utc);
        var localExpiry = new DateTimeOffset(2024, 5, 1, 12, 30, 0, TimeSpan.FromHours(8));
        IClock clock = new FixedClock(localExpiry.AddTicks(ticksFromExpiry));

        clock.UtcNow.Offset.ShouldBe(TimeSpan.Zero);
        clock.UtcNow.UtcDateTime.Ticks.ShouldBe(expires.AddTicks(ticksFromExpiry).Ticks);
        var isValid = created.IsInThePast(includeBoundary: true, clock)
                      && expires.IsInTheFuture(includeBoundary: false, clock);
        isValid.ShouldBe(expectedValid);
    }
}

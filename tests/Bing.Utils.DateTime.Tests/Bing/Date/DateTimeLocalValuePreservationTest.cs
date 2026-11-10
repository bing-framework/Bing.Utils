using System;
using Bing.Date.Calendars;

namespace Bing.Date;
/// <summary>
/// 验证本地日期值的复制、分量设置和取整保留夏令时重复时间选择。
/// </summary>
public class DateTimeLocalValuePreservationTest
{
    /// <summary>
    /// 验证两个克隆入口均保留回拨期间同一钟面时间的两种实际时间点。
    /// </summary>
    [NewYorkFact]
    public void Clone_BothFoldOccurrences_PreserveUtcInstant()
    {
        foreach (var utcHour in new[] { 5, 6 })
        {
            var utc = new DateTime(2024, 11, 3, utcHour, 30, 30, 123, DateTimeKind.Utc).AddTicks(4567);
            var local = utc.ToLocalTime();
            TimeZoneInfo.Local.IsAmbiguousTime(local).ShouldBeTrue();

            AssertSameLocalValue(local.Clone(), local, utc);
            AssertSameLocalValue(System.BingDateTimeExtensions.Clone(local), local, utc);

            // 按程序集定位同名类型，验证旧静态调用的运行时签名仍可绑定。
            var legacyClone = (Func<DateTime, DateTime>)typeof(DateInfo).Assembly
                .GetType("Bing.Date.DateTimeExtensions")!
                .GetMethod("Clone", new[] { typeof(DateTime) })!
                .CreateDelegate(typeof(Func<DateTime, DateTime>));
            AssertSameLocalValue(legacyClone(local), local, utc);
        }
    }
    /// <summary>
    /// 验证设置原有分量或同一重复小时内的新分量不会将第一次出现改成第二次。
    /// </summary>
    [NewYorkFact]
    public void ComponentSetters_FoldOccurrences_PreserveChoiceAndTicks()
    {
        foreach (var utcHour in new[] { 5, 6 })
        {
            var utc = new DateTime(2024, 11, 3, utcHour, 30, 30, 123, DateTimeKind.Utc).AddTicks(4567);
            var local = utc.ToLocalTime();
            var unchanged = new[]
            {
                local.SetTime(1), local.SetTime(1, 30), local.SetTime(1, 30, 30), local.SetTime(1, 30, 30, 123),
                local.SetHour(1), local.SetMinute(30), local.SetSecond(30), local.SetMillisecond(123),
                local.SetDate(2024), local.SetDate(2024, 11), local.SetDate(2024, 11, 3),
                local.SetYear(2024), local.SetMonth(11), local.SetDay(3), local.SetKind(DateTimeKind.Local),
                local.At(1, 30), local.On(2024, 11, 3)
            };
            foreach (var result in unchanged)
                AssertSameLocalValue(result, local, utc);

            AssertSameLocalValue(local.SetMinute(40), local.AddMinutes(10), utc.AddMinutes(10));
            AssertSameLocalValue(local.SetSecond(31), local.AddSeconds(1), utc.AddSeconds(1));
            AssertSameLocalValue(local.SetMillisecond(124), local.AddTicks(TimeSpan.TicksPerMillisecond),
                utc.AddTicks(TimeSpan.TicksPerMillisecond));
        }
    }
    /// <summary>
    /// 验证按 tick 取整及按秒、分钟四舍五入保留重复小时的实际时间点。
    /// </summary>
    [NewYorkFact]
    public void Rounding_FoldOccurrences_PreserveChoiceAndClockAdjustment()
    {
        foreach (var utcHour in new[] { 5, 6 })
        {
            var utc = new DateTime(2024, 11, 3, utcHour, 30, 30, 123, DateTimeKind.Utc).AddTicks(4567);
            var local = utc.ToLocalTime();
            AssertSameLocalValue(local.RoundDown(TimeSpan.FromTicks(1)), local, utc);
            AssertSameLocalValue(local.RoundUp(TimeSpan.FromTicks(1)), local, utc);

            var wholeSecond = new DateTime(2024, 11, 3, utcHour, 30, 30, DateTimeKind.Utc);
            AssertSameLocalValue(local.RoundDown(TimeSpan.FromSeconds(1)), wholeSecond.ToLocalTime(), wholeSecond);
            AssertSameLocalValue(local.RoundUp(TimeSpan.FromSeconds(1)), wholeSecond.AddSeconds(1).ToLocalTime(), wholeSecond.AddSeconds(1));
            AssertSameLocalValue(local.Round(RoundTo.Second), wholeSecond.ToLocalTime(), wholeSecond);
            AssertSameLocalValue(local.AddTicks(6_000_000).Round(RoundTo.Second),
                wholeSecond.AddSeconds(1).ToLocalTime(), wholeSecond.AddSeconds(1));

            var wholeMinute = new DateTime(2024, 11, 3, utcHour, 31, 0, DateTimeKind.Utc);
            AssertSameLocalValue(local.Round(RoundTo.Minute), wholeMinute.ToLocalTime(), wholeMinute);
            var secondHour = new DateTime(2024, 11, 3, 7, 0, 0, DateTimeKind.Utc);
            AssertSameLocalValue(local.Round(RoundTo.Hour), secondHour.ToLocalTime(), secondHour);
        }
    }
    /// <summary>
    /// 同时比较钟面 ticks、Kind 和 UTC 时间，避免相等运算漏检本地 DST 标记。
    /// </summary>
    /// <param name="actual">实际值。</param>
    /// <param name="expectedLocal">预期钟面值。</param>
    /// <param name="expectedUtc">预期实际时间点。</param>
    private static void AssertSameLocalValue(DateTime actual, DateTime expectedLocal, DateTime expectedUtc)
    {
        actual.Ticks.ShouldBe(expectedLocal.Ticks);
        actual.Kind.ShouldBe(DateTimeKind.Local);
        actual.ToUniversalTime().Ticks.ShouldBe(expectedUtc.Ticks);
    }
}

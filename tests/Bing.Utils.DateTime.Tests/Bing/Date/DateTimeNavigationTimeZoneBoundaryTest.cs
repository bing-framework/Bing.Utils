using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 验证日期与周边界应用小时偏移时的跨日行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Navigation.TimeZoneBoundary")]
public class DateTimeNavigationTimeZoneBoundaryTest
{
    /// <summary>
    /// 验证日首边界应用超过一天的正偏移后跨入下一天。
    /// </summary>
    [Fact]
    public void BeginningOfDay_LargePositiveOffset_CanCrossToNextDay()
    {
        var source = new DateTime(2024, 1, 1, 8, 30, 0, DateTimeKind.Utc);

        var result = source.BeginningOfDay(30);

        result.ShouldBe(new DateTime(2024, 1, 2, 6, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// 验证日末边界应用超过一天的负偏移后跨入前一天。
    /// </summary>
    [Fact]
    public void EndOfDay_LargeNegativeOffset_CanCrossToPreviousDay()
    {
        var source = new DateTime(2024, 1, 1, 8, 30, 0, DateTimeKind.Utc);

        var result = source.EndOfDay(-30);

        result.ShouldBe(new DateTime(2023, 12, 31, 17, 59, 59, 999, DateTimeKind.Utc));
    }

    /// <summary>
    /// 验证周首边界先应用区域性周首日再计算小时偏移。
    /// </summary>
    [Fact]
    public void BeginningOfWeek_OffsetAppliedAfterWeekStart_UsesFirstDayThenAppliesOffset()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var usCulture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = usCulture;
            CultureInfo.CurrentUICulture = usCulture;

            var source = new DateTime(2024, 1, 3, 10, 0, 0, DateTimeKind.Unspecified); // Wednesday
            var result = source.BeginningOfWeek(26);

            result.ShouldBe(new DateTime(2024, 1, 1, 2, 0, 0, DateTimeKind.Unspecified));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}

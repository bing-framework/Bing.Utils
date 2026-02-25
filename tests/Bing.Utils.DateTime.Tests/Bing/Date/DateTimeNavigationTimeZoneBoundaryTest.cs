using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 测试类：覆盖 `DateTimeExtensions.Navigation` 的时区偏移边界行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Navigation.TimeZoneBoundary")]
public class DateTimeNavigationTimeZoneBoundaryTest
{
    /// <summary>
    /// 测试用例：验证 `BeginningOfDay` 在 `LargePositiveOffset` 场景下，结果为 `CanCrossToNextDay`。
    /// </summary>
    [Fact]
    public void BeginningOfDay_LargePositiveOffset_CanCrossToNextDay()
    {
        var source = new DateTime(2024, 1, 1, 8, 30, 0, DateTimeKind.Utc);

        var result = source.BeginningOfDay(30);

        result.ShouldBe(new DateTime(2024, 1, 2, 6, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// 测试用例：验证 `EndOfDay` 在 `LargeNegativeOffset` 场景下，结果为 `CanCrossToPreviousDay`。
    /// </summary>
    [Fact]
    public void EndOfDay_LargeNegativeOffset_CanCrossToPreviousDay()
    {
        var source = new DateTime(2024, 1, 1, 8, 30, 0, DateTimeKind.Utc);

        var result = source.EndOfDay(-30);

        result.ShouldBe(new DateTime(2023, 12, 31, 17, 59, 59, 999, DateTimeKind.Utc));
    }

    /// <summary>
    /// 测试用例：验证 `BeginningOfWeek` 在 `OffsetAppliedAfterWeekStart` 场景下，结果为 `UsesFirstDayThenAppliesOffset`。
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

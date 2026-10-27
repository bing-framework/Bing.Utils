using Bing.Date.Cron;

namespace Bing.Utils.Cron.Tests;

/// <summary>
/// 验证 Cron 计划解析与时间计算行为。
/// </summary>
public class CronScheduleTests
{
	/// <summary>
	/// 六段表达式保留秒字段，月末规则能跨过不同长度的月份。
	/// </summary>
	[Fact]
	public void SecondsAndMonthEnd_AreCalculatedPrecisely()
	{
		var seconds = CronSchedule.Parse("30 * * * * *", CronExpressionFormat.IncludeSeconds);
		var from = new DateTimeOffset(2024, 1, 1, 0, 0, 29, TimeSpan.Zero);
		seconds.GetNextOccurrence(from, TimeZoneInfo.Utc).ShouldBe(from.AddSeconds(1));
		var monthEnd = CronSchedule.Parse("0 0 L * *");
		var dates = monthEnd.GetOccurrences(from, from.AddMonths(3), TimeZoneInfo.Utc, 3).ToArray();
		dates.Select(x => x.Day).ShouldBe(new[] { 31, 29, 31 });
	}

	/// <summary>
	/// 预取消枚举不会计算或返回第一个匹配时间。
	/// </summary>
	[Fact]
	public void PreCanceledEnumeration_Throws()
	{
		var schedule = CronSchedule.Parse("* * * * *");
		var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		Should.Throw<OperationCanceledException>(() => schedule.GetOccurrences(from, from.AddDays(1), TimeZoneInfo.Utc, 1, canceled.Token).ToArray());
	}

	/// <summary>
	/// 验证五段与六段表达式均按指定格式解析。
	/// </summary>
	[Fact]
	public void Parse_WhenFormatMatches_ShouldCreateSchedule()
	{
		var standard = CronSchedule.Parse("0 9 * * 1-5");
		var withSeconds = CronSchedule.Parse("30 0 9 * * 1-5", CronExpressionFormat.IncludeSeconds);
		var parsedStandard = CronSchedule.TryParse("0 9 * * 1-5", out var tryParsed);
		var sixFieldsWithStandardFormat = CronSchedule.TryParse("30 0 9 * * 1-5", out _);

		standard.ShouldNotBeNull();
		withSeconds.ShouldNotBeNull();
		parsedStandard.ShouldBeTrue();
		tryParsed.ShouldNotBeNull();
		sixFieldsWithStandardFormat.ShouldBeFalse();
	}

	/// <summary>
	/// 验证非法表达式提供中文错误并让 TryParse 返回失败。
	/// </summary>
	[Fact]
	public void Parse_WhenExpressionIsInvalid_ShouldReportChineseError()
	{
		var exception = Should.Throw<FormatException>(() => CronSchedule.Parse("61 * * * *"));
		var parsed = CronSchedule.TryParse("61 * * * *", out var schedule, out var error);
		var jitterParsed = CronSchedule.TryParse("H * * * *", out _, out var jitterError);

		parsed.ShouldBeFalse();
		schedule.ShouldBeNull();
		error.ShouldContain("Cron 表达式格式无效");
		exception.Message.ShouldContain("Cron 表达式");
		jitterParsed.ShouldBeFalse();
		jitterError.ShouldContain("随机种子");
	}

	/// <summary>
	/// 验证空表达式与无效格式值在解析入口被拒绝。
	/// </summary>
	[Fact]
	public void Parse_WhenArgumentsAreInvalid_ShouldThrowArgumentExceptions()
	{
		var empty = Should.Throw<ArgumentException>(() => CronSchedule.Parse(" "));
		var invalidFormat = Should.Throw<ArgumentOutOfRangeException>(() => CronSchedule.Parse("* * * * *", (CronExpressionFormat)99));
		var tryParseInvalidFormat = Should.Throw<ArgumentOutOfRangeException>(() => CronSchedule.TryParse("* * * * *", out _, (CronExpressionFormat)99));

		empty.Message.ShouldContain("不能为空");
		invalidFormat.ParamName.ShouldBe("format");
		tryParseInvalidFormat.ParamName.ShouldBe("format");
		CronSchedule.TryParse(null, out var schedule, out var error).ShouldBeFalse();
		schedule.ShouldBeNull();
		error.ShouldContain("不能为空");
	}

	/// <summary>
	/// 验证日字段和星期字段同时出现时使用 AND 匹配。
	/// </summary>
	[Fact]
	public void GetNextOccurrence_WhenDayAndWeekAreBothSpecified_ShouldRequireBoth()
	{
		var schedule = CronSchedule.Parse("0 0 1 * 1");
		var from = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);

		var next = schedule.GetNextOccurrence(from, TimeZoneInfo.Utc);

		next.ShouldBe(new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero));
	}

	/// <summary>
	/// 验证闰日表达式会跳过非闰年并返回 UTC 结果。
	/// </summary>
	[Fact]
	public void GetNextOccurrence_WhenLeapDayIsRequested_ShouldReturnNextLeapDayInUtc()
	{
		var schedule = CronSchedule.Parse("0 0 29 2 *");
		var from = new DateTimeOffset(2023, 3, 1, 8, 0, 0, TimeSpan.FromHours(8));

		var next = schedule.GetNextOccurrence(from, TimeZoneInfo.Utc);

		next.ShouldBe(new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero));
		next.Value.Offset.ShouldBe(TimeSpan.Zero);
	}

	/// <summary>
	/// 验证 inclusive 控制起点匹配且默认结果按 UTC 表示。
	/// </summary>
	[Fact]
	public void GetNextOccurrence_WhenStartIsAnOccurrence_ShouldHonorInclusive()
	{
		var schedule = CronSchedule.Parse("0 * * * *");
		var from = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.FromHours(9));

		var exclusive = schedule.GetNextOccurrence(from, TimeZoneInfo.Utc);
		var inclusive = schedule.GetNextOccurrence(from, TimeZoneInfo.Utc, inclusive: true);

		exclusive.ShouldBe(new DateTimeOffset(2024, 1, 1, 1, 0, 0, TimeSpan.Zero));
		inclusive.ShouldBe(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
	}

	/// <summary>
	/// 验证无可达日期的表达式返回空结果。
	/// </summary>
	[Fact]
	public void GetNextOccurrence_WhenScheduleHasNoMatchingDate_ShouldReturnNull()
	{
		var schedule = CronSchedule.Parse("0 0 31 2 *");

		var next = schedule.GetNextOccurrence(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);

		next.ShouldBeNull();
	}

	/// <summary>
	/// 验证区间枚举包含起点、排除终点并遵守结果上限。
	/// </summary>
	[Fact]
	public void GetOccurrences_WhenRangeAndLimitAreProvided_ShouldReturnBoundedHalfOpenResults()
	{
		var schedule = CronSchedule.Parse("* * * * *");
		var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var to = from.AddMinutes(5);

		var occurrences = schedule.GetOccurrences(from, to, TimeZoneInfo.Utc, maxCount: 3).ToArray();
		var allInRange = schedule.GetOccurrences(from, to, TimeZoneInfo.Utc, maxCount: 10).ToArray();

		occurrences.ShouldBe(new[] { from, from.AddMinutes(1), from.AddMinutes(2) });
		allInRange.ShouldBe(new[] { from, from.AddMinutes(1), from.AddMinutes(2), from.AddMinutes(3), from.AddMinutes(4) });
		occurrences.All(item => item.Offset == TimeSpan.Zero).ShouldBeTrue();
		schedule.GetOccurrences(from, from, TimeZoneInfo.Utc, maxCount: 3).ShouldBeEmpty();
	}

	/// <summary>
	/// 验证区间参数在调用时完成检查。
	/// </summary>
	[Fact]
	public void GetOccurrences_WhenArgumentsAreInvalid_ShouldThrowImmediately()
	{
		var schedule = CronSchedule.Parse("* * * * *");
		var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

		Should.Throw<ArgumentNullException>(() => schedule.GetOccurrences(from, from, null, 1));
		Should.Throw<ArgumentOutOfRangeException>(() => schedule.GetOccurrences(from, from, TimeZoneInfo.Utc, 0));
		Should.Throw<ArgumentException>(() => schedule.GetOccurrences(from.AddMinutes(1), from, TimeZoneInfo.Utc, 1));
		Should.Throw<ArgumentNullException>(() => schedule.GetNextOccurrence(from, null));
	}

	/// <summary>
	/// 验证枚举过程中的取消会抛出取消异常。
	/// </summary>
	[Fact]
	public void GetOccurrences_WhenCancelledDuringEnumeration_ShouldThrowOperationCanceledException()
	{
		var schedule = CronSchedule.Parse("* * * * *");
		var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
		using var cancellation = new CancellationTokenSource();
		using var enumerator = schedule.GetOccurrences(from, from.AddHours(1), TimeZoneInfo.Utc, 100, cancellation.Token).GetEnumerator();

		enumerator.MoveNext().ShouldBeTrue();
		cancellation.Cancel();
		Should.Throw<OperationCanceledException>(() => enumerator.MoveNext());
	}

	/// <summary>
	/// 验证时区跳时会按 Cronos 规则调整到第一个有效时刻。
	/// </summary>
	[Fact]
	public void GetNextOccurrence_WhenLocalTimeIsSkipped_ShouldAdjustToTransitionStart()
	{
		var schedule = CronSchedule.Parse("30 2 * * *");
		var timeZone = CreateTransitionTimeZone();
		var from = new DateTimeOffset(2024, 3, 10, 6, 0, 0, TimeSpan.Zero);

		var next = schedule.GetNextOccurrence(from, timeZone);

		next.ShouldBe(new DateTimeOffset(2024, 3, 10, 7, 0, 0, TimeSpan.Zero));
	}

	/// <summary>
	/// 验证秋季回拨期间间隔表达式会保留两个本地重复时刻。
	/// </summary>
	[Fact]
	public void GetOccurrences_WhenLocalTimeRepeats_ShouldPreserveIntervalOccurrences()
	{
		var schedule = CronSchedule.Parse("*/30 * * * *");
		var timeZone = CreateTransitionTimeZone();
		var from = new DateTimeOffset(2024, 11, 3, 4, 59, 0, TimeSpan.Zero);
		var to = new DateTimeOffset(2024, 11, 3, 7, 1, 0, TimeSpan.Zero);

		var occurrences = schedule.GetOccurrences(from, to, timeZone, maxCount: 6).ToArray();

		occurrences.ShouldBe(new[]
		{
			new DateTimeOffset(2024, 11, 3, 5, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2024, 11, 3, 5, 30, 0, TimeSpan.Zero),
			new DateTimeOffset(2024, 11, 3, 6, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2024, 11, 3, 6, 30, 0, TimeSpan.Zero),
			new DateTimeOffset(2024, 11, 3, 7, 0, 0, TimeSpan.Zero)
		});
	}

	/// <summary>
	/// 创建只在 2024 年实施固定日期夏令时规则的测试时区。
	/// </summary>
	/// <returns>春季跳时和秋季回拨均为一小时的时区。</returns>
	private static TimeZoneInfo CreateTransitionTimeZone()
	{
		var daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 10);
		var daylightEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 3);
		var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31),
			TimeSpan.FromHours(1),
			daylightStart,
			daylightEnd);

		return TimeZoneInfo.CreateCustomTimeZone(
			"CronScheduleTests/2024-DST",
			TimeSpan.FromHours(-5),
			"CronSchedule test time zone",
			"Standard",
			"Daylight",
			new[] { rule });
	}
}

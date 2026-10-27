using System;
using System.Collections.Generic;
using System.Threading;
using Cronos;

namespace Bing.Date.Cron;

/// <summary>
/// 表示可按指定时区计算触发时刻的 Cron 计划。
/// </summary>
/// <remarks>
/// 计划支持五段或六段格式；日期中的月日和星期同时指定时按 AND 规则匹配，并沿用 Cronos 的夏令时跳时与回拨语义。
/// </remarks>
public sealed class CronSchedule
{
	/// <summary>
	/// 保存经过验证的 Cron 表达式。
	/// </summary>
	private readonly CronExpression _expression;

	/// <summary>
	/// 使用已解析的表达式创建计划。
	/// </summary>
	/// <param name="expression">Cronos 解析后的表达式。</param>
	private CronSchedule(CronExpression expression)
	{
		_expression = expression;
	}

	/// <summary>
	/// 解析 Cron 表达式并创建计划。
	/// </summary>
	/// <param name="expression">五段或六段 Cron 表达式文本。</param>
	/// <param name="format">期望的字段格式。</param>
	/// <returns>已解析的 Cron 计划。</returns>
	/// <exception cref="ArgumentException">表达式为空。</exception>
	/// <exception cref="FormatException">表达式格式无效或包含未提供随机种子的 H 字段。</exception>
	/// <exception cref="ArgumentOutOfRangeException">格式枚举值无效。</exception>
	public static CronSchedule Parse(string expression, CronExpressionFormat format = CronExpressionFormat.Standard)
	{
		ValidateFormat(format);
		if (string.IsNullOrWhiteSpace(expression))
			throw new ArgumentException("Cron 表达式不能为空。", nameof(expression));

		try
		{
			return new CronSchedule(CronExpression.Parse(expression, ToCronFormat(format)));
		}
		catch (CronFormatException exception)
		{
			throw new FormatException("Cron 表达式格式无效，请检查字段数量和字段取值。", exception);
		}
		catch (MissingSeedException exception)
		{
			throw new FormatException("Cron 表达式包含 H 字段，但当前接口未提供随机种子。", exception);
		}
	}

	/// <summary>
	/// 尝试解析 Cron 表达式并创建计划。
	/// </summary>
	/// <param name="expression">五段或六段 Cron 表达式文本。</param>
	/// <param name="schedule">成功时返回解析后的计划，否则为 <see langword="null"/>。</param>
	/// <param name="format">期望的字段格式。</param>
	/// <returns>解析成功时为 <see langword="true"/>；表达式为空或格式无效时为 <see langword="false"/>。</returns>
	/// <exception cref="ArgumentOutOfRangeException">格式枚举值无效。</exception>
	public static bool TryParse(string expression, out CronSchedule schedule, CronExpressionFormat format = CronExpressionFormat.Standard)
	{
		return TryParse(expression, out schedule, out _, format);
	}

	/// <summary>
	/// 尝试解析 Cron 表达式并返回中文错误说明。
	/// </summary>
	/// <param name="expression">五段或六段 Cron 表达式文本。</param>
	/// <param name="schedule">成功时返回解析后的计划，否则为 <see langword="null"/>。</param>
	/// <param name="error">失败时返回中文错误说明，成功时为 <see langword="null"/>。</param>
	/// <param name="format">期望的字段格式。</param>
	/// <returns>解析成功时为 <see langword="true"/>；表达式为空或格式无效时为 <see langword="false"/>。</returns>
	/// <exception cref="ArgumentOutOfRangeException">格式枚举值无效。</exception>
	public static bool TryParse(string expression, out CronSchedule schedule, out string error, CronExpressionFormat format = CronExpressionFormat.Standard)
	{
		ValidateFormat(format);
		schedule = null;
		error = null;

		if (string.IsNullOrWhiteSpace(expression))
		{
			error = "Cron 表达式不能为空。";
			return false;
		}

		try
		{
			schedule = new CronSchedule(CronExpression.Parse(expression, ToCronFormat(format)));
			return true;
		}
		catch (CronFormatException)
		{
			error = "Cron 表达式格式无效，请检查字段数量和字段取值。";
			return false;
		}
		catch (MissingSeedException)
		{
			error = "Cron 表达式包含 H 字段，但当前接口未提供随机种子。";
			return false;
		}
	}

	/// <summary>
	/// 获取给定时刻之后或包含该时刻的下一次触发时间。
	/// </summary>
	/// <param name="from">搜索起点，按其 UTC 时间点进行比较。</param>
	/// <param name="timeZone">解释表达式本地字段的时区。</param>
	/// <param name="inclusive">为 <see langword="true"/> 时允许起点本身作为匹配结果。</param>
	/// <returns>下一次触发时间，结果始终使用 UTC；不存在后续匹配时返回 <see langword="null"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="timeZone"/> 为 <see langword="null"/>。</exception>
	public DateTimeOffset? GetNextOccurrence(DateTimeOffset from, TimeZoneInfo timeZone, bool inclusive = false)
	{
		if (timeZone == null)
			throw new ArgumentNullException(nameof(timeZone), "时区不能为空。");

		var occurrence = _expression.GetNextOccurrence(from, timeZone, inclusive);
		return occurrence?.ToUniversalTime();
	}

	/// <summary>
	/// 按时间顺序枚举指定区间内的触发时间。
	/// </summary>
	/// <param name="from">包含的时间区间起点。</param>
	/// <param name="to">不包含的时间区间终点。</param>
	/// <param name="timeZone">解释表达式本地字段的时区。</param>
	/// <param name="maxCount">最多返回的触发次数，必须大于零。</param>
	/// <param name="cancellationToken">用于取消枚举的令牌。</param>
	/// <returns>按 UTC 升序排列的触发时间序列。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="timeZone"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> 不大于零。</exception>
	/// <exception cref="ArgumentException"><paramref name="from"/> 晚于 <paramref name="to"/>。</exception>
	/// <exception cref="OperationCanceledException">枚举期间取消令牌已取消。</exception>
	public IEnumerable<DateTimeOffset> GetOccurrences(
		DateTimeOffset from,
		DateTimeOffset to,
		TimeZoneInfo timeZone,
		int maxCount,
		CancellationToken cancellationToken = default)
	{
		if (timeZone == null)
			throw new ArgumentNullException(nameof(timeZone), "时区不能为空。");
		if (maxCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "最多返回次数必须大于零。");
		if (from > to)
			throw new ArgumentException("区间起点不能晚于终点。", nameof(from));

		return EnumerateOccurrences(from, to, timeZone, maxCount, cancellationToken);
	}

	/// <summary>
	/// 在枚举期间惰性计算有界区间内的匹配结果。
	/// </summary>
	/// <param name="from">包含的时间区间起点。</param>
	/// <param name="to">不包含的时间区间终点。</param>
	/// <param name="timeZone">解释表达式本地字段的时区。</param>
	/// <param name="maxCount">最多返回的触发次数。</param>
	/// <param name="cancellationToken">用于取消枚举的令牌。</param>
	/// <returns>按 UTC 升序排列的触发时间序列。</returns>
	private IEnumerable<DateTimeOffset> EnumerateOccurrences(
		DateTimeOffset from,
		DateTimeOffset to,
		TimeZoneInfo timeZone,
		int maxCount,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (from == to)
			yield break;

		var occurrence = _expression.GetNextOccurrence(from, timeZone, inclusive: true);
		cancellationToken.ThrowIfCancellationRequested();
		var count = 0;
		while (occurrence.HasValue && occurrence.Value < to && count < maxCount)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return occurrence.Value.ToUniversalTime();
			count++;
			if (count == maxCount)
				yield break;

			cancellationToken.ThrowIfCancellationRequested();
			occurrence = _expression.GetNextOccurrence(occurrence.Value, timeZone, inclusive: false);
			cancellationToken.ThrowIfCancellationRequested();
		}
	}

	/// <summary>
	/// 验证调用方提供的 Cron 字段格式。
	/// </summary>
	/// <param name="format">需要验证的格式值。</param>
	private static void ValidateFormat(CronExpressionFormat format)
	{
		if (!Enum.IsDefined(typeof(CronExpressionFormat), format))
			throw new ArgumentOutOfRangeException(nameof(format), format, "不支持的 Cron 表达式格式。");
	}

	/// <summary>
	/// 将公开格式映射为 Cronos 的字段格式。
	/// </summary>
	/// <param name="format">已验证的公开格式值。</param>
	/// <returns>对应的 Cronos 格式。</returns>
	private static CronFormat ToCronFormat(CronExpressionFormat format)
	{
		return format == CronExpressionFormat.IncludeSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard;
	}
}

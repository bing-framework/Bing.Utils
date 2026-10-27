namespace Bing.Date.Cron;

/// <summary>
/// Cron 表达式支持的字段格式。
/// </summary>
public enum CronExpressionFormat
{
	/// <summary>
	/// 五段标准格式，依次表示分、时、日、月和星期。
	/// </summary>
	Standard,

	/// <summary>
	/// 六段格式，在标准字段前增加秒字段。
	/// </summary>
	IncludeSeconds
}

using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 时间输出帮助类
/// </summary>
public static class DateTimeOutputHelper
{
    /// <summary>
    /// 如果条件成立则A，不成立则B
    /// </summary>
    /// <param name="condition">条件</param>
    /// <param name="format1">格式化1</param>
    /// <param name="format2">格式化2</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string IfTtt(bool condition, string format1, string format2) => condition ? format1 : format2;
}

/// <summary>
/// 时间输出 扩展
/// </summary>
public static class DateTimeOutputExtensions
{
    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="dt">日期时间</param>
    /// <param name="styles">日期时间输出样式</param>
    /// <param name="isRemoveSecond">是否移除秒。true:是,false:否</param>
    public static string ToString(this DateTime dt, DateTimeOutputStyles styles, bool isRemoveSecond = false) =>
        styles switch
        {
            DateTimeOutputStyles.DateTime => dt.ToString(DateTimeOutputHelper.IfTtt(isRemoveSecond, "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss")),
            DateTimeOutputStyles.Date => dt.ToString("yyyy-MM-dd"),
            DateTimeOutputStyles.Time => dt.ToString(DateTimeOutputHelper.IfTtt(isRemoveSecond, "HH:mm", "HH:mm:ss")),
            DateTimeOutputStyles.LongDate => dt.ToLongDateString(),
            DateTimeOutputStyles.LongTime => dt.ToLongTimeString(),
            DateTimeOutputStyles.ShortDate => dt.ToShortDateString(),
            DateTimeOutputStyles.ShortTime => dt.ToShortTimeString(),
            DateTimeOutputStyles.Millisecond => dt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            _ => dt.ToString(CultureInfo.InvariantCulture)
        };

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="dt">日期时间</param>
    /// <param name="styles">日期时间输出样式</param>
    /// <param name="isRemoveSecond">是否移除秒。true:是,false:否</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString(this DateTime? dt, DateTimeOutputStyles styles, bool isRemoveSecond = false) => dt is null ? string.Empty : dt.Value.ToString(styles, isRemoveSecond);

    /// <summary>
    /// 将时间转换为时间点
    /// </summary>
    /// <param name="localDateTime">DateTime</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToDateTimeOffset(this DateTime localDateTime) => localDateTime.ToDateTimeOffset(null);

    /// <summary>
    /// 将时间转换为时间点
    /// </summary>
    /// <param name="localDateTime">DateTime</param>
    /// <param name="localTimeZone">时区</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToDateTimeOffset(this DateTime localDateTime, TimeZoneInfo localTimeZone)
    {
        if (localDateTime.Kind != DateTimeKind.Unspecified)
            localDateTime = new DateTime(localDateTime.Ticks, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTime(localDateTime, localTimeZone ?? TimeZoneInfo.Local);
    }

    /// <summary>
    /// 将时间点转换为时间
    /// </summary>
    /// <param name="dateTimeUtc">DateTimeOffset</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public static DateTime ToLocalDateTime(this DateTimeOffset dateTimeUtc) => dateTimeUtc.ToLocalDateTime(null);

    /// <summary>
    /// 将时间点转换为时间
    /// </summary>
    /// <param name="dateTimeUtc">DateTimeOffset</param>
    /// <param name="localTimeZone">时区</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public static DateTime ToLocalDateTime(this DateTimeOffset dateTimeUtc, TimeZoneInfo localTimeZone) => TimeZoneInfo.ConvertTime(dateTimeUtc, localTimeZone ?? TimeZoneInfo.Local).DateTime;
}
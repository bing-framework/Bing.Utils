using Bing.Date;
using Bing.Extensions;

namespace Bing.Helpers;

/// <summary>
/// 时间操作
/// </summary>
public static partial class Time
{
    /// <summary>
    /// 日期
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static readonly AsyncLocal<DateTime?> _dateTime = new();

    /// <summary>
    /// 是否使用Utc日期
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static readonly AsyncLocal<bool?> _isUseUtc = new();

    /// <summary>
    /// 是否使用Utc日期
    /// </summary>
    private static bool IsUseUtc => _isUseUtc.Value != null ? _isUseUtc.Value.SafeValue() : TimeOptions.IsUseUtc;

    /// <summary>
    /// 获取当前日期时间
    /// </summary>
    public static DateTime Now
    {
        get
        {
            if (_dateTime.Value != null)
                return _dateTime.Value.Value;
            return IsUseUtc ? DateTime.UtcNow : DateTime.Now;
        }
    }

    /// <summary>
    /// 设置时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    public static void SetTime(DateTime? dateTime) => _dateTime.Value = dateTime;

    /// <summary>
    /// 设置时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    public static void SetTime(string dateTime) => SetTime(Conv.ToDateOrNull(dateTime));

    /// <summary>
    /// 设置使用Utc日期
    /// </summary>
    /// <param name="isUseUtc">是否使用Utc日期。默认值：true</param>
    public static void UseUtc(bool? isUseUtc = true) => _isUseUtc.Value = isUseUtc;

    /// <summary>
    /// 重置时间和Utc标志
    /// </summary>
    public static void Reset()
    {
        _dateTime.Value = null;
        _isUseUtc.Value = null;
    }

    /// <summary>
    /// 转换为标准化日期
    /// </summary>
    /// <param name="date">日期</param>
    public static DateTime? Normalize(DateTime? date)
    {
        if (date == null)
            return null;
        return Normalize(date.Value);
    }

    /// <summary>
    /// 转换为标准化日期
    /// </summary>
    /// <param name="date">日期</param>
    public static DateTime Normalize(DateTime date)
    {
        if (IsUseUtc)
            return ToUniversalTime(date);
        return ToLocalTime(date);
    }

    /// <summary>
    /// 转换为UTC日期
    /// </summary>
    /// <param name="date">日期</param>
    public static DateTime ToUniversalTime(DateTime date)
    {
        if (date == DateTime.MinValue)
            return DateTime.MinValue;
        switch (date.Kind)
        {
            case DateTimeKind.Local:
                return date.ToUniversalTime();
            case DateTimeKind.Unspecified:
                return DateTime.SpecifyKind(date, DateTimeKind.Local).ToUniversalTime();
            default:
                return date;
        }
    }

    /// <summary>
    /// 转换为本地日期
    /// </summary>
    /// <param name="date">日期</param>
    public static DateTime ToLocalTime(DateTime date)
    {
        if (date == DateTime.MinValue)
            return DateTime.MinValue;
        switch (date.Kind)
        {
            case DateTimeKind.Utc:
                return date.ToLocalTime();
            case DateTimeKind.Unspecified:
                return DateTime.SpecifyKind(date, DateTimeKind.Local);
            default:
                return date;
        }
    }

    /// <summary>
    /// Utc日期转换为本地化日期
    /// </summary>
    /// <param name="date">日期</param>
    public static DateTime UtcToLocalTime(DateTime date)
    {
        if (date == DateTime.MinValue)
            return DateTime.MinValue;
        if (date.Kind == DateTimeKind.Utc)
            return date.ToLocalTime();
        if (date.Kind == DateTimeKind.Local)
            return date;
        if (IsUseUtc)
            return DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();
        return date;
    }

    /// <summary>
    /// 获取当前日期时间
    /// </summary>
    public static DateTime GetDateTime() => _dateTime.Value ?? DateTime.Now;

    /// <summary>
    /// 获取当前日期，不带时间
    /// </summary>
    public static DateTime GetDate() => GetDateTime().Date;

    /// <summary>
    /// 获取Unix时间戳
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetUnixTimestamp() => GetUnixTimestamp(DateTime.Now);

    /// <summary>
    /// 获取Unix时间戳
    /// </summary>
    /// <param name="time">时间</param>
    /// <remarks>当前时间必须是 <see cref="TimeZoneInfo.Local"/></remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetUnixTimestamp(DateTime time) => ToEpochSecond(time);

    /// <summary>
    /// 从Unix时间戳获取时间
    /// </summary>
    /// <param name="timestamp">Unix时间戳</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetTimeFromUnixTimestamp(long timestamp) => OfEpochSecond(timestamp);

    /// <summary>
    /// 将 UNIX 时间戳（以秒为单位）转换为本地时间 <see cref="DateTime"/> 对象。
    /// </summary>
    /// <param name="epochSecond">UNIX 时间戳（以秒为单位）。</param>
    /// <returns>本地时间 <see cref="DateTime"/> 对象。</returns>
    public static DateTime OfEpochSecond(long epochSecond)
    {
        var ticks = TimeOptions.UnixEpochTicks + epochSecond * TimeOptions.TicksPerSec;
        return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
    }

    /// <summary>
    /// 将 UNIX 时间戳（以毫秒为单位）转换为本地时间 <see cref="DateTime"/> 对象。
    /// </summary>
    /// <param name="epochMilli">UNIX 时间戳（以毫秒为单位）。</param>
    /// <returns>本地时间 <see cref="DateTime"/> 对象。</returns>
    public static DateTime OfEpochMilli(long epochMilli)
    {
        var ticks = TimeOptions.UnixEpochTicks + epochMilli * TimeOptions.TicksPreMillisecond;
        return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
    }

    /// <summary>
    /// 将 <see cref="DateTime"/> 对象转换为 UNIX 时间戳（以秒为单位）。
    /// </summary>
    /// <param name="datetime">要转换的 <see cref="DateTime"/> 对象。</param>
    /// <returns>UNIX 时间戳（毫秒为单位）。</returns>
    public static long ToEpochSecond(DateTime datetime)=> (datetime.ToUniversalTime().Ticks - TimeOptions.UnixEpochTicks) / TimeOptions.TicksPerSec;

    /// <summary>
    /// 将 <see cref="DateTime"/> 对象转换为 UNIX 时间戳（以毫秒为单位）。
    /// </summary>
    /// <param name="datetime">要转换的 <see cref="DateTime"/> 对象。</param>
    /// <returns>UNIX 时间戳（以毫秒为单位）。</returns>
    public static long ToEpochMilli(DateTime datetime)=> (datetime.ToUniversalTime().Ticks - TimeOptions.UnixEpochTicks) / TimeOptions.TicksPreMillisecond;
}
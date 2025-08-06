using Bing.Date;
using Bing.Extensions;

namespace Bing.Helpers;

/// <summary>
/// 时间操作
/// </summary>
public static partial class Time
{
    /// <summary>
    /// 异步本地存储的日期时间值
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static readonly AsyncLocal<DateTime?> _dateTime = new();

    /// <summary>
    /// 异步本地存储的UTC时间使用标志
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static readonly AsyncLocal<bool?> _isUseUtc = new();

    /// <summary>
    /// 获取当前是否使用UTC时间的配置
    /// </summary>
    private static bool IsUseUtc => _isUseUtc.Value != null ? _isUseUtc.Value.SafeValue() : TimeOptions.IsUseUtc;

    /// <summary>
    /// 获取当前日期时间，支持模拟时间和UTC时间配置
    /// </summary>
    /// <returns>
    /// 如果设置了模拟时间，返回模拟时间；
    /// 否则根据UTC配置返回当前UTC时间或本地时间
    /// </returns>
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
    /// 设置模拟时间，用于测试场景
    /// </summary>
    /// <param name="dateTime">要设置的模拟时间，为null时清除模拟时间</param>
    /// <remarks>
    /// 设置后，Time.Now属性将返回此模拟时间而不是系统当前时间。
    /// 在单元测试中特别有用，可以模拟特定的时间点进行测试。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 设置模拟时间
    /// Time.SetTime(new DateTime(2023, 1, 1, 12, 0, 0));
    /// var now = Time.Now; // 返回 2023-01-01 12:00:00
    /// 
    /// // 清除模拟时间
    /// Time.SetTime(null);
    /// var realNow = Time.Now; // 返回真实的当前时间
    /// </code>
    /// </example>
    public static void SetTime(DateTime? dateTime) => _dateTime.Value = dateTime;

    /// <summary>
    /// 通过字符串设置模拟时间
    /// </summary>
    /// <param name="dateTime">日期时间字符串，支持多种格式</param>
    /// <exception cref="ArgumentException">当日期时间字符串格式无效时抛出</exception>
    /// <remarks>
    /// 内部使用Conv.ToDateOrNull进行字符串解析，支持常见的日期时间格式。
    /// 如果解析失败，将设置为null（清除模拟时间）。
    /// </remarks>
    /// <example>
    /// <code>
    /// Time.SetTime("2023-01-01 12:00:00");
    /// Time.SetTime("2023/1/1 12:00");
    /// Time.SetTime("Jan 1, 2023 12:00 PM");
    /// </code>
    /// </example>
    public static void SetTime(string dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime))
        {
            SetTime((DateTime?)null);
            return;
        }

        var parsedDate = Conv.ToDateOrNull(dateTime);
        if (parsedDate == null)
            throw new ArgumentException($"无效的日期时间格式: {dateTime}", nameof(dateTime));

        SetTime(parsedDate);
    }

    /// <summary>
    /// 设置是否使用UTC时间
    /// </summary>
    /// <param name="isUseUtc">是否使用UTC时间，null表示使用全局配置，true表示使用UTC时间，false表示使用本地时间</param>
    /// <remarks>
    /// 此设置会影响Time.Now属性的返回值（当未设置模拟时间时）。
    /// 设置为null时，将使用TimeOptions.IsUseUtc的全局配置。
    /// </remarks>
    /// <example>
    /// <code>
    /// Time.UseUtc(true);   // 使用UTC时间
    /// Time.UseUtc(false);  // 使用本地时间
    /// Time.UseUtc(null);   // 使用全局配置
    /// </code>
    /// </example>
    public static void UseUtc(bool? isUseUtc = true) => _isUseUtc.Value = isUseUtc;

    /// <summary>
    /// 重置所有时间配置，清除模拟时间和UTC时间设置
    /// </summary>
    /// <remarks>
    /// 调用此方法后，Time.Now将返回系统当前时间，
    /// UTC时间配置将恢复为全局默认值。
    /// 通常在测试结束后调用以清理测试环境。
    /// </remarks>
    public static void Reset()
    {
        _dateTime.Value = null;
        _isUseUtc.Value = null;
    }

    /// <summary>
    /// 将日期时间标准化为配置的时区格式
    /// </summary>
    /// <param name="date">要标准化的可空日期时间</param>
    /// <returns>标准化后的日期时间，如果输入为null则返回null</returns>
    /// <remarks>
    /// 根据当前的UTC配置，将输入日期转换为相应的时区格式。
    /// 如果配置为使用UTC时间，则转换为UTC时间；否则转换为本地时间。
    /// </remarks>
    public static DateTime? Normalize(DateTime? date)
    {
        if (date == null)
            return null;
        return Normalize(date.Value);
    }

    /// <summary>
    /// 将日期时间标准化为配置的时区格式
    /// </summary>
    /// <param name="date">要标准化的日期时间</param>
    /// <returns>标准化后的日期时间</returns>
    /// <remarks>
    /// 根据当前的UTC配置，将输入日期转换为相应的时区格式。
    /// 如果配置为使用UTC时间，则转换为UTC时间；否则转换为本地时间。
    /// </remarks>
    public static DateTime Normalize(DateTime date)
    {
        if (IsUseUtc)
            return ToUniversalTime(date);
        return ToLocalTime(date);
    }

    /// <summary>
    /// 将日期时间转换为UTC时间
    /// </summary>
    /// <param name="date">要转换的日期时间</param>
    /// <returns>转换后的UTC时间</returns>
    /// <remarks>
    /// 根据输入日期的DateTimeKind属性进行相应的转换：
    /// - Local: 转换为UTC时间
    /// - Unspecified: 假定为本地时间并转换为UTC时间  
    /// - Utc: 直接返回原值
    /// - DateTime.MinValue: 直接返回以避免转换异常
    /// </remarks>
    /// <example>
    /// <code>
    /// var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
    /// var utcTime = Time.ToUniversalTime(localTime); // 转换为UTC时间
    /// 
    /// var unspecifiedTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
    /// var utcTime2 = Time.ToUniversalTime(unspecifiedTime); // 假定为本地时间并转换
    /// </code>
    /// </example>
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
    /// 将日期时间转换为本地时间
    /// </summary>
    /// <param name="date">要转换的日期时间</param>
    /// <returns>转换后的本地时间</returns>
    /// <remarks>
    /// 根据输入日期的DateTimeKind属性进行相应的转换：
    /// - Utc: 转换为本地时间
    /// - Unspecified: 假定为本地时间并设置正确的Kind
    /// - Local: 直接返回原值
    /// - DateTime.MinValue: 直接返回以避免转换异常
    /// </remarks>
    /// <example>
    /// <code>
    /// var utcTime = new DateTime(2023, 1, 1, 4, 0, 0, DateTimeKind.Utc);
    /// var localTime = Time.ToLocalTime(utcTime); // 转换为本地时间
    /// 
    /// var unspecifiedTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
    /// var localTime2 = Time.ToLocalTime(unspecifiedTime); // 设置为本地时间Kind
    /// </code>
    /// </example>
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
    /// 将UTC日期时间转换为本地时间
    /// </summary>
    /// <param name="date">要转换的日期时间</param>
    /// <returns>转换后的本地时间</returns>
    /// <remarks>
    /// 这是一个更智能的UTC到本地时间转换方法：
    /// - 如果输入已经是本地时间，直接返回
    /// - 如果输入是UTC时间，转换为本地时间
    /// - 如果输入是未指定类型且当前配置使用UTC，则假定为UTC时间并转换
    /// - 其他情况直接返回原值
    /// </remarks>
    /// <example>
    /// <code>
    /// var utcTime = new DateTime(2023, 1, 1, 4, 0, 0, DateTimeKind.Utc);
    /// var localTime = Time.UtcToLocalTime(utcTime); // 转换为本地时间
    /// 
    /// var localTime2 = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
    /// var result = Time.UtcToLocalTime(localTime2); // 直接返回，不做转换
    /// </code>
    /// </example>
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
    /// 获取当前日期时间（兼容性方法）
    /// </summary>
    /// <returns>当前日期时间</returns>
    /// <remarks>
    /// 此方法与Now属性功能相同，主要用于向后兼容。
    /// 如果设置了模拟时间，返回模拟时间；否则返回系统当前时间。
    /// 建议优先使用Time.Now属性。
    /// </remarks>
    [Obsolete("建议使用 Time.Now 属性替代此方法")]
    public static DateTime GetDateTime() => _dateTime.Value ?? DateTime.Now;

    /// <summary>
    /// 获取当前日期，不包含时间部分
    /// </summary>
    /// <returns>当前日期（时间部分为00:00:00）</returns>
    /// <remarks>
    /// 基于Time.Now获取当前日期时间，然后通过Date属性获取日期部分。
    /// 如果设置了模拟时间，将基于模拟时间获取日期。
    /// </remarks>
    /// <example>
    /// <code>
    /// var today = Time.GetDate(); // 例如：2023-01-01 00:00:00
    /// </code>
    /// </example>
    public static DateTime GetDate() => GetDateTime().Date;

    /// <summary>
    /// 获取当前时间的Unix时间戳（秒）
    /// </summary>
    /// <returns>自1970年1月1日UTC时间以来的秒数</returns>
    /// <remarks>
    /// 基于当前本地时间计算Unix时间戳。内部调用ToEpochSecond方法进行转换。
    /// </remarks>
    /// <example>
    /// <code>
    /// long timestamp = Time.GetUnixTimestamp();
    /// Console.WriteLine(timestamp); // 例如：1640995200
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetUnixTimestamp() => GetUnixTimestamp(DateTime.Now);

    /// <summary>
    /// 获取指定时间的Unix时间戳（秒）
    /// </summary>
    /// <param name="time">要转换的时间</param>
    /// <returns>自1970年1月1日UTC时间以来的秒数</returns>
    /// <remarks>
    /// 将指定的日期时间转换为Unix时间戳。输入时间必须是本地时间或可以转换为UTC时间的格式。
    /// </remarks>
    /// <example>
    /// <code>
    /// var dateTime = new DateTime(2023, 1, 1, 12, 0, 0);
    /// long timestamp = Time.GetUnixTimestamp(dateTime);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetUnixTimestamp(DateTime time) => ToEpochSecond(time);

    /// <summary>
    /// 从Unix时间戳获取对应的日期时间
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>对应的本地日期时间</returns>
    /// <remarks>
    /// 将Unix时间戳转换为本地日期时间。内部调用OfEpochSecond方法进行转换。
    /// </remarks>
    /// <example>
    /// <code>
    /// long timestamp = 1640995200;
    /// DateTime dateTime = Time.GetTimeFromUnixTimestamp(timestamp);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetTimeFromUnixTimestamp(long timestamp) => OfEpochSecond(timestamp);

    /// <summary>
    /// 将Unix时间戳（秒）转换为本地时间
    /// </summary>
    /// <param name="epochSecond">Unix时间戳（秒）</param>
    /// <returns>对应的本地日期时间</returns>
    /// <remarks>
    /// Unix时间戳是自1970年1月1日00:00:00 UTC以来的秒数。
    /// 此方法将时间戳转换为UTC时间，然后转换为本地时间。
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">当时间戳超出DateTime支持的范围时抛出</exception>
    /// <example>
    /// <code>
    /// DateTime dateTime = Time.OfEpochSecond(1640995200);
    /// // 返回对应的本地时间，例如：2022-01-01 00:00:00
    /// </code>
    /// </example>
    public static DateTime OfEpochSecond(long epochSecond)
    {
        try
        {
            var ticks = TimeOptions.UnixEpochTicks + epochSecond * TimeOptions.TicksPerSec;
            return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentOutOfRangeException($"Unix时间戳超出支持范围: {epochSecond}", ex);
        }
    }

    /// <summary>
    /// 将Unix时间戳（毫秒）转换为本地时间
    /// </summary>
    /// <param name="epochMilli">Unix时间戳（毫秒）</param>
    /// <returns>对应的本地日期时间</returns>
    /// <remarks>
    /// Unix时间戳（毫秒）是自1970年1月1日00:00:00 UTC以来的毫秒数。
    /// 此方法将时间戳转换为UTC时间，然后转换为本地时间。
    /// 相比秒级时间戳，毫秒级提供更高的精度。
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">当时间戳超出DateTime支持的范围时抛出</exception>
    /// <example>
    /// <code>
    /// DateTime dateTime = Time.OfEpochMilli(1640995200000);
    /// // 返回对应的本地时间，例如：2022-01-01 00:00:00.000
    /// </code>
    /// </example>
    public static DateTime OfEpochMilli(long epochMilli)
    {
        try
        {
            var ticks = TimeOptions.UnixEpochTicks + epochMilli * TimeOptions.TicksPreMillisecond;
            return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentOutOfRangeException($"Unix时间戳（毫秒）超出支持范围: {epochMilli}", ex);
        }
    }

    /// <summary>
    /// 将日期时间转换为Unix时间戳（秒）
    /// </summary>
    /// <param name="datetime">要转换的日期时间</param>
    /// <returns>Unix时间戳（秒）</returns>
    /// <remarks>
    /// 将输入的日期时间转换为UTC时间，然后计算自1970年1月1日以来的秒数。
    /// 如果输入时间早于1970年1月1日，将返回负数。
    /// </remarks>
    /// <example>
    /// <code>
    /// var dateTime = new DateTime(2023, 1, 1, 12, 0, 0);
    /// long timestamp = Time.ToEpochSecond(dateTime);
    /// </code>
    /// </example>
    public static long ToEpochSecond(DateTime datetime)=> (datetime.ToUniversalTime().Ticks - TimeOptions.UnixEpochTicks) / TimeOptions.TicksPerSec;

    /// <summary>
    /// 将日期时间转换为Unix时间戳（毫秒）
    /// </summary>
    /// <param name="datetime">要转换的日期时间</param>
    /// <returns>Unix时间戳（毫秒）</returns>
    /// <remarks>
    /// 将输入的日期时间转换为UTC时间，然后计算自1970年1月1日以来的毫秒数。
    /// 如果输入时间早于1970年1月1日，将返回负数。
    /// 相比秒级时间戳，毫秒级提供更高的精度。
    /// </remarks>
    /// <example>
    /// <code>
    /// var dateTime = new DateTime(2023, 1, 1, 12, 0, 0, 500);
    /// long timestamp = Time.ToEpochMilli(dateTime);
    /// </code>
    /// </example>
    public static long ToEpochMilli(DateTime datetime)=> (datetime.ToUniversalTime().Ticks - TimeOptions.UnixEpochTicks) / TimeOptions.TicksPreMillisecond;
}
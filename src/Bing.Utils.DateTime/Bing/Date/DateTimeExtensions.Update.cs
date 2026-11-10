namespace Bing.Date;

// 日期时间 - 更新；除显式起点操作外，保留未设置分量、毫秒以下 ticks 和本地重复时间选择标记。
/// <summary>
/// 日期时间扩展方法。
/// </summary>
public static partial class DateTimeExtensions
{
    #region At
    /// <summary>
    /// 设置日期时间的时间分量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <param name="minute">分</param>
    /// <returns>替换时间分量后的日期时间。</returns>
    public static DateTime At(this DateTime dt, int hour, int minute) => dt.SetTime(hour, minute);
    /// <summary>
    /// 设置日期时间的时间分量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <param name="minute">分</param>
    /// <param name="second">秒</param>
    /// <returns>替换时间分量后的日期时间。</returns>
    public static DateTime At(this DateTime dt, int hour, int minute, int second) => dt.SetTime(hour, minute, second);
    /// <summary>
    /// 设置日期时间的时间分量。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <param name="minute">分</param>
    /// <param name="second">秒</param>
    /// <param name="milliseconds">毫秒</param>
    /// <returns>替换时间分量后的日期时间。</returns>
    public static DateTime At(this DateTime dt, int hour, int minute, int second, int milliseconds) => dt.SetTime(hour, minute, second, milliseconds);
    /// <summary>
    /// 设置时间为凌晨0点（午夜）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>当天午夜的日期时间。</returns>
    public static DateTime AtMidnight(this DateTime dt) => dt.Date;
    /// <summary>
    /// 设置时间为中午12点（正午）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <returns>当天中午的日期时间。</returns>
    public static DateTime AtNoon(this DateTime dt) => dt.Date.AddHours(12);

    #endregion

    #region On
    /// <summary>
    /// 日期，修改它的年月日。
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="day">日</param>
    /// <returns>替换日期分量后的日期时间。</returns>
    public static DateTime On(this DateTime dt, int year, int month, int day) => dt.SetDate(year, month, day);

    #endregion

    #region Set
    /// <summary>
    /// 设置时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetTime(this DateTime dt, int hour) => SetComponents(dt, dt.Year, dt.Month, dt.Day, hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <param name="minute">分</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetTime(this DateTime dt, int hour, int minute) => SetComponents(dt, dt.Year, dt.Month, dt.Day, hour, minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <param name="minute">分</param>
    /// <param name="second">秒</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetTime(this DateTime dt, int hour, int minute, int second) => SetComponents(dt, dt.Year, dt.Month, dt.Day, hour, minute, second, dt.Millisecond);
    /// <summary>
    /// 设置时间
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <param name="minute">分</param>
    /// <param name="second">秒</param>
    /// <param name="millisecond">毫秒</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetTime(this DateTime dt, int hour, int minute, int second, int millisecond) => SetComponents(dt, dt.Year, dt.Month, dt.Day, hour, minute, second, millisecond);
    /// <summary>
    /// 设置时间 - 小时
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="hour">时</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetHour(this DateTime dt, int hour) => SetComponents(dt, dt.Year, dt.Month, dt.Day, hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置时间 - 分钟
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="minute">分</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetMinute(this DateTime dt, int minute) => SetComponents(dt, dt.Year, dt.Month, dt.Day, dt.Hour, minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置时间 - 秒
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="second">秒</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetSecond(this DateTime dt, int second) => SetComponents(dt, dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, second, dt.Millisecond);
    /// <summary>
    /// 设置时间 - 毫秒
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="millisecond">毫秒</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetMillisecond(this DateTime dt, int millisecond) => SetComponents(dt, dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, millisecond);
    /// <summary>
    /// 设置日期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="year">年</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetDate(this DateTime dt, int year) => SetComponents(dt, year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置日期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetDate(this DateTime dt, int year, int month) => SetComponents(dt, year, month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置日期
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="day">日</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetDate(this DateTime dt, int year, int month, int day) => SetComponents(dt, year, month, day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置日期 - 年
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="year">年</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetYear(this DateTime dt, int year) => SetComponents(dt, year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置日期 - 月
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="month">月</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetMonth(this DateTime dt, int month) => SetComponents(dt, dt.Year, month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置日期 - 日
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="day">日</param>
    /// <returns>保留未设置分量、Kind 和本地重复时间选择标记的日期时间。</returns>
    public static DateTime SetDay(this DateTime dt, int day) => SetComponents(dt, dt.Year, dt.Month, day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
    /// <summary>
    /// 设置日期种类。本地/UTC
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="kind">日期种类</param>
    /// <returns>只修改 Kind 标签的值；Kind 不变时保留原有完整状态。</returns>
    public static DateTime SetKind(this DateTime dt, DateTimeKind kind) => dt.Kind == kind ? dt : DateTime.SpecifyKind(dt, kind);
    /// <summary>
    /// 验证目标分量并通过钟面 tick 偏移保留原值的 Kind 和本地重复时间标记。
    /// </summary>
    /// <param name="original">保留未修改精度和内部本地时间标记的原值。</param>
    /// <param name="year">目标年。</param>
    /// <param name="month">目标月。</param>
    /// <param name="day">目标日。</param>
    /// <param name="hour">目标小时。</param>
    /// <param name="minute">目标分钟。</param>
    /// <param name="second">目标秒。</param>
    /// <param name="millisecond">目标毫秒。</param>
    /// <returns>保留原值毫秒以下 ticks 的目标日期时间。</returns>
    /// <exception cref="ArgumentOutOfRangeException">目标日期或时间分量不合法。</exception>
    private static DateTime SetComponents(DateTime original, int year, int month, int day,
        int hour, int minute, int second, int millisecond)
    {
        var target = new DateTime(year, month, day, hour, minute, second, millisecond);
        var adjustment = target.Ticks - original.Ticks + original.Ticks % TimeSpan.TicksPerMillisecond;
        return original.AddTicks(adjustment);
    }

    #endregion
}

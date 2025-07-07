namespace Bing.Date.DateUtils;

/// <summary>
/// 星座帮助类
/// </summary>
public static class ConstellationHelper
{
    /// <summary>
    /// 星座名称数组
    /// </summary>
    private static readonly string[] ConstellationNames =
    [
        "白羊座",
        "金牛座",
        "双子座",
        "巨蟹座",
        "狮子座",
        "处女座",
        "天秤座",
        "天蝎座",
        "射手座",
        "摩羯座",
        "水瓶座",
        "双鱼座"
    ];

    /// <summary>
    /// 星座日期区间数组，每个元素表示一个星座的起始月日
    /// </summary>
    private static readonly (int Month, int Day)[] ConstellationDateRanges =
    [
        (3, 21), // 白羊座起始日期
        (4, 20), // 金牛座起始日期
        (5, 21), // 双子座起始日期
        (6, 21), // 巨蟹座起始日期
        (7, 23), // 狮子座起始日期
        (8, 23), // 处女座起始日期
        (9, 23), // 天秤座起始日期
        (10, 23), // 天蝎座起始日期
        (11, 22), // 射手座起始日期
        (12, 22), // 摩羯座起始日期
        (1, 20), // 水瓶座起始日期
        (2, 19)  // 双鱼座起始日期
    ];

    /// <summary>
    /// 获取指定日期的星座名称
    /// </summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>对应的星座名称</returns>
    public static string Get(DateTime dateTime) => Get(dateTime.Month, dateTime.Day);

    /// <summary>
    /// 获取指定月份和日期的星座名称
    /// </summary>
    /// <param name="month">月份，范围1-12</param>
    /// <param name="day">日期</param>
    /// <returns>对应的星座名称</returns>
    /// <exception cref="ArgumentOutOfRangeException">月份或日期超出有效范围时抛出</exception>
    public static string Get(int month, int day)
    {
        // 验证月份和日期范围
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "月份必须在1到12之间");

        // 验证日期在当月的有效范围内
        var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, month);
        if (day < 1 || day > daysInMonth)
            throw new ArgumentOutOfRangeException(nameof(day), $"日期必须在1到{daysInMonth}之间");
        // 计算星座索引
        for (var i = 0; i < ConstellationDateRanges.Length; i++)
        {
            var currentRange = ConstellationDateRanges[i];
            var nextRangeIndex = (i + 1) % ConstellationDateRanges.Length;
            var nextRange = ConstellationDateRanges[nextRangeIndex];

            // 处理特殊情况：摩羯座跨年（12月22日 - 1月19日）
            if (i == 9) // 摩羯座
            {
                if ((month == 12 && day >= 22) || (month == 1 && day <= 19))
                    return ConstellationNames[i];
                continue;
            }

            // 处理普通情况
            if ((month == currentRange.Month && day >= currentRange.Day) ||
                (month == nextRange.Month && day < nextRange.Day))
            {
                return ConstellationNames[i];
            }
        }

        // 理论上代码不会执行到这里
        throw new InvalidOperationException("无法确定星座");
    }
}
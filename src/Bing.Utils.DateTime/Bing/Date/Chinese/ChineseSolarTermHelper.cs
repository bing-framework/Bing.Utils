using System.Globalization;
using System.Linq.Expressions;

namespace Bing.Date.Chinese;

/// <summary>
/// 中国二十四节气帮助类
/// </summary>
public static class ChineseSolarTermHelper
{
    /// <summary>
    /// 24节气 - 简体
    /// </summary>
    private static readonly string[] SOLAR_TERM_S =
    [
        "小寒", "大寒", "立春", "雨水", "惊蛰", "春分",
        "清明", "谷雨", "立夏", "小满", "芒种", "夏至",
        "小暑", "大暑", "立秋", "处暑", "白露", "秋分",
        "寒露", "霜降", "立冬", "小雪", "大雪", "冬至"
    ];

    /// <summary>
    /// 24节气 - 繁体
    /// </summary>
    private static readonly string[] SOLAR_TERM_Z =
    [
        "小寒", "大寒", "立春", "雨水", "驚蟄", "春分",
        "清明", "谷雨", "立夏", "小滿", "芒種", "夏至",
        "小暑", "大暑", "立秋", "處暑", "白露", "秋分",
        "寒露", "霜降", "立冬", "小雪", "大雪", "冬至"
    ];

    /// <summary>
    /// 24节气 - 英文
    /// </summary>
    private static readonly string[] SOLAR_TERM_E =
    [
        "Slight Cold", "Great Cold", "Beginning of Spring", "Rain Water", "The Waking of Insects", "Vernal Equinox",
        "Qingming Festival", "Grain Rain", "Beginning of Summer", "Grain Full", "Grain in Ear", "Summer Solstice",
        "Slight Heat", "Great Heat", "Beginning of Autumn", "The Limit of Heat", "White Dew", "Autumnal Equinox",
        "Cold Dew", "Frost's Descent", "Beginning of Winter", "Slight Snow", "Great Snow", "Winter Solstice"
    ];

    /// <summary>
    /// 24节气 - 枚举
    /// </summary>
    private static readonly List<ChineseSolarTerms> SOLAR_TERM_ENUM =
    [
        ChineseSolarTerms.SlightCold,
        ChineseSolarTerms.GreatCold,
        ChineseSolarTerms.BeginningOfSpring,
        ChineseSolarTerms.RainWater,
        ChineseSolarTerms.TheWakingOfInsects,
        ChineseSolarTerms.VernalEquinox,
        ChineseSolarTerms.QingmingFestival,
        ChineseSolarTerms.GrainRain,
        ChineseSolarTerms.BeginningOfSummer,
        ChineseSolarTerms.GrainFull,
        ChineseSolarTerms.GrainInEar,
        ChineseSolarTerms.SummerSolstice,
        ChineseSolarTerms.SlightHeat,
        ChineseSolarTerms.GreatHeat,
        ChineseSolarTerms.BeginningOfAutumn,
        ChineseSolarTerms.TheLimitOfHeat,
        ChineseSolarTerms.WhiteDew,
        ChineseSolarTerms.AutumnalEquinox,
        ChineseSolarTerms.ColdDew,
        ChineseSolarTerms.FrostsDescent,
        ChineseSolarTerms.BeginningOfWinter,
        ChineseSolarTerms.SlightSnow,
        ChineseSolarTerms.GreatSnow,
        ChineseSolarTerms.WinterSolstice
    ];

    /// <summary>
    /// 24节气信息
    /// </summary>
    private static readonly int[] SOLAR_TERM_INFO =
    [
        0, 21208, 42467, 63836, 85337, 107014, 128867, 150921, 173149, 195551, 218072, 240693, 263343, 285989,
        308563, 331033, 353350, 375494, 397447, 419210, 440795, 462224, 483532, 504758
    ];

    /// <summary>
    /// 获取指定节气的中文名称
    /// </summary>
    /// <param name="solarTerms">节气枚举</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为false，使用简体中文</param>
    /// <returns>返回指定节气的中文名称</returns>
    public static string GetName(ChineseSolarTerms solarTerms, bool traditionalChineseCharacters = false)
    {
        var index = SOLAR_TERM_ENUM.IndexOf(solarTerms);
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return solarTermP[index];
    }

    /// <summary>
    /// 获取指定节气的英文名称
    /// </summary>
    /// <param name="solarTerms">节气枚举</param>
    /// <returns>返回指定节气的英文名称</returns>
    public static string GetEnglishName(ChineseSolarTerms solarTerms)
    {
        var index = SOLAR_TERM_ENUM.IndexOf(solarTerms);
        return SOLAR_TERM_E[index];
    }

    /// <summary>
    /// 获取指定日期（公历）的节气名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为false，使用简体中文</param>
    /// <returns>如果指定日期是节气，则返回节气的名称；否则返回空字符串</returns>
    public static string GetSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, bool traditionalChineseCharacters = false)
    {
        var i = SolarTermFunc(calendar, targetDt, (x, y) => x == y, out _);
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return i == -1 ? "" : solarTermP[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为false，使用简体中文</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    public static string GetLastSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, bool traditionalChineseCharacters = false) => GetLastSolarTerm(calendar, targetDt, out _, traditionalChineseCharacters);

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为false，使用简体中文</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    public static string GetLastSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt, bool traditionalChineseCharacters = false)
    {
        var i = SolarTermFunc(calendar, targetDt, (x, y) => x < y, out dt);
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return i == -1 ? "" : solarTermP[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为false，使用简体中文</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    public static string GetNextSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, bool traditionalChineseCharacters = false) => GetNextSolarTerm(calendar, targetDt, out _, traditionalChineseCharacters);

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为false，使用简体中文</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    public static string GetNextSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt, bool traditionalChineseCharacters = false)
    {
        var i = SolarTermFunc(calendar, targetDt, (x, y) => x > y, out dt);
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return i == -1 ? "" : solarTermP[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的节气枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <returns>如果指定日期是节气，则返回节气的枚举值；否则返回null</returns>
    public static ChineseSolarTerms? GetSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt)
    {
        var i = SolarTermFunc(calendar, targetDt, (x, y) => x == y, out _);
        return i == -1 ? null : SOLAR_TERM_ENUM[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    public static ChineseSolarTerms? GetLastSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt) => GetLastSolarTermEnum(calendar, targetDt, out _);

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    public static ChineseSolarTerms? GetLastSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt)
    {
        var i = SolarTermFunc(calendar, targetDt, (x, y) => x < y, out dt);
        return i == -1 ? null : SOLAR_TERM_ENUM[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    public static ChineseSolarTerms? GetNextSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt) => GetNextSolarTermEnum(calendar, targetDt, out _);

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    public static ChineseSolarTerms? GetNextSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt)
    {
        var i = SolarTermFunc(calendar, targetDt, (x, y) => x > y, out dt);
        return i == -1 ? null : SOLAR_TERM_ENUM[i];
    }

    /// <summary>
    /// 节气计算（当前年），返回指定条件的节气序及日期（公历）
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="func">条件表达式，用于确定如何比较日期</param>
    /// <param name="dateTime">计算得到的符合条件的节气日期</param>
    /// <returns>返回节气的索引，如果未找到符合条件的节气，则返回-1</returns>
    private static int SolarTermFunc(ChineseLunisolarCalendar calendar, DateTime targetDt, Expression<Func<int, int, bool>> func, out DateTime dateTime)
    {
        var baseDateAndTime = new DateTime(1900, 1, 6, 2, 5, 0); //#1900-01-06 2:05:00 AM#
        var year = targetDt.Year;
        int[] solar = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24];
        var expressionType = func.Body.NodeType;
        if (expressionType != ExpressionType.LessThan && expressionType != ExpressionType.LessThanOrEqual &&
            expressionType != ExpressionType.GreaterThan && expressionType != ExpressionType.GreaterThanOrEqual &&
            expressionType != ExpressionType.Equal)
            throw new NotSupportedException("不受支持的操作符。");
        if (expressionType == ExpressionType.LessThan || expressionType == ExpressionType.LessThanOrEqual)
            solar = solar.OrderByDescending(x => x).ToArray();
        foreach (var item in solar)
        {
            var num = 525948.76 * (year - 1900) + SOLAR_TERM_INFO[item - 1];
            var newDate = baseDateAndTime.AddMinutes(num); // 按分钟计算
            if (func.Compile()(newDate.DayOfYear, targetDt.DayOfYear))
            {
                dateTime = newDate;
                return item - 1;
            }
        }

        dateTime = calendar.MinSupportedDateTime;
        return -1;
    }
}
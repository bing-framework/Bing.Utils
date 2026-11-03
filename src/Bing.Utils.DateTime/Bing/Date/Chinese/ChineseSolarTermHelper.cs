using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 中国二十四节气帮助类
/// </summary>
/// <remarks>节气按官方 UTC+8 自然日固定表查询，支持 1900 至 2100 年。</remarks>
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
    /// 24节气枚举，顺序与固定自然日表一致
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
    /// 获取指定节气的中文名称
    /// </summary>
    /// <param name="solarTerms">节气枚举</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>返回指定节气的中文名称</returns>
    /// <exception cref="ArgumentOutOfRangeException">节气枚举不在有效范围内时抛出</exception>
    public static string GetName(ChineseSolarTerms solarTerms, bool traditionalChineseCharacters = false)
    {
        var index = SOLAR_TERM_ENUM.IndexOf(solarTerms);
        if (index == -1)
            throw new ArgumentOutOfRangeException(nameof(solarTerms), "无效的节气枚举值");
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return solarTermP[index];
    }

    /// <summary>
    /// 获取指定节气的英文名称
    /// </summary>
    /// <param name="solarTerms">节气枚举</param>
    /// <returns>返回指定节气的英文名称</returns>
    /// <exception cref="ArgumentOutOfRangeException">节气枚举不在有效范围内时抛出</exception>
    public static string GetEnglishName(ChineseSolarTerms solarTerms)
    {
        var index = SOLAR_TERM_ENUM.IndexOf(solarTerms);
        if (index == -1)
            throw new ArgumentOutOfRangeException(nameof(solarTerms), "无效的节气枚举值");
        return SOLAR_TERM_E[index];
    }

    /// <summary>
    /// 获取指定日期（公历）的节气名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>如果指定日期是节气，则返回节气的名称；否则返回空字符串</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static string GetSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, bool traditionalChineseCharacters = false)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        var i = GetSolarTermIndex(calendar, targetDt, SearchMode.On, out _);
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return i == -1 ? string.Empty : solarTermP[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static string GetLastSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, bool traditionalChineseCharacters = false) => GetLastSolarTerm(calendar, targetDt, out _, traditionalChineseCharacters);

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static string GetLastSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt, bool traditionalChineseCharacters = false)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        var i = GetSolarTermIndex(calendar, targetDt, SearchMode.Before, out dt);
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return i == -1 ? string.Empty : solarTermP[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static string GetNextSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, bool traditionalChineseCharacters = false) => GetNextSolarTerm(calendar, targetDt, out _, traditionalChineseCharacters);

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的名称
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>如果找到符合条件的节气，则返回节气的名称；否则返回空字符串</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static string GetNextSolarTerm(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt, bool traditionalChineseCharacters = false)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        var i = GetSolarTermIndex(calendar, targetDt, SearchMode.After, out dt);
        var solarTermP = traditionalChineseCharacters ? SOLAR_TERM_Z : SOLAR_TERM_S;
        return i == -1 ? string.Empty : solarTermP[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的节气枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <returns>如果指定日期是节气，则返回节气的枚举值；否则返回null</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static ChineseSolarTerms? GetSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        var i = GetSolarTermIndex(calendar, targetDt, SearchMode.On, out _);
        return i == -1 ? null : SOLAR_TERM_ENUM[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static ChineseSolarTerms? GetLastSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt) => GetLastSolarTermEnum(calendar, targetDt, out _);

    /// <summary>
    /// 获取指定日期（公历）的上一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static ChineseSolarTerms? GetLastSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        var i = GetSolarTermIndex(calendar, targetDt, SearchMode.Before, out dt);
        return i == -1 ? null : SOLAR_TERM_ENUM[i];
    }

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static ChineseSolarTerms? GetNextSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt) => GetNextSolarTermEnum(calendar, targetDt, out _);

    /// <summary>
    /// 获取指定日期（公历）的下一个节气的枚举
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="dt">计算得到的符合条件的节气日期</param>
    /// <returns>如果找到符合条件的节气，则返回节气的枚举值；否则返回null</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static ChineseSolarTerms? GetNextSolarTermEnum(ChineseLunisolarCalendar calendar, DateTime targetDt, out DateTime dt)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        var i = GetSolarTermIndex(calendar, targetDt, SearchMode.After, out dt);
        return i == -1 ? null : SOLAR_TERM_ENUM[i];
    }

    private enum SearchMode
    {
        On,
        Before,
        After
    }

    /// <summary>
    /// 在固定 UTC+8 自然日表中查找节气，并以午夜 Unspecified 返回日期。
    /// </summary>
    private static int GetSolarTermIndex(ChineseLunisolarCalendar calendar, DateTime targetDt, SearchMode mode, out DateTime dateTime)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");

        dateTime = calendar.MinSupportedDateTime;
        if (!ChineseSolarTermDateData.IsSupportedYear(targetDt.Year))
            return -1;

        if (mode == SearchMode.Before)
        {
            for (var index = 23; index >= 0; index--)
            {
                if (!IsBeforeTarget(targetDt, index))
                    continue;

                dateTime = CreateTermDate(targetDt.Year, index);
                return index;
            }

            if (targetDt.Year > ChineseSolarTermDateData.MinYear)
            {
                dateTime = CreateTermDate(targetDt.Year - 1, 23);
                return 23;
            }

            return -1;
        }

        if (mode == SearchMode.After)
        {
            for (var index = 0; index < 24; index++)
            {
                if (!IsAfterTarget(targetDt, index))
                    continue;

                dateTime = CreateTermDate(targetDt.Year, index);
                return index;
            }

            if (targetDt.Year < ChineseSolarTermDateData.MaxYear)
            {
                dateTime = CreateTermDate(targetDt.Year + 1, 0);
                return 0;
            }

            return -1;
        }

        for (var index = 0; index < 24; index++)
        {
            var termMonth = index / 2 + 1;
            if (targetDt.Month == termMonth && targetDt.Day == ChineseSolarTermDateData.GetDayOfMonth(targetDt.Year, index))
            {
                dateTime = CreateTermDate(targetDt.Year, index);
                return index;
            }
        }

        return -1;
    }

    private static DateTime CreateTermDate(int year, int index)
    {
        var month = index / 2 + 1;
        var day = ChineseSolarTermDateData.GetDayOfMonth(year, index);
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
    }

    private static bool IsBeforeTarget(DateTime targetDt, int index)
    {
        var month = index / 2 + 1;
        var day = ChineseSolarTermDateData.GetDayOfMonth(targetDt.Year, index);
        return month < targetDt.Month || month == targetDt.Month && day < targetDt.Day;
    }

    private static bool IsAfterTarget(DateTime targetDt, int index)
    {
        var month = index / 2 + 1;
        var day = ChineseSolarTermDateData.GetDayOfMonth(targetDt.Year, index);
        return month > targetDt.Month || month == targetDt.Month && day > targetDt.Day;
    }
}

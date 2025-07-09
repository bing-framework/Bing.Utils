using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 中国二十四节气帮助类
/// </summary>
public static class ChineseSolarTermHelper
{
    /// <summary>
    /// 基准日期时间：1900年1月6日2:05:00
    /// </summary>
    private static readonly DateTime BaseDateAndTime = new(1900, 1, 6, 2, 5, 0);

    /// <summary>
    /// 每年的分钟偏移量
    /// </summary>
    private const double MinutesPerYear = 525948.76;

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
    /// 24节气信息，保存每个节气的分钟偏移量
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
        var i = GetSolarTermIndex(calendar, targetDt, (x, y) => Math.Abs(x - y) <= 1, out _);
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
        var i = GetSolarTermIndex(calendar, targetDt, (x, y) => x < y, out dt);
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
        var i = GetSolarTermIndex(calendar, targetDt, (x, y) => x > y, out dt);
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
        var i = GetSolarTermIndex(calendar, targetDt, (x, y) => Math.Abs(x - y) <= 1, out _);
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
        var i = GetSolarTermIndex(calendar, targetDt, (x, y) => x < y, out dt);
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
        var i = GetSolarTermIndex(calendar, targetDt, (x, y) => x > y, out dt);
        return i == -1 ? null : SOLAR_TERM_ENUM[i];
    }

    /// <summary>
    /// 节气计算，返回指定条件的节气序及日期（公历）
    /// </summary>
    /// <param name="calendar">中国农历日历实例</param>
    /// <param name="targetDt">目标日期</param>
    /// <param name="comparer">用于比较日期的函数</param>
    /// <param name="dateTime">计算得到的符合条件的节气日期</param>
    /// <returns>返回节气的索引，如果未找到符合条件的节气，则返回-1</returns>
    private static int GetSolarTermIndex(ChineseLunisolarCalendar calendar, DateTime targetDt, Func<int, int, bool> comparer, out DateTime dateTime)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        if (targetDt < BaseDateAndTime || targetDt > new DateTime(2100, 12, 31))
        {
            dateTime = calendar.MinSupportedDateTime;
            return -1;
        }

        var year = targetDt.Year;
        // 获取24节气的序号列表，根据比较函数决定顺序
        var solarTermIndices = Enumerable.Range(1, 24).ToArray();
        if (comparer(0, 1)) // 降序
            solarTermIndices = solarTermIndices.OrderByDescending(x => x).ToArray();
        foreach (var termIndex in solarTermIndices)
        {
            // 计算节气对应的分钟数
            var minutes = MinutesPerYear * (year - 1900) + SOLAR_TERM_INFO[termIndex - 1];
            // 计算节气对应的日期
            var termDate = BaseDateAndTime.AddMinutes(minutes);
            // 根据比较函数判断是否符合条件
            if (comparer(termDate.DayOfYear, targetDt.DayOfYear))
            {
                dateTime = termDate;
                return termIndex - 1;
            }
        }
        dateTime = calendar.MinSupportedDateTime;
        return -1;
    }
}
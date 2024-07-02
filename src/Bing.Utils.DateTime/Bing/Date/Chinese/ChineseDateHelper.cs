using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 农历帮助类
/// </summary>
public static class ChineseDateHelper
{
    #region 简体

    /// <summary>
    /// 十天干 - 简体
    /// </summary>
    /// <remarks>
    /// 十天干：甲（jiǎ）、乙（yǐ）、丙（bǐng）、丁（dīng）、戊（wù）、己（jǐ）、庚（gēng）、辛（xīn）、壬（rén）、癸（guǐ）
    /// </remarks>
    private static readonly string[] GAN_S = ["甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸"];

    /// <summary>
    /// 十二地支 - 简体
    /// </summary>
    /// <remarks>
    /// 十二地支：子（zǐ）、丑（chǒu）、寅（yín）、卯（mǎo）、辰（chén）、巳（sì）、午（wǔ）、未（wèi）、申（shēn）、酉（yǒu）、戌（xū）、亥（hài）<br />
    /// 十二地支对应十二生肖：子-鼠，丑-牛，寅-虎，卯-兔，辰-龙，巳-蛇， 午-马，未-羊，申-猴，酉-鸡，戌-狗，亥-猪
    /// </remarks>
    private static readonly string[] ZHI_S = ["子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"];

    /// <summary>
    /// 定义了中国农历的月份名称数组，从正月开始 - 简体
    /// </summary>
    private static readonly string[] YUE_S = ["正", "二", "三", "四", "五", "六", "七", "八", "九", "十", "十一", "腊"];

    /// <summary>
    /// 定义了中国农历的日的前缀名称数组，包括初、十、廿、卅 - 简体
    /// </summary>
    private static readonly string[] PRI_S = ["初", "十", "廿", "卅"];

    /// <summary>
    /// 定义了中国农历的日的后缀名称数组，从日开始 - 简体
    /// </summary>
    private static readonly string[] SRI_S = ["日", "一", "二", "三", "四", "五", "六", "七", "八", "九"];

    /// <summary>
    /// 定义了中国农历的日的中间部分名称数组，包括初十、二十、三十 - 简体
    /// </summary>
    private static readonly string[] MRI_S = ["", "初十", "二十", "三十"];

    /// <summary>
    /// 定义了中文数字名称数组，从零开始 - 简体
    /// </summary>
    private static readonly string[] HZNUM_S = ["零", "一", "二", "三", "四", "五", "六", "七", "八", "九"];

    /// <summary>
    /// 定义了闰月的中文名称 - 简体
    /// </summary>
    private static readonly string RUN_S = "闰";

    /// <summary>
    /// 定义了时辰的中文名称 - 简体
    /// </summary>
    private static readonly string SHI_S = "时";

    #endregion

    #region 繁体

    /// <summary>
    /// 十天干 - 繁体
    /// </summary>
    /// <remarks>
    /// 十天干：甲（jiǎ）、乙（yǐ）、丙（bǐng）、丁（dīng）、戊（wù）、己（jǐ）、庚（gēng）、辛（xīn）、壬（rén）、癸（guǐ）
    /// </remarks>
    private static readonly string[] GAN_Z = ["甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸"];

    /// <summary>
    /// 十二地支 - 繁体
    /// </summary>
    /// <remarks>
    /// 十二地支：子（zǐ）、丑（chǒu）、寅（yín）、卯（mǎo）、辰（chén）、巳（sì）、午（wǔ）、未（wèi）、申（shēn）、酉（yǒu）、戌（xū）、亥（hài）<br />
    /// 十二地支对应十二生肖：子-鼠，丑-牛，寅-虎，卯-兔，辰-龙，巳-蛇， 午-马，未-羊，申-猴，酉-鸡，戌-狗，亥-猪
    /// </remarks>
    private static readonly string[] ZHI_Z = ["子", "醜", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"];

    /// <summary>
    /// 定义了中国农历的月份名称数组，从正月开始 - 繁体
    /// </summary>
    private static readonly string[] YUE_Z = ["正", "貳", "叁", "肆", "伍", "陸", "柒", "捌", "玖", "拾", "拾壹", "臘"];

    /// <summary>
    /// 定义了中国农历的日的前缀名称数组，包括初、十、廿、卅 - 繁体
    /// </summary>
    private static readonly string[] PRI_Z = ["初", "拾", "廿", "卅"];

    /// <summary>
    /// 定义了中国农历的日的后缀名称数组，从日开始 - 繁体
    /// </summary>
    private static readonly string[] SRI_Z = ["日", "壹", "貳", "叁", "肆", "伍", "陸", "柒", "捌", "玖"];

    /// <summary>
    /// 定义了中国农历的日的中间部分名称数组，包括初拾、貳拾、叁拾 - 繁体
    /// </summary>
    private static readonly string[] MRI_Z = ["", "初拾", "貳拾", "叁拾"];

    /// <summary>
    /// 定义了中文数字名称数组，从零开始 - 繁体
    /// </summary>
    private static readonly string[] HZNUM_Z = ["零", "壹", "貳", "叁", "肆", "伍", "陸", "柒", "捌", "玖"];

    /// <summary>
    /// 定义了闰月的中文名称 - 繁体
    /// </summary>
    private static readonly string RUN_Z = "閏";

    /// <summary>
    /// 定义了时辰的中文名称 - 繁体
    /// </summary>
    private static readonly string SHI_Z = "時";

    #endregion

    /// <summary>
    /// 获取农历年
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符</param>
    /// <returns>农历年</returns>
    public static string GetChineseYear(DateTime dt, bool traditionalChineseCharacters = false)
    {
        var hzNumP = traditionalChineseCharacters ? HZNUM_Z : HZNUM_S;

        var year = dt.Year.ToString().ToCharArray();
        var yearStr = new string[year.Length];
        for (var i = 0; i < year.Length; i++)
            yearStr[i] = hzNumP[year[i]];
        return $"{string.Join("", yearStr)}年";
    }

    /// <summary>
    /// 获取干支年
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符</param>
    /// <returns>干支年份</returns>
    public static string GetSexagenaryYear(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters = false)
    {
        var ganP = traditionalChineseCharacters ? GAN_Z : GAN_S;
        var zhiP = traditionalChineseCharacters ? ZHI_Z : ZHI_S;

        var sexagenaryYear = calendar.GetSexagenaryYear(dt);
        var stemIndex = calendar.GetCelestialStem(sexagenaryYear - 1);
        var branchIndex = calendar.GetTerrestrialBranch(sexagenaryYear) - 1;
        return $"{ganP[stemIndex]}{zhiP[branchIndex]}年";
    }

    /// <summary>
    /// 获取农历月
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符</param>
    /// <returns>农历月</returns>
    public static string GetChineseMonth(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters = false)
    {
        var run = traditionalChineseCharacters ? RUN_Z : RUN_S;
        var yueP = traditionalChineseCharacters ? YUE_Z : YUE_S;

        var isLeapMonth = calendar.IsLeapMonth(dt.Year, dt.Month);
        return $"{(isLeapMonth ? run : string.Empty)}{yueP[calendar.GetMonth(dt)]}月";
    }

    /// <summary>
    /// 获取农历日
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符</param>
    /// <returns>农历日</returns>
    public static string GetChineseDay(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters = false)
    {
        var day = calendar.GetDayOfMonth(dt);
        var priP = traditionalChineseCharacters ? PRI_Z : PRI_S;
        var sriP = traditionalChineseCharacters ? SRI_Z : SRI_S;
        var mriP = traditionalChineseCharacters ? MRI_Z : MRI_S;
        switch (day)
        {
            case 0:
            case 10:
            case 20:
            case 30:
                return mriP[day / 10];
            default:
                return $"{priP[day / 10]}{sriP[day % 10]}";
        }
    }

    /// <summary>
    /// 获取农历时辰
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符</param>
    /// <returns>农历时辰</returns>
    public static string GetChineseHour(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters = false)
    {
        var shiP = traditionalChineseCharacters ? SHI_Z : SHI_S;
        var zhiP = traditionalChineseCharacters ? ZHI_Z : ZHI_S;
        var hour = dt.Hour;
        if (dt.Minute != 0)
            hour += 1;
        var offset = hour / 2;
        if (offset >= 12)
            offset = 0;
        return $"{zhiP[offset]}{shiP}";
    }

    /// <summary>
    /// 指定年份是否为闰年
    /// </summary>
    /// <param name="calendar">中国农历日历，如果为null，则创建一个新的实例</param>
    /// <param name="dt">日期，用于获取年份</param>
    /// <returns>如果指定的年份是闰年，则返回true；否则返回false</returns>
    public static bool IsLeapYear(ChineseLunisolarCalendar calendar, DateTime dt)
    {
        calendar ??= new ChineseLunisolarCalendar();
        return calendar.IsLeapYear(dt.Year);
    }

    /// <summary>
    /// 指定月份是否为闰月
    /// </summary>
    /// <param name="calendar">中国农历日历，如果为null，则创建一个新的实例</param>
    /// <param name="dt">日期，用于获取年份和月份</param>
    /// <returns>如果指定的月份是闰月，则返回true；否则返回false</returns>
    public static bool IsLeapMonth(ChineseLunisolarCalendar calendar, DateTime dt)
    {
        calendar ??= new ChineseLunisolarCalendar();
        return calendar.IsLeapMonth(dt.Year, dt.Month);
    }

    /// <summary>
    /// 指定日期是否为闰日
    /// </summary>
    /// <param name="calendar">中国农历日历，如果为null，则创建一个新的实例</param>
    /// <param name="dt">日期，用于获取年份、月份和日</param>
    /// <returns>如果指定的日期是闰日，则返回true；否则返回false</returns>
    public static bool IsLeapDay(ChineseLunisolarCalendar calendar, DateTime dt)
    {
        calendar ??= new ChineseLunisolarCalendar();
        return calendar.IsLeapDay(dt.Year, dt.Month, dt.Day);
    }
}
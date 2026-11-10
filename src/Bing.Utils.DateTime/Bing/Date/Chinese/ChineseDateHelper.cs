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
    /// 将公历年份转换为中文数字。
    /// </summary>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>公历年份的中文数字。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期超出有效范围。</exception>
    public static string GetChineseYear(DateTime dt, bool traditionalChineseCharacters = false)
    {
        if (!DateJudge.IsValid(dt))
            throw new ArgumentOutOfRangeException(nameof(dt), "日期必须在有效范围内");

        return FormatChineseYear(dt.Year, traditionalChineseCharacters);
    }

    /// <summary>
    /// 将有效农历日期所属的年份转换为中文数字。
    /// </summary>
    /// <param name="calendar">中国农历历法。</param>
    /// <param name="dt">公历日期。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符。</param>
    /// <returns>农历年份的中文数字。</returns>
    internal static string GetChineseYear(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters)
    {
        ValidateLunarDate(calendar, dt);
        return FormatChineseYear(calendar.GetYear(dt), traditionalChineseCharacters);
    }

    /// <summary>
    /// 使用现有字表格式化年份数字。
    /// </summary>
    /// <param name="year">待格式化的年份。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符。</param>
    /// <returns>年份的中文数字。</returns>
    private static string FormatChineseYear(int year, bool traditionalChineseCharacters)
    {
        var hzNumP = traditionalChineseCharacters ? HZNUM_Z : HZNUM_S;

        var result = string.Empty;
        foreach (var digit in year.ToString())
        {
            // 将字符转换为数字并获取对应的中文数字
            if (int.TryParse(digit.ToString(), out int number) && number >= 0 && number <= 9)
                result += hzNumP[number];
        }
        return $"{result}年";
    }

    /// <summary>
    /// 根据已验证的农历分量格式化简体或繁体日期。
    /// </summary>
    /// <param name="year">农历年。</param>
    /// <param name="month">农历显示月份。</param>
    /// <param name="day">农历日。</param>
    /// <param name="isLeapMonth">是否为闰月。</param>
    /// <param name="traditional">是否使用繁体字形。</param>
    /// <param name="includeYear">是否包含农历年份。</param>
    /// <returns>按现有字表组成的农历日期。</returns>
    internal static string FormatLunarDate(int year, int month, int day, bool isLeapMonth, bool traditional, bool includeYear)
    {
        var builder = new System.Text.StringBuilder(16);
        if (includeYear)
        {
            var numbers = traditional ? HZNUM_Z : HZNUM_S;
            builder.Append(numbers[year / 1000]);
            builder.Append(numbers[year / 100 % 10]);
            builder.Append(numbers[year / 10 % 10]);
            builder.Append(numbers[year % 10]);
            builder.Append('年');
        }

        if (isLeapMonth)
            builder.Append(traditional ? RUN_Z : RUN_S);
        builder.Append((traditional ? YUE_Z : YUE_S)[month - 1]);
        builder.Append('月');
        if (day % 10 == 0)
            builder.Append((traditional ? MRI_Z : MRI_S)[day / 10]);
        else
        {
            builder.Append((traditional ? PRI_Z : PRI_S)[day / 10]);
            builder.Append((traditional ? SRI_Z : SRI_S)[day % 10]);
        }
        return builder.ToString();
    }

    /// <summary>
    /// 获取干支年
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>干支年份</returns>
    /// <exception cref="ArgumentNullException">日历或日期为空时抛出</exception>
    public static string GetSexagenaryYear(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters = false)
    {
        ValidateLunarDate(calendar, dt);

        var ganP = traditionalChineseCharacters ? GAN_Z : GAN_S;
        var zhiP = traditionalChineseCharacters ? ZHI_Z : ZHI_S;

        var sexagenaryYear = calendar.GetSexagenaryYear(dt);
        var stemIndex = calendar.GetCelestialStem(sexagenaryYear) - 1;
        var branchIndex = calendar.GetTerrestrialBranch(sexagenaryYear) - 1;
        return $"{ganP[stemIndex]}{zhiP[branchIndex]}年";
    }

    /// <summary>
    /// 获取农历月
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>农历月</returns>
    /// <exception cref="ArgumentNullException">日历或日期为空时抛出</exception>
    public static string GetChineseMonth(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters = false)
    {
        ValidateLunarDate(calendar, dt);

        var run = traditionalChineseCharacters ? RUN_Z : RUN_S;
        var yueP = traditionalChineseCharacters ? YUE_Z : YUE_S;

        // 获取农历年和月
        var lunarYear = calendar.GetYear(dt);
        var lunarMonth = calendar.GetMonth(dt);

        // 获取当年的闰月
        var leapMonth = calendar.GetLeapMonth(lunarYear);

        // 判断是否是闰月
        var isLeapMonth = calendar.IsLeapMonth(lunarYear, lunarMonth);

        // 计算月份索引：对于闰N月，显示为"闰N月"；对于大于闰月的月份，索引-1以正确映射
        int monthIndex;
        if (isLeapMonth)
        {
            // 闰月使用前一个月的索引
            monthIndex = lunarMonth - 2;
        }
        else if (leapMonth > 0 && lunarMonth > leapMonth)
        {
            // 闰月之后的月份索引减1
            monthIndex = lunarMonth - 2;
        }
        else
        {
            // 正常月份
            monthIndex = lunarMonth - 1;
        }

        // 确保索引在有效范围内
        if (monthIndex < 0 || monthIndex >= yueP.Length)
            monthIndex = 0;

        return $"{(isLeapMonth ? run : string.Empty)}{yueP[monthIndex]}月";
    }

    /// <summary>
    /// 获取农历日
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>农历日</returns>
    /// <exception cref="ArgumentNullException">日历或日期为空时抛出</exception>
    public static string GetChineseDay(ChineseLunisolarCalendar calendar, DateTime dt, bool traditionalChineseCharacters = false)
    {
        ValidateLunarDate(calendar, dt);

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
    /// <param name="dt">日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>农历时辰</returns>
    /// <exception cref="ArgumentNullException">日期为空时抛出</exception>
    public static string GetChineseHour(DateTime dt, bool traditionalChineseCharacters = false)
    {
        if (!DateJudge.IsValid(dt))
            throw new ArgumentOutOfRangeException(nameof(dt), "日期必须在有效范围内");

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
    /// 获取完整农历日期
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <param name="includeHour">是否包含时辰</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符</param>
    /// <returns>完整农历日期表示</returns>
    /// <exception cref="ArgumentNullException">日历或日期为空时抛出</exception>
    public static string GetChineseDateTime(ChineseLunisolarCalendar calendar, DateTime dt, bool includeHour = true, bool traditionalChineseCharacters = false)
    {
        ValidateLunarDate(calendar, dt);

        var year = GetSexagenaryYear(calendar, dt, traditionalChineseCharacters);
        var month = GetChineseMonth(calendar, dt, traditionalChineseCharacters);
        var day = GetChineseDay(calendar, dt, traditionalChineseCharacters);

        var result = $"{year}{month}{day}";
        if (includeHour)
            result += GetChineseHour(dt, traditionalChineseCharacters);
        return result;
    }

    /// <summary>
    /// 指定年份是否为闰年
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="year">年份</param>
    /// <returns>如果指定的年份是闰年，则返回true</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static bool IsLeapYear(ChineseLunisolarCalendar calendar, int year)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        return calendar.IsLeapYear(year);
    }

    /// <summary>
    /// 指定年份是否为闰年
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <returns>如果指定的年份是闰年，则返回true</returns>
    /// <exception cref="ArgumentNullException">日历或日期为空时抛出</exception>
    public static bool IsLeapYear(ChineseLunisolarCalendar calendar, DateTime dt)
    {
        ValidateLunarDate(calendar, dt);
        return calendar.IsLeapYear(calendar.GetYear(dt));
    }

    /// <summary>
    /// 指定月份是否为闰月
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>如果指定的月份是闰月，则返回true</returns>
    /// <exception cref="ArgumentNullException">日历为空时抛出</exception>
    public static bool IsLeapMonth(ChineseLunisolarCalendar calendar, int year, int month)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar), "中国农历日历不能为空");
        return calendar.IsLeapMonth(year, month);
    }

    /// <summary>
    /// 指定月份是否为闰月
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <returns>如果指定的月份是闰月，则返回true</returns>
    /// <exception cref="ArgumentNullException">日历或日期为空时抛出</exception>
    public static bool IsLeapMonth(ChineseLunisolarCalendar calendar, DateTime dt)
    {
        ValidateLunarDate(calendar, dt);
        var lunarYear = calendar.GetYear(dt);
        var lunarMonth = calendar.GetMonth(dt);
        return calendar.IsLeapMonth(lunarYear, lunarMonth);
    }

    /// <summary>
    /// 指定日期是否为闰日
    /// </summary>
    /// <param name="calendar">中国农历日历</param>
    /// <param name="dt">日期</param>
    /// <returns>如果指定的日期是闰日，则返回true</returns>
    /// <exception cref="ArgumentNullException">日历或日期为空时抛出</exception>
    public static bool IsLeapDay(ChineseLunisolarCalendar calendar, DateTime dt)
    {
        ValidateLunarDate(calendar, dt);
        var lunarYear = calendar.GetYear(dt);
        var lunarMonth = calendar.GetMonth(dt);
        var lunarDay = calendar.GetDayOfMonth(dt);
        return calendar.IsLeapDay(lunarYear, lunarMonth, lunarDay);
    }
    /// <summary>
    /// 验证公历日期位于指定农历的支持范围内。
    /// </summary>
    /// <param name="calendar">使用的中国农历实例。</param>
    /// <param name="dt">待验证的公历日期时间。</param>
    private static void ValidateLunarDate(ChineseLunisolarCalendar calendar, DateTime dt)
    {
        if (calendar == null)
            throw new ArgumentNullException(nameof(calendar));
        if (dt < calendar.MinSupportedDateTime || dt > calendar.MaxSupportedDateTime)
            throw new ArgumentOutOfRangeException(nameof(dt), dt, "日期超出中国农历支持范围。");
    }
}

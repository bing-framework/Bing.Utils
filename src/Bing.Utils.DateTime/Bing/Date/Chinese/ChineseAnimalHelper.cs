namespace Bing.Date.Chinese;

/// <summary>
/// 中国生肖帮助类
/// </summary>
public static class ChineseAnimalHelper
{
    /// <summary>
    /// 生肖 - 简体
    /// </summary>
    private static readonly string[] ANIMAL_S = ["鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪"];

    /// <summary>
    /// 生肖 - 繁体
    /// </summary>
    private static readonly string[] ANIMAL_Z = ["鼠", "牛", "虎", "兔", "龍", "蛇", "馬", "羊", "猴", "雞", "狗", "豬"];

    /// <summary>
    /// 中国生肖的起始年份，1900年为鼠年
    /// </summary>
    private const int ANIMAL_START_YEAR = 1900;

    /// <summary>
    /// 计算指定年份的生肖在生肖数组中的索引。
    /// </summary>
    /// <param name="year">年份。</param>
    /// <returns>生肖在生肖数组中的索引。</returns>
    private static int Index(int year)
    {
        var offset = year - ANIMAL_START_YEAR;
        return offset % 12;
    }

    /// <summary>
    /// 获取指定日期的生肖。
    /// </summary>
    /// <param name="dt">需要获取生肖的日期。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>指定日期的生肖。</returns>
    public static string Get(DateTime dt, bool traditionalChineseCharacters = false) => Get(dt.Year, traditionalChineseCharacters);

    /// <summary>
    /// 获取指定年份的生肖。
    /// </summary>
    /// <param name="year">需要获取生肖的年份</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符。</param>
    /// <returns>指定年份的生肖。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当年份小于1900时抛出此异常。</exception>
    public static string Get(int year, bool traditionalChineseCharacters = false)
    {
        if (year < ANIMAL_START_YEAR)
            throw new ArgumentOutOfRangeException(nameof(year), $"年份不能小于{ANIMAL_START_YEAR}。");
        var animal = traditionalChineseCharacters ? ANIMAL_Z : ANIMAL_S;
        return animal[Index(year)];
    }
}
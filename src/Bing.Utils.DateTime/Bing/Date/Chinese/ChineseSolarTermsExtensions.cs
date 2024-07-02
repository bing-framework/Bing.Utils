namespace Bing.Date.Chinese;

/// <summary>
/// 农历二十四节气扩展
/// </summary>
public static class ChineseSolarTermsExtensions
{
    /// <summary>
    /// 获取中文名称
    /// </summary>
    /// <param name="chineseSolarTerms">农历二十四节气</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为false，使用简体中文</param>
    /// <returns>返回指定节气的中文名称</returns>
    public static string GetName(this ChineseSolarTerms chineseSolarTerms, bool traditionalChineseCharacters = false) =>
        ChineseSolarTermHelper.GetName(chineseSolarTerms, traditionalChineseCharacters);

    /// <summary>
    /// 获取英文名称
    /// </summary>
    /// <param name="chineseSolarTerms">节气枚举</param>
    /// <returns>返回指定节气的英文名称</returns>
    public static string GetEnglishName(this ChineseSolarTerms chineseSolarTerms) =>
        ChineseSolarTermHelper.GetEnglishName(chineseSolarTerms);
}
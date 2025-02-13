using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 区域文化
/// </summary>
public static class Culture
{
    /// <summary>
    /// 获取当前区域文化
    /// </summary>
    public static CultureInfo GetCurrentCulture() => CultureInfo.CurrentCulture;

    /// <summary>
    /// 获取当前UI区域文化
    /// </summary>
    public static CultureInfo GetCurrentUICulture() => CultureInfo.CurrentUICulture;

    /// <summary>
    /// 获取当前区域文化名称
    /// </summary>
    public static string GetCurrentCultureName() => CultureInfo.CurrentCulture.Name;

    /// <summary>
    /// 获取当前UI区域文化名称
    /// </summary>
    public static string GetCurrentUICultureName() => CultureInfo.CurrentUICulture.Name;

    /// <summary>
    /// 获取当前区域文化信息列表，包含所有父区域文化
    /// </summary>
    public static List<CultureInfo> GetCurrentCultures() => GetCultures(GetCurrentCulture());

    /// <summary>
    /// 获取当前UI区域文化信息列表，包含所有父区域文化
    /// </summary>
    public static List<CultureInfo> GetCurrentUICultures() => GetCultures(GetCurrentUICulture());

    /// <summary>
    /// 获取区域文化信息列表，包含所有父区域文化
    /// </summary>
    /// <param name="culture">区域文化信息</param>
    public static List<CultureInfo> GetCultures(CultureInfo culture)
    {
        var result = new List<CultureInfo>();
        if (culture == null)
            return result;
        while (culture.Equals(culture.Parent) == false)
        {
            result.Add(culture);
            culture = culture.Parent;
        }
        return result;
    }

    /// <summary>
    /// 临时指定区域文化
    /// </summary>
    /// <param name="culture">区域文化名称</param>
    /// <param name="uiCulture">UI区域文化名称</param>
    public static IDisposable Use(string culture, string uiCulture = null)
    {
        Check.NotNull(culture, nameof(culture));
        return Use(new CultureInfo(culture), uiCulture == null ? null : new CultureInfo(uiCulture));
    }

    /// <summary>
    /// 临时指定区域文化
    /// </summary>
    /// <param name="culture">区域文化名称</param>
    /// <param name="uiCulture">UI区域文化名称</param>
    public static IDisposable Use(CultureInfo culture, CultureInfo uiCulture = null)
    {
        Check.NotNull(culture, nameof(culture));

        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture ?? culture;

        return new DisposeAction<ValueTuple<CultureInfo, CultureInfo>>(static (state) =>
        {
            var (currentCulture, currentUiCulture) = state;
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        }, (currentCulture, currentUiCulture));
    }

    /// <summary>
    /// 文本书写方向是否从右到左
    /// </summary>
    public static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    /// <summary>
    /// 是否有效的区域文化编码
    /// </summary>
    /// <param name="cultureCode">区域文化编码</param>
    public static bool IsValidCultureCode(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            return false;
        try
        {
            _ = CultureInfo.GetCultureInfo(cultureCode);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取父区域文化名称
    /// </summary>
    /// <param name="cultureName">区域文化名称</param>
    public static string GetBaseCultureName(string cultureName) => new CultureInfo(cultureName).Parent.Name;

    /// <summary>
    /// 是否兼容的区域文化
    /// </summary>
    /// <param name="sourceCultureName">源区域文化名称</param>
    /// <param name="targetCultureName">目标区域文化名称</param>
    public static bool IsCompatibleCulture(string sourceCultureName, string targetCultureName)
    {
        if (sourceCultureName == targetCultureName)
            return true;
        if (sourceCultureName.StartsWith("zh") && targetCultureName.StartsWith("zh"))
        {
            var culture = new CultureInfo(targetCultureName);
            do
            {
                if (culture.Name == sourceCultureName)
                    return true;
                culture = new CultureInfo(culture.Name).Parent;
            } while (!culture.Equals(CultureInfo.InvariantCulture));
        }

        if (sourceCultureName.Contains("-"))
            return false;
        if (!targetCultureName.Contains("-"))
            return false;
        if (sourceCultureName == GetBaseCultureName(targetCultureName))
            return true;
        return false;
    }
}
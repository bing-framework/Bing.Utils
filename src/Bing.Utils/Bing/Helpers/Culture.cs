using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 区域文化工具类，提供区域文化信息的获取、切换和验证功能
/// </summary>
public static class Culture
{
    /// <summary>
    /// 获取当前线程的区域文化信息
    /// </summary>
    /// <returns>当前线程的 <see cref="CultureInfo"/> 对象</returns>
    /// <remarks>
    /// 此方法是对 <see cref="CultureInfo.CurrentCulture"/> 的简单封装。
    /// CurrentCulture 主要影响数字、日期、时间和货币的格式化。
    /// </remarks>
    /// <example>
    /// <code>
    /// var culture = Culture.GetCurrentCulture();
    /// Console.WriteLine($"当前区域文化: {culture.Name}");
    /// Console.WriteLine($"语言: {culture.DisplayName}");
    /// </code>
    /// </example>
    public static CultureInfo GetCurrentCulture() => CultureInfo.CurrentCulture;

    /// <summary>
    /// 获取当前线程的UI区域文化信息
    /// </summary>
    /// <returns>当前线程的UI <see cref="CultureInfo"/> 对象</returns>
    /// <remarks>
    /// 此方法是对 <see cref="CultureInfo.CurrentUICulture"/> 的简单封装。
    /// CurrentUICulture 主要影响用户界面文本的本地化和资源文件的选择。
    /// </remarks>
    /// <example>
    /// <code>
    /// var uiCulture = Culture.GetCurrentUICulture();
    /// Console.WriteLine($"当前UI区域文化: {uiCulture.Name}");
    /// Console.WriteLine($"界面语言: {uiCulture.DisplayName}");
    /// </code>
    /// </example>
    public static CultureInfo GetCurrentUICulture() => CultureInfo.CurrentUICulture;

    /// <summary>
    /// 获取当前线程的区域文化名称
    /// </summary>
    /// <returns>当前区域文化的名称，如 "zh-CN"、"en-US" 等</returns>
    /// <remarks>
    /// 返回的是区域文化的标准名称，通常采用 "语言-地区" 的格式。
    /// 例如：zh-CN（中文-中国）、en-US（英语-美国）、fr-FR（法语-法国）等。
    /// </remarks>
    /// <example>
    /// <code>
    /// string cultureName = Culture.GetCurrentCultureName();
    /// Console.WriteLine($"当前区域文化名称: {cultureName}"); // 输出如: zh-CN
    /// </code>
    /// </example>
    public static string GetCurrentCultureName() => CultureInfo.CurrentCulture.Name;

    /// <summary>
    /// 获取当前线程的UI区域文化名称
    /// </summary>
    /// <returns>当前UI区域文化的名称，如 "zh-CN"、"en-US" 等</returns>
    /// <remarks>
    /// 返回的是UI区域文化的标准名称，用于确定界面显示语言。
    /// 在多语言应用程序中，此值决定了加载哪个资源文件。
    /// </remarks>
    /// <example>
    /// <code>
    /// string uiCultureName = Culture.GetCurrentUICultureName();
    /// Console.WriteLine($"当前UI区域文化名称: {uiCultureName}"); // 输出如: zh-CN
    /// </code>
    /// </example>
    public static string GetCurrentUICultureName() => CultureInfo.CurrentUICulture.Name;

    /// <summary>
    /// 获取当前线程区域文化的完整层次结构列表
    /// </summary>
    /// <returns>包含当前区域文化及其所有父区域文化的列表</returns>
    /// <remarks>
    /// 返回的列表按照从具体到一般的顺序排列。
    /// 例如，对于 "zh-CN"，列表可能包含：zh-CN → zh-Hans → zh。
    /// </remarks>
    /// <example>
    /// <code>
    /// var cultures = Culture.GetCurrentCultures();
    /// foreach (var culture in cultures)
    /// {
    ///     Console.WriteLine($"层次: {culture.Name} - {culture.DisplayName}");
    /// }
    /// </code>
    /// </example>
    public static List<CultureInfo> GetCurrentCultures() => GetCultures(GetCurrentCulture());

    /// <summary>
    /// 获取当前线程UI区域文化的完整层次结构列表
    /// </summary>
    /// <returns>包含当前UI区域文化及其所有父区域文化的列表</returns>
    /// <remarks>
    /// 返回的列表按照从具体到一般的顺序排列。
    /// 这个层次结构在资源文件查找时非常有用，系统会按此顺序查找合适的资源。
    /// </remarks>
    /// <example>
    /// <code>
    /// var uiCultures = Culture.GetCurrentUICultures();
    /// foreach (var culture in uiCultures)
    /// {
    ///     Console.WriteLine($"UI层次: {culture.Name} - {culture.DisplayName}");
    /// }
    /// </code>
    /// </example>
    public static List<CultureInfo> GetCurrentUICultures() => GetCultures(GetCurrentUICulture());

    /// <summary>
    /// 获取指定区域文化的完整层次结构列表
    /// </summary>
    /// <param name="culture">要获取层次结构的区域文化信息</param>
    /// <returns>包含指定区域文化及其所有父区域文化的列表</returns>
    /// <remarks>
    /// 此方法遍历区域文化的父级链，直到到达不变区域文化为止。
    /// 返回的列表按照从具体到一般的顺序排列。
    /// 对于 null 输入，返回空列表。
    /// </remarks>
    /// <example>
    /// <code>
    /// var zhCN = new CultureInfo("zh-CN");
    /// var cultures = Culture.GetCultures(zhCN);
    /// // 可能返回: ["zh-CN", "zh-Hans", "zh"]
    /// 
    /// var enUS = new CultureInfo("en-US");
    /// var cultures2 = Culture.GetCultures(enUS);
    /// // 可能返回: ["en-US", "en"]
    /// </code>
    /// </example>
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
    /// 临时切换到指定的区域文化，返回一个可释放的对象用于恢复原始设置
    /// </summary>
    /// <param name="culture">要切换到的区域文化名称，如 "zh-CN"、"en-US" 等</param>
    /// <param name="uiCulture">要切换到的UI区域文化名称，如果为null则使用与culture相同的值</param>
    /// <returns>一个 <see cref="IDisposable"/> 对象，释放时会恢复原始的区域文化设置</returns>
    /// <exception cref="ArgumentNullException">当 culture 参数为 null 时抛出</exception>
    /// <exception cref="CultureNotFoundException">当指定的区域文化名称无效时抛出</exception>
    /// <remarks>
    /// 此方法提供了一个安全的方式来临时切换区域文化。
    /// 建议在 using 语句中使用，以确保区域文化能够正确恢复。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 临时切换到英语环境
    /// using (Culture.Use("en-US"))
    /// {
    ///     // 在这个作用域内，数字、日期等格式都会使用英语格式
    ///     Console.WriteLine(DateTime.Now.ToString("D")); // 输出英文日期格式
    ///     Console.WriteLine((1234.56).ToString("C"));    // 输出美元货币格式
    /// }
    /// // 离开作用域后，区域文化自动恢复为原始设置
    /// </code>
    /// </example>
    public static IDisposable Use(string culture, string uiCulture = null)
    {
        Check.NotNull(culture, nameof(culture));
        try
        {
            var cultureInfo = new CultureInfo(culture);
            var uiCultureInfo = uiCulture == null ? null : new CultureInfo(uiCulture);
            return Use(cultureInfo, uiCultureInfo);
        }
        catch (CultureNotFoundException ex)
        {
            throw new CultureNotFoundException($"无效的区域文化名称: '{culture}' 或 '{uiCulture}'", ex);
        }
    }

    /// <summary>
    /// 临时切换到指定的区域文化，返回一个可释放的对象用于恢复原始设置
    /// </summary>
    /// <param name="culture">要切换到的区域文化信息</param>
    /// <param name="uiCulture">要切换到的UI区域文化信息，如果为null则使用与culture相同的值</param>
    /// <returns>一个 <see cref="IDisposable"/> 对象，释放时会恢复原始的区域文化设置</returns>
    /// <exception cref="ArgumentNullException">当 culture 参数为 null 时抛出</exception>
    /// <remarks>
    /// 此方法是 Use(string, string) 重载的底层实现。
    /// 它直接接受 CultureInfo 对象，避免了字符串解析的开销。
    /// </remarks>
    /// <example>
    /// <code>
    /// var usCulture = new CultureInfo("en-US");
    /// var frenchUiCulture = new CultureInfo("fr-FR");
    /// 
    /// using (Culture.Use(usCulture, frenchUiCulture))
    /// {
    ///     // 数字格式使用美式英语，但UI显示使用法语
    ///     Console.WriteLine((1234.56).ToString("C")); // 美元格式
    ///     // UI文本会尝试加载法语资源
    /// }
    /// </code>
    /// </example>
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
    /// 获取当前UI区域文化的文本书写方向是否为从右到左
    /// </summary>
    /// <returns>如果当前UI区域文化是从右到左的文字方向（如阿拉伯语、希伯来语），返回 true；否则返回 false</returns>
    /// <remarks>
    /// 此属性主要用于UI布局和文本渲染。
    /// 从右到左的语言包括：阿拉伯语(ar)、希伯来语(he)、乌尔都语(ur)等。
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isRtl = Culture.IsRtl;
    /// if (isRtl)
    /// {
    ///     // 调整UI布局为从右到左
    ///     textBox.RightToLeft = RightToLeft.Yes;
    /// }
    /// </code>
    /// </example>
    public static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    /// <summary>
    /// 验证指定的区域文化代码是否有效
    /// </summary>
    /// <param name="cultureCode">要验证的区域文化代码，如 "zh-CN"、"en-US" 等</param>
    /// <returns>如果区域文化代码有效返回 true，否则返回 false</returns>
    /// <remarks>
    /// 此方法通过尝试创建 CultureInfo 对象来验证代码的有效性。
    /// 对于 null、空字符串或空白字符串，返回 false。
    /// 此方法不会抛出异常，而是返回验证结果。
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isValid1 = Culture.IsValidCultureCode("zh-CN");    // true
    /// bool isValid2 = Culture.IsValidCultureCode("en-US");    // true
    /// bool isValid3 = Culture.IsValidCultureCode("invalid");  // false
    /// bool isValid4 = Culture.IsValidCultureCode(null);       // false
    /// bool isValid5 = Culture.IsValidCultureCode("");         // false
    /// 
    /// // 在动态设置区域文化前验证
    /// string userInput = GetUserSelectedCulture();
    /// if (Culture.IsValidCultureCode(userInput))
    /// {
    ///     using (Culture.Use(userInput))
    ///     {
    ///         // 安全地使用用户选择的区域文化
    ///     }
    /// }
    /// </code>
    /// </example>
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
        catch (ArgumentException)
        {
            // 处理格式错误的文化代码
            return false;
        }
    }

    /// <summary>
    /// 获取指定区域文化的父级区域文化名称
    /// </summary>
    /// <param name="cultureName">区域文化名称，如 "zh-CN"、"en-US" 等</param>
    /// <returns>父级区域文化名称，如 "zh-CN" 的父级是 "zh-Hans" 或 "zh"</returns>
    /// <exception cref="ArgumentNullException">当 cultureName 为 null 时抛出</exception>
    /// <exception cref="CultureNotFoundException">当指定的区域文化名称无效时抛出</exception>
    /// <remarks>
    /// 此方法返回区域文化层次结构中的直接父级。
    /// 对于已经是根级别的区域文化，返回不变区域文化("")的名称。
    /// </remarks>
    /// <example>
    /// <code>
    /// string parent1 = Culture.GetBaseCultureName("zh-CN");    // 可能返回 "zh-Hans"
    /// string parent2 = Culture.GetBaseCultureName("en-US");    // 返回 "en"
    /// string parent3 = Culture.GetBaseCultureName("zh");       // 返回 ""
    /// </code>
    /// </example>
    public static string GetBaseCultureName(string cultureName)
    {
        Check.NotNull(cultureName, nameof(cultureName));

        try
        {
            return new CultureInfo(cultureName).Parent.Name;
        }
        catch (CultureNotFoundException ex)
        {
            throw new CultureNotFoundException($"无效的区域文化名称: '{cultureName}'", ex);
        }
    }

    /// <summary>
    /// 检查两个区域文化是否兼容
    /// </summary>
    /// <param name="sourceCultureName">源区域文化名称</param>
    /// <param name="targetCultureName">目标区域文化名称</param>
    /// <returns>如果两个区域文化兼容返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当任一参数为 null 时抛出</exception>
    /// <remarks>
    /// 区域文化兼容性规则：
    /// 1. 相同的区域文化总是兼容的
    /// 2. 对于中文区域文化，会检查完整的层次结构
    /// 3. 对于其他语言，父级语言与子级区域兼容（如 "en" 与 "en-US" 兼容）
    /// 4. 具体区域之间不兼容（如 "en-US" 与 "en-GB" 不兼容）
    /// </remarks>
    /// <example>
    /// <code>
    /// // 相同文化
    /// bool compatible1 = Culture.IsCompatibleCulture("zh-CN", "zh-CN");     // true
    /// 
    /// // 中文文化层次
    /// bool compatible2 = Culture.IsCompatibleCulture("zh", "zh-CN");        // true
    /// bool compatible3 = Culture.IsCompatibleCulture("zh-Hans", "zh-CN");   // true
    /// 
    /// // 英文文化层次
    /// bool compatible4 = Culture.IsCompatibleCulture("en", "en-US");        // true
    /// bool compatible5 = Culture.IsCompatibleCulture("en-US", "en-GB");     // false
    /// 
    /// // 不同语言
    /// bool compatible6 = Culture.IsCompatibleCulture("zh", "en");           // false
    /// </code>
    /// </example>
    public static bool IsCompatibleCulture(string sourceCultureName, string targetCultureName)
    {
        Check.NotNull(sourceCultureName, nameof(sourceCultureName));
        Check.NotNull(targetCultureName, nameof(targetCultureName));

        if (sourceCultureName == targetCultureName)
            return true;

        try
        {
            // 对中文文化进行特殊处理，检查完整的层次结构
            if (sourceCultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase) &&
                targetCultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return IsChineseCultureCompatible(sourceCultureName, targetCultureName);
            
            // 检查一般的语言-地区兼容性
            return IsGeneralCultureCompatible(sourceCultureName, targetCultureName);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// 检查中文区域文化的兼容性
    /// </summary>
    /// <param name="sourceCultureName">源中文区域文化名称</param>
    /// <param name="targetCultureName">目标中文区域文化名称</param>
    /// <returns>如果兼容返回 true，否则返回 false</returns>
    private static bool IsChineseCultureCompatible(string sourceCultureName, string targetCultureName)
    {
        var targetCulture = new CultureInfo(targetCultureName);

        // 遍历目标文化的层次结构，查找是否包含源文化
        do
        {
            if (targetCulture.Name.Equals(sourceCultureName, StringComparison.OrdinalIgnoreCase))
                return true;
            targetCulture = targetCulture.Parent;
        } while (!targetCulture.Equals(CultureInfo.InvariantCulture));

        return false;
    }

    /// <summary>
    /// 检查一般区域文化的兼容性
    /// </summary>
    /// <param name="sourceCultureName">源区域文化名称</param>
    /// <param name="targetCultureName">目标区域文化名称</param>
    /// <returns>如果兼容返回 true，否则返回 false</returns>
    private static bool IsGeneralCultureCompatible(string sourceCultureName, string targetCultureName)
    {
        // 如果源文化包含地区信息，则不兼容（具体地区间不兼容）
        if (sourceCultureName.IndexOf('-') >= 0)
            return false;

        // 如果目标文化不包含地区信息，则不兼容
        if (targetCultureName.IndexOf('-') < 0)
            return false;

        // 检查源文化是否是目标文化的父级
        try
        {
            var baseCultureName = GetBaseCultureName(targetCultureName);
            return sourceCultureName.Equals(baseCultureName, StringComparison.OrdinalIgnoreCase);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
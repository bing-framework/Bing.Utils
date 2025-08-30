using Bing.Text;

namespace Bing.Helpers;

/// <summary>
/// UserAgent操作辅助类
/// </summary>
public static class UserAgentHelper
{
    #region 常量

    /// <summary>
    /// 未知操作系统标识
    /// </summary>
    private const string UnknownOperatingSystem = "Other OperationSystem";

    /// <summary>
    /// 未知浏览器标识
    /// </summary>
    private const string UnknownBrowser = "Other Browser";

    /// <summary>
    /// 微信浏览器标识
    /// </summary>
    private const string WechatIdentifier = "MicroMessenger";

    #endregion

    #region 字典

    /// <summary>
    /// 操作系统字典
    /// </summary>
    public static IDictionary<string, string> OperationSystemDict { get; set; } = new Dictionary<string, string>()
    {
        // Windows版本（从新到旧，从具体到通用）
        {"NT 10.0","Windows 10" },
        {"NT 6.2","Windows 8" },
        {"NT 6.1","Windows 7" },
        {"NT 6.0","Windows Vista/Server 2008" },
        {"NT 5.2","Windows Server 2003" },
        {"NT 5.1","Windows XP" },
        {"NT 5.0","Windows 2000" },

        // 移动操作系统（更具体的匹配）
        {"Android","Linux" },               // Android基于Linux
        {"iPhone OS","Mac" },               // iOS
        {"CPU iPhone OS","Mac" },           // iOS的另一种表示
        {"CPU OS","Mac" },                  // iPad iOS
        {"iPad","Mac" },                    // iPad

        // Mac相关（按特异性从高到低排序）
        {"Macintosh","Mac" },               // macOS/Mac OS X
        {"Mac OS X","Mac" },                // Mac OS X
        {"Darwin","Mac" },                  // macOS内核
        {"Mac","Mac" },                     // 通用Mac标识，放在后面避免过早匹配

        // Linux和Unix系统
        {"Linux","Linux" },                 // Linux
        {"Ubuntu","Linux" },                // Ubuntu Linux
        {"Debian","Linux" },                // Debian Linux
        {"CentOS","Linux" },                // CentOS Linux
        {"Fedora","Linux" },                // Fedora Linux
        {"Unix","UNIX" },                   // UNIX
        {"FreeBSD","FreeBSD" },             // FreeBSD
        {"SunOS","Solaris" },               // Solaris
        {"Solaris","Solaris" },             // Solaris

        // 主要操作系统
        {"SUSE","Linux" },                  // SUSE Linux
        {"Red Hat","Linux" },               // Red Hat Linux
        {"OpenBSD","FreeBSD" },             // OpenBSD (归类为FreeBSD家族)
        {"NetBSD","FreeBSD" },              // NetBSD (归类为FreeBSD家族)
        {"X11","Linux" },                   // X11通常表示Linux/Unix

        // Windows旧版本（放在最后，避免误匹配）
        {"Windows ME","Windows ME" },       // 完整的Windows ME标识
        {"Win 9x 4.90","Windows ME" },      // Windows ME的另一种标识
        {"ME","Windows ME" },               // 最后匹配ME，避免误匹配MicroMessenger等
    };

    /// <summary>
    /// 浏览器字典
    /// </summary>
    public static IDictionary<string, string> BrowserDict { get; set; } = new Dictionary<string, string>()
    {
        // 360浏览器相关标识符
        {"LBBROWSER","360安全浏览器" },        // 360安全浏览器的另一个标识符
        {"360se","360安全浏览器" },
        {"360ee","360极速浏览器" },            // 360极速浏览器
        {"360chrome","360浏览器" },            // 360浏览器通用标识
    
        // QQ和腾讯系浏览器
        {"QQBrowser","QQ浏览器" },
        {"TencentTraveler","腾讯TT浏览器" },
    
        // 搜狗浏览器
        {"MetaSr","搜狗高速浏览器" },
        {"SogouMobileBrowser","搜狗手机浏览器" },
    
        // 百度浏览器
        {"BIDUBrowser","百度浏览器" },
        {"baidubrowser","百度浏览器" },        // 小写版本
    
        // 遨游浏览器
        {"Maxthon","遨游浏览器" },

        // UC浏览器
        {"UCBrowser","UC浏览器" },
        {"UCWEB","UC浏览器" },                 // UC浏览器移动版标识
    
        // 小米浏览器
        {"MiuiBrowser","小米浏览器" },
        {"XiaoMi","小米浏览器" },
        {"MIUI","MIUI浏览器" },
    
        // 华为浏览器
        {"HuaweiBrowser","华为浏览器" },
    
        // Vivo和Oppo浏览器
        {"VivoBrowser","Vivo浏览器" },
        {"OppoBrowser","Oppo浏览器" },
    
        // 三星浏览器
        {"SamsungBrowser","三星浏览器" },

        // 现代基于Chromium的浏览器
        {"Edg","Microsoft Edge" },            // 新版Edge (Chromium内核)
        {"Edge","Microsoft Edge Legacy" },    // 旧版Edge
        {"Brave","Brave" },
        {"Vivaldi","Vivaldi" },
        {"YaBrowser","Yandex浏览器" },
        {"DuckDuckGo","DuckDuckGo" },
        {"Whale","Naver Whale" },             // 鲸鱼浏览器
        {"Quark","夸克浏览器" },

        // 其他浏览器
        {"GreenBrowser","Green浏览器" },
        {"TheWorld","世界之窗浏览器" },
        {"Avant","Avant浏览器" },
        {"Sleipnir","Sleipnir" },
        {"Flock","Flock" },
        {"Camino","Camino" },
        {"Konqueror","Konqueror" },
        {"Epiphany","Epiphany" },
        {"SeaMonkey","SeaMonkey" },
        {"Galeon","Galeon" },
        {"Netscape","Netscape" },
        {"K-Meleon","K-Meleon" },

        // IE浏览器（版本特定的标识符）
        {"MSIE 6.0","Internet Explorer 6.0" },
        {"MSIE 7.0","Internet Explorer 7.0" },
        {"MSIE 8.0","Internet Explorer 8.0" },
        {"MSIE 9.0","Internet Explorer 9.0" },
        {"MSIE 10.0","Internet Explorer 10.0" },
        {"MSIE 11.0","Internet Explorer 11.0" },
        {"MSIE","Internet Explorer" },         // 通用IE标识

        // 主流浏览器（放在最后，作为回退选项）
        {"Firefox","Firefox" },
        {"Opera","Opera" },
        {"Safari","Safari" },
        {"Chrome","Chrome" },                  // Chrome放在最后，因为很多浏览器都基于Chrome

        // 文本浏览器
        {"Lynx","Lynx"},                  // Lynx文本浏览器
        {"Links","Links"},                // Links文本浏览器
        {"w3m","w3m"},                    // w3m文本浏览器
        {"Elinks","Elinks"},              // Elinks文本浏览器
    };

    #endregion

    #region GetOperatingSystemName(根据 UserAgent 获取操作系统名称)

    /// <summary>
    /// 根据 UserAgent 获取操作系统名称
    /// </summary>
    /// <param name="userAgent">用户代理字符串</param>
    /// <returns>操作系统名称，如果无法识别则返回 "Other OperationSystem"</returns>
    /// <exception cref="ArgumentException">当 userAgent 参数为空或空白字符串时</exception>
    public static string GetOperatingSystemName(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("用户代理字符串不能为空或空白字符串", nameof(userAgent));
        return GetMatchedValue(userAgent, OperationSystemDict, UnknownOperatingSystem);
    }

    #endregion

    #region GetBrowserName(根据 UserAgent 获取浏览器名称)

    /// <summary>
    /// 根据 UserAgent 获取浏览器名称
    /// </summary>
    /// <param name="userAgent">用户代理字符串</param>
    /// <returns>浏览器名称，如果无法识别则返回 "Other Browser"</returns>
    /// <exception cref="ArgumentException">当 userAgent 参数为空或空白字符串时</exception>
    public static string GetBrowserName(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("用户代理字符串不能为空或空白字符串", nameof(userAgent));
        return GetMatchedValue(userAgent, BrowserDict, UnknownBrowser);
    }

    #endregion

    #region IsWechatBrowser(是否微信浏览器)

    /// <summary>
    /// 判断是否为微信浏览器
    /// </summary>
    /// <param name="userAgent">用户代理字符串</param>
    /// <returns>如果是微信浏览器返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentException">当 userAgent 参数为空或空白字符串时</exception>
    public static bool IsWechatBrowser(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("用户代理字符串不能为空或空白字符串", nameof(userAgent));
        return userAgent.Contains(WechatIdentifier, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    /// <summary>
    /// 添加自定义浏览器支持
    /// </summary>
    /// <param name="identifier">浏览器标识符（在UserAgent中出现的字符串）</param>
    /// <param name="displayName">浏览器显示名称</param>
    /// <param name="priority">优先级（数字越小优先级越高，用于控制匹配顺序）</param>
    /// <example>
    /// <code>
    /// UserAgentHelper.AddBrowserSupport("NewBrowser", "新浏览器", 1);
    /// </code>
    /// </example>
    public static void AddBrowserSupport(string identifier, string displayName, int priority = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("浏览器标识符不能为空", nameof(identifier));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("浏览器显示名称不能为空", nameof(displayName));

        // 如果需要按优先级重新排序字典
        if (priority < int.MaxValue)
        {
            var sortedDict = new Dictionary<string, string>();
            var tempList = new List<(string key, string value, int priority)>();

            // 将现有条目添加到临时列表
            foreach (var kvp in BrowserDict)
            {
                tempList.Add((kvp.Key, kvp.Value, int.MaxValue));
            }

            // 添加新条目
            tempList.Add((identifier, displayName, priority));

            // 按优先级排序
            tempList.Sort((x, y) => x.priority.CompareTo(y.priority));

            // 重建字典
            foreach (var item in tempList)
            {
                sortedDict[item.key] = item.value;
            }

            BrowserDict = sortedDict;
        }
        else
        {
            // 直接添加到末尾
            BrowserDict[identifier] = displayName;
        }
    }

    /// <summary>
    /// 移除浏览器支持
    /// </summary>
    /// <param name="identifier">浏览器标识符</param>
    /// <returns>是否成功移除</returns>
    public static bool RemoveBrowserSupport(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        return BrowserDict.Remove(identifier);
    }

    /// <summary>
    /// 检查是否支持指定浏览器
    /// </summary>
    /// <param name="identifier">浏览器标识符</param>
    /// <returns>是否支持</returns>
    public static bool IsBrowserSupported(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && BrowserDict.ContainsKey(identifier);
    }

    #region 私有辅助方法

    /// <summary>
    /// 从字典中获取匹配的值
    /// </summary>
    /// <param name="userAgent">用户代理字符串</param>
    /// <param name="dictionary">匹配字典</param>
    /// <param name="defaultValue">默认返回值</param>
    /// <returns>匹配的值或默认值</returns>
    private static string GetMatchedValue(string userAgent, IDictionary<string, string> dictionary, string defaultValue)
    {
        foreach (var keyValue in dictionary)
        {
            if (userAgent.Contains(keyValue.Key, StringComparison.OrdinalIgnoreCase))
                return keyValue.Value;
        }
        return defaultValue;
    }

    #endregion
}

/// <summary>
/// 用户代理信息
/// 参考地址：https://github.com/mumuy/browser/blob/master/Browser.js
/// </summary>
public class UserAgentInfo
{
    /// <summary>
    /// 浏览器
    /// </summary>
    public string Browser { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 内核
    /// </summary>
    public string Engine { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string Os { get; set; }

    /// <summary>
    /// 操作系统版本号
    /// </summary>
    public string OsVersion { get; set; }

    /// <summary>
    /// 设备
    /// </summary>
    public string Device { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; }
}
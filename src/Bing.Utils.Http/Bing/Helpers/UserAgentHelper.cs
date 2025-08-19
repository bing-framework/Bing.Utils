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
        {"NT 10.0","Windows 10" },
        {"NT 6.2","Windows 8" },
        {"NT 6.1","Windows 7" },
        {"NT 6.0","Windows Vista/Server 2008" },
        {"NT 5.2","Windows Server 2003" },
        {"NT 5.1","Windows XP" },
        {"NT 5.0","Windows 2000" },
        {"ME","Windows ME" },
        {"Mac","Mac" },
        {"Unix","UNIX" },
        {"Linux","Linux" },
        {"SunOs","Solaris" },
        {"FreeBSD","FreeBSD" },
    };

    /// <summary>
    /// 浏览器字典
    /// </summary>
    public static IDictionary<string, string> BrowserDict { get; set; } = new Dictionary<string, string>()
    {
        {"Maxthon","遨游浏览器" },
        {"MetaSr","搜狗高速浏览器" },
        {"BIDUBrowser","百度浏览器" },
        {"QQBrowser","QQ浏览器" },
        {"GreenBrowser","Green浏览器" },
        {"360se","360安全浏览器" },
        {"MSIE 6.0","Internet Explorer 6.0" },
        {"MSIE 7.0","Internet Explorer 7.0" },
        {"MSIE 8.0","Internet Explorer 8.0" },
        {"MSIE 9.0","Internet Explorer 9.0" },
        {"MSIE 10.0","Internet Explorer 10.0" },
        {"Firefox","Firefox" },
        {"Opera","Opera" },
        {"Chrome","Chrome" },
        {"Safari","Safari" },
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
using Microsoft.AspNetCore.Http;

namespace Bing.Helpers;

/// <summary>
/// Cookie 操作辅助类
/// </summary>
public static class CookieHelper
{
    /// <summary>
    /// 获取 Cookie 值
    /// </summary>
    /// <param name="name">Cookie 名称</param>
    /// <returns>Cookie 值，如果不存在则返回空字符串</returns>
    /// <exception cref="ArgumentException">当 name 参数为空或空白字符串时</exception>
    public static string GetCookie(string name) => GetCookie(Web.HttpContext, name);

    /// <summary>
    /// 获取 Cookie 值
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="name">Cookie 名称</param>
    /// <returns>Cookie 值，如果不存在则返回空字符串</returns>
    /// <exception cref="ArgumentException">当 name 参数为空或空白字符串时</exception>
    public static string GetCookie(HttpContext context, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cookie 名称不能为空或空白字符串", nameof(name));
        return context?.Request?.Cookies?[name] ?? string.Empty;
    }

    /// <summary>
    /// 写入 Cookie 值
    /// </summary>
    /// <param name="name">Cookie 名称</param>
    /// <param name="value">Cookie 值</param>
    /// <exception cref="ArgumentException">当 name 参数为空或空白字符串时</exception>
    /// <remarks>
    /// <list>
    /// <item><description>未设置过期时间，写入的是浏览器进程 Cookie</description></item>
    /// <item><description>浏览器关闭后 Cookie 自动失效</description></item>
    /// <item><description>默认启用 HttpOnly 安全选项</description></item>
    /// </list>
    /// </remarks>
    public static void WriteCookie(string name, string value) =>
        WriteCookie(Web.HttpContext, name, value);

    /// <summary>
    /// 写入 Cookie 值
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="name">Cookie 名称</param>
    /// <param name="value">Cookie 值</param>
    /// <exception cref="ArgumentException">当 name 参数为空或空白字符串时</exception>
    /// <exception cref="ArgumentNullException">当 context 参数为空时</exception>
    /// <remarks>
    /// <list>
    /// <item><description>未设置过期时间，写入的是浏览器进程 Cookie</description></item>
    /// <item><description>浏览器关闭后 Cookie 自动失效</description></item>
    /// <item><description>默认启用 HttpOnly 安全选项</description></item>
    /// </list>
    /// </remarks>
    public static void WriteCookie(HttpContext context, string name, string value)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context), "HTTP 上下文不能为空");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cookie 名称不能为空或空白字符串", nameof(name));

        var cookieOptions = new CookieOptions { HttpOnly = true };
        context.Response.Cookies.Append(name, value ?? string.Empty, cookieOptions);
    }

    /// <summary>
    /// 清空所有 Cookie
    /// </summary>
    public static void ClearCookie() => ClearCookie(Web.HttpContext);

    /// <summary>
    /// 清空所有 Cookie
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <exception cref="ArgumentNullException">当 context 参数为空时</exception>
    public static void ClearCookie(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context), "HTTP 上下文不能为空");
        foreach (var cookie in context.Request.Cookies.Keys)
            context.Response.Cookies.Delete(cookie);
    }
}
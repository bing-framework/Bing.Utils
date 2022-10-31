using System;
using System.IO;
using System.Reflection;

namespace Bing.Helpers;

/// <summary>
/// 平台操作
/// </summary>
public static class Platform
{
    /// <summary>
    /// 应用程序根目录
    /// </summary>
    /// <remarks>
    /// 等价于  <see cref="AppContext.BaseDirectory"/>
    /// </remarks>
    public static readonly string AppRoot = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// 应用程序名称
    /// </summary>
    public static string ApplicationName => Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// 获取物理路径
    /// </summary>
    /// <param name="relativePath">相对路径。范例："test/a.txt" 或 "/test/a.txt"</param>
    /// <returns>虚拟路径对应的物理路径</returns>
    public static string GetPhysicalPath(string relativePath)
    {
        if (relativePath.StartsWith("~"))
            relativePath = relativePath.TrimStart('~');
        if (relativePath.StartsWith("/"))
            relativePath = relativePath.TrimStart('/');
        if (relativePath.StartsWith("\\"))
            relativePath = relativePath.TrimStart('\\');
        return Path.Combine(AppRoot, relativePath);
    }
}
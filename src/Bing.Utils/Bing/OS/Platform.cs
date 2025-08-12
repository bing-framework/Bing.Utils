using System.Runtime.InteropServices;
using Bing.Helpers;

namespace Bing.OS;

/// <summary>
/// 平台操作
/// </summary>
[Obsolete("此类已过时，请使用 Bing.Helpers.Env 类获得更完整的环境操作功能。此类将在未来版本中移除。", false)]
public static class Platform
{
    /// <summary>
    /// 获取平台操作系统
    /// </summary>
    [Obsolete("请使用 Env.PlatformName 属性获得更完整的平台信息")] 
    public static string GetOSPlatform() =>
        Env.PlatformName switch
        {
            "Windows" => "Windows",
            "Linux" => "Linux",
            "macOS" => "OSX", // 注意：这里保持原有的 "OSX" 返回值以维持兼容性
            _ => string.Empty
        };

    /// <summary>
    /// 当前操作系统是否为 Linux 操作系统
    /// </summary>
    [Obsolete("请使用 Env.IsLinux 属性")]
    public static bool IsLinux => Env.IsLinux;

    /// <summary>
    /// 当前操作系统是否为 微软视窗（Windows）操作系统
    /// </summary>
    [Obsolete("请使用 Env.IsWindows 属性")]
    public static bool IsWindows => Env.IsWindows;

    /// <summary>
    /// 当前操作系统是否为苹果 MacOS（OSX） 操作系统
    /// </summary>
    [Obsolete("请使用 Env.IsOSX 属性")]
    public static bool IsOSX => Env.IsOSX;

    /// <summary>
    /// 服务器名称
    /// </summary>
    [Obsolete("请使用 Env.MachineName 属性")]
    public static string MachineName => Env.MachineName;

    /// <summary>
    /// 系统名称
    /// </summary>
    [Obsolete("请使用 Env.OSDescription 属性")]
    public static string OSDescription => Env.OSDescription;

    /// <summary>
    /// 系统及版本
    /// </summary>
    [Obsolete("请使用 Env.OSVersion 属性")]
    public static string OSVersion => Env.OSVersion;

    /// <summary>
    /// 系统框架
    /// </summary>
    [Obsolete("请使用 Env.FrameworkDescription 属性")]
    public static string FrameworkDescription => Env.FrameworkDescription;

    /// <summary>
    /// 系统架构
    /// </summary>
    [Obsolete("请使用 Env.OSArchitecture 属性")]
    public static Architecture OSArchitecture => Env.OSArchitecture;

    /// <summary>
    /// 进程架构
    /// </summary>
    [Obsolete("请使用 Env.ProcessArchitecture 属性")]
    public static Architecture ProcessArchitecture => Env.ProcessArchitecture;

    /// <summary>
    /// 当前项目目录
    /// </summary>
    /// <remarks>
    /// 注意：此属性已不再支持设置操作。如需修改工作目录，请使用 Env.WorkingDirectory。
    /// </remarks>
    [Obsolete("请使用 Env.WorkingDirectory 属性，该属性提供更安全的目录管理")]
    public static string CurrentDirectory => Env.WorkingDirectory;

    /// <summary>
    /// 系统目录
    /// </summary>
    [Obsolete("请使用 Env.SystemDirectory 属性")]
    public static string SystemDirectory => Env.SystemDirectory;

    /// <summary>
    /// 应用程序根目录
    /// </summary>
    /// <remarks>
    /// 等价于  <see cref="AppContext.BaseDirectory"/>
    /// </remarks>
    [Obsolete("请使用 Env.ApplicationBaseDirectory 属性")]
    public static string AppRoot => Env.ApplicationBaseDirectory;

    /// <summary>
    /// 应用程序名称
    /// </summary>
    [Obsolete("请使用 Env.ApplicationName 属性")]
    public static string ApplicationName => Env.ApplicationName;

    /// <summary>
    /// 获取物理路径
    /// </summary>
    /// <param name="relativePath">相对路径。范例："test/a.txt" 或 "/test/a.txt"</param>
    /// <param name="basePath">基路径。默认值：<see cref="AppContext.BaseDirectory"/></param>
    /// <returns>虚拟路径对应的物理路径</returns>
    [Obsolete("请使用 Common.GetPhysicalPath 方法，该方法提供更完善的路径处理功能")]
    public static string GetPhysicalPath(string relativePath, string basePath = null)
    {
        if (relativePath == null)
            throw new ArgumentNullException(nameof(relativePath));

        // 清理相对路径
        var cleanPath = relativePath;
        if (cleanPath.StartsWith("~"))
            cleanPath = cleanPath.TrimStart('~');
        if (cleanPath.StartsWith("/"))
            cleanPath = cleanPath.TrimStart('/');
        if (cleanPath.StartsWith("\\"))
            cleanPath = cleanPath.TrimStart('\\');

        // 使用提供的基路径或默认的应用程序基目录
        var baseDirectory = basePath ?? Env.ApplicationBaseDirectory;

        return Path.Combine(baseDirectory, cleanPath);
    }
}
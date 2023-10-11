using System.Runtime.InteropServices;

namespace Bing.OS;

/// <summary>
/// 平台操作
/// </summary>
public class Platform : IDisposable
{
    /// <summary>
    /// 获取平台操作系统
    /// </summary>
    public static string GetOSPlatform() => IsWindows ? "Windows" : IsLinux ? "Linux" : IsOSX ? "OSX" : string.Empty;

    /// <summary>
    /// 当前操作系统是否为 Linux 操作系统
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// 当前操作系统是否为 微软视窗（Windows）操作系统
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// 当前操作系统是否为苹果 MacOS（OSX） 操作系统
    /// </summary>
    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// 服务器名称
    /// </summary>
    public static string MachineName => Environment.MachineName;

    /// <summary>
    /// 系统名称
    /// </summary>
    public static string OSDescription => RuntimeInformation.OSDescription;

    /// <summary>
    /// 系统及版本
    /// </summary>
    public static string OSVersion => Environment.OSVersion.ToString();

    /// <summary>
    /// 系统框架
    /// </summary>
    public static string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// 系统架构
    /// </summary>
    public static Architecture OSArchitecture => RuntimeInformation.OSArchitecture;

    /// <summary>
    /// 进程架构
    /// </summary>
    public static Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <summary>
    /// 当前项目目录
    /// </summary>
    private static string _currentDirectory = Directory.GetCurrentDirectory();

    /// <summary>
    /// 当前项目目录
    /// </summary>
    internal static string CurrentDirectory
    {
        get => _currentDirectory;
        set
        {
            if (!string.IsNullOrEmpty(value))
                _currentDirectory = value;
        }
    }

    /// <summary>
    /// 系统目录
    /// </summary>
    public static string SystemDirectory => Environment.SystemDirectory;

    /// <summary>
    /// 应用程序根目录
    /// </summary>
    /// <remarks>
    /// 等价于  <see cref="AppContext.BaseDirectory"/>
    /// </remarks>
    [Obsolete("请使用 Common.ApplicationBaseDirectory 属性")]
    public static readonly string AppRoot = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// 应用程序名称
    /// </summary>
    public static string ApplicationName => Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// 获取物理路径
    /// </summary>
    /// <param name="relativePath">相对路径。范例："test/a.txt" 或 "/test/a.txt"</param>
    /// <param name="basePath">基路径。默认值：<see cref="AppContext.BaseDirectory"/></param>
    /// <returns>虚拟路径对应的物理路径</returns>
    [Obsolete("请使用 Common.GetPhysicalPath 方法")]
    public static string GetPhysicalPath(string relativePath, string basePath = null)
    {
        if (relativePath.StartsWith("~"))
            relativePath = relativePath.TrimStart('~');
        if (relativePath.StartsWith("/"))
            relativePath = relativePath.TrimStart('/');
        if (relativePath.StartsWith("\\"))
            relativePath = relativePath.TrimStart('\\');
        basePath ??= AppRoot;
        return Path.Combine(basePath, relativePath);
    }

    #region Dispose(检测冗余调用)

    /// <summary>
    /// 要检测冗余调用
    /// </summary>
    private bool _disposeValue = false;

    /// <summary>
    /// 释放托管
    /// </summary>
    /// <param name="disposing">状态</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposeValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                GC.SuppressFinalize(this);
            }

            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
            // TODO: 将大型字段设置为 null。
            _disposeValue = true;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// 析构器
    /// </summary>
    ~Platform()
    {
        Dispose(false);
    }

    #endregion
}
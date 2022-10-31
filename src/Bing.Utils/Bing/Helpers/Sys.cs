using System.Runtime.InteropServices;

namespace Bing.Helpers;

/// <summary>
/// 系统操作
/// </summary>
public static partial class Sys
{
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
    // ReSharper disable once InconsistentNaming
    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// 当前操作系统
    /// </summary>
    public static string System => IsWindows ? "Windows" : IsLinux ? "Linux" : IsOSX ? "OSX" : string.Empty;
}
using System;
using System.Diagnostics;
using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 程序集访问工具
/// </summary>
public static partial class AssemblyVisit
{
    /// <summary>
    /// 获取 <see cref="Assembly"/> 文件的版本
    /// </summary>
    /// <param name="assembly">程序集</param>
    public static string GetFileVersion(Assembly assembly) => V(assembly).FileVersion;

    /// <summary>
    /// 获取 <see cref="Assembly"/> 文件的产品版本
    /// </summary>
    /// <param name="assembly">程序集</param>
    public static string GetProductVersion(Assembly assembly) => V(assembly).ProductVersion;

    /// <summary>
    /// 访问
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static FileVersionInfo V(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));
        return FileVersionInfo.GetVersionInfo(assembly.Location);
    }
}
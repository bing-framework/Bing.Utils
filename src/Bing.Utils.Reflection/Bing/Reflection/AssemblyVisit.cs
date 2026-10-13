using System.Diagnostics;
using System.Reflection;
using Bing.Extensions;

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
    public static string GetProductVersion(Assembly assembly)
    {
        var version = V(assembly).ProductVersion;
        if (version?.Contains("+") == true)
            version = version.ReplaceWith(@"\+(\w+)?", "");
        return version;
    }

    /// <summary>
    /// 获取程序集文件的版本信息。
    /// </summary>
    /// <param name="assembly">要查询版本信息的程序集。</param>
    /// <returns>程序集文件的版本信息。</returns>
    private static FileVersionInfo V(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));
        return FileVersionInfo.GetVersionInfo(assembly.Location);
    }
}

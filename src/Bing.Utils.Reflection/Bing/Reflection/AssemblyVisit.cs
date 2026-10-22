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
    public static string GetFileVersion(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? string.Empty;
    }

    /// <summary>
    /// 获取 <see cref="Assembly"/> 文件的产品版本
    /// </summary>
    /// <param name="assembly">程序集</param>
    public static string GetProductVersion(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? string.Empty;
        if (version.Contains("+"))
            version = version.ReplaceWith(@"\+(\w+)?", "");
        return version;
    }
}

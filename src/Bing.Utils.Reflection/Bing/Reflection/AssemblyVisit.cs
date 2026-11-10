using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 程序集访问工具
/// </summary>
public static partial class AssemblyVisit
{
    /// <summary>
    /// 获取 <see cref="Assembly"/> 文件的版本。
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <returns>程序集文件版本；未找到版本信息时返回空字符串。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="assembly"/> 为 null 时抛出。</exception>
    public static string GetFileVersion(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? string.Empty;
    }

    /// <summary>
    /// 获取 <see cref="Assembly"/> 文件的产品版本。
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <returns>移除构建元数据后的产品版本；未找到版本信息时返回空字符串。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="assembly"/> 为 null 时抛出。</exception>
    /// <remarks>
    /// 优先使用信息版本，其次使用文件版本或程序集名称版本；仅移除第一个加号及其后的构建元数据，保留预发布版本。
    /// </remarks>
    public static string GetProductVersion(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? string.Empty;
        var suffixIndex = version.IndexOf('+');
        return suffixIndex >= 0 ? version.Substring(0, suffixIndex) : version;
    }
}

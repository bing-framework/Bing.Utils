
// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 反射 操作
/// </summary>
public static partial class Reflections
{
    #region GetCurrentAssemblyName(获取当前程序集名称)

    /// <summary>
    /// 获取当前程序集名称
    /// </summary>
    public static string GetCurrentAssemblyName() => Assembly.GetCallingAssembly().GetName().Name;

    #endregion

    #region GetAssembly(获取程序集)

    /// <summary>
    /// 获取程序集
    /// </summary>
    /// <param name="assemblyName">程序集名称</param>
    /// <returns></returns>
    public static Assembly GetAssembly(string assemblyName) => Assembly.Load(new AssemblyName(assemblyName));

    #endregion

    #region GetAssemblies(从目录获取所有程序集)

    /// <summary>
    /// 从目录获取所有程序集
    /// </summary>
    /// <param name="directoryPath">目录绝对路径</param>
    /// <exception cref="ArgumentException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static List<Assembly> GetAssemblies(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("程序集目录路径不能为空。", nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"程序集目录不存在: {directoryPath}");

        return Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                           || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(Assembly.LoadFrom)
            .ToList();
    }

    #endregion
}


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
    public static List<Assembly> GetAssemblies(string directoryPath) =>
        Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .ToList()
            .Where(t => t.EndsWith(".exe") || t.EndsWith(".dll"))
            .Select(path => Assembly.Load(new AssemblyName(path)))
            .ToList();

    #endregion
}
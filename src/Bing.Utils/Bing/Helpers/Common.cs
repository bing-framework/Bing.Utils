using Bing.IO;

namespace Bing.Helpers;

/// <summary>
/// 常用公共操作
/// </summary>
public static partial class Common
{
    #region ApplicationBaseDirectory(当前应用程序基路径)

    /// <summary>
    /// 当前应用程序基路径。路径末尾包含“\”。
    /// </summary>
    /// <remarks>
    /// 等价于  <see cref="AppContext.BaseDirectory"/>
    /// </remarks>
    public static string ApplicationBaseDirectory => AppContext.BaseDirectory;

    #endregion

    #region Line(换行符)

    /// <summary>
    /// 换行符
    /// </summary>
    public static string Line => Env.NewLine;

    #endregion

    #region GetType(获取类型)

    /// <summary>
    /// 获取类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static Type GetType<T>() => GetType(typeof(T));

    /// <summary>
    /// 获取类型
    /// </summary>
    /// <param name="type">类型</param>
    public static Type GetType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    #endregion

    #region Swap(交换值)

    /// <summary>
    /// 交换值。交换两个提供的变量中的值
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="a">变量A</param>
    /// <param name="b">变量B</param>
    public static void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

    #endregion

    #region GetPhysicalPath(获取物理路径)

    /// <summary>
    /// 获取物理路径
    /// </summary>
    /// <param name="relativePath">相对路径。范例："test/a.txt" 或 "/test/a.txt"</param>
    /// <param name="basePath">基路径。默认值：<see cref="AppContext.BaseDirectory"/></param>
    /// <returns>虚拟路径对应的物理路径</returns>
    public static string GetPhysicalPath(string relativePath, string basePath = null) => PathHelper.GetPhysicalPath(relativePath, basePath);

    #endregion

    #region JoinPath(连接路径)

    /// <summary>
    /// 连接路径
    /// </summary>
    /// <param name="paths">路径列表</param>
    public static string JoinPath(params string[] paths) => Url.Combine(paths);

    #endregion

    #region GetCurrentDirectory(获取当前目录路径)

    /// <summary>
    /// 获取当前目录路径
    /// </summary>
    public static string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    #endregion

    #region GetParentDirectory(获取当前目录的上级目录)

    /// <summary>
    /// 获取当前目录的上级路径
    /// </summary>
    /// <param name="depth">向上钻取的深度</param>
    public static string GetParentDirectory(int depth = 1)
    {
        var path = Directory.GetCurrentDirectory();
        for (var i = 0; i < depth; i++)
        {
            var parent = Directory.GetParent(path);
            if (parent is { Exists: true })
                path = parent.FullName;
        }
        return path;
    }

    #endregion
}
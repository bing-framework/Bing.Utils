using Bing.Helpers;
using Bing.OS;

namespace Bing.IO;

/// <summary>
/// 路径操作辅助类
/// </summary>
public static class PathHelper
{
    #region GetPhysicalPath(获取物理路径)

    /// <summary>
    /// 获取物理路径
    /// </summary>
    /// <param name="relativePath">相对路径。范例："test/a.txt" 或 "/test/a.txt"</param>
    /// <param name="basePath">基路径。默认值：<see cref="AppContext.BaseDirectory"/></param>
    /// <returns>虚拟路径对应的物理路径</returns>
    public static string GetPhysicalPath(string relativePath, string basePath = null)
    {
        if (relativePath.StartsWith("~"))
            relativePath = relativePath.TrimStart('~');
        if (relativePath.StartsWith("/"))
            relativePath = relativePath.TrimStart('/');
        if (relativePath.StartsWith("\\"))
            relativePath = relativePath.TrimStart('\\');
        basePath ??= Common.ApplicationBaseDirectory;
        return Path.Combine(basePath, relativePath);
    }

    #endregion

    #region GetWebRootPath(获取wwwroot路径)

    /// <summary>
    /// 获取wwwroot路径
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    public static string GetWebRootPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;
        var rootPath = Common.ApplicationBaseDirectory;
        if (string.IsNullOrWhiteSpace(rootPath))
            return Path.GetFullPath(relativePath);
        return $"{Common.ApplicationBaseDirectory}\\wwwroot\\{relativePath.Replace("/", "\\").TrimStart('\\')}";
    }

    #endregion

    /// <summary>
    /// 将 Windows 路径转换为 Unix 路径
    /// </summary>
    /// <param name="path">Windows路径</param>
    public static string ConvertWindowsPathToUnixPath(string path) => 
        string.IsNullOrWhiteSpace(path) ? null : path.Replace('\\', '/');

    /// <summary>
    /// 将 Unix 路径转换为 Windows 路径
    /// </summary>
    /// <param name="path">Unix路径</param>
    public static string ConvertUnixPathToWindowsPath(string path) =>
        string.IsNullOrWhiteSpace(path) ? null : path.Replace('/', '\\');

    /// <summary>
    /// 根据当前系统自动转换路径
    /// </summary>
    /// <param name="path">路径</param>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static string AutoPathConvert(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        if (Platform.IsLinux)
            return path.Replace('\\', '/');
        if (Platform.IsWindows || Platform.IsOSX)
            return path.Replace('/', '\\');
        throw new PlatformNotSupportedException("Convert path split failed. Unknown system.");
    }
}
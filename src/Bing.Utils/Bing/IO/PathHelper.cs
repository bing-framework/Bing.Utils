using Bing.Helpers;
using Bing.OS;

namespace Bing.IO;

/// <summary>
/// 路径操作辅助类
/// </summary>
public static class PathHelper
{
    #region 常量

    /// <summary>
    /// WWW根目录名称
    /// </summary>
    private const string WwwRootDirectoryName = "wwwroot";

    /// <summary>
    /// 路径分隔符集合
    /// </summary>
    private static readonly char[] PathSeparators = ['/', '\\', '~'];

    #endregion

    #region GetPhysicalPath(获取物理路径)

    /// <summary>
    /// 获取物理路径
    /// </summary>
    /// <param name="relativePath">相对路径。范例："test/a.txt" 或 "/test/a.txt"</param>
    /// <param name="basePath">基路径。默认值：<see cref="AppContext.BaseDirectory"/></param>
    /// <returns>虚拟路径对应的物理路径</returns>
    /// <exception cref="ArgumentNullException">当相对路径为空时抛出</exception>
    public static string GetPhysicalPath(string relativePath, string basePath = null)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentNullException(nameof(relativePath), "相对路径不能为空");

        // 标准化相对路径
        var normalizedPath = NormalizeRelativePath(relativePath);
        basePath ??= Common.ApplicationBaseDirectory;
        return Path.GetFullPath(Path.Combine(basePath, normalizedPath));
    }

    /// <summary>
    /// 标准化相对路径，移除前导分隔符
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <returns>标准化后的相对路径</returns>
    private static string NormalizeRelativePath(string relativePath) => string.IsNullOrWhiteSpace(relativePath) ? string.Empty : relativePath.TrimStart(PathSeparators);

    #endregion

    #region GetWebRootPath(获取wwwroot路径)

    /// <summary>
    /// 获取wwwroot路径
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <param name="basePath">基路径。默认值：<see cref="AppContext.BaseDirectory"/></param>
    /// <returns>wwwroot下的完整路径</returns>
    public static string GetWebRootPath(string relativePath, string basePath = null)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        basePath ??= Common.ApplicationBaseDirectory;
        if (string.IsNullOrWhiteSpace(basePath))
            return Path.GetFullPath(relativePath);

        // 标准化相对路径
        var normalizedPath = NormalizeRelativePath(relativePath);

        // 使用 Path.Combine 确保跨平台兼容性
        var webRootPath = Path.Combine(basePath, WwwRootDirectoryName);
        return Path.GetFullPath(Path.Combine(webRootPath, normalizedPath));
    }

    #endregion

    #region 路径分隔符转换

    /// <summary>
    /// 将 Windows 路径转换为 Unix 路径
    /// </summary>
    /// <param name="path">Windows路径</param>
    /// <returns>Unix格式路径，如果输入为空则返回null</returns>
    public static string ConvertWindowsPathToUnixPath(string path) =>
        string.IsNullOrWhiteSpace(path) ? null : path.Replace('\\', '/');

    /// <summary>
    /// 将 Unix 路径转换为 Windows 路径
    /// </summary>
    /// <param name="path">Unix路径</param>
    /// <returns>Windows格式路径，如果输入为空则返回null</returns>
    public static string ConvertUnixPathToWindowsPath(string path) =>
        string.IsNullOrWhiteSpace(path) ? null : path.Replace('/', '\\');

    /// <summary>
    /// 根据当前系统自动转换路径分隔符
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>当前系统格式的路径</returns>
    /// <exception cref="PlatformNotSupportedException">不支持的平台</exception>
    public static string AutoPathConvert(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        if (Platform.IsLinux)
            return path.Replace('\\', '/');
        if (Platform.IsWindows || Platform.IsOSX)
            return path.Replace('/', '\\');
        throw new PlatformNotSupportedException($"不支持的操作系统平台: {Environment.OSVersion.Platform}");
    }

    /// <summary>
    /// 标准化路径分隔符（使用当前系统的标准分隔符）
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>标准化后的路径</returns>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // 使用 Path.DirectorySeparatorChar 确保使用正确的分隔符
        var separator = Path.DirectorySeparatorChar;
        var altSeparator = Path.AltDirectorySeparatorChar;

        return path.Replace(altSeparator, separator);
    }

    #endregion

    #region 路径验证和处理

    /// <summary>
    /// 验证路径是否为有效的相对路径
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>是否为有效的相对路径</returns>
    public static bool IsValidRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // 检查是否包含非法字符
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            // 检查是否为绝对路径
            if (Path.IsPathRooted(path))
                return false;

            // 检查是否包含危险的路径遍历
            if (path.Contains(".."))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 安全地合并路径，防止路径遍历攻击
    /// </summary>
    /// <param name="basePath">基础路径</param>
    /// <param name="relativePath">相对路径</param>
    /// <returns>安全的完整路径</returns>
    /// <exception cref="ArgumentException">当路径不安全时抛出</exception>
    public static string SafeCombinePaths(string basePath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentNullException(nameof(basePath));

        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentNullException(nameof(relativePath));

        if (!IsValidRelativePath(relativePath))
            throw new ArgumentException("相对路径包含非法字符或路径遍历", nameof(relativePath));

        var fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
        var baseFullPath = Path.GetFullPath(basePath);

        // 确保结果路径在基础路径内
        if (!fullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("路径遍历被阻止", nameof(relativePath));

        return fullPath;
    }

    /// <summary>
    /// 获取相对路径
    /// </summary>
    /// <param name="fromPath">起始路径</param>
    /// <param name="toPath">目标路径</param>
    /// <returns>从起始路径到目标路径的相对路径</returns>
    public static string GetRelativePath(string fromPath, string toPath)
    {
        if (string.IsNullOrWhiteSpace(fromPath))
            throw new ArgumentNullException(nameof(fromPath));

        if (string.IsNullOrWhiteSpace(toPath))
            throw new ArgumentNullException(nameof(toPath));

#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        // .NET Core 3.0+ 提供了内置方法
        return Path.GetRelativePath(fromPath, toPath);
#else
        // .NET Standard 2.0 兼容实现
        return GetRelativePathCompat(fromPath, toPath);
#endif
    }

    /// <summary>
    /// 获取相对路径的兼容实现（适用于 .NET Standard 2.0）
    /// </summary>
    /// <param name="fromPath">起始路径</param>
    /// <param name="toPath">目标路径</param>
    /// <returns>相对路径</returns>
    private static string GetRelativePathCompat(string fromPath, string toPath)
    {
        // 标准化路径
        var fromUri = new Uri(Path.GetFullPath(fromPath) + Path.DirectorySeparatorChar);
        var toUri = new Uri(Path.GetFullPath(toPath));

        // 获取相对URI
        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        // 转换为本地路径分隔符
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    #endregion

    #region 路径信息获取

    /// <summary>
    /// 获取路径的各个组成部分
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>路径信息</returns>
    public static PathInfo GetPathInfo(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new PathInfo();

        return new PathInfo
        {
            FullPath = Path.GetFullPath(path),
            DirectoryName = Path.GetDirectoryName(path),
            FileName = Path.GetFileName(path),
            FileNameWithoutExtension = Path.GetFileNameWithoutExtension(path),
            Extension = Path.GetExtension(path),
            IsAbsolute = Path.IsPathRooted(path)
        };
    }

    /// <summary>
    /// 确保目录存在，如果不存在则创建
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>目录信息</returns>
    public static DirectoryInfo EnsureDirectoryExists(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));

        var dirInfo = new DirectoryInfo(directoryPath);
        if (!dirInfo.Exists)
            dirInfo.Create();

        return dirInfo;
    }

    /// <summary>
    /// 生成唯一的文件路径（如果文件已存在，则在文件名后添加数字）
    /// </summary>
    /// <param name="filePath">原始文件路径</param>
    /// <returns>唯一的文件路径</returns>
    public static string GetUniqueFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        var counter = 1;
        string uniquePath;

        do
        {
            var uniqueFileName = $"{fileNameWithoutExt}({counter}){extension}";
            uniquePath = Path.Combine(directory, uniqueFileName);
            counter++;
        } while (File.Exists(uniquePath));

        return uniquePath;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查两个路径是否相等
    /// </summary>
    /// <param name="path1">路径1</param>
    /// <param name="path2">路径2</param>
    /// <returns>是否相等</returns>
    public static bool PathEquals(string path1, string path2)
    {
        if (path1 == null && path2 == null)
            return true;

        if (path1 == null || path2 == null)
            return false;

        try
        {
            var fullPath1 = Path.GetFullPath(path1);
            var fullPath2 = Path.GetFullPath(path2);

            return string.Equals(fullPath1, fullPath2, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查路径是否在指定的基础路径下
    /// </summary>
    /// <param name="path">要检查的路径</param>
    /// <param name="basePath">基础路径</param>
    /// <returns>是否在基础路径下</returns>
    public static bool IsPathUnderBase(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullBasePath = Path.GetFullPath(basePath);

            return fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// 路径信息
/// </summary>
public class PathInfo
{
    /// <summary>
    /// 完整路径
    /// </summary>
    public string FullPath { get; set; }

    /// <summary>
    /// 目录名
    /// </summary>
    public string DirectoryName { get; set; }

    /// <summary>
    /// 文件名（包含扩展名）
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 文件名（不包含扩展名）
    /// </summary>
    public string FileNameWithoutExtension { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// 是否为绝对路径
    /// </summary>
    public bool IsAbsolute { get; set; }
}
using System.Diagnostics;
using Bing.Extensions;
using Bing.Helpers;
using Bing.OS;
using Bing.Reflection;

namespace Bing.IO;

/// <summary>
/// 目录操作辅助类
/// </summary>
public static class DirectoryHelper
{
    #region CreateDirectory(创建目录)

    /// <summary>
    /// 创建目录，如果目录已存在则不创建
    /// </summary>
    /// <param name="path">文件或目录绝对路径</param>
    /// <returns>创建的目录信息</returns>
    /// <exception cref="ArgumentException">当路径为空或无效时抛出</exception>
    public static DirectoryInfo CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("路径不能为空或空白字符", nameof(path));

        try
        {
            // 判断路径是文件路径还是目录路径
            var directoryPath = Path.HasExtension(path) ? Path.GetDirectoryName(path) : path;

            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentException("无法确定有效的目录路径", nameof(path));

            return Directory.CreateDirectory(directoryPath);
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            throw new InvalidOperationException($"创建目录失败: {ex.Message}", ex);
        }
    }

    #endregion

    #region CreateIfNotExists(创建文件夹，如果文件夹不存在)

    /// <summary>
    /// 创建文件夹，如果文件夹不存在
    /// </summary>
    /// <param name="directory">要创建的文件夹路径</param>
    /// <returns>目录信息</returns>
    public static DirectoryInfo CreateIfNotExists(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return null;
        if (!Directory.Exists(directory))
            return Directory.CreateDirectory(directory);
        return new DirectoryInfo(directory);
    }

    /// <summary>
    /// 创建文件夹，如果文件夹不存在
    /// </summary>
    /// <param name="directory">文件夹信息</param>
    /// <returns>目录信息</returns>
    public static DirectoryInfo CreateIfNotExists(DirectoryInfo directory)
    {
        if (directory == null)
            return null;
        if (!directory.Exists)
        {
            directory.Create();
            directory.Refresh(); // 刷新状态
        }
        return directory;
    }

    #endregion

    #region DeleteIfExists(删除文件夹，如果文件夹存在)

    /// <summary>
    /// 删除文件夹，如果文件夹存在
    /// </summary>
    /// <param name="directory">要删除的文件夹路径</param>
    /// <returns>是否成功删除</returns>
    public static bool DeleteIfExists(string directory) => DeleteIfExists(directory, false);

    /// <summary>
    /// 删除文件夹，如果文件夹存在
    /// </summary>
    /// <param name="directory">要删除的文件夹路径</param>
    /// <param name="recursive">是否递归删除所有子目录和文件</param>
    /// <returns>是否成功删除</returns>
    public static bool DeleteIfExists(string directory, bool recursive)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return false;
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"删除目录失败: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region IsSubDirectoryOf(是否指定父目录路径的子目录)

    /// <summary>
    /// 是否指定父目录路径的子目录
    /// </summary>
    /// <param name="parentDirectoryPath">父目录路径</param>
    /// <param name="childDirectoryPath">子目录路径</param>
    /// <returns>是否为子目录</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    public static bool IsSubDirectoryOf(string parentDirectoryPath, string childDirectoryPath)
    {
        Check.NotNull(parentDirectoryPath, nameof(parentDirectoryPath));
        Check.NotNull(childDirectoryPath, nameof(childDirectoryPath));

        try
        {
            var parentInfo = new DirectoryInfo(parentDirectoryPath);
            var childInfo = new DirectoryInfo(childDirectoryPath);
            return IsSubDirectoryOf(parentInfo, childInfo);
        }
        catch (Exception e)
        {
            InvokeHelper.OnInvokeException?.Invoke(e);
            return false;
        }
    }

    /// <summary>
    /// 是否指定父目录路径的子目录
    /// </summary>
    /// <param name="parentDirectory">父目录</param>
    /// <param name="childDirectory">子目录</param>
    /// <returns>是否为子目录</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    public static bool IsSubDirectoryOf(DirectoryInfo parentDirectory, DirectoryInfo childDirectory)
    {
        Check.NotNull(parentDirectory, nameof(parentDirectory));
        Check.NotNull(childDirectory, nameof(childDirectory));

        if (parentDirectory.FullName == childDirectory.FullName)
            return true;

        var parentOfChild = childDirectory.Parent;
        if (parentOfChild == null)
            return false;

        return IsSubDirectoryOf(parentDirectory, parentOfChild);
    }

    #endregion

    #region ChangeCurrentDirectory(更改当前目录)

    /// <summary>
    /// 更改当前目录
    /// </summary>
    /// <param name="targetDirectory">目标目录</param>
    /// <returns>用于恢复原目录的 IDisposable 对象</returns>
    /// <exception cref="ArgumentNullException">当目标目录为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目标目录不存在时抛出</exception>
    public static IDisposable ChangeCurrentDirectory(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new ArgumentNullException(nameof(targetDirectory));
        if (!Directory.Exists(targetDirectory))
            throw new DirectoryNotFoundException($"目录不存在: {targetDirectory}");

        var currentDirectory = Directory.GetCurrentDirectory();
        if (currentDirectory.Equals(targetDirectory, StringComparison.OrdinalIgnoreCase))
            return NullDisposable.Instance;

        Directory.SetCurrentDirectory(targetDirectory);
        return new DisposeAction<string>(Directory.SetCurrentDirectory, currentDirectory);
    }

    #endregion

    #region GetFiles(获取指定目录中的文件列表)

    /// <summary>
    /// 获取指定目录中的文件列表
    /// </summary>
    /// <param name="directoryPath">目录绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。范例："Log*.xml"表示搜索所有以Log开头的Xml文件。默认：*</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    /// <returns>文件路径数组</returns>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static string[] GetFiles(string directoryPath, string pattern = "*", bool includeChildPath = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

        pattern = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;

        try
        {
            return Directory.GetFiles(directoryPath, pattern, includeChildPath ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            InvokeHelper.OnInvokeException?.Invoke(ex);
            return Array.Empty<string>();
        }
    }

    #endregion

    #region GetFileNames(获取指定目录中的文件名称列表)

    /// <summary>
    /// 获取指定目录中的文件名称列表
    /// </summary>
    /// <param name="directoryPath">目录绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。范例："Log*.xml"表示搜索所有以Log开头的Xml文件。默认：*</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    /// <returns>文件名数组</returns>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static string[] GetFileNames(string directoryPath, string pattern = "*", bool includeChildPath = false)
    {
        var filePaths = GetFiles(directoryPath, pattern, includeChildPath);
        var names = new string[filePaths.Length];
        for (var i = 0; i < filePaths.Length; i++) 
            names[i] = Path.GetFileName(filePaths[i]);
        return names;
    }

    #endregion

    #region GetDirectories(获取指定目录中的目录列表)

    /// <summary>
    /// 获取指定目录中的目录列表
    /// </summary>
    /// <param name="directoryPath">目录绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    /// <returns>目录路径数组</returns>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static string[] GetDirectories(string directoryPath, string pattern = "*", bool includeChildPath = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

        try
        {
            return Directory.GetDirectories(directoryPath, pattern, includeChildPath ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            InvokeHelper.OnInvokeException?.Invoke(ex);
            return Array.Empty<string>();
        }
    }

    #endregion

    #region Contains(查找指定目录中是否存在指定的文件)

    /// <summary>
    /// 查找指定目录中是否存在指定的文件
    /// </summary>
    /// <param name="directoryPath">目录的绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。范例："Log*.xml"表示搜索所有以Log开头的Xml文件。</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    /// <returns>是否存在匹配的文件</returns>
    public static bool Contains(string directoryPath, string pattern, bool includeChildPath = false)
    {
        try
        {
            var fileNames = GetFiles(directoryPath, pattern, includeChildPath);
            return fileNames.Length > 0;
        }
        catch (Exception e)
        {
            InvokeHelper.OnInvokeException?.Invoke(e);
            return false;
        }
    }

    #endregion

    #region IsEmptyDirectory(检查文件夹是否为空目录)

    /// <summary>
    /// 检查文件夹是否为空目录
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <returns>是否为空目录</returns>
    /// <exception cref="ArgumentNullException">当文件夹路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当文件夹不存在时抛出</exception>
    public static bool IsEmptyDirectory(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentNullException(nameof(folderPath));
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"目录不存在: {folderPath}");

        try
        {
            return !Directory.EnumerateFileSystemEntries(folderPath).Any();
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    #endregion

    #region IsOverdueDirectory(检查文件夹的创建时间是否超过指定天数)

    /// <summary>
    /// 检查文件夹的创建时间是否超过指定天数
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <param name="days">指定天数</param>
    /// <returns>是否超过指定天数</returns>
    /// <exception cref="ArgumentNullException">当文件夹路径为空时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当天数为负数时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当文件夹不存在时抛出</exception>
    public static bool IsOverdueDirectory(string folderPath, int days)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentNullException(nameof(folderPath));
        if (days < 0)
            throw new ArgumentOutOfRangeException(nameof(days), "天数不能为负数");
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"目录不存在: {folderPath}");

        try
        {
            var createTime = Directory.GetCreationTime(folderPath);
            var daysSinceCreation = (DateTime.Now - createTime).Days;
            return daysSinceCreation > days;
        }
        catch (Exception ex)
        {
            InvokeHelper.OnInvokeException?.Invoke(ex);
            return false;
        }
    }

    #endregion

    #region GetDirectoryPath(获取目录路径)

    /// <summary>
    /// 获取目录路径，标准化路径分隔符
    /// </summary>
    /// <param name="path">路径。例如：C:\Users\A\</param>
    /// <returns>标准化后的目录路径</returns>
    public static string GetDirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var normalizedPath = path.Replace("\\", "/");
        var pathSegments = normalizedPath.Split('/');
        if (pathSegments.Length <= 1)
            return string.Empty;
        var result = string.Join("/", pathSegments.Take(pathSegments.Length - 1)) + "/";
        return result == "/" ? string.Empty : result;
    }

    #endregion

    #region Copy(递归复制文件夹及文件夹/文件)

    /// <summary>
    /// 递归复制文件夹及文件夹/文件
    /// </summary>
    /// <param name="sourcePath">源文件夹路径</param>
    /// <param name="targetPath">目标文件夹路径</param>
    /// <param name="searchPatterns">要复制的文件扩展名数组</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <exception cref="ArgumentNullException">当路径参数为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当源目录不存在时抛出</exception>
    public static void Copy(string sourcePath, string targetPath, string[] searchPatterns = null, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentNullException(nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentNullException(nameof(targetPath));
        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"递归复制文件夹时源目录\"{sourcePath}\"不存在。");

        // 创建目标目录
        Directory.CreateDirectory(targetPath);

        // 复制子目录
        var directories = Directory.GetDirectories(sourcePath);
        foreach (var directory in directories)
        {
            var dirName = Path.GetFileName(directory);
            var targetDir = Path.Combine(targetPath, dirName);
            Copy(directory, targetDir, searchPatterns, overwrite);
        }

        // 复制文件
        CopyFiles(sourcePath, targetPath, searchPatterns, overwrite);
    }

    /// <summary>
    /// 复制文件到目标目录
    /// </summary>
    /// <param name="sourcePath">源目录路径</param>
    /// <param name="targetPath">目标目录路径</param>
    /// <param name="searchPatterns">搜索模式数组</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    private static void CopyFiles(string sourcePath, string targetPath, string[] searchPatterns, bool overwrite)
    {
        if (searchPatterns != null && searchPatterns.Length > 0)
        {
            foreach (var pattern in searchPatterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    continue;

                var files = Directory.GetFiles(sourcePath, pattern);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var targetFile = Path.Combine(targetPath, fileName);
                    File.Copy(file, targetFile, overwrite);
                }
            }
        }
        else
        {
            var files = Directory.GetFiles(sourcePath);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetPath, fileName);
                File.Copy(file, targetFile, overwrite);
            }
        }
    }

    #endregion

    #region Delete(递归删除目录)

    /// <summary>
    /// 递归删除目录下所有文件夹/文件包括子文件夹
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <param name="isDeleteRoot">是否删除根目录</param>
    /// <returns>是否删除成功</returns>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    public static bool Delete(string directory, bool isDeleteRoot = true)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentNullException(nameof(directory));

        var dirPathInfo = new DirectoryInfo(directory);
        if (!dirPathInfo.Exists)
            return false;

        try
        {
            // 删除目录下所有文件
            DeleteFiles(dirPathInfo);

            // 递归删除所有子目录
            foreach (var subDirectory in dirPathInfo.GetDirectories())
                Delete(subDirectory.FullName, true);

            // 删除目录
            if (isDeleteRoot)
            {
                try
                {
                    // 移除只读属性
                    if ((dirPathInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        dirPathInfo.Attributes &= ~FileAttributes.ReadOnly;

                    dirPathInfo.Delete();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"删除目录 {dirPathInfo.FullName} 失败: {ex.Message}");
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"删除目录时出错：{e.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 删除目录中的所有文件
    /// </summary>
    /// <param name="dirPathInfo">目录信息</param>
    private static void DeleteFiles(DirectoryInfo dirPathInfo)
    {
        foreach (var fileInfo in dirPathInfo.GetFiles())
        {
            try
            {
                // 移除只读属性
                if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                fileInfo.Delete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除文件 {fileInfo.FullName} 失败: {ex.Message}");
                InvokeHelper.OnInvokeException?.Invoke(ex);
            }
        }
    }

    #endregion

    #region TryClearFolder(尝试删除文件夹及子文件夹)

    /// <summary>
    /// 尝试删除文件夹及子文件夹
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <returns>是否清理成功</returns>
    public static bool TryClearFolder(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
                return false;

            var fileSystemEntries = Directory.GetFileSystemEntries(directory);
            foreach (var fileOrFolder in fileSystemEntries)
            {
                if (Directory.Exists(fileOrFolder))
                {
                    // 递归清理子文件夹
                    if (!TryClearFolder(fileOrFolder))
                        return false;

                    // 删除空文件夹
                    if (IsEmptyDirectory(fileOrFolder))
                        Directory.Delete(fileOrFolder);
                }
                else if (File.Exists(fileOrFolder))
                {
                    // 清理文件
                    File.Delete(fileOrFolder);
                }
            }

            return true;
        }
        catch (Exception e)
        {
            InvokeHelper.OnInvokeException?.Invoke(e);
            return false;
        }
    }

    #endregion

    #region TryClearOverdueFolder(尝试删除创建时间超过指定天数的文件夹)

    /// <summary>
    /// 尝试删除创建时间超过指定天数的文件夹
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <param name="days">指定天数</param>
    /// <returns>是否清理成功</returns>
    public static bool TryClearOverdueFolder(string directory, int days)
    {
        try
        {
            if (!Directory.Exists(directory))
                return false;

            var fileSystemEntries = Directory.GetFileSystemEntries(directory);
            foreach (var fileOrFolder in fileSystemEntries)
            {
                if (Directory.Exists(fileOrFolder))
                {
                    // 递归清理子文件夹
                    if (!TryClearOverdueFolder(fileOrFolder, days))
                        return false;

                    // 删除过期的空文件夹
                    if (IsEmptyDirectory(fileOrFolder) && IsOverdueDirectory(fileOrFolder, days))
                        Directory.Delete(fileOrFolder);
                }
                else if (File.Exists(fileOrFolder) && FileHelper.IsOverdueFile(fileOrFolder, days))
                {
                    // 清理过期的文件
                    File.Delete(fileOrFolder);
                }
            }
        }
        catch (Exception e)
        {
            InvokeHelper.OnInvokeException?.Invoke(e);
            return false;
        }

        return true;
    }

    #endregion

    #region SetAttributes(设置目录属性)

    /// <summary>
    /// 设置目录属性
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <param name="attribute">要设置的目录属性</param>
    /// <param name="isSet">是否为设置属性,true:设置,false:取消</param>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static void SetAttributes(string directory, FileAttributes attribute, bool isSet)
    {
        if(string.IsNullOrWhiteSpace(directory))
            throw new ArgumentNullException(nameof(directory));
        var di = new DirectoryInfo(directory);
        if (!di.Exists)
            throw new DirectoryNotFoundException("设置目录属性时指定文件夹不存在");

        if (isSet)
            di.Attributes = di.Attributes | attribute;
        else
            di.Attributes = di.Attributes & ~attribute;
    }

    #endregion

    #region SetCurrentDirectory(设置当前目录)

    /// <summary>
    /// 设置当前目录
    /// </summary>
    /// <param name="path">目录路径</param>
    public static string SetCurrentDirectory(string path) => Platform.CurrentDirectory = path;

    #endregion

    #region GetCurrentDirectory(获取当前目录)

    /// <summary>
    /// 获取当前目录
    /// </summary>
    public static string GetCurrentDirectory() => Platform.CurrentDirectory;

    #endregion

    #region GetDirectorySize(获取目录大小)

    /// <summary>
    /// 获取目录大小（包含所有子目录和文件）
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>目录大小（字节）</returns>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static long GetDirectorySize(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

        try
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            return GetDirectorySize(dirInfo);
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取目录大小（包含所有子目录和文件）
    /// </summary>
    /// <param name="directoryInfo">目录信息</param>
    /// <returns>目录大小（字节）</returns>
    public static long GetDirectorySize(DirectoryInfo directoryInfo)
    {
        if (directoryInfo == null || !directoryInfo.Exists)
            return 0;

        long size = 0;

        try
        {
            // 计算文件大小
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    size += file.Length;
                }
                catch (UnauthorizedAccessException)
                {
                    // 跳过无权限访问的文件
                }
            }

            // 递归计算子目录大小
            foreach (var subDir in directoryInfo.GetDirectories())
            {
                try
                {
                    size += GetDirectorySize(subDir);
                }
                catch (UnauthorizedAccessException)
                {
                    // 跳过无权限访问的目录
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 跳过无权限访问的目录
        }

        return size;
    }

    #endregion

    #region Move(移动目录到新位置)

    /// <summary>
    /// 移动目录到新位置
    /// </summary>
    /// <param name="sourcePath">源目录路径</param>
    /// <param name="destinationPath">目标目录路径</param>
    /// <param name="overwrite">是否覆盖已存在的目录</param>
    /// <exception cref="ArgumentNullException">当路径参数为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当源目录不存在时抛出</exception>
    /// <exception cref="InvalidOperationException">当目标目录已存在且不允许覆盖时抛出</exception>
    public static void Move(string sourcePath, string destinationPath, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentNullException(nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentNullException(nameof(destinationPath));

        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"源目录不存在: {sourcePath}");

        if (Directory.Exists(destinationPath))
        {
            if (!overwrite)
                throw new InvalidOperationException($"目标目录已存在: {destinationPath}");

            // 删除目标目录
            Directory.Delete(destinationPath, true);
        }

        Directory.Move(sourcePath, destinationPath);
    }

    #endregion

    #region GetFileCount(计算目录中文件的数量)

    /// <summary>
    /// 计算目录中文件的数量
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="includeSubDirectories">是否包含子目录</param>
    /// <returns>文件数量</returns>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static int GetFileCount(string directoryPath, bool includeSubDirectories = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

        try
        {
            return Directory.GetFiles(directoryPath, "*", includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Length;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    #endregion

    #region GetDirectoryCount(计算目录中子目录的数量)

    /// <summary>
    /// 计算目录中子目录的数量
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="includeSubDirectories">是否包含子目录</param>
    /// <returns>目录数量</returns>
    /// <exception cref="ArgumentNullException">当目录路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    public static int GetDirectoryCount(string directoryPath, bool includeSubDirectories = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

        try
        {
            return Directory.GetDirectories(directoryPath, "*", includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Length;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    #endregion
}
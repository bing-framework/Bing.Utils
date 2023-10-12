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
    /// 创建目录
    /// </summary>
    /// <param name="path">文件或目录绝对路径</param>
    public static void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var file = new FileInfo(path);
        var directoryPath = file.Directory?.FullName;
        CreateIfNotExists(directoryPath);
    }

    #endregion

    #region CreateIfNotExists(创建文件夹，如果不存在)

    /// <summary>
    /// 创建文件夹，如果不存在
    /// </summary>
    /// <param name="directory">要创建的文件夹路径</param>
    public static void CreateIfNotExists(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    #endregion

    #region IsSubDirectoryOf(是否指定父目录路径的子目录)

    /// <summary>
    /// 是否指定父目录路径的子目录
    /// </summary>
    /// <param name="parentDirectoryPath">父目录路径</param>
    /// <param name="childDirectoryPath">子目录路径</param>
    public static bool IsSubDirectoryOf(string parentDirectoryPath, string childDirectoryPath)
    {
        Check.NotNull(parentDirectoryPath, nameof(parentDirectoryPath));
        Check.NotNull(childDirectoryPath, nameof(childDirectoryPath));

        return IsSubDirectoryOf(new DirectoryInfo(parentDirectoryPath), new DirectoryInfo(childDirectoryPath));
    }

    /// <summary>
    /// 是否指定父目录路径的子目录
    /// </summary>
    /// <param name="parentDirectory">父目录</param>
    /// <param name="childDirectory">子目录</param>
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
    public static IDisposable ChangeCurrentDirectory(string targetDirectory)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        if (currentDirectory.Equals(targetDirectory, StringComparison.OrdinalIgnoreCase))
            return NullDisposable.Instance;
        Directory.SetCurrentDirectory(targetDirectory);
        return new DisposeAction(() => { Directory.SetCurrentDirectory(currentDirectory); });
    }

    #endregion

    #region GetFiles(获取指定目录中的文件列表)

    /// <summary>
    /// 获取指定目录中的文件列表
    /// </summary>
    /// <param name="directoryPath">目录绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。范例："Log*.xml"表示搜索所有以Log开头的Xml文件。默认：*</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static string[] GetFiles(string directoryPath, string pattern = "*", bool includeChildPath = false)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录\"{directoryPath}\"不存在");
        return Directory.GetFiles(directoryPath, pattern,
            includeChildPath ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    }

    #endregion

    #region GetFileNames(获取指定目录中的文件名称列表)

    /// <summary>
    /// 获取指定目录中的文件名称列表
    /// </summary>
    /// <param name="directoryPath">目录绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。范例："Log*.xml"表示搜索所有以Log开头的Xml文件。默认：*</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static string[] GetFileNames(string directoryPath, string pattern = "*", bool includeChildPath = false)
    {
        var names = new List<string>();
        foreach (var filePath in GetFiles(directoryPath,pattern,includeChildPath))
            names.Add(Path.GetFileName(filePath));
        return names.ToArray();
    }

    #endregion

    #region GetDirectories(获取指定目录中的目录列表)

    /// <summary>
    /// 获取指定目录中的目录列表
    /// </summary>
    /// <param name="directoryPath">目录绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static string[] GetDirectories(string directoryPath, string pattern = "*", bool includeChildPath = false)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录\"{directoryPath}\"不存在");
        return Directory.GetDirectories(directoryPath, pattern,
            includeChildPath ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    }

    #endregion

    #region Contains(查找指定目录中是否存在指定的文件)

    /// <summary>
    /// 查找指定目录中是否存在指定的文件
    /// </summary>
    /// <param name="directoryPath">目录的绝对路径</param>
    /// <param name="pattern">模式字符串。"*"代表0或N个字符，"?"代表1个字符。范例："Log*.xml"表示搜索所有以Log开头的Xml文件。</param>
    /// <param name="includeChildPath">是否包含子目录</param>
    public static bool Contains(string directoryPath, string pattern, bool includeChildPath = false)
    {
        var fileNames = GetFiles(directoryPath, pattern, includeChildPath);
        return fileNames.Length != 0;
    }

    #endregion

    #region IsEmptyDirectory(检查文件夹是否为空目录)

    /// <summary>
    /// 检查文件夹是否为空目录
    /// </summary>
    /// <param name="folderName">文件夹名称</param>
    public static bool IsEmptyDirectory(string folderName)
    {
        return Directory.GetFiles(folderName).Length == 0 && Directory.GetDirectories(folderName).Length == 0;
    }

    #endregion

    #region IsOverdueDirectory(检查文件夹的创建时间是否超过指定天数)

    /// <summary>
    /// 检查文件夹的创建时间是否超过指定天数
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <param name="days">指定天数</param>
    public static bool IsOverdueDirectory(string folderPath, int days)
    {
        var createTime = Directory.GetCreationTime(folderPath);
        var date = DateTime.Now.Date.Subtract(createTime);
        return date.Days > days;
    }

    #endregion

    #region GetDirectoryPath(获取目录路径)

    /// <summary>
    /// 获取目录路径
    /// </summary>
    /// <param name="path">路径。例如：C:\Users\A\</param>
    public static string GetDirectoryPath(string path)
    {
        var result = "";
        if (path.IndexOf("\\", StringComparison.OrdinalIgnoreCase) > 0)
            path = path.Replace("\\", "/");
        var sArray = path.Split('/');
        for (var i = 0; i < sArray.Length - 1; i++)
            result += sArray[i] + "/";
        if (result == "/")
            result = "";
        return result;
    }

    #endregion

    #region Copy(递归复制文件夹及文件夹/文件)

    /// <summary>
    /// 递归复制文件夹及文件夹/文件
    /// </summary>
    /// <param name="sourcePath">源文件夹路径</param>
    /// <param name="targetPath">目标文件夹路径</param>
    /// <param name="searchPatterns">要复制的文件扩展名数组</param>
    public static void Copy(string sourcePath, string targetPath, string[] searchPatterns = null)
    {
        sourcePath.CheckNotNullOrEmpty(nameof(sourcePath));
        sourcePath.CheckNotNullOrEmpty(nameof(targetPath));

        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"递归复制文件夹时源目录\"{sourcePath}\"不存在。");

        if (!Directory.Exists(targetPath)) 
            Directory.CreateDirectory(targetPath);

        string[] dirs = Directory.GetDirectories(sourcePath);
        if (dirs.Length > 0)
        {
            foreach (var dir in dirs)
            {
                Copy(dir, targetPath + dir.Substring(dir.LastIndexOf("\\", StringComparison.Ordinal)));
            }
        }

        if (searchPatterns != null && searchPatterns.Length > 0)
        {
            foreach (var searchPattern in searchPatterns)
            {
                string[] files = Directory.GetFiles(sourcePath, searchPattern);
                if (files.Length <= 0)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    File.Copy(file, targetPath + file.Substring(file.LastIndexOf("\\", StringComparison.Ordinal)));
                }
            }
        }
        else
        {
            string[] files = Directory.GetFiles(sourcePath);
            if (files.Length <= 0)
            {
                return;
            }

            foreach (var file in files)
            {
                File.Copy(file, targetPath + file.Substring(file.LastIndexOf("\\", StringComparison.Ordinal)));
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
    public static bool Delete(string directory, bool isDeleteRoot = true)
    {
        directory.CheckNotNullOrEmpty(nameof(directory));

        bool flag = false;
        DirectoryInfo dirPathInfo = new DirectoryInfo(directory);
        if (dirPathInfo.Exists)
        {
            //删除目录下所有文件
            foreach (FileInfo fileInfo in dirPathInfo.GetFiles())
            {
                fileInfo.Delete();
            }

            //递归删除所有子目录
            foreach (DirectoryInfo subDirectory in dirPathInfo.GetDirectories())
            {
                Delete(subDirectory.FullName);
            }

            //删除目录
            if (isDeleteRoot)
            {
                dirPathInfo.Attributes = FileAttributes.Normal;
                dirPathInfo.Delete();
            }

            flag = true;
        }

        return flag;
    }

    #endregion

    #region TryClearFolder(尝试删除文件夹及子文件夹)

    /// <summary>
    /// 尝试删除文件夹及子文件夹
    /// </summary>
    /// <param name="directory">目录路径</param>
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
        }
        catch (Exception e)
        {
            InvokeHelper.OnInvokeException?.Invoke(e);
            return false;
        }
        return true;
    }

    #endregion

    #region TryClearOverdueFolder(尝试删除创建时间超过指定天数的文件夹)

    /// <summary>
    /// 尝试删除创建时间超过指定天数的文件夹
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <param name="days">指定天数</param>
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
    public static void SetAttributes(string directory, FileAttributes attribute, bool isSet)
    {
        directory.CheckNotNullOrEmpty(nameof(directory));

        DirectoryInfo di = new DirectoryInfo(directory);
        if (!di.Exists)
        {
            throw new DirectoryNotFoundException("设置目录属性时指定文件夹不存在");
        }

        if (isSet)
        {
            di.Attributes = di.Attributes | attribute;
        }
        else
        {
            di.Attributes = di.Attributes & ~attribute;
        }
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
}
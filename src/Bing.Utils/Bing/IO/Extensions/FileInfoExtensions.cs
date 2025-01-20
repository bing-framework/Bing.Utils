// ReSharper disable once CheckNamespace
namespace Bing.IO;

/// <summary>
/// 文件信息(<see cref="FileInfo"/>) 扩展
/// </summary>
public static class FileInfoExtensions
{
    /// <summary>
    /// 判断文件是否具有只读或归档属性。
    /// </summary>
    /// <param name="fileInfo">文件信息对象</param>
    /// <returns>如果文件具有只读或归档属性，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool HasReadOnlyOrArchivedAttribute(this FileInfo fileInfo)
    {
        if (HasAttributeSet(fileInfo, FileAttributes.Archive))
            return true;
        if (HasAttributeSet(fileInfo, FileAttributes.ReadOnly))
            return true;
        return false;
    }

    /// <summary>
    /// 移除文件的只读或归档属性。
    /// </summary>
    /// <param name="fileInfo">文件信息对象</param>
    public static void RemoveReadOnlyOrArchivedAttribute(this FileInfo fileInfo)
    {
        RemoveAttribute(fileInfo, FileAttributes.Archive);
        RemoveAttribute(fileInfo, FileAttributes.ReadOnly);
    }

    /// <summary>
    /// 判断文件是否具有指定的属性。
    /// </summary>
    /// <param name="fileInfo">文件信息对象</param>
    /// <param name="attributes">指定的文件属性。</param>
    /// <returns>如果文件具有所有指定的属性，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool HasAttributeSet(this FileInfo fileInfo, FileAttributes attributes)
    {
        return (fileInfo.Attributes & attributes) == attributes;
    }

    /// <summary>
    /// 移除文件的指定属性。
    /// </summary>
    /// <param name="fileInfo">文件信息对象</param>
    /// <param name="attributes">要移除的文件属性。</param>
    public static void RemoveAttribute(this FileInfo fileInfo, FileAttributes attributes)
    {
        if (!fileInfo.HasAttributeSet(attributes))
            return;
        fileInfo.Attributes = fileInfo.Attributes & ~attributes;
        fileInfo.Refresh();
    }
}
using System.IO.Compression;

namespace Bing.IO;

/// <summary>
/// 压缩操作辅助类
/// </summary>
public static class ZipHelper
{
    /// <summary>
    /// 压缩指定文件夹到Zip文件
    /// </summary>
    /// <param name="folderPath">要压缩的文件夹路径</param>
    /// <param name="zipPath">生成的Zip文件路径</param>
    /// <exception cref="ArgumentNullException">当文件夹路径或Zip文件路径为空时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当文件夹路径不存在时抛出</exception>
    public static void Zip(string folderPath, string zipPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentNullException(nameof(folderPath), "文件夹路径不能为空");
        if (string.IsNullOrWhiteSpace(zipPath))
            throw new ArgumentNullException(nameof(zipPath), "Zip文件路径不能为空");
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"文件夹路径不存在: {folderPath}");

        DirectoryInfo directoryInfo = new(zipPath);
        if (directoryInfo.Parent != null)
            directoryInfo = directoryInfo.Parent;
        if (!directoryInfo.Exists)
            directoryInfo.Create();
        ZipFile.CreateFromDirectory(folderPath, zipPath, CompressionLevel.Optimal, false);
    }

    /// <summary>
    /// 解压Zip文件到指定目录
    /// </summary>
    /// <param name="zipPath">Zip压缩文件路径，例如：D:/test.zip</param>
    /// <param name="folderPath">解压缩目标文件夹路径，例如：D:/test/</param>
    /// <exception cref="ArgumentNullException">当压缩文件路径或目标文件夹路径为空时抛出</exception>
    /// <exception cref="FileNotFoundException">当压缩文件路径不存在时抛出</exception>
    public static void UnZip(string zipPath, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
            throw new ArgumentNullException(nameof(zipPath), "压缩文件路径不能为空");
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentNullException(nameof(folderPath), "目标文件夹路径不能为空");
        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"压缩文件路径不存在: {zipPath}");

        DirectoryInfo directoryInfo = new(folderPath);
        if(!directoryInfo.Exists)
            directoryInfo.Create();
        ZipFile.ExtractToDirectory(zipPath, folderPath, true);
    }
}
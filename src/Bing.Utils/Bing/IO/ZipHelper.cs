using System.IO.Compression;

namespace Bing.IO;

/// <summary>
/// 压缩操作辅助类
/// </summary>
public static class ZipHelper
{
    /// <summary>
    /// 使用路径校验、多维预算和取消支持解压 ZIP。
    /// </summary>
    /// <param name="zipPath">ZIP 路径。</param>
    /// <param name="folderPath">目标目录。</param>
    /// <param name="options">解压预算。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static void UnZipWithOptions(string zipPath, string folderPath, Bing.Helpers.ZipExtractionOptions options, CancellationToken cancellationToken = default)
        => Bing.Helpers.Compression.UnZipWithOptions(zipPath, folderPath, options, cancellationToken);

    /// <summary>
    /// 异步按预算解压 ZIP；失败不回滚已提交文件。
    /// </summary>
    /// <param name="zipPath">ZIP 路径。</param>
    /// <param name="folderPath">目标目录。</param>
    /// <param name="options">解压预算。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static Task UnZipWithOptionsAsync(string zipPath, string folderPath, Bing.Helpers.ZipExtractionOptions options, CancellationToken cancellationToken = default)
        => Bing.Helpers.Compression.UnZipWithOptionsAsync(zipPath, folderPath, options, cancellationToken);

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
    /// <exception cref="InvalidDataException">归档无效，或条目路径越界、使用绝对路径、落入既有重解析点。</exception>
    /// <remarks>
    /// 创建目录或写入文件前预检所有条目。调用方须控制目标目录及其祖先，防止解压期间被并发替换。
    /// netstandard2.0 保留拒绝覆盖已有文件的行为，其他目标框架允许覆盖。
    /// 此入口不限制解压输出大小；处理不可信归档时建议使用 <see cref="UnZipWithOptions"/> 配置预算。
    /// </remarks>
    public static void UnZip(string zipPath, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
            throw new ArgumentNullException(nameof(zipPath), "压缩文件路径不能为空");
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentNullException(nameof(folderPath), "目标文件夹路径不能为空");
        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"压缩文件路径不存在: {zipPath}");

        // 与 Compression 入口共用预检，避免既有链接把正常条目重定向到目录外。
        Bing.Helpers.Compression.ValidateArchivePaths(zipPath, folderPath);
        DirectoryInfo directoryInfo = new(folderPath);
        if (!directoryInfo.Exists)
            directoryInfo.Create();
#if NETSTANDARD
        ZipFile.ExtractToDirectory(zipPath, folderPath);
#else
        ZipFile.ExtractToDirectory(zipPath, folderPath, true);
#endif
    }
}

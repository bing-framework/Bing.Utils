using System.IO.Compression;

namespace Bing.Helpers;

/// <summary>
/// 压缩工具类，提供字节数组、字符串和文件的压缩与解压功能
/// </summary>
public static partial class Compression
{
    #region 常量

    /// <summary>
    /// 默认缓冲区大小
    /// </summary>
    private const int DefaultBufferSize = 4096;

    #endregion

    #region Compress(对byte[]数组进行压缩)

    /// <summary>
    /// 对字节数组进行 GZip 压缩
    /// </summary>
    /// <param name="data">待压缩的字节数组</param>
    /// <param name="compressionLevel">压缩级别</param>
    /// <returns>压缩后的字节数组</returns>
    /// <exception cref="ArgumentNullException">当 data 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当压缩操作失败时抛出</exception>
    /// <remarks>
    /// 压缩级别说明：
    /// - Optimal: 最佳压缩率，速度较慢
    /// - Fastest: 最快压缩速度，压缩率较低
    /// - NoCompression: 不压缩，仅打包
    /// - SmallestSize: .NET 6+ 新增，最小尺寸压缩
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] data = Encoding.UTF8.GetBytes("大量重复的文本内容...");
    /// byte[] fastCompressed = Compression.Compress(data, CompressionLevel.Fastest);
    /// byte[] optimalCompressed = Compression.Compress(data, CompressionLevel.Optimal);
    /// </code>
    /// </example>
    public static byte[] Compress(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "待压缩的数据不能为 null");
        if (data.Length == 0)
            return [];
        try
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, compressionLevel))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("压缩操作失败", ex);
        }
    }

    /// <summary>
    /// 异步对字节数组进行 GZip 压缩（指定压缩级别）
    /// </summary>
    /// <param name="data">待压缩的字节数组</param>
    /// <param name="compressionLevel">压缩级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>压缩后的字节数组</returns>
    public static async Task<byte[]> CompressAsync(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "待压缩的数据不能为 null");

        if (data.Length == 0)
            return [];

        try
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, compressionLevel))
            {
                await gzipStream.WriteAsync(data, 0, data.Length, cancellationToken);
            }
            return outputStream.ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("异步压缩操作失败", ex);
        }
    }

    #endregion

    #region Decompress(对byte[]数组进行解压)

    /// <summary>
    /// 对字节数组进行 GZip 解压
    /// </summary>
    /// <param name="data">待解压的字节数组</param>
    /// <returns>解压后的字节数组</returns>
    /// <exception cref="ArgumentNullException">当 data 为 null 时抛出</exception>
    /// <exception cref="InvalidDataException">当数据格式无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当解压操作失败时抛出</exception>
    /// <remarks>
    /// 解压使用 GZip 格式压缩的数据。
    /// 对于空数组，返回空数组。
    /// 如果数据不是有效的 GZip 格式，将抛出 InvalidDataException。
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] compressedData = GetCompressedData();
    /// byte[] originalData = Compression.Decompress(compressedData);
    /// string text = Encoding.UTF8.GetString(originalData);
    /// </code>
    /// </example>
    public static byte[] Decompress(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "待解压的数据不能为 null");
        if (data.Length == 0)
            return [];
        try
        {
            using var inputStream = new MemoryStream(data);
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(outputStream);
            }
            return outputStream.ToArray();
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("解压操作失败", ex);
        }
    }

    /// <summary>
    /// 异步对字节数组进行 GZip 解压
    /// </summary>
    /// <param name="data">待解压的字节数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解压后的字节数组</returns>
    /// <exception cref="ArgumentNullException">当 data 为 null 时抛出</exception>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    /// <exception cref="InvalidDataException">当数据格式无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当解压操作失败时抛出</exception>
    public static async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "待解压的数据不能为 null");
        if (data.Length == 0)
            return [];
        try
        {
            using var inputStream = new MemoryStream(data);
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                await gzipStream.CopyToAsync(outputStream, DefaultBufferSize, cancellationToken);
            }
            return outputStream.ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("异步解压操作失败", ex);
        }
    }

    #endregion

    #region Compress(对字符串进行压缩)
    
    /// <summary>
    /// 对字符串进行压缩并转换为 Base64 字符串
    /// </summary>
    /// <param name="value">待压缩的字符串</param>
    /// <param name="encoding">字符编码，如果为 null 则使用 UTF-8</param>
    /// <returns>压缩后的 Base64 编码字符串</returns>
    /// <exception cref="InvalidOperationException">当压缩操作失败时抛出</exception>
    public static string Compress(string value, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        encoding ??= Encoding.UTF8;
        try
        {
            var bytes = encoding.GetBytes(value);
            var compressedBytes = Compress(bytes);
            return Convert.ToBase64String(compressedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("字符串压缩操作失败", ex);
        }
    }

    /// <summary>
    /// 异步对字符串进行压缩并转换为 Base64 字符串
    /// </summary>
    /// <param name="value">待压缩的字符串</param>
    /// <param name="encoding">字符编码，如果为 null 则使用 UTF-8</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>压缩后的 Base64 编码字符串</returns>
    public static async Task<string> CompressAsync(string value, Encoding encoding = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        encoding ??= Encoding.UTF8;
        try
        {
            var bytes = encoding.GetBytes(value);
            var compressedBytes = await CompressAsync(bytes, cancellationToken: cancellationToken);
            return Convert.ToBase64String(compressedBytes);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new InvalidOperationException("异步字符串压缩操作失败", ex);
        }
    }

    #endregion

    #region Decompress(对字符串进行解压)

    /// <summary>
    /// 对 Base64 编码的压缩字符串进行解压（指定编码）
    /// </summary>
    /// <param name="value">Base64 编码的压缩字符串</param>
    /// <param name="encoding">字符编码，如果为 null 则使用 UTF-8</param>
    /// <returns>解压后的原始字符串</returns>
    /// <exception cref="FormatException">当 Base64 格式无效时抛出</exception>
    /// <exception cref="InvalidDataException">当压缩数据格式无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当解压操作失败时抛出</exception>
    public static string Decompress(string value, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        encoding ??= Encoding.UTF8;
        try
        {
            var bytes = Convert.FromBase64String(value);
            var decompressedBytes = Decompress(bytes);
            return encoding.GetString(decompressedBytes);
        }
        catch (FormatException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("字符串解压操作失败", ex);
        }
    }

    /// <summary>
    /// 异步对 Base64 编码的压缩字符串进行解压
    /// </summary>
    /// <param name="value">Base64 编码的压缩字符串</param>
    /// <param name="encoding">字符编码，如果为 null 则使用 UTF-8</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解压后的原始字符串</returns>
    public static async Task<string> DecompressAsync(string value, Encoding encoding = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        encoding ??= Encoding.UTF8;
        try
        {
            var bytes = Convert.FromBase64String(value);
            var decompressedBytes = await DecompressAsync(bytes, cancellationToken);
            return encoding.GetString(decompressedBytes);
        }
        catch (FormatException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("异步字符串解压操作失败", ex);
        }
    }

    #endregion

    #region Zip(将文件夹压缩成zip文件)

    /// <summary>
    /// 将文件夹压缩成 ZIP 文件
    /// </summary>
    /// <param name="sourceDir">源文件夹路径</param>
    /// <param name="zipFile">目标 ZIP 文件路径</param>
    /// <exception cref="ArgumentException">当路径参数无效时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当源目录不存在时抛出</exception>
    /// <exception cref="IOException">当文件 I/O 操作失败时抛出</exception>
    /// <exception cref="UnauthorizedAccessException">当没有足够权限时抛出</exception>
    /// <remarks>
    /// 此方法会递归压缩源文件夹中的所有文件和子文件夹。
    /// 如果目标 ZIP 文件已存在，将被覆盖。
    /// 压缩过程中会保持原有的文件夹结构。
    /// </remarks>
    /// <example>
    /// <code>
    /// string sourceFolder = @"C:\MyDocuments";
    /// string zipPath = @"C:\Backup\documents.zip";
    /// Compression.Zip(sourceFolder, zipPath);
    /// Console.WriteLine("文件夹压缩完成");
    /// </code>
    /// </example>
    public static void Zip(string sourceDir, string zipFile)
    {
        if (string.IsNullOrWhiteSpace(sourceDir))
            throw new ArgumentException("源目录路径不能为空", nameof(sourceDir));
        if (string.IsNullOrWhiteSpace(zipFile))
            throw new ArgumentException("ZIP 文件路径不能为空", nameof(zipFile));
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");
        try
        {
            // 确保目标目录存在
            var targetDir = Path.GetDirectoryName(zipFile);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) 
                Directory.CreateDirectory(targetDir);
            // 如果目标文件已存在，先删除
            if (File.Exists(zipFile)) 
                File.Delete(zipFile);
            ZipFile.CreateFromDirectory(sourceDir, zipFile, CompressionLevel.Optimal, false);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is DirectoryNotFoundException))
        {
            throw new InvalidOperationException($"压缩文件夹失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将文件夹压缩成 ZIP 文件（指定压缩级别和是否包含根目录）
    /// </summary>
    /// <param name="sourceDir">源文件夹路径</param>
    /// <param name="zipFile">目标 ZIP 文件路径</param>
    /// <param name="compressionLevel">压缩级别</param>
    /// <param name="includeBaseDirectory">是否在 ZIP 文件中包含根目录</param>
    /// <exception cref="ArgumentException">当路径参数无效时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当源目录不存在时抛出</exception>
    /// <exception cref="IOException">当文件 I/O 操作失败时抛出</exception>
    /// <example>
    /// <code>
    /// // 最快压缩，不包含根目录
    /// Compression.Zip(@"C:\Source", @"C:\Archive.zip", CompressionLevel.Fastest, false);
    /// 
    /// // 最优压缩，包含根目录
    /// Compression.Zip(@"C:\Source", @"C:\Archive.zip", CompressionLevel.Optimal, true);
    /// </code>
    /// </example>
    public static void Zip(string sourceDir, string zipFile, CompressionLevel compressionLevel, bool includeBaseDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDir))
            throw new ArgumentException("源目录路径不能为空", nameof(sourceDir));
        if (string.IsNullOrWhiteSpace(zipFile))
            throw new ArgumentException("ZIP 文件路径不能为空", nameof(zipFile));
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");
        try
        {
            // 确保目标目录存在
            var targetDir = Path.GetDirectoryName(zipFile);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) 
                Directory.CreateDirectory(targetDir);
            // 如果目标文件已存在，先删除
            if (File.Exists(zipFile)) 
                File.Delete(zipFile);
            ZipFile.CreateFromDirectory(sourceDir, zipFile, compressionLevel, includeBaseDirectory);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is DirectoryNotFoundException))
        {
            throw new InvalidOperationException($"压缩文件夹失败: {ex.Message}", ex);
        }
    }

    #endregion

    #region UnZip(将zip文件解压到指定文件夹)

    /// <summary>
    /// 将 ZIP 文件解压到指定文件夹
    /// </summary>
    /// <param name="zipFile">ZIP 文件路径</param>
    /// <param name="targetDir">目标解压目录</param>
    /// <exception cref="ArgumentException">当路径参数无效时抛出</exception>
    /// <exception cref="FileNotFoundException">当 ZIP 文件不存在时抛出</exception>
    /// <exception cref="IOException">当文件 I/O 操作失败时抛出</exception>
    /// <exception cref="InvalidDataException">当 ZIP 文件格式无效时抛出</exception>
    /// <exception cref="UnauthorizedAccessException">当没有足够权限时抛出</exception>
    /// <remarks>
    /// 此方法会将 ZIP 文件中的所有内容解压到目标目录。
    /// 如果目标目录不存在，会自动创建。
    /// 如果目标目录中已存在同名文件，将被覆盖。
    /// </remarks>
    /// <example>
    /// <code>
    /// string zipPath = @"C:\Archive\documents.zip";
    /// string extractPath = @"C:\ExtractedFiles";
    /// Compression.UnZip(zipPath, extractPath);
    /// Console.WriteLine("ZIP 文件解压完成");
    /// </code>
    /// </example>
    public static void UnZip(string zipFile, string targetDir)
    {
        if (string.IsNullOrWhiteSpace(zipFile))
            throw new ArgumentException("ZIP 文件路径不能为空", nameof(zipFile));
        if (string.IsNullOrWhiteSpace(targetDir))
            throw new ArgumentException("目标目录路径不能为空", nameof(targetDir));
        if (!File.Exists(zipFile))
            throw new FileNotFoundException($"ZIP 文件不存在: {zipFile}");
        try
        {
            // 确保目标目录存在
            if (!Directory.Exists(targetDir)) 
                Directory.CreateDirectory(targetDir);
#if NET5_0_OR_GREATER
            ZipFile.ExtractToDirectory(zipFile, targetDir, true); // 允许覆盖
#else
            ZipFile.ExtractToDirectory(zipFile, targetDir);
#endif

        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException))
        {
            throw new InvalidOperationException($"解压 ZIP 文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将 ZIP 文件解压到指定文件夹（指定是否覆盖现有文件）
    /// </summary>
    /// <param name="zipFile">ZIP 文件路径</param>
    /// <param name="targetDir">目标解压目录</param>
    /// <param name="overwriteFiles">是否覆盖现有文件</param>
    /// <exception cref="ArgumentException">当路径参数无效时抛出</exception>
    /// <exception cref="FileNotFoundException">当 ZIP 文件不存在时抛出</exception>
    /// <exception cref="IOException">当文件 I/O 操作失败或文件已存在且不允许覆盖时抛出</exception>
    /// <example>
    /// <code>
    /// // 解压但不覆盖现有文件
    /// Compression.UnZip(@"C:\Archive.zip", @"C:\Extract", false);
    /// 
    /// // 解压并覆盖现有文件
    /// Compression.UnZip(@"C:\Archive.zip", @"C:\Extract", true);
    /// </code>
    /// </example>
    public static void UnZip(string zipFile, string targetDir, bool overwriteFiles)
    {
        if (string.IsNullOrWhiteSpace(zipFile))
            throw new ArgumentException("ZIP 文件路径不能为空", nameof(zipFile));
        if (string.IsNullOrWhiteSpace(targetDir))
            throw new ArgumentException("目标目录路径不能为空", nameof(targetDir));
        if (!File.Exists(zipFile))
            throw new FileNotFoundException($"ZIP 文件不存在: {zipFile}");
        try
        {
            // 确保目标目录存在
            if (!Directory.Exists(targetDir)) 
                Directory.CreateDirectory(targetDir);

#if NET5_0_OR_GREATER
            ZipFile.ExtractToDirectory(zipFile, targetDir, overwriteFiles);
#elif NETCOREAPP3_1_OR_GREATER
            ZipFile.ExtractToDirectory(zipFile, targetDir, overwriteFiles);
#else
            // .NET Standard 2.0 不支持 overwriteFiles 参数
            if (overwriteFiles)
            {
                // 如果需要覆盖，先删除目标目录中的文件
                ExtractWithOverwrite(zipFile, targetDir);
            }
            else
            {
                ZipFile.ExtractToDirectory(zipFile, targetDir);
            }
#endif
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException))
        {
            throw new InvalidOperationException($"解压 ZIP 文件失败: {ex.Message}", ex);
        }
    }

#if NETSTANDARD2_0
    /// <summary>
    /// 在 .NET Standard 2.0 中支持覆盖解压的辅助方法
    /// </summary>
    /// <param name="zipFile">ZIP 文件路径</param>
    /// <param name="targetDir">目标目录</param>
    private static void ExtractWithOverwrite(string zipFile, string targetDir)
    {
        using var archive = ZipFile.OpenRead(zipFile);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue; // 跳过目录条目
            var destinationPath = Path.Combine(targetDir, entry.FullName);
            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir)) 
                Directory.CreateDirectory(destinationDir);
            // 如果文件已存在，先删除
            if (File.Exists(destinationPath)) 
                File.Delete(destinationPath);
            entry.ExtractToFile(destinationPath);
        }
    }
#endif

    #endregion

    #region 辅助方法

    /// <summary>
    /// 计算压缩率
    /// </summary>
    /// <param name="originalSize">原始大小（字节）</param>
    /// <param name="compressedSize">压缩后大小（字节）</param>
    /// <returns>压缩率百分比（0-100）</returns>
    /// <remarks>
    /// 压缩率计算公式：(1 - 压缩后大小 / 原始大小) × 100%
    /// 返回值越大表示压缩效果越好。
    /// </remarks>
    /// <example>
    /// <code>
    /// long originalSize = 1000;
    /// long compressedSize = 300;
    /// double ratio = Compression.CalculateCompressionRatio(originalSize, compressedSize);
    /// Console.WriteLine($"压缩率: {ratio:F2}%"); // 输出: 压缩率: 70.00%
    /// </code>
    /// </example>
    public static double CalculateCompressionRatio(long originalSize, long compressedSize)
    {
        if (originalSize <= 0)
            return 0;
        if (compressedSize <= 0)
            return 100;
        if (compressedSize >= originalSize)
            return 0;
        return (1.0 - (double)compressedSize / originalSize) * 100;
    }

    /// <summary>
    /// 检查数据是否为有效的 GZip 格式
    /// </summary>
    /// <param name="data">要检查的数据</param>
    /// <returns>如果是有效的 GZip 格式返回 true，否则返回 false</returns>
    /// <remarks>
    /// 通过检查 GZip 文件头的魔术数字（0x1F, 0x8B）来判断。
    /// 这是一个快速的格式检查，不进行完整的解压验证。
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] data = GetSomeData();
    /// if (Compression.IsValidGZipData(data))
    /// {
    ///     var decompressed = Compression.Decompress(data);
    ///     // 处理解压后的数据
    /// }
    /// else
    /// {
    ///     Console.WriteLine("不是有效的 GZip 数据");
    /// }
    /// </code>
    /// </example>
    public static bool IsValidGZipData(byte[] data)
    {
        if (data == null || data.Length < 2)
            return false;
        // GZip 魔术数字: 0x1F 0x8B
        return data[0] == 0x1F && data[1] == 0x8B;
    }

    #endregion
}
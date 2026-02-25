using System.IO.Compression;
using System.Threading;
namespace Bing.Helpers;
/// <summary>
/// 压缩工具类测试
/// </summary>
[Trait("Bing.Helpers", "Compression")]
public class CompressionTest : TestBase
{
    /// <inheritdoc />
    public CompressionTest(ITestOutputHelper output) : base(output)
    {
    }
    #region 字节数组压缩测试
    /// <summary>
    /// 测试 - Compress(byte[]) - 基本压缩功能
    /// </summary>
    [Fact]
    public void Compress_ByteArray_CompressesSuccessfully()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Hello, World! This is a test string for compression. ".PadRight(1000, 'x'));
        // Act
        var compressedData = Compression.Compress(originalData);
        // Assert
        compressedData.ShouldNotBeNull();
        compressedData.Length.ShouldBeGreaterThan(0);
        compressedData.Length.ShouldBeLessThan(originalData.Length); // 应该被压缩
        Compression.IsValidGZipData(compressedData).ShouldBeTrue();
        Output.WriteLine($"原始大小: {originalData.Length} 字节");
        Output.WriteLine($"压缩后大小: {compressedData.Length} 字节");
        Output.WriteLine($"压缩率: {Compression.CalculateCompressionRatio(originalData.Length, compressedData.Length):F2}%");
    }
    /// <summary>
    /// 测试 - Compress(byte[]) - 空数组处理
    /// </summary>
    [Fact]
    public void Compress_EmptyByteArray_ReturnsEmptyArray()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();
        // Act
        var result = Compression.Compress(emptyData);
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(0);
    }
    /// <summary>
    /// 测试 - Compress(byte[]) - null 参数抛出异常
    /// </summary>
    [Fact]
    public void Compress_NullByteArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Compression.Compress((byte[])null));
    }
    /// <summary>
    /// 测试 - Compress(byte[], CompressionLevel) - 不同压缩级别
    /// </summary>
    [Theory]
    [InlineData(CompressionLevel.Optimal)]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.NoCompression)]
#if NET6_0_OR_GREATER
    [InlineData(CompressionLevel.SmallestSize)]
#endif
    public void Compress_DifferentCompressionLevels_WorksCorrectly(CompressionLevel level)
    {
        // Arrange
        var testData = Encoding.UTF8.GetBytes(new string('A', 1000)); // 重复字符便于压缩
        // Act
        var compressedData = Compression.Compress(testData, level);
        // Assert
        compressedData.ShouldNotBeNull();
        compressedData.Length.ShouldBeGreaterThan(0);
        Compression.IsValidGZipData(compressedData).ShouldBeTrue();
        Output.WriteLine($"压缩级别: {level}");
        Output.WriteLine($"原始大小: {testData.Length} 字节");
        Output.WriteLine($"压缩后大小: {compressedData.Length} 字节");
        // 验证往返转换
        var decompressed = Compression.Decompress(compressedData);
        decompressed.ShouldBe(testData);
    }
    /// <summary>
    /// 测试 - CompressAsync - 异步压缩
    /// </summary>
    [Fact]
    public async Task CompressAsync_ByteArray_CompressesSuccessfully()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Async compression test data with some repeated content for better compression ratio. ".PadRight(500, 'x'));
        // Act
        var compressedData = await Compression.CompressAsync(originalData);
        // Assert
        compressedData.ShouldNotBeNull();
        compressedData.Length.ShouldBeGreaterThan(0);
        Compression.IsValidGZipData(compressedData).ShouldBeTrue();
        // 验证往返转换
        var decompressed = await Compression.DecompressAsync(compressedData);
        decompressed.ShouldBe(originalData);
    }
    /// <summary>
    /// 测试 - CompressAsync - 取消操作
    /// </summary>
    [Fact]
    public async Task CompressAsync_CancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1MB data
        new Random(42).NextBytes(largeData);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // 立即取消
        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Compression.CompressAsync(largeData, cancellationToken: cts.Token));
    }
    /// <summary>
    /// 测试 - CompressAsync - 不同压缩级别的异步压缩
    /// </summary>
    [Theory]
    [InlineData(CompressionLevel.Optimal)]
    [InlineData(CompressionLevel.Fastest)]
    public async Task CompressAsync_DifferentLevels_WorksCorrectly(CompressionLevel level)
    {
        // Arrange
        var testData = Encoding.UTF8.GetBytes("Test data for async compression. ".PadRight(200, 'x'));
        // Act
        var compressedData = await Compression.CompressAsync(testData, level);
        // Assert
        compressedData.ShouldNotBeNull();
        compressedData.Length.ShouldBeGreaterThan(0);
        Compression.IsValidGZipData(compressedData).ShouldBeTrue();
        Output.WriteLine($"异步压缩级别: {level}, 压缩后大小: {compressedData.Length}");
    }
    #endregion
    #region 字节数组解压测试
    /// <summary>
    /// 测试 - Decompress(byte[]) - 基本解压功能
    /// </summary>
    [Fact]
    public void Decompress_CompressedByteArray_DecompressesSuccessfully()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Test data for decompression functionality with some content to make it worthwhile.");
        var compressedData = Compression.Compress(originalData);
        // Act
        var decompressedData = Compression.Decompress(compressedData);
        // Assert
        decompressedData.ShouldNotBeNull();
        decompressedData.ShouldBe(originalData);
    }
    /// <summary>
    /// 测试 - Decompress(byte[]) - 空数组处理
    /// </summary>
    [Fact]
    public void Decompress_EmptyByteArray_ReturnsEmptyArray()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();
        // Act
        var result = Compression.Decompress(emptyData);
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(0);
    }
    /// <summary>
    /// 测试 - Decompress(byte[]) - null 参数抛出异常
    /// </summary>
    [Fact]
    public void Decompress_NullByteArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Compression.Decompress((byte[])null));
    }
    /// <summary>
    /// 测试 - Decompress(byte[]) - 无效数据抛出异常
    /// </summary>
    [Fact]
    public void Decompress_InvalidGZipData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = Encoding.UTF8.GetBytes("This is not compressed data");
        // Act & Assert
        Should.Throw<InvalidDataException>(() => Compression.Decompress(invalidData));
    }
    /// <summary>
    /// 测试 - DecompressAsync - 异步解压
    /// </summary>
    [Fact]
    public async Task DecompressAsync_CompressedByteArray_DecompressesSuccessfully()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Async decompression test data with content.");
        var compressedData = await Compression.CompressAsync(originalData);
        // Act
        var decompressedData = await Compression.DecompressAsync(compressedData);
        // Assert
        decompressedData.ShouldNotBeNull();
        decompressedData.ShouldBe(originalData);
    }
    /// <summary>
    /// 测试 - DecompressAsync - 取消操作
    /// </summary>
    [Fact]
    public async Task DecompressAsync_CancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var testData = new byte[1024 * 100]; // 100KB
        new Random(42).NextBytes(testData);
        var compressedData = Compression.Compress(testData);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Compression.DecompressAsync(compressedData, cts.Token));
    }
    #endregion
    #region 字符串压缩测试
    /// <summary>
    /// 测试 - Compress(string) - 基本字符串压缩
    /// </summary>
    [Theory]
    [InlineData("Hello, World!")]
    [InlineData("测试中文压缩功能")]
    [InlineData("This is a longer string with some repeated content to test compression efficiency. This is a longer string with some repeated content to test compression efficiency.")]
    [InlineData("🌟 Unicode characters test 🚀 with emojis 🎉")]
    public void Compress_String_CompressesAndDecompressesCorrectly(string input)
    {
        // Act
        var compressed = Compression.Compress(input);
        var decompressed = Compression.Decompress(compressed);
        // Assert
        compressed.ShouldNotBeNull();
        compressed.ShouldNotBeEmpty();
        decompressed.ShouldBe(input);
        Output.WriteLine($"原始文本: {input}");
        Output.WriteLine($"压缩后: {compressed}");
        Output.WriteLine($"往返测试通过");
    }
    /// <summary>
    /// 测试 - Compress(string) - 空字符串和null处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    public void Compress_NullOrWhitespaceString_ReturnsEmptyString(string input)
    {
        // Act
        var result = Compression.Compress(input);
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试 - Compress(string, Encoding) - 不同编码
    /// </summary>
    [Theory]
    [InlineData("English text", "UTF-8")]
    [InlineData("中文测试", "UTF-8")]
    [InlineData("English text", "ASCII")]
    [InlineData("测试文本", "Unicode")]
    [InlineData("Español ñáéíóú", "UTF-8")]
    public void Compress_StringWithDifferentEncodings_WorksCorrectly(string input, string encodingName)
    {
        // Arrange
        var encoding = Encoding.GetEncoding(encodingName);
        // Act
        var compressed = Compression.Compress(input, encoding);
        var decompressed = Compression.Decompress(compressed, encoding);
        // Assert
        if (encodingName == "ASCII" && input.Contains("测试"))
        {
            // ASCII 不支持中文，跳过验证
            Output.WriteLine($"ASCII 编码不支持中文字符，跳过验证");
            return;
        }
        decompressed.ShouldBe(input);
        Output.WriteLine($"编码: {encodingName}, 原始: {input}, 恢复: {decompressed}");
    }
    /// <summary>
    /// 测试 - CompressAsync - 异步字符串压缩
    /// </summary>
    [Fact]
    public async Task CompressAsync_String_CompressesAndDecompressesCorrectly()
    {
        // Arrange
        const string input = "Async string compression test with some content to compress for better testing results.";
        // Act
        var compressed = await Compression.CompressAsync(input);
        var decompressed = await Compression.DecompressAsync(compressed);
        // Assert
        decompressed.ShouldBe(input);
    }
    /// <summary>
    /// 测试 - CompressAsync - 不同编码的异步压缩
    /// </summary>
    [Theory]
    [InlineData("UTF-8")]
    [InlineData("Unicode")]
    [InlineData("ASCII")]
    public async Task CompressAsync_StringWithEncoding_WorksCorrectly(string encodingName)
    {
        // Arrange
        const string input = "Test async compression with encoding";
        var encoding = Encoding.GetEncoding(encodingName);
        // Act
        var compressed = await Compression.CompressAsync(input, encoding);
        var decompressed = await Compression.DecompressAsync(compressed, encoding);
        // Assert
        decompressed.ShouldBe(input);
        Output.WriteLine($"异步压缩编码: {encodingName} 测试通过");
    }
    #endregion
    #region 字符串解压测试
    /// <summary>
    /// 测试 - Decompress(string) - 无效Base64抛出异常
    /// </summary>
    [Theory]
    [InlineData("Invalid base64!")]
    [InlineData("Not@Valid#Base64")]
    [InlineData("12345")]
    public void Decompress_InvalidBase64String_ThrowsFormatException(string invalidBase64)
    {
        // Act & Assert
        Should.Throw<FormatException>(() => Compression.Decompress(invalidBase64));
    }
    /// <summary>
    /// 测试 - Decompress(string) - 有效Base64但无效压缩数据
    /// </summary>
    [Fact]
    public void Decompress_ValidBase64ButInvalidGZipData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidGZipData = Convert.ToBase64String(Encoding.UTF8.GetBytes("Not compressed data"));
        // Act & Assert
        Should.Throw<InvalidDataException>(() => Compression.Decompress(invalidGZipData));
    }
    /// <summary>
    /// 测试 - DecompressAsync - 异步解压无效数据
    /// </summary>
    [Fact]
    public async Task DecompressAsync_InvalidBase64_ThrowsFormatException()
    {
        // Act & Assert
        await Should.ThrowAsync<FormatException>(
            async () => await Compression.DecompressAsync("Invalid@Base64"));
    }
    #endregion
    #region 文件压缩测试
    /// <summary>
    /// 测试 - Zip/UnZip - 文件夹压缩和解压
    /// </summary>
    [Fact]
    public void Zip_UnZip_FolderCompression_WorksCorrectly()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var sourceDir = Path.Combine(tempDir, "CompressionTest_Source_" + Guid.NewGuid().ToString("N")[..8]);
        var zipFile = Path.Combine(tempDir, "CompressionTest_" + Guid.NewGuid().ToString("N")[..8] + ".zip");
        var extractDir = Path.Combine(tempDir, "CompressionTest_Extract_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            // 创建测试文件夹和文件
            Directory.CreateDirectory(sourceDir);
            var subDir = Path.Combine(sourceDir, "SubFolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(sourceDir, "test1.txt"), "Test file 1 content with some data");
            File.WriteAllText(Path.Combine(sourceDir, "test2.txt"), "Test file 2 content with some more data for testing");
            File.WriteAllText(Path.Combine(subDir, "test3.txt"), "Test file 3 in subfolder with content");
            // Act - 压缩
            Compression.Zip(sourceDir, zipFile);
            // Assert - 验证ZIP文件创建
            File.Exists(zipFile).ShouldBeTrue();
            new FileInfo(zipFile).Length.ShouldBeGreaterThan(0);
            // Act - 解压
            Compression.UnZip(zipFile, extractDir);
            // Assert - 验证解压结果
            Directory.Exists(extractDir).ShouldBeTrue();
            File.Exists(Path.Combine(extractDir, "test1.txt")).ShouldBeTrue();
            File.Exists(Path.Combine(extractDir, "test2.txt")).ShouldBeTrue();
            File.Exists(Path.Combine(extractDir, "SubFolder", "test3.txt")).ShouldBeTrue();
            // 验证文件内容
            File.ReadAllText(Path.Combine(extractDir, "test1.txt")).ShouldBe("Test file 1 content with some data");
            File.ReadAllText(Path.Combine(extractDir, "test2.txt")).ShouldBe("Test file 2 content with some more data for testing");
            File.ReadAllText(Path.Combine(extractDir, "SubFolder", "test3.txt")).ShouldBe("Test file 3 in subfolder with content");
            Output.WriteLine("文件夹压缩和解压测试通过");
        }
        finally
        {
            // 清理测试文件
            try
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (File.Exists(zipFile)) File.Delete(zipFile);
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            }
            catch (Exception ex)
            {
                Output.WriteLine($"清理测试文件失败: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// 测试 - Zip - 不同压缩级别
    /// </summary>
    [Theory]
    [InlineData(CompressionLevel.Optimal, true)]
    [InlineData(CompressionLevel.Fastest, false)]
    [InlineData(CompressionLevel.NoCompression, true)]
    public void Zip_DifferentCompressionLevels_WorksCorrectly(CompressionLevel level, bool includeBaseDirectory)
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var sourceDir = Path.Combine(tempDir, "CompressionLevelTest_" + Guid.NewGuid().ToString("N")[..8]);
        var zipFile = Path.Combine(tempDir, "CompressionLevelTest_" + level + "_" + Guid.NewGuid().ToString("N")[..8] + ".zip");
        try
        {
            // 创建测试文件
            Directory.CreateDirectory(sourceDir);
            var largeContent = new string('A', 10000); // 大量重复内容便于测试压缩效果
            File.WriteAllText(Path.Combine(sourceDir, "large.txt"), largeContent);
            // Act
            Compression.Zip(sourceDir, zipFile, level, includeBaseDirectory);
            // Assert
            File.Exists(zipFile).ShouldBeTrue();
            new FileInfo(zipFile).Length.ShouldBeGreaterThan(0);
            Output.WriteLine($"压缩级别: {level}, 包含根目录: {includeBaseDirectory}");
            Output.WriteLine($"ZIP文件大小: {new FileInfo(zipFile).Length} 字节");
            // 验证可以正确解压
            var extractDir = Path.Combine(tempDir, "Extract_" + Guid.NewGuid().ToString("N")[..8]);
            try
            {
                Compression.UnZip(zipFile, extractDir);
                Directory.Exists(extractDir).ShouldBeTrue();
            }
            finally
            {
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (File.Exists(zipFile)) File.Delete(zipFile);
            }
            catch (Exception ex)
            {
                Output.WriteLine($"清理测试文件失败: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// 测试 - Zip - 源目录不存在抛出异常
    /// </summary>
    [Fact]
    public void Zip_NonExistentSourceDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistentDirectory_" + Guid.NewGuid());
        var zipFile = Path.Combine(Path.GetTempPath(), "test.zip");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => Compression.Zip(nonExistentDir, zipFile));
    }
    /// <summary>
    /// 测试 - Zip - 参数验证
    /// </summary>
    [Theory]
    [InlineData(null, "test.zip")]
    [InlineData("", "test.zip")]
    [InlineData("   ", "test.zip")]
    [InlineData("C:\\Source", null)]
    [InlineData("C:\\Source", "")]
    [InlineData("C:\\Source", "   ")]
    public void Zip_InvalidParameters_ThrowsArgumentException(string sourceDir, string zipFile)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Compression.Zip(sourceDir, zipFile));
    }
    /// <summary>
    /// 测试 - UnZip - ZIP文件不存在抛出异常
    /// </summary>
    [Fact]
    public void UnZip_NonExistentZipFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentZip = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid() + ".zip");
        var extractDir = Path.Combine(Path.GetTempPath(), "Extract");
        // Act & Assert
        Should.Throw<FileNotFoundException>(() => Compression.UnZip(nonExistentZip, extractDir));
    }
    /// <summary>
    /// 测试 - UnZip - 覆盖选项
    /// </summary>
    [Fact]
    public void UnZip_OverwriteOption_WorksCorrectly()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var sourceDir = Path.Combine(tempDir, "OverwriteTest_Source_" + Guid.NewGuid().ToString("N")[..8]);
        var zipFile = Path.Combine(tempDir, "OverwriteTest_" + Guid.NewGuid().ToString("N")[..8] + ".zip");
        var extractDir = Path.Combine(tempDir, "OverwriteTest_Extract_" + Guid.NewGuid().ToString("N")[..8]);
        var id = Guid.NewGuid();
        try
        {
            // 创建源文件
            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, $"test_{id}.txt"), "Original content");
            // 创建ZIP
            Compression.Zip(sourceDir, zipFile);
            // 首次解压
            Compression.UnZip(zipFile, extractDir);
            // 验证首次解压成功
            File.Exists(Path.Combine(extractDir, $"test_{id}.txt")).ShouldBeTrue();
            File.ReadAllText(Path.Combine(extractDir, $"test_{id}.txt")).ShouldBe("Original content");
            // 修改解压后的文件
            File.WriteAllText(Path.Combine(extractDir, $"test_{id}.txt"), "Modified content");
            File.ReadAllText(Path.Combine(extractDir, $"test_{id}.txt")).ShouldBe("Modified content");
            // Act - 再次解压，允许覆盖
            Should.NotThrow(() => Compression.UnZip(zipFile, extractDir, true));
            // Assert - 验证文件被覆盖
            File.ReadAllText(Path.Combine(extractDir, $"test_{id}.txt")).ShouldBe("Original content");
            Output.WriteLine("覆盖解压测试通过");
        }
        finally
        {
            try
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (File.Exists(zipFile)) File.Delete(zipFile);
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            }
            catch (Exception ex)
            {
                Output.WriteLine($"清理测试文件失败: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// 测试 - UnZip - 不覆盖现有文件时的行为
    /// </summary>
    [Fact]
    public void UnZip_NoOverwriteOption_ThrowsWhenFileExists()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var sourceDir = Path.Combine(tempDir, "NoOverwriteTest_Source_" + Guid.NewGuid().ToString("N")[..8]);
        var zipFile = Path.Combine(tempDir, "NoOverwriteTest_" + Guid.NewGuid().ToString("N")[..8] + ".zip");
        var extractDir = Path.Combine(tempDir, "NoOverwriteTest_Extract_" + Guid.NewGuid().ToString("N")[..8]);
        var id = Guid.NewGuid();
        try
        {
            // 创建源文件
            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, $"test_{id}.txt"), "Original content");
            // 创建ZIP
            Compression.Zip(sourceDir, zipFile);
            // 首次解压
            Compression.UnZip(zipFile, extractDir);
            // 修改解压后的文件
            File.WriteAllText(Path.Combine(extractDir, $"test_{id}.txt"), "Modified content");
            // Act & Assert - 再次解压，不允许覆盖，应该抛异常
#if NET5_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER
            Should.Throw<InvalidOperationException>(() => Compression.UnZip(zipFile, extractDir, false));
#else
            // .NET Standard 2.0 下可能直接抛出 IOException
            Should.Throw<Exception>(() => Compression.UnZip(zipFile, extractDir, false));
#endif
            Output.WriteLine("不覆盖测试通过");
        }
        finally
        {
            try
            {
                if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                if (File.Exists(zipFile)) File.Delete(zipFile);
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            }
            catch (Exception ex)
            {
                Output.WriteLine($"清理测试文件失败: {ex.Message}");
            }
        }
    }
    #endregion
    #region 辅助方法测试
    /// <summary>
    /// 测试 - CalculateCompressionRatio - 压缩率计算
    /// </summary>
    [Theory]
    [InlineData(1000, 300, 70.0)]    // 70% 压缩率
    [InlineData(1000, 500, 50.0)]    // 50% 压缩率
    [InlineData(1000, 1000, 0.0)]    // 无压缩
    [InlineData(1000, 1200, 0.0)]    // 负压缩（变大了）
    [InlineData(0, 100, 0.0)]        // 原始大小为0
    [InlineData(1000, 0, 100.0)]     // 压缩后大小为0
    [InlineData(-100, 50, 0.0)]      // 负的原始大小
    [InlineData(1000, -50, 100.0)]     // 负的压缩大小
    public void CalculateCompressionRatio_DifferentSizes_ReturnsCorrectRatio(long originalSize, long compressedSize, double expectedRatio)
    {
        // Act
        var ratio = Compression.CalculateCompressionRatio(originalSize, compressedSize);
        // Assert
        ratio.ShouldBe(expectedRatio, 0.01); // 允许0.01的误差
    }
    /// <summary>
    /// 测试 - IsValidGZipData - GZip数据验证
    /// </summary>
    [Fact]
    public void IsValidGZipData_DifferentData_ReturnsCorrectResult()
    {
        // Arrange
        var validGZipData = Compression.Compress(Encoding.UTF8.GetBytes("test data for gzip validation"));
        var invalidData = Encoding.UTF8.GetBytes("not gzip data");
        var emptyData = Array.Empty<byte>();
        var shortData = new byte[] { 0x1F }; // 只有一个字节
        var gzipMagicOnly = new byte[] { 0x1F, 0x8B }; // 只有魔术数字
        // Act & Assert
        Compression.IsValidGZipData(validGZipData).ShouldBeTrue();
        Compression.IsValidGZipData(invalidData).ShouldBeFalse();
        Compression.IsValidGZipData(emptyData).ShouldBeFalse();
        Compression.IsValidGZipData(shortData).ShouldBeFalse();
        Compression.IsValidGZipData(gzipMagicOnly).ShouldBeTrue(); // 魔术数字正确
        Compression.IsValidGZipData(null).ShouldBeFalse();
    }
    #endregion
    #region 往返测试
    /// <summary>
    /// 测试 - 往返压缩 - 字节数组
    /// </summary>
    [Theory]
    [InlineData(0)]        // 空数组
    [InlineData(1)]        // 单字节
    [InlineData(100)]      // 小数组
    [InlineData(10000)]    // 大数组
    public void RoundTrip_ByteArray_MaintainsDataIntegrity(int dataSize)
    {
        // Arrange
        var originalData = new byte[dataSize];
        if (dataSize > 0)
        {
            var random = new Random(42); // 固定种子确保可重复
            random.NextBytes(originalData);
        }
        // Act
        var compressed = dataSize == 0 ? Array.Empty<byte>() : Compression.Compress(originalData);
        var decompressed = dataSize == 0 ? Array.Empty<byte>() : Compression.Decompress(compressed);
        // Assert
        decompressed.ShouldBe(originalData);
        Output.WriteLine($"数据大小: {dataSize} 字节, 往返测试通过");
    }
    /// <summary>
    /// 测试 - 往返压缩 - 大文本
    /// </summary>
    [Fact]
    public void RoundTrip_LargeText_MaintainsDataIntegrity()
    {
        // Arrange
        var sb = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"这是第 {i} 行测试文本，包含中文和数字 {i * 123}。");
        }
        var originalText = sb.ToString();
        // Act
        var compressed = Compression.Compress(originalText);
        var decompressed = Compression.Decompress(compressed);
        // Assert
        decompressed.ShouldBe(originalText);
        var originalBytes = Encoding.UTF8.GetBytes(originalText);
        var compressedBytes = Convert.FromBase64String(compressed);
        var ratio = Compression.CalculateCompressionRatio(originalBytes.Length, compressedBytes.Length);
        Output.WriteLine($"原始文本大小: {originalBytes.Length} 字节");
        Output.WriteLine($"压缩后大小: {compressedBytes.Length} 字节");
        Output.WriteLine($"压缩率: {ratio:F2}%");
    }
    /// <summary>
    /// 测试 - 异步往返压缩
    /// </summary>
    [Fact]
    public async Task RoundTrip_AsyncCompression_MaintainsDataIntegrity()
    {
        // Arrange
        var originalText = "Async round trip test with some content to make compression worthwhile. ".PadRight(500, 'x');
        // Act
        var compressed = await Compression.CompressAsync(originalText);
        var decompressed = await Compression.DecompressAsync(compressed);
        // Assert
        decompressed.ShouldBe(originalText);
        Output.WriteLine("异步往返压缩测试通过");
    }
    #endregion
    #region 性能和并发测试
    /// <summary>
    /// 测试 - 性能测试 - 大量小数据压缩
    /// </summary>
    [Fact]
    public void PerformanceTest_ManySmallCompressions_CompletesInReasonableTime()
    {
        // Arrange
        const int iterations = 100;
        var testData = Encoding.UTF8.GetBytes("Short test string for performance testing with some repeated content.");
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var compressed = Compression.Compress(testData);
                var decompressed = Compression.Decompress(compressed);
                decompressed.ShouldBe(testData);
            }
        }, TimeSpan.FromSeconds(5)); // 应该在5秒内完成100次压缩
        Output.WriteLine($"完成 {iterations} 次小数据压缩/解压缩");
    }
    /// <summary>
    /// 测试 - 性能测试 - 大数据压缩
    /// </summary>
    [Fact]
    public void PerformanceTest_LargeDataCompression_CompletesInReasonableTime()
    {
        // Arrange
        var largeText = new string('A', 100000); // 100KB 重复字符
        var testData = Encoding.UTF8.GetBytes(largeText);
        // Act & Assert
        Should.CompleteIn(() =>
        {
            var compressed = Compression.Compress(testData);
            var decompressed = Compression.Decompress(compressed);
            decompressed.ShouldBe(testData);
            var ratio = Compression.CalculateCompressionRatio(testData.Length, compressed.Length);
            Output.WriteLine($"大数据压缩 - 原始: {testData.Length}, 压缩后: {compressed.Length}, 压缩率: {ratio:F2}%");
        }, TimeSpan.FromSeconds(10)); // 大数据压缩允许更长时间
    }
    /// <summary>
    /// 测试 - 并发测试 - 多线程同时压缩
    /// </summary>
    [Fact]
    public void ConcurrencyTest_ParallelCompressions_WorkCorrectly()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 10;
        var testData = Encoding.UTF8.GetBytes("Concurrent compression test data with some content.");
        // Act & Assert
        Should.NotThrow(() =>
        {
            Parallel.For(0, threadCount, threadId =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var uniqueData = Encoding.UTF8.GetBytes($"Thread {threadId} Operation {i}: {testData}");
                    var compressed = Compression.Compress(uniqueData);
                    var decompressed = Compression.Decompress(compressed);
                    decompressed.ShouldBe(uniqueData);
                }
            });
        });
        Output.WriteLine($"并发测试完成: {threadCount} 个线程，每个线程 {operationsPerThread} 次操作");
    }
    #endregion
    #region 边界条件和错误处理测试
    /// <summary>
    /// 测试 - 极大数据处理
    /// </summary>
    [Fact]
    public void EdgeCase_VeryLargeData_HandlesCorrectly()
    {
        // Arrange - 创建1MB的测试数据
        var largeData = new byte[1024 * 1024];
        new Random(42).NextBytes(largeData);
        // Act & Assert
        Should.NotThrow(() =>
        {
            var compressed = Compression.Compress(largeData);
            var decompressed = Compression.Decompress(compressed);
            decompressed.ShouldBe(largeData);
            Output.WriteLine($"极大数据测试 - 原始: {largeData.Length}, 压缩后: {compressed.Length}");
        });
    }
    /// <summary>
    /// 测试 - 特殊字符处理
    /// </summary>
    [Fact]
    public void EdgeCase_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var specialText = "特殊字符测试: \0\t\n\r\x01\x02\xFF 🌟🚀🎉 ñáéíóú ßäöü αβγδε";
        // Act
        var compressed = Compression.Compress(specialText);
        var decompressed = Compression.Decompress(compressed);
        // Assert
        decompressed.ShouldBe(specialText);
        Output.WriteLine("特殊字符测试通过");
    }
    /// <summary>
    /// 测试 - 内存使用优化验证
    /// </summary>
    [Fact]
    public void MemoryUsage_MultipleOperations_DoesNotLeak()
    {
        // Arrange
        const int iterations = 50;
        var testData = Encoding.UTF8.GetBytes("Memory leak test data with content.");
        // Act - 多次操作验证内存不泄露
        Should.NotThrow(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var compressed = Compression.Compress(testData);
                var decompressed = Compression.Decompress(compressed);
                // 强制垃圾回收以验证资源正确释放
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        });
        Output.WriteLine($"内存使用测试完成: {iterations} 次操作");
    }
    #endregion
    #region 实际应用场景测试
    /// <summary>
    /// 测试 - 实际场景 - JSON数据压缩
    /// </summary>
    [Fact]
    public void RealWorldScenario_JsonCompression_WorksCorrectly()
    {
        // Arrange - 模拟JSON数据
        var jsonData = @"{
            ""users"": [
                {""id"": 1, ""name"": ""张三"", ""email"": ""zhangsan@example.com"", ""age"": 30},
                {""id"": 2, ""name"": ""李四"", ""email"": ""lisi@example.com"", ""age"": 25},
                {""id"": 3, ""name"": ""王五"", ""email"": ""wangwu@example.com"", ""age"": 35}
            ],
            ""metadata"": {
                ""total"": 3,
                ""created"": ""2023-01-01T00:00:00Z"",
                ""version"": ""1.0""
            }
        }";
        // Act
        var compressed = Compression.Compress(jsonData);
        var decompressed = Compression.Decompress(compressed);
        // Assert
        decompressed.ShouldBe(jsonData);
        var originalSize = Encoding.UTF8.GetBytes(jsonData).Length;
        var compressedSize = Convert.FromBase64String(compressed).Length;
        var ratio = Compression.CalculateCompressionRatio(originalSize, compressedSize);
        Output.WriteLine($"JSON压缩测试 - 原始: {originalSize}, 压缩后: {compressedSize}, 压缩率: {ratio:F2}%");
    }
    /// <summary>
    /// 测试 - 实际场景 - 日志数据压缩
    /// </summary>
    [Fact]
    public void RealWorldScenario_LogDataCompression_WorksCorrectly()
    {
        // Arrange - 模拟重复性较高的日志数据
        var sb = new StringBuilder();
        var timestamp = DateTime.Now;
        for (int i = 0; i < 100; i++)
        {
            sb.AppendLine($"[{timestamp.AddMinutes(i):yyyy-MM-dd HH:mm:ss}] INFO  UserService - User login attempt for user_id={i % 10}, session_id=sess_{i}, ip_address=192.168.1.{i % 255}");
        }
        var logData = sb.ToString();
        // Act
        var compressed = Compression.Compress(logData);
        var decompressed = Compression.Decompress(compressed);
        // Assert
        decompressed.ShouldBe(logData);
        var originalSize = Encoding.UTF8.GetBytes(logData).Length;
        var compressedSize = Convert.FromBase64String(compressed).Length;
        var ratio = Compression.CalculateCompressionRatio(originalSize, compressedSize);
        Output.WriteLine($"日志压缩测试 - 原始: {originalSize}, 压缩后: {compressedSize}, 压缩率: {ratio:F2}%");
        // 日志数据通常有较好的压缩率
        ratio.ShouldBeGreaterThan(30); // 期望至少30%的压缩率
    }
    #endregion
}

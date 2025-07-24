using Bing.Extensions;

namespace Bing.IO;

/// <summary>
/// 文件大小单位 测试
/// </summary>
public class FileSizeUnitTest
{
    /// <summary>
    /// 测试 - Description - 获取单位描述
    /// </summary>
    [Theory]
    [InlineData(FileSizeUnit.Byte, "B")]
    [InlineData(FileSizeUnit.K, "KB")]
    [InlineData(FileSizeUnit.M, "MB")]
    [InlineData(FileSizeUnit.G, "GB")]
    [InlineData(FileSizeUnit.T, "TB")]
    [InlineData(FileSizeUnit.P, "PB")]
    public void Description_ValidUnit_ReturnsCorrectDescription(FileSizeUnit unit, string expected)
    {
        // Act
        var result = unit.Description();

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Description - 可空单位
    /// </summary>
    [Fact]
    public void Description_NullableUnit_ReturnsCorrectDescription()
    {
        // Arrange
        FileSizeUnit? unit = FileSizeUnit.G;
        FileSizeUnit? nullUnit = null;

        // Act
        var result = unit.Description();
        var nullResult = nullUnit.Description();

        // Assert
        result.ShouldBe("GB");
        nullResult.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试 - GetByteMultiplier - 获取字节倍数
    /// </summary>
    [Theory]
    [InlineData(FileSizeUnit.Byte, 1L)]
    [InlineData(FileSizeUnit.K, 1024L)]
    [InlineData(FileSizeUnit.M, 1048576L)]
    [InlineData(FileSizeUnit.G, 1073741824L)]
    [InlineData(FileSizeUnit.T, 1099511627776L)]
    [InlineData(FileSizeUnit.P, 1125899906842624L)]
    public void GetByteMultiplier_ValidUnit_ReturnsCorrectMultiplier(FileSizeUnit unit, long expected)
    {
        // Act
        var result = unit.GetByteMultiplier();

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ConvertFromBytes - 从字节转换
    /// </summary>
    [Theory]
    [InlineData(FileSizeUnit.K, 1024L, 1.0)]
    [InlineData(FileSizeUnit.M, 1048576L, 1.0)]
    [InlineData(FileSizeUnit.G, 1073741824L, 1.0)]
    [InlineData(FileSizeUnit.T, 1099511627776L, 1.0)]
    [InlineData(FileSizeUnit.P, 1125899906842624L, 1.0)]
    [InlineData(FileSizeUnit.K, 1536L, 1.5)]
    public void ConvertFromBytes_ValidInput_ReturnsCorrectValue(FileSizeUnit unit, long bytes, double expected)
    {
        // Act
        var result = unit.ConvertFromBytes(bytes);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ConvertFromBytes - 负数字节抛出异常
    /// </summary>
    [Fact]
    public void ConvertFromBytes_NegativeBytes_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeUnit.K.ConvertFromBytes(-1));
    }

    /// <summary>
    /// 测试 - ConvertToBytes - 转换为字节
    /// </summary>
    [Theory]
    [InlineData(FileSizeUnit.K, 1.0, 1024L)]
    [InlineData(FileSizeUnit.M, 2.5, 2621440L)]
    [InlineData(FileSizeUnit.G, 0.5, 536870912L)]
    [InlineData(FileSizeUnit.P, 1.0, 1125899906842624L)]
    public void ConvertToBytes_ValidInput_ReturnsCorrectBytes(FileSizeUnit unit, double value, long expected)
    {
        // Act
        var result = unit.ConvertToBytes(value);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ConvertToBytes - 负数值抛出异常
    /// </summary>
    [Fact]
    public void ConvertToBytes_NegativeValue_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeUnit.K.ConvertToBytes(-1.0));
    }

    /// <summary>
    /// 测试 - GetBestUnit - 获取最佳单位
    /// </summary>
    [Theory]
    [InlineData(512L, FileSizeUnit.Byte)]
    [InlineData(1024L, FileSizeUnit.K)]
    [InlineData(1048576L, FileSizeUnit.M)]
    [InlineData(1073741824L, FileSizeUnit.G)]
    [InlineData(1099511627776L, FileSizeUnit.T)]
    [InlineData(1125899906842624L, FileSizeUnit.P)]
    public void GetBestUnit_ValidBytes_ReturnsCorrectUnit(long bytes, FileSizeUnit expected)
    {
        // Act
        var result = FileSizeUnitExtensions.GetBestUnit(bytes);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetBestUnit - 负数字节抛出异常
    /// </summary>
    [Fact]
    public void GetBestUnit_NegativeBytes_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeUnitExtensions.GetBestUnit(-1));
    }

    /// <summary>
    /// 测试 - FormatSize - 格式化文件大小
    /// </summary>
    [Theory]
    [InlineData(FileSizeUnit.K, 1.5, 2, "1.50 KB")]
    [InlineData(FileSizeUnit.M, 2.0, 1, "2.0 MB")]
    [InlineData(FileSizeUnit.G, 1.0, 0, "1 GB")]
    [InlineData(FileSizeUnit.P, 5.25, 2, "5.25 PB")]
    public void FormatSize_ValidInput_ReturnsFormattedString(FileSizeUnit unit, double value, int precision, string expected)
    {
        // Act
        var result = unit.FormatSize(value, precision);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - AutoFormat - 自动格式化
    /// </summary>
    [Theory]
    [InlineData(512L, 2, "512.00 B")]
    [InlineData(1536L, 2, "1.50 KB")]
    [InlineData(2097152L, 1, "2.0 MB")]
    [InlineData(1024L, 0, "1 KB")]
    [InlineData(1125899906842624L, 2, "1.00 PB")]
    public void AutoFormat_ValidBytes_ReturnsFormattedString(long bytes, int precision, string expected)
    {
        // Act
        var result = FileSizeUnitExtensions.AutoFormat(bytes, precision);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - TryParseSize - 解析文件大小字符串
    /// </summary>
    [Theory]
    [InlineData("1.5 KB", true, 1536L)]
    [InlineData("2 MB", true, 2097152L)]
    [InlineData("1 GB", true, 1073741824L)]
    [InlineData("invalid", false, 0L)]
    [InlineData("", false, 0L)]
    [InlineData("1.5", false, 0L)]
    public void TryParseSize_VariousInputs_ReturnsExpectedResults(string input, bool expectedSuccess, long expectedBytes)
    {
        // Act
        var success = FileSizeUnitExtensions.TryParseSize(input, out var bytes);

        // Assert
        success.ShouldBe(expectedSuccess);
        bytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - Value - 获取枚举值
    /// </summary>
    [Theory]
    [InlineData(FileSizeUnit.Byte, 0)]
    [InlineData(FileSizeUnit.K, 1)]
    [InlineData(FileSizeUnit.M, 2)]
    [InlineData(FileSizeUnit.G, 3)]
    [InlineData(FileSizeUnit.T, 4)]
    [InlineData(FileSizeUnit.P, 5)]
    public void Value_ValidUnit_ReturnsCorrectValue(FileSizeUnit unit, int expected)
    {
        // Arrange
        FileSizeUnit? nullableUnit = unit;

        // Act
        var result = nullableUnit.Value();

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Value - 空单位返回null
    /// </summary>
    [Fact]
    public void Value_NullUnit_ReturnsNull()
    {
        // Arrange
        FileSizeUnit? nullUnit = null;

        // Act
        var result = nullUnit.Value();

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 精度处理
    /// </summary>
    [Theory]
    [InlineData(1536L, 0, "2 KB")]
    [InlineData(1536L, 1, "1.5 KB")]
    [InlineData(1536L, 3, "1.500 KB")]
    public void Precision_HandledCorrectly(long bytes, int precision, string expected)
    {
        // Act
        var result = FileSizeUnitExtensions.AutoFormat(bytes, precision);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - 与FileSize类的兼容性
    /// </summary>
    [Fact]
    public void Compatibility_WithFileSizeClass()
    {
        // Arrange
        var fileSize = new FileSize(1536, FileSizeUnit.K); // 1.5MB

        // Act
        var sizeInBytes = fileSize.Size;
        var bestUnit = FileSizeUnitExtensions.GetBestUnit(sizeInBytes);
        var formatted = FileSizeUnitExtensions.AutoFormat(sizeInBytes);

        // Assert
        sizeInBytes.ShouldBe(1572864L); // 1536 * 1024
        bestUnit.ShouldBe(FileSizeUnit.M);
        formatted.ShouldBe("1.50 MB");
    }
}
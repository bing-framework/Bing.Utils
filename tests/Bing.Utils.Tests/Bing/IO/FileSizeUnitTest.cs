using Bing.Extensions;
namespace Bing.IO;
/// <summary>
/// 文件大小单位 测试
/// </summary>
public class FileSizeUnitTest
{
    #region 基础功能测试
    /// <summary>
    /// 验证文件大小单位返回预期描述。
    /// </summary>
    /// <param name="unit">待检查的文件大小单位。</param>
    /// <param name="expected">预期的单位描述。</param>
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
    /// 验证文件大小单位对应的字节倍数。
    /// </summary>
    /// <param name="unit">待检查的文件大小单位。</param>
    /// <param name="expected">预期的字节倍数。</param>
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
    #endregion
    #region 单位转换测试
    /// <summary>
    /// 验证字节数转换为指定文件大小单位。
    /// </summary>
    /// <param name="unit">转换后的文件大小单位。</param>
    /// <param name="bytes">待转换的字节数。</param>
    /// <param name="expected">预期的单位数值。</param>
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
    /// 验证字节数按指定精度转换为文件大小单位。
    /// </summary>
    /// <param name="unit">转换后的文件大小单位。</param>
    /// <param name="bytes">待转换的字节数。</param>
    /// <param name="precision">保留的小数位数。</param>
    /// <param name="expected">预期的单位数值。</param>
    [Theory]
    [InlineData(FileSizeUnit.K, 1024L, 2, 1.0)]
    [InlineData(FileSizeUnit.M, 1048576L, 2, 1.0)]
    [InlineData(FileSizeUnit.G, 1073741824L, 2, 1.0)]
    [InlineData(FileSizeUnit.K, 1536L, 1, 1.5)]
    [InlineData(FileSizeUnit.M, 2621440L, 1, 2.5)]
    public void ConvertFromBytes_WithValidInput_ShouldReturnCorrectValue(FileSizeUnit unit, long bytes, int precision, double expected)
    {
        // Act
        var result = unit.ConvertFromBytes(bytes, precision);
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
    /// 测试 - ConvertFromBytes - 负数字节应抛出异常
    /// </summary>
    [Fact]
    public void ConvertFromBytes_WithNegativeBytes_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeUnit.K.ConvertFromBytes(-1))
            .ParamName.ShouldBe("bytes");
    }
    /// <summary>
    /// 测试 - ConvertFromBytes - 负数精度应抛出异常
    /// </summary>
    [Fact]
    public void ConvertFromBytes_WithNegativePrecision_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeUnit.K.ConvertFromBytes(1024, -1))
            .ParamName.ShouldBe("precision");
    }
    /// <summary>
    /// 验证文件大小单位数值转换为字节数。
    /// </summary>
    /// <param name="unit">输入值采用的文件大小单位。</param>
    /// <param name="value">待转换的单位数值。</param>
    /// <param name="expected">预期的字节数。</param>
    [Theory]
    [InlineData(FileSizeUnit.K, 1.0, 1024L)]
    [InlineData(FileSizeUnit.M, 2.5, 2621440L)]
    [InlineData(FileSizeUnit.G, 0.5, 536870912L)]
    [InlineData(FileSizeUnit.P, 1.0, 1125899906842624L)]
    [InlineData(FileSizeUnit.Byte, 100.0, 100L)]
    public void ConvertToBytes_WithValidInput_ShouldReturnCorrectBytes(FileSizeUnit unit, double value, long expected)
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
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeUnit.K.ConvertToBytes(-1.0))
            .ParamName.ShouldBe("value");
    }
    /// <summary>
    /// 测试 - ConvertToBytes - 溢出应抛出异常
    /// </summary>
    [Fact]
    public void ConvertToBytes_WithOverflowValue_ShouldThrowOverflowException()
    {
        // Act & Assert
        Should.Throw<OverflowException>(() => FileSizeUnit.P.ConvertToBytes(double.MaxValue))
            .Message.ShouldContain("转换结果超出长整型范围");
    }
    #endregion
    #region 格式化测试
    /// <summary>
    /// 验证文件大小按指定精度和单位格式化。
    /// </summary>
    /// <param name="unit">文件大小单位。</param>
    /// <param name="value">待格式化的单位数值。</param>
    /// <param name="precision">保留的小数位数。</param>
    /// <param name="expected">预期的格式化文本。</param>
    [Theory]
    [InlineData(FileSizeUnit.K, 1.5, 2, "1.5 KB")]
    [InlineData(FileSizeUnit.M, 2.0, 1, "2 MB")]
    [InlineData(FileSizeUnit.G, 1.0, 0, "1 GB")]
    [InlineData(FileSizeUnit.G, 1.25, 2, "1.25 GB")]
    [InlineData(FileSizeUnit.P, 5.0, 1, "5 PB")]
    [InlineData(FileSizeUnit.P, 5.25, 2, "5.25 PB")]
    [InlineData(FileSizeUnit.Byte, 512.0, 0, "512 B")]
    public void FormatSize_ValidInput_ReturnsFormattedString(FileSizeUnit unit, double value, int precision, string expected)
    {
        // Act
        var result = unit.FormatSize(value, precision);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证文件大小格式化移除小数部分的尾随零。
    /// </summary>
    /// <param name="unit">文件大小单位。</param>
    /// <param name="value">待格式化的单位数值。</param>
    /// <param name="precision">保留的小数位数。</param>
    /// <param name="expected">预期的格式化文本。</param>
    [Theory]
    [InlineData(FileSizeUnit.K, 1.0, 2, "1 KB")]      // 移除 .00
    [InlineData(FileSizeUnit.M, 2.5, 2, "2.5 MB")]    // 移除 .50 -> .5
    [InlineData(FileSizeUnit.G, 3.125, 3, "3.125 GB")] // 保留有效位数
    [InlineData(FileSizeUnit.T, 4.100, 3, "4.1 TB")]   // 移除尾随0
    public void FormatSize_TrailingZeroRemoval_ReturnsCleanString(FileSizeUnit unit, double value, int precision, string expected)
    {
        // Act
        var result = unit.FormatSize(value, precision);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - FormatSize - 负数精度应抛出异常
    /// </summary>
    [Fact]
    public void FormatSize_WithNegativePrecision_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeUnit.K.FormatSize(1.5, -1))
            .ParamName.ShouldBe("precision");
    }
    #endregion
    #region 兼容性和集成测试
    /// <summary>
    /// 验证可空文件大小单位返回枚举数值。
    /// </summary>
    /// <param name="unit">待检查的文件大小单位。</param>
    /// <param name="expected">预期的枚举数值。</param>
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
    /// 验证文件大小与字节数往返转换后的误差在范围内。
    /// </summary>
    /// <param name="unit">文件大小单位。</param>
    /// <param name="originalValue">原始单位数值。</param>
    [Theory]
    [InlineData(FileSizeUnit.K, 1.5)]
    [InlineData(FileSizeUnit.M, 2.5)]
    [InlineData(FileSizeUnit.G, 1.25)]
    [InlineData(FileSizeUnit.T, 0.75)]
    public void RoundTrip_ConversionConsistency(FileSizeUnit unit, double originalValue)
    {
        // Act
        var bytes = unit.ConvertToBytes(originalValue);
        var convertedBack = unit.ConvertFromBytes(bytes);
        // Assert - 允许微小的浮点精度差异
        Math.Abs(convertedBack - originalValue).ShouldBeLessThan(0.01);
    }
    #endregion
}

namespace Bing.IO;
/// <summary>
/// 测试文件大小
/// </summary>
public class FileSizeTest
{
    #region 构造函数测试
    /// <summary>
    /// 验证构造函数将指定单位的大小转换为字节数。
    /// </summary>
    /// <param name="size">输入单位下的大小数值。</param>
    /// <param name="unit">文件大小单位。</param>
    /// <param name="expectedBytes">预期的字节数。</param>
    [Theory]
    [InlineData(1, FileSizeUnit.Byte, 1)]
    [InlineData(1, FileSizeUnit.K, 1024)]
    [InlineData(1, FileSizeUnit.M, 1024 * 1024)]
    [InlineData(1, FileSizeUnit.G, 1024L * 1024L * 1024L)]
    [InlineData(1, FileSizeUnit.T, 1024L * 1024L * 1024L * 1024L)]
    [InlineData(1, FileSizeUnit.P, 1024L * 1024L * 1024L * 1024L * 1024L)]
    [InlineData(0, FileSizeUnit.Byte, 0)]
    [InlineData(2, FileSizeUnit.K, 2048)]
    public void Constructor_WithValidSizeAndUnit_ShouldCalculateCorrectBytes(long size, FileSizeUnit unit, long expectedBytes)
    {
        // Act
        var fileSize = new FileSize(size, unit);
        // Assert
        fileSize.Size.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - FileSize构造函数 - 负数应抛出异常
    /// </summary>
    [Fact]
    public void Constructor_WithNegativeSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new FileSize(-1, FileSizeUnit.Byte))
            .ParamName.ShouldBe("size");
    }
    /// <summary>
    /// 测试 - FileSize构造函数 - 溢出应抛出异常
    /// </summary>
    [Fact]
    public void Constructor_WithOverflowSize_ShouldThrowOverflowException()
    {
        // Act & Assert
        Should.Throw<OverflowException>(() => new FileSize(long.MaxValue, FileSizeUnit.P));
    }
    #endregion
    #region 工厂方法测试
    /// <summary>
    /// 验证字节数工厂保存输入大小。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expectedSize">预期的字节大小。</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1024, 1024)]
    [InlineData(long.MaxValue, long.MaxValue)]
    public void FromBytes_WithValidBytes_ShouldCreateCorrectInstance(long bytes, long expectedSize)
    {
        // Act
        var fileSize = FileSize.FromBytes(bytes);
        // Assert
        fileSize.Size.ShouldBe(expectedSize);
    }
    /// <summary>
    /// 测试 - FromBytes工厂方法 - 负数应抛出异常
    /// </summary>
    [Fact]
    public void FromBytes_WithNegativeBytes_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => FileSize.FromBytes(-1))
            .ParamName.ShouldBe("bytes");
    }
    /// <summary>
    /// 验证有效文件大小文本解析为预期字节数。
    /// </summary>
    /// <param name="sizeString">待解析的文件大小文本。</param>
    /// <param name="expectedBytes">预期的字节数。</param>
    [Theory]
    [InlineData("1024 B", 1024)]
    [InlineData("1.5 KB", 1536)]
    [InlineData("2 MB", 2097152)]
    [InlineData("1 GB", 1073741824)]
    public void Parse_WithValidString_ShouldReturnCorrectFileSize(string sizeString, long expectedBytes)
    {
        // Act
        var fileSize = FileSize.Parse(sizeString);
        // Assert
        fileSize.Size.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 验证文件大小解析拒绝无效文本。
    /// </summary>
    /// <param name="invalidString">格式无效的文件大小文本。</param>
    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("1.5 XX")]
    public void Parse_WithInvalidString_ShouldThrowArgumentException(string invalidString)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => FileSize.Parse(invalidString));
    }
    /// <summary>
    /// 验证文件大小尝试解析的标记及结果字节数。
    /// </summary>
    /// <param name="input">待解析的文件大小文本。</param>
    /// <param name="expectedSuccess">预期的解析成功标记。</param>
    /// <param name="expectedBytes">预期的字节数。</param>
    [Theory]
    [InlineData("1.5 KB", true, 1536)]
    [InlineData("2 MB", true, 2097152)]
    [InlineData("invalid", false, 0)]
    [InlineData("", false, 0)]
    public void TryParse_WithVariousInputs_ShouldReturnExpectedResults(string input, bool expectedSuccess, long expectedBytes)
    {
        // Act
        var success = FileSize.TryParse(input, out var fileSize);
        // Assert
        success.ShouldBe(expectedSuccess);
        fileSize.Size.ShouldBe(expectedBytes);
    }
    #endregion
    #region 单位转换测试
    /// <summary>
    /// 验证长整数大小读取保留原始字节数。
    /// </summary>
    /// <param name="inputSize">输入的字节大小。</param>
    /// <param name="expectedSize">预期的字节大小。</param>
    [Theory]
    [InlineData(1024, 1024)]
    [InlineData(0, 0)]
    [InlineData(long.MaxValue, long.MaxValue)]
    public void GetLongSize_ShouldReturnCorrectValue(long inputSize, long expectedSize)
    {
        // Arrange
        var fileSize = new FileSize(inputSize, FileSizeUnit.Byte);
        // Act
        var result = fileSize.GetLongSize();
        // Assert
        result.ShouldBe(expectedSize);
    }
    /// <summary>
    /// 验证字节数按指定单位和精度转换。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="unit">文件大小单位。</param>
    /// <param name="precision">保留的小数位数。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(1536, FileSizeUnit.K, 2, 1.5)]
    [InlineData(2097152, FileSizeUnit.M, 1, 2.0)]
    [InlineData(1073741824, FileSizeUnit.G, 0, 1.0)]
    public void GetSize_WithSpecificUnit_ShouldReturnCorrectValue(long bytes, FileSizeUnit unit, int precision, double expected)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(bytes);
        // Act
        var result = fileSize.GetSize(unit, precision);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证字节数转换为 KB 大小。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expectedKB">预期的 KB 大小。</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(512, 0.5)]
    [InlineData(1024, 1.0)]
    [InlineData(2048, 2.0)]
    [InlineData(1536, 1.5)]
    public void GetSizeByK_ShouldReturnCorrectKilobyteSize(long bytes, double expectedKB)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(bytes);
        // Act
        var result = fileSize.GetSizeByK();
        // Assert
        result.ShouldBe(expectedKB);
    }
    /// <summary>
    /// 验证字节数转换为 MB 大小。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expectedMB">预期的 MB 大小。</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1024 * 1024, 1.0)]
    [InlineData(2 * 1024 * 1024, 2.0)]
    [InlineData(1536 * 1024, 1.5)] // 1.5MB
    public void GetSizeByM_ShouldReturnCorrectMegabyteSize(long bytes, double expectedMB)
    {
        // Arrange
        var fileSize = new FileSize(bytes, FileSizeUnit.Byte);
        // Act
        var result = fileSize.GetSizeByM();
        // Assert
        result.ShouldBe(expectedMB);
    }
    /// <summary>
    /// 验证字节数转换为 GB 大小。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expectedGB">预期的 GB 大小。</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1024L * 1024L * 1024L, 1.0)]
    [InlineData(2L * 1024L * 1024L * 1024L, 2.0)]
    public void GetSizeByG_ShouldReturnCorrectGigabyteSize(long bytes, double expectedGB)
    {
        // Arrange
        var fileSize = new FileSize(bytes, FileSizeUnit.Byte);
        // Act
        var result = fileSize.GetSizeByG();
        // Assert
        result.ShouldBe(expectedGB);
    }
    /// <summary>
    /// 验证字节数转换为 TB 大小。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expectedTB">预期的 TB 大小。</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1024L * 1024L * 1024L * 1024L, 1.0)]
    [InlineData(2L * 1024L * 1024L * 1024L * 1024L, 2.0)]
    public void GetSizeByT_ShouldReturnCorrectTerabyteSize(long bytes, double expectedTB)
    {
        // Arrange
        var fileSize = new FileSize(bytes, FileSizeUnit.Byte);
        // Act
        var result = fileSize.GetSizeByT();
        // Assert
        result.ShouldBe(expectedTB);
    }
    /// <summary>
    /// 验证字节数转换为 PB 大小。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expectedPB">预期的 PB 大小。</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1024L * 1024L * 1024L * 1024L * 1024L, 1.0)]
    [InlineData(2L * 1024L * 1024L * 1024L * 1024L * 1024L, 2.0)]
    public void GetSizeByP_ShouldReturnCorrectPetabyteSize(long bytes, double expectedPB)
    {
        // Arrange
        var fileSize = new FileSize(bytes, FileSizeUnit.Byte);
        // Act
        var result = fileSize.GetSizeByP();
        // Assert
        result.ShouldBe(expectedPB);
    }
    #endregion
    #region GetOptimalUnit 测试
    /// <summary>
    /// 验证最佳单位选择及对应的单位数值。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expectedValue">预期的单位数值。</param>
    /// <param name="expectedUnit">预期的文件大小单位。</param>
    [Theory]
    [InlineData(100, 100.0, FileSizeUnit.Byte)]
    [InlineData(1024, 1.0, FileSizeUnit.K)]
    [InlineData(1024 * 1024, 1.0, FileSizeUnit.M)]
    [InlineData(1024L * 1024L * 1024L, 1.0, FileSizeUnit.G)]
    [InlineData(1024L * 1024L * 1024L * 1024L, 1.0, FileSizeUnit.T)]
    [InlineData(1024L * 1024L * 1024L * 1024L * 1024L, 1.0, FileSizeUnit.P)]
    [InlineData(1536, 1.5, FileSizeUnit.K)] // 1.5KB
    public void GetOptimalUnit_ShouldReturnCorrectUnitAndValue(long bytes, double expectedValue, FileSizeUnit expectedUnit)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(bytes);
        // Act
        var (value, unit) = fileSize.GetOptimalUnit();
        // Assert
        value.ShouldBe(expectedValue);
        unit.ShouldBe(expectedUnit);
    }
    #endregion
    #region ToString 测试
    /// <summary>
    /// 验证按指定单位和精度输出文件大小。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="unit">文件大小单位。</param>
    /// <param name="precision">保留的小数位数。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(1024, FileSizeUnit.K, 2, "1 KB")]
    [InlineData(1536, FileSizeUnit.K, 1, "1.5 KB")]
    [InlineData(2097152, FileSizeUnit.M, 0, "2 MB")]
    public void ToString_WithSpecificUnit_ShouldReturnFormattedString(long bytes, FileSizeUnit unit, int precision, string expected)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(bytes);
        // Act
        var result = fileSize.ToString(unit, precision);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证文件大小自动选择单位输出文本。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(1, "1 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1024 * 1024, "1 MB")]
    [InlineData(1024L * 1024L * 1024L, "1 GB")]
    [InlineData(1024L * 1024L * 1024L * 1024L, "1 TB")]
    [InlineData(1024L * 1024L * 1024L * 1024L * 1024L, "1 PB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(0, "0 B")]
    public void ToString_ShouldReturnCorrectFormattedString(long bytes, string expected)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(bytes);
        // Act
        var result = fileSize.ToString();
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 运算符重载测试
    /// <summary>
    /// 测试 - 加法运算符 - 正确计算结果
    /// </summary>
    [Fact]
    public void AdditionOperator_ShouldReturnCorrectResult()
    {
        // Arrange
        var size1 = FileSize.FromBytes(1024);
        var size2 = FileSize.FromBytes(512);
        // Act
        var result = size1 + size2;
        // Assert
        result.Size.ShouldBe(1536);
    }
    /// <summary>
    /// 测试 - 减法运算符 - 正确计算结果
    /// </summary>
    [Fact]
    public void SubtractionOperator_ShouldReturnCorrectResult()
    {
        // Arrange
        var size1 = FileSize.FromBytes(1024);
        var size2 = FileSize.FromBytes(512);
        // Act
        var result = size1 - size2;
        // Assert
        result.Size.ShouldBe(512);
    }
    /// <summary>
    /// 测试 - 减法运算符 - 结果为负数应抛出异常
    /// </summary>
    [Fact]
    public void SubtractionOperator_WithNegativeResult_ShouldThrowException()
    {
        // Arrange
        var size1 = FileSize.FromBytes(512);
        var size2 = FileSize.FromBytes(1024);
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => size1 - size2);
    }
    /// <summary>
    /// 验证文件大小按倍数相乘后的字节数。
    /// </summary>
    /// <param name="originalBytes">运算前的字节数。</param>
    /// <param name="multiplier">相乘的倍数。</param>
    /// <param name="expectedBytes">预期的字节数。</param>
    [Theory]
    [InlineData(1024, 2.0, 2048)]
    [InlineData(1024, 0.5, 512)]
    [InlineData(1024, 1.0, 1024)]
    public void MultiplicationOperator_ShouldReturnCorrectResult(long originalBytes, double multiplier, long expectedBytes)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(originalBytes);
        // Act
        var result = fileSize * multiplier;
        // Assert
        result.Size.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - 乘法运算符 - 负数倍数应抛出异常
    /// </summary>
    [Fact]
    public void MultiplicationOperator_WithNegativeMultiplier_ShouldThrowException()
    {
        // Arrange
        var fileSize = FileSize.FromBytes(1024);
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => fileSize * -1.0);
    }
    /// <summary>
    /// 验证文件大小按除数相除后的字节数。
    /// </summary>
    /// <param name="originalBytes">运算前的字节数。</param>
    /// <param name="divisor">相除的除数。</param>
    /// <param name="expectedBytes">预期的字节数。</param>
    [Theory]
    [InlineData(1024, 2.0, 512)]
    [InlineData(1024, 0.5, 2048)]
    [InlineData(1024, 1.0, 1024)]
    public void DivisionOperator_ShouldReturnCorrectResult(long originalBytes, double divisor, long expectedBytes)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(originalBytes);
        // Act
        var result = fileSize / divisor;
        // Assert
        result.Size.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - 除法运算符 - 零除数应抛出异常
    /// </summary>
    [Fact]
    public void DivisionOperator_WithZeroDivisor_ShouldThrowException()
    {
        // Arrange
        var fileSize = FileSize.FromBytes(1024);
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => fileSize / 0.0);
    }
    #endregion
    #region 类型转换测试
    /// <summary>
    /// 验证长整数隐式转换为文件大小后保留字节数。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(long.MaxValue)]
    public void ImplicitConversion_FromLong_ShouldWork(long bytes)
    {
        // Act
        FileSize fileSize = bytes;
        // Assert
        fileSize.Size.ShouldBe(bytes);
    }
    /// <summary>
    /// 验证文件大小显式转换为长整数后保留字节数。
    /// </summary>
    /// <param name="bytes">待处理的字节数。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(long.MaxValue)]
    public void ExplicitConversion_ToLong_ShouldWork(long bytes)
    {
        // Arrange
        var fileSize = FileSize.FromBytes(bytes);
        // Act
        var result = (long)fileSize;
        // Assert
        result.ShouldBe(bytes);
    }
    #endregion
    #region 相等性和比较测试
    /// <summary>
    /// 测试 - Equals - 相同大小应返回true
    /// </summary>
    [Fact]
    public void Equals_WithSameSize_ShouldReturnTrue()
    {
        // Arrange
        var fileSize1 = new FileSize(1024, FileSizeUnit.Byte);
        var fileSize2 = new FileSize(1, FileSizeUnit.K);
        // Act & Assert
        fileSize1.Equals(fileSize2).ShouldBeTrue();
        (fileSize1 == fileSize2).ShouldBeTrue();
        (fileSize1 != fileSize2).ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - Equals - 不同大小应返回false
    /// </summary>
    [Fact]
    public void Equals_WithDifferentSize_ShouldReturnFalse()
    {
        // Arrange
        var fileSize1 = new FileSize(1024, FileSizeUnit.Byte);
        var fileSize2 = new FileSize(2048, FileSizeUnit.Byte);
        // Act & Assert
        fileSize1.Equals(fileSize2).ShouldBeFalse();
        (fileSize1 == fileSize2).ShouldBeFalse();
        (fileSize1 != fileSize2).ShouldBeTrue();
    }
    /// <summary>
    /// 验证文件大小比较结果的符号。
    /// </summary>
    /// <param name="size1">第一个字节大小。</param>
    /// <param name="size2">第二个字节大小。</param>
    /// <param name="expectedSign">预期的比较结果符号。</param>
    [Theory]
    [InlineData(1024, 2048, -1)] // 小于
    [InlineData(2048, 1024, 1)]  // 大于
    [InlineData(1024, 1024, 0)]  // 等于
    public void CompareTo_ShouldReturnCorrectComparisonResult(long size1, long size2, int expectedSign)
    {
        // Arrange
        var fileSize1 = new FileSize(size1, FileSizeUnit.Byte);
        var fileSize2 = new FileSize(size2, FileSizeUnit.Byte);
        // Act
        var result = fileSize1.CompareTo(fileSize2);
        // Assert
        Math.Sign(result).ShouldBe(expectedSign);
    }
    /// <summary>
    /// 测试 - 比较运算符 - 返回正确结果
    /// </summary>
    [Fact]
    public void ComparisonOperators_ShouldReturnCorrectResults()
    {
        // Arrange
        var small = new FileSize(1024, FileSizeUnit.Byte);
        var large = new FileSize(2048, FileSizeUnit.Byte);
        var equal = new FileSize(1024, FileSizeUnit.Byte);
        // Act & Assert
        (small < large).ShouldBeTrue();
        (large > small).ShouldBeTrue();
        (small <= equal).ShouldBeTrue();
        (small >= equal).ShouldBeTrue();
        (small <= large).ShouldBeTrue();
        (large >= small).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - GetHashCode - 相等对象应有相同哈希码
    /// </summary>
    [Fact]
    public void GetHashCode_WithEqualObjects_ShouldReturnSameHashCode()
    {
        // Arrange
        var fileSize1 = new FileSize(1024, FileSizeUnit.Byte);
        var fileSize2 = new FileSize(1, FileSizeUnit.K);
        // Act & Assert
        fileSize1.GetHashCode().ShouldBe(fileSize2.GetHashCode());
    }
    #endregion
    #region 边界值测试
    /// <summary>
    /// 测试 - 边界值 - 零值处理
    /// </summary>
    [Fact]
    public void BoundaryValue_Zero_ShouldHandleCorrectly()
    {
        // Arrange
        var fileSize = new FileSize(0, FileSizeUnit.Byte);
        // Act & Assert
        fileSize.Size.ShouldBe(0);
        fileSize.GetSizeByK().ShouldBe(0);
        fileSize.GetSizeByM().ShouldBe(0);
        fileSize.ToString().ShouldBe("0 B");
    }
    /// <summary>
    /// 测试 - 边界值 - 最大值处理
    /// </summary>
    [Fact]
    public void BoundaryValue_MaxValue_ShouldHandleCorrectly()
    {
        // Arrange
        var fileSize = new FileSize(long.MaxValue, FileSizeUnit.Byte);
        // Act & Assert
        fileSize.Size.ShouldBe(long.MaxValue);
        fileSize.GetLongSize().ShouldBe(long.MaxValue);
    }
    #endregion
}

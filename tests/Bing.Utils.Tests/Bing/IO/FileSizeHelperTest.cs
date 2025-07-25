namespace Bing.IO;

/// <summary>
/// 文件大小辅助工具类 测试
/// </summary>
public class FileSizeHelperTest
{
    #region 最佳单位选择测试

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
    [InlineData(0L, FileSizeUnit.Byte)]
    public void GetBestUnit_ValidBytes_ReturnsCorrectUnit(long bytes, FileSizeUnit expected)
    {
        // Act
        var result = FileSizeHelper.GetBestUnit(bytes);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetBestUnit - 边界值测试
    /// </summary>
    [Theory]
    [InlineData(1023L, FileSizeUnit.Byte)]    // 小于1KB
    [InlineData(1025L, FileSizeUnit.K)]       // 稍大于1KB
    [InlineData(1048575L, FileSizeUnit.K)]    // 小于1MB
    [InlineData(1048577L, FileSizeUnit.M)]    // 稍大于1MB
    public void GetBestUnit_BoundaryValues_ReturnsCorrectUnit(long bytes, FileSizeUnit expected)
    {
        // Act
        var result = FileSizeHelper.GetBestUnit(bytes);

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
        Should.Throw<ArgumentOutOfRangeException>(() => FileSizeHelper.GetBestUnit(-1))
            .ParamName.ShouldBe("bytes");
    }

    #endregion

    #region 格式化测试

    /// <summary>
    /// 测试 - AutoFormat - 自动格式化
    /// </summary>
    [Theory]
    [InlineData(0L, 2, "0 B")]
    [InlineData(512L, 2, "512 B")]
    [InlineData(1536L, 2, "1.5 KB")]
    [InlineData(2097152L, 1, "2 MB")]
    [InlineData(1024L, 0, "1 KB")]
    [InlineData(1125899906842624L, 2, "1 PB")]
    public void AutoFormat_ValidBytes_ReturnsFormattedString(long bytes, int precision, string expected)
    {
        // Act
        var result = FileSizeHelper.AutoFormat(bytes, precision);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - AutoFormat - 复杂边界值
    /// </summary>
    [Theory]
    [InlineData(1023L, 0, "1023 B")]
    [InlineData(1025L, 1, "1 KB")]
    [InlineData(1536L, 1, "1.5 KB")]
    [InlineData(1572864L, 2, "1.5 MB")]  // 1.5 * 1024 * 1024
    public void AutoFormat_ComplexBoundaryValues_ReturnsCorrectFormat(long bytes, int precision, string expected)
    {
        // Act
        var result = FileSizeHelper.AutoFormat(bytes, precision);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region 解析测试

    /// <summary>
    /// 测试 - TryParseSize - 解析文件大小字符串
    /// </summary>
    [Theory]
    [InlineData("1.5 KB", true, 1536L)]
    [InlineData("2 MB", true, 2097152L)]
    [InlineData("1 GB", true, 1073741824L)]
    [InlineData("500 B", true, 500L)]
    [InlineData("1.25 TB", true, 1374389534720L)]
    [InlineData("invalid", false, 0L)]
    [InlineData("", false, 0L)]
    [InlineData("1.5", false, 0L)]
    [InlineData("1.5 XX", false, 0L)]
    [InlineData("   2.5 GB   ", true, 2684354560L)] // 测试空格处理
    public void TryParseSize_WithVariousInputs_ShouldReturnExpectedResults(string input, bool expectedSuccess, long expectedBytes)
    {
        // Act
        var success = FileSizeHelper.TryParseSize(input, out var bytes);

        // Assert
        success.ShouldBe(expectedSuccess);
        bytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - TryParseSize - 大小写不敏感
    /// </summary>
    [Theory]
    [InlineData("1.5 kb", true, 1536L)]
    [InlineData("2 mb", true, 2097152L)]
    [InlineData("1 gb", true, 1073741824L)]
    [InlineData("1.5 KB", true, 1536L)]
    [InlineData("2 MB", true, 2097152L)]
    [InlineData("1 Gb", true, 1073741824L)]
    public void TryParseSize_CaseInsensitive_ShouldParseCorrectly(string input, bool expectedSuccess, long expectedBytes)
    {
        // Act
        var success = FileSizeHelper.TryParseSize(input, out var bytes);

        // Assert
        success.ShouldBe(expectedSuccess);
        bytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - TryParseSize - 无效格式
    /// </summary>
    [Theory]
    [InlineData("1.5 KB MB")]  // 多个单位
    [InlineData("abc KB")]     // 无效数值
    [InlineData("1.5 XYZ")]    // 无效单位
    [InlineData(null)]         // null
    [InlineData("   ")]        // 只有空格
    [InlineData("KB 1.5")]     // 顺序错误
    [InlineData("1.5.2 KB")]   // 无效数值格式
    [InlineData("-1.5 KB")]    // 负数（这个可能能解析，但在ConvertToBytes时会抛出异常）
    public void TryParseSize_InvalidFormats_ShouldReturnFalse(string input)
    {
        // Act
        var success = FileSizeHelper.TryParseSize(input, out var bytes);

        // Assert
        success.ShouldBeFalse();
        bytes.ShouldBe(0L);
    }

    /// <summary>
    /// 测试 - TryParseSize - 多个空格应该能正常解析
    /// </summary>
    [Theory]
    [InlineData("1.5KB", true, 1536L)]        // 无空格
    [InlineData("1.5 KB", true, 1536L)]       // 单个空格
    [InlineData("1.5  KB", true, 1536L)]      // 多个空格
    [InlineData("  1.5   KB  ", true, 1536L)] // 前后及中间多个空格
    [InlineData("\t1.5\t\tKB\t", true, 1536L)] // 制表符
    public void TryParseSize_MultipleSpaces_ShouldParseCorrectly(string input, bool expectedSuccess, long expectedBytes)
    {
        // Act
        var success = FileSizeHelper.TryParseSize(input, out var bytes);

        // Assert
        success.ShouldBe(expectedSuccess);
        bytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - TryParseSize - 负数处理
    /// </summary>
    [Theory]
    [InlineData("-1.5 KB")]    // 负数（正则可能匹配，但ConvertToBytes会失败）
    [InlineData("+1.5 KB")]    // 正号（正则不支持）
    public void TryParseSize_SignedNumbers_ShouldReturnFalse(string input)
    {
        // Act
        var success = FileSizeHelper.TryParseSize(input, out var bytes);

        // Assert
        success.ShouldBeFalse();
        bytes.ShouldBe(0L);
    }

    /// <summary>
    /// 测试 - TryParseSize - 边界数值格式
    /// </summary>
    [Theory]
    [InlineData("0 KB", true, 0L)]           // 零值
    [InlineData("0.0 KB", true, 0L)]         // 零值小数
    [InlineData("1000000 KB", true, 1024000000L)] // 大数值
    [InlineData("1.00000 KB", true, 1024L)]  // 多位小数
    [InlineData(".5 KB")]                    // 缺少整数部分
    [InlineData("1. KB")]                    // 缺少小数部分
    [InlineData("1.5e2 KB")]                 // 科学记数法
    public void TryParseSize_BoundaryNumberFormats_ShouldHandleCorrectly(string input, bool expectedSuccess = false, long expectedBytes = 0L)
    {
        // Act
        var success = FileSizeHelper.TryParseSize(input, out var bytes);

        // Assert
        success.ShouldBe(expectedSuccess);
        bytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - ParseSize - 解析有效字符串
    /// </summary>
    [Theory]
    [InlineData("1.5 KB", 1536L)]
    [InlineData("2 MB", 2097152L)]
    [InlineData("1 GB", 1073741824L)]
    public void ParseSize_WithValidString_ShouldReturnCorrectBytes(string sizeString, long expected)
    {
        // Act
        var result = FileSizeHelper.ParseSize(sizeString);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ParseSize - 无效字符串应抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("1.5 XX")]
    public void ParseSize_WithInvalidString_ShouldThrowArgumentException(string invalidString)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => FileSizeHelper.ParseSize(invalidString))
            .ParamName.ShouldBe("sizeString");
    }

    /// <summary>
    /// 测试 - ParseSize - 异常消息包含原始字符串
    /// </summary>
    [Fact]
    public void ParseSize_WithInvalidString_ShouldIncludeOriginalStringInMessage()
    {
        // Arrange
        const string invalidString = "invalid input";

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => FileSizeHelper.ParseSize(invalidString));
        exception.Message.ShouldContain(invalidString);
    }

    #endregion

    #region 工具方法测试

    /// <summary>
    /// 测试 - GetAllUnitDescriptions - 返回所有单位描述
    /// </summary>
    [Fact]
    public void GetAllUnitDescriptions_ShouldReturnAllUnits()
    {
        // Act
        var descriptions = FileSizeHelper.GetAllUnitDescriptions();

        // Assert
        descriptions.ShouldNotBeEmpty();
        descriptions.ShouldContain("B");
        descriptions.ShouldContain("KB");
        descriptions.ShouldContain("MB");
        descriptions.ShouldContain("GB");
        descriptions.ShouldContain("TB");
        descriptions.ShouldContain("PB");
        descriptions.Length.ShouldBe(6);
    }

    /// <summary>
    /// 测试 - IsValidUnitString - 验证单位字符串
    /// </summary>
    [Theory]
    [InlineData("B", true)]
    [InlineData("KB", true)]
    [InlineData("mb", true)]      // 测试大小写不敏感
    [InlineData("GB", true)]
    [InlineData("XX", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  KB  ", true)]  // 测试空格处理
    public void IsValidUnitString_WithVariousInputs_ShouldReturnExpectedResults(string unitString, bool expected)
    {
        // Act
        var result = FileSizeHelper.IsValidUnitString(unitString);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - TryGetUnitFromString - 从字符串获取枚举值
    /// </summary>
    [Theory]
    [InlineData("B", true, FileSizeUnit.Byte)]
    [InlineData("KB", true, FileSizeUnit.K)]
    [InlineData("mb", true, FileSizeUnit.M)]     // 测试大小写不敏感
    [InlineData("GB", true, FileSizeUnit.G)]
    [InlineData("XX", false, FileSizeUnit.Byte)]
    [InlineData("", false, FileSizeUnit.Byte)]
    [InlineData("  TB  ", true, FileSizeUnit.T)] // 测试空格处理
    public void TryGetUnitFromString_WithVariousInputs_ShouldReturnExpectedResults(string unitString, bool expectedSuccess, FileSizeUnit expectedUnit)
    {
        // Act
        var success = FileSizeHelper.TryGetUnitFromString(unitString, out var unit);

        // Assert
        success.ShouldBe(expectedSuccess);
        if (expectedSuccess)
        {
            unit.ShouldBe(expectedUnit);
        }
    }

    /// <summary>
    /// 测试 - TryGetUnitFromString - null输入处理
    /// </summary>
    [Fact]
    public void TryGetUnitFromString_WithNullInput_ShouldReturnFalse()
    {
        // Act
        var success = FileSizeHelper.TryGetUnitFromString(null, out var unit);

        // Assert
        success.ShouldBeFalse();
        unit.ShouldBe(default(FileSizeUnit));
    }

    #endregion

    #region 兼容性和集成测试

    /// <summary>
    /// 测试 - 精度处理
    /// </summary>
    [Theory]
    [InlineData(1536L, 0, "2 KB")]
    [InlineData(1536L, 1, "1.5 KB")]
    [InlineData(1536L, 3, "1.5 KB")]    // 自动移除尾随零
    [InlineData(1000L, 2, "1000 B")]    // 小于阈值，保持字节单位
    public void Precision_HandledCorrectly(long bytes, int precision, string expected)
    {
        // Act
        var result = FileSizeHelper.AutoFormat(bytes, precision);

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
        var bestUnit = FileSizeHelper.GetBestUnit(sizeInBytes);
        var formatted = FileSizeHelper.AutoFormat(sizeInBytes);

        // Assert
        sizeInBytes.ShouldBe(1572864L); // 1536 * 1024
        bestUnit.ShouldBe(FileSizeUnit.M);
        formatted.ShouldBe("1.5 MB");
    }

    /// <summary>
    /// 测试 - 解析和格式化往返一致性
    /// </summary>
    [Theory]
    [InlineData("1.5 KB")]
    [InlineData("2 MB")]
    [InlineData("1.25 GB")]
    [InlineData("500 B")]
    public void ParseFormat_RoundTripConsistency(string originalString)
    {
        // Act
        var parsed = FileSizeHelper.TryParseSize(originalString, out var bytes);
        var formatted = FileSizeHelper.AutoFormat(bytes, 2);

        // Assert
        parsed.ShouldBeTrue();
        // 注意：格式化可能会标准化字符串格式，所以我们检查数值是否一致
        FileSizeHelper.TryParseSize(formatted, out var formattedBytes).ShouldBeTrue();
        formattedBytes.ShouldBe(bytes);
    }

    #endregion

    #region 性能和边界测试

    /// <summary>
    /// 测试 - 大数值处理
    /// </summary>
    [Theory]
    [InlineData(long.MaxValue, FileSizeUnit.P)]
    [InlineData(long.MaxValue / 2, FileSizeUnit.P)]
    [InlineData(1L, FileSizeUnit.Byte)]
    public void LargeValues_ShouldHandleCorrectly(long bytes, FileSizeUnit expectedUnit)
    {
        // Act
        var bestUnit = FileSizeHelper.GetBestUnit(bytes);

        // Assert
        bestUnit.ShouldBe(expectedUnit);
    }

    /// <summary>
    /// 测试 - 零值处理的一致性
    /// </summary>
    [Fact]
    public void ZeroValues_ShouldHandleConsistently()
    {
        // Act
        var bestUnit = FileSizeHelper.GetBestUnit(0);
        var formatted = FileSizeHelper.AutoFormat(0, 2);
        var converted = FileSizeUnit.K.ConvertFromBytes(0);

        // Assert
        bestUnit.ShouldBe(FileSizeUnit.Byte);
        formatted.ShouldBe("0 B");
        converted.ShouldBe(0.0);
    }

    /// <summary>
    /// 测试 - 国际化数值格式兼容性
    /// </summary>
    [Theory]
    [InlineData("1.5 KB")]    // 英文小数点
    [InlineData("1,5 KB")]    // 某些地区使用逗号作为小数分隔符，但我们的实现应该只支持点号
    public void Internationalization_NumberFormat(string input)
    {
        // Act
        var success = FileSizeHelper.TryParseSize(input, out var bytes);

        // Assert
        if (input.Contains('.'))
        {
            success.ShouldBeTrue();
            bytes.ShouldBe(1536L);
        }
        else
        {
            // 逗号作为小数分隔符应该解析失败（因为我们使用InvariantCulture）
            success.ShouldBeFalse();
        }
    }

    #endregion
}
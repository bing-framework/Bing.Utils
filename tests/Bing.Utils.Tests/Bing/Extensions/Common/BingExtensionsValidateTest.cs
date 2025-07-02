namespace Bing.Extensions.Common;

/// <summary>
/// 系统扩展 - 验证扩展测试
/// </summary>
public class BingExtensionsValidateTest
{
    #region IsEmpty

    #region IsEmpty(bool)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 布尔值为 false 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Bool_WithFalse_ReturnsTrue()
    {
        // Arrange
        bool value = false;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 布尔值为 true 时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_Bool_WithTrue_ReturnsFalse()
    {
        // Arrange
        bool value = true;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(bool?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空布尔值为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableBool_WithNull_ReturnsTrue()
    {
        // Arrange
        bool? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空布尔值为 false 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableBool_WithFalse_ReturnsTrue()
    {
        // Arrange
        bool? value = false;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空布尔值为 true 时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableBool_WithTrue_ReturnsFalse()
    {
        // Arrange
        bool? value = true;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(int)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 整数值为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Int_WithZero_ReturnsTrue()
    {
        // Arrange
        int value = 0;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 整数值不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1)]        // 测试正整数
    [InlineData(-1)]       // 测试负整数
    [InlineData(int.MaxValue)]  // 测试最大整数值
    [InlineData(int.MinValue)]  // 测试最小整数值
    public void IsEmpty_Int_WithNonZero_ReturnsFalse(int value)
    {
        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(int?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空整数值为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableInt_WithNull_ReturnsTrue()
    {
        // Arrange
        int? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空整数值为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableInt_WithZero_ReturnsTrue()
    {
        // Arrange
        int? value = 0;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空整数值不为 null 且不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void IsEmpty_NullableInt_WithNonZero_ReturnsFalse(int input)
    {
        // Arrange
        int? value = input;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(long)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 长整数值为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Long_WithZero_ReturnsTrue()
    {
        // Arrange
        long value = 0L;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 长整数值不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void IsEmpty_Long_WithNonZero_ReturnsFalse(long value)
    {
        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(long?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空长整数值为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableLong_WithNull_ReturnsTrue()
    {
        // Arrange
        long? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空长整数值为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableLong_WithZero_ReturnsTrue()
    {
        // Arrange
        long? value = 0L;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空长整数值不为 null 且不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void IsEmpty_NullableLong_WithNonZero_ReturnsFalse(long input)
    {
        // Arrange
        long? value = input;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(float)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 单精度浮点数为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Float_WithZero_ReturnsTrue()
    {
        // Arrange
        float value = 0f;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 单精度浮点数不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1.0f)]     // 测试正单精度浮点数
    [InlineData(-1.0f)]    // 测试负单精度浮点数
    [InlineData(0.1f)]     // 测试小于 1 的正单精度浮点数
    [InlineData(-0.1f)]    // 测试小于 1 的负单精度浮点数
    [InlineData(float.MaxValue)] // 测试最大单精度浮点数值
    [InlineData(float.MinValue)] // 测试最小单精度浮点数值
    [InlineData(float.Epsilon)]  // 测试最小正单精度浮点数
    [InlineData(-float.Epsilon)] // 测试最小负单精度浮点数
    public void IsEmpty_Float_WithNonZero_ReturnsFalse(float value)
    {
        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(float?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空单精度浮点数为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableFloat_WithNull_ReturnsTrue()
    {
        // Arrange
        float? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空单精度浮点数为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableFloat_WithZero_ReturnsTrue()
    {
        // Arrange
        float? value = 0f;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空单精度浮点数不为 null 且不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(0.1f)]
    [InlineData(-0.1f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    [InlineData(float.Epsilon)]
    [InlineData(-float.Epsilon)]
    public void IsEmpty_NullableFloat_WithNonZero_ReturnsFalse(float input)
    {
        // Arrange
        float? value = input;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(double)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 双精度浮点数为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Double_WithZero_ReturnsTrue()
    {
        // Arrange
        double value = 0d;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 双精度浮点数不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.1)]
    [InlineData(-0.1)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.Epsilon)]
    [InlineData(-double.Epsilon)]
    public void IsEmpty_Double_WithNonZero_ReturnsFalse(double value)
    {
        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(double?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空双精度浮点数为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDouble_WithNull_ReturnsTrue()
    {
        // Arrange
        double? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空双精度浮点数为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDouble_WithZero_ReturnsTrue()
    {
        // Arrange
        double? value = 0d;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空双精度浮点数不为 null 且不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.1)]
    [InlineData(-0.1)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.Epsilon)]
    [InlineData(-double.Epsilon)]
    public void IsEmpty_NullableDouble_WithNonZero_ReturnsFalse(double input)
    {
        // Arrange
        double? value = input;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(decimal)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 十进制数为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Decimal_WithZero_ReturnsTrue()
    {
        // Arrange
        decimal value = 0m;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 十进制数不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.1)]
    [InlineData(-0.1)]
    public void IsEmpty_Decimal_WithNonZero_ReturnsFalse(double input)
    {
        // Arrange - Convert double to decimal for test data
        decimal value = (decimal)input;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 十进制数为最大值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_Decimal_WithMaxValue_ReturnsFalse()
    {
        // Arrange
        decimal value = decimal.MaxValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 十进制数为最小值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_Decimal_WithMinValue_ReturnsFalse()
    {
        // Arrange
        decimal value = decimal.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(decimal?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空十进制数为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDecimal_WithNull_ReturnsTrue()
    {
        // Arrange
        decimal? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空十进制数为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDecimal_WithZero_ReturnsTrue()
    {
        // Arrange
        decimal? value = 0m;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空十进制数不为 null 且不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.1)]
    [InlineData(-0.1)]
    public void IsEmpty_NullableDecimal_WithNonZero_ReturnsFalse(double input)
    {
        // Arrange - Convert double to decimal for test data
        decimal? value = (decimal)input;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空十进制数为最大值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDecimal_WithMaxValue_ReturnsFalse()
    {
        // Arrange
        decimal? value = decimal.MaxValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空十进制数为最小值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDecimal_WithMinValue_ReturnsFalse()
    {
        // Arrange
        decimal? value = decimal.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(string)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 字符串为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_String_WithNullString_ReturnsTrue()
    {
        // Arrange
        string value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 字符串为空字符串时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_String_WithEmptyString_ReturnsTrue()
    {
        // Arrange
        string value = string.Empty;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 字符串仅包含空白字符时应返回 true
    /// </summary>
    [Theory]
    [InlineData(" ")]              // 空格
    [InlineData("   ")]            // 多个空格
    [InlineData("\t")]             // 制表符
    [InlineData("\n")]             // 换行符
    [InlineData("\r")]             // 回车符
    [InlineData(" \t\n\r")]        // 混合空白字符
    [InlineData("\u2000")]         // 不间断空格
    [InlineData("\u3000")]         // 全角空格
    public void IsEmpty_String_WithWhitespaceString_ReturnsTrue(string value)
    {
        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 字符串包含非空白字符时应返回 false
    /// </summary>
    [Theory]
    [InlineData("a")]              // 单个字符
    [InlineData(" a")]             // 前导空格
    [InlineData("a ")]             // 尾随空格
    [InlineData(" a ")]            // 前后空格
    [InlineData("Hello World")]    // 英文字符串
    [InlineData(" Hello World ")]  // 带空格的英文字符串
    [InlineData("你好世界")]        // 中文字符串
    [InlineData("123")]            // 数字字符串
    [InlineData("!@#")]            // 特殊字符字符串
    public void IsEmpty_String_WithNonEmptyString_ReturnsFalse(string value)
    {
        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 字符串包含空白字符和非空白字符时应返回 false
    /// </summary>
    [Theory]
    [InlineData("a b c")]          // 含空格的字符串
    [InlineData("a\tb\nc")]        // 含制表符和换行符的字符串
    public void IsEmpty_String_WithStringContainingWhitespace_ReturnsFalse(string value)
    {
        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 字符串包含零宽空格时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_String_WithZeroWidthSpace_ReturnsFalse()
    {
        // Arrange - 零宽空格 (U+200B)
        string value = "\u200B";

        // Act
        bool result = value.IsEmpty();

        // Assert
        // 零宽空格不是空白字符，而是不可见的字符
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 性能测试，确保方法执行速度足够快
    /// </summary>
    [Fact]
    public void IsEmpty_String_PerformanceCheck_ShouldBeOptimized()
    {
        // Arrange
        const int iterations = 100000;
        string testValue = "   ";

        // Act
        var startTime = DateTime.Now;
        for (int i = 0; i < iterations; i++)
        {
            bool _ = testValue.IsEmpty();
        }
        var endTime = DateTime.Now;

        // Assert
        // 这个测试仅检查方法的执行时间，通常应该很快完成
        // 在大多数情况下应该少于几毫秒，具体取决于硬件
        Assert.True((endTime - startTime).TotalMilliseconds < 500);
    }

    #endregion

    #region IsEmpty(DateTime)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 日期时间为默认值时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_DateTime_WithDefaultValue_ReturnsTrue()
    {
        // Arrange
        DateTime value = default;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 日期时间为最小值时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_DateTime_WithMinValue_ReturnsTrue()
    {
        // Arrange
        DateTime value = DateTime.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 -- IsEmpty 方法 - 日期时间为非空值时应返回 false
    /// </summary>
    [Theory]
    [InlineData(2023, 1, 1)]       // 2023年1月1日
    [InlineData(2000, 12, 31)]     // 2000年12月31日
    [InlineData(9999, 12, 31)]     // 9999年12月31日（接近最大值）
    public void IsEmpty_DateTime_WithNonEmptyValue_ReturnsFalse(int year, int month, int day)
    {
        // Arrange
        DateTime value = new DateTime(year, month, day);

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 日期时间为当前时间时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_DateTime_WithNow_ReturnsFalse()
    {
        // Arrange
        DateTime value = DateTime.Now;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(DateTime?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDateTime_WithNull_ReturnsTrue()
    {
        // Arrange
        DateTime? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间为最小值时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDateTime_WithMinValue_ReturnsTrue()
    {
        // Arrange
        DateTime? value = DateTime.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间为非空值时应返回 false
    /// </summary>
    [Theory]
    [InlineData(2023, 1, 1)]
    [InlineData(2000, 12, 31)]
    [InlineData(9999, 12, 31)]
    public void IsEmpty_NullableDateTime_WithNonEmptyValue_ReturnsFalse(int year, int month, int day)
    {
        // Arrange
        DateTime? value = new DateTime(year, month, day);

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间为当前时间时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDateTime_WithNow_ReturnsFalse()
    {
        // Arrange
        DateTime? value = DateTime.Now;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(DateTimeOffset)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 日期时间偏移量为默认值时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_DateTimeOffset_WithDefaultValue_ReturnsTrue()
    {
        // Arrange
        DateTimeOffset value = default;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 日期时间偏移量为最小值时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_DateTimeOffset_WithMinValue_ReturnsTrue()
    {
        // Arrange
        DateTimeOffset value = DateTimeOffset.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 日期时间偏移量为非空值时应返回 false
    /// </summary>
    [Theory]
    [InlineData(2023, 1, 1)]
    [InlineData(2000, 12, 31)]
    [InlineData(9999, 12, 31)]
    public void IsEmpty_DateTimeOffset_WithNonEmptyValue_ReturnsFalse(int year, int month, int day)
    {
        // Arrange
        DateTimeOffset value = new DateTimeOffset(new DateTime(year, month, day));

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 日期时间偏移量为当前时间时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_DateTimeOffset_WithNow_ReturnsFalse()
    {
        // Arrange
        DateTimeOffset value = DateTimeOffset.Now;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(DateTimeOffset?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间偏移量为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDateTimeOffset_WithNull_ReturnsTrue()
    {
        // Arrange
        DateTimeOffset? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间偏移量为最小值时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDateTimeOffset_WithMinValue_ReturnsTrue()
    {
        // Arrange
        DateTimeOffset? value = DateTimeOffset.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间偏移量为非空值时应返回 false
    /// </summary>
    [Theory]
    [InlineData(2023, 1, 1)]
    [InlineData(2000, 12, 31)]
    [InlineData(9999, 12, 31)]
    public void IsEmpty_NullableDateTimeOffset_WithNonEmptyValue_ReturnsFalse(int year, int month, int day)
    {
        // Arrange
        DateTimeOffset? value = new DateTimeOffset(new DateTime(year, month, day));

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空日期时间偏移量为当前时间时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableDateTimeOffset_WithNow_ReturnsFalse()
    {
        // Arrange
        DateTimeOffset? value = DateTimeOffset.Now;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(TimeSpan)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 时间间隔为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_TimeSpan_WithZero_ReturnsTrue()
    {
        // Arrange
        TimeSpan value = TimeSpan.Zero;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 时间间隔不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1)]                // 1毫秒
    [InlineData(-1)]               // -1毫秒
    [InlineData(86400000)]         // 1天（毫秒表示）
    [InlineData(-86400000)]        // -1天（毫秒表示）
    public void IsEmpty_TimeSpan_WithNonZero_ReturnsFalse(double milliseconds)
    {
        // Arrange
        TimeSpan value = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 时间间隔为最大值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_TimeSpan_WithMaxValue_ReturnsFalse()
    {
        // Arrange
        TimeSpan value = TimeSpan.MaxValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 时间间隔为最小值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_TimeSpan_WithMinValue_ReturnsFalse()
    {
        // Arrange
        TimeSpan value = TimeSpan.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(TimeSpan?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空时间间隔为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableTimeSpan_WithNull_ReturnsTrue()
    {
        // Arrange
        TimeSpan? value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空时间间隔为零时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableTimeSpan_WithZero_ReturnsTrue()
    {
        // Arrange
        TimeSpan? value = TimeSpan.Zero;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空时间间隔不为 null 且不为零时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1)]                // 1毫秒
    [InlineData(-1)]               // -1毫秒
    [InlineData(86400000)]         // 1天（毫秒表示）
    [InlineData(-86400000)]        // -1天（毫秒表示）
    public void IsEmpty_NullableTimeSpan_WithNonZero_ReturnsFalse(double milliseconds)
    {
        // Arrange
        TimeSpan? value = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空时间间隔为最大值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableTimeSpan_WithMaxValue_ReturnsFalse()
    {
        // Arrange
        TimeSpan? value = TimeSpan.MaxValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 可空时间间隔为最小值时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableTimeSpan_WithMinValue_ReturnsFalse()
    {
        // Arrange
        TimeSpan? value = TimeSpan.MinValue;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(Guid)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 空 GUID 应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Guid_WithEmpty_ReturnsTrue()
    {
        // Arrange - 准备一个空 GUID
        Guid value = Guid.Empty;

        // Act - 调用 IsEmpty 方法
        bool result = value.IsEmpty();

        // Assert - 验证结果为 true
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 非空 GUID 应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_Guid_WithNonEmpty_ReturnsFalse()
    {
        // Arrange - 准备一个非空 GUID
        Guid value = Guid.NewGuid();

        // Act - 调用 IsEmpty 方法
        bool result = value.IsEmpty();

        // Assert - 验证结果为 false
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 从字符串构造的 GUID 应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_Guid_WithParsedValue_ReturnsFalse()
    {
        // Arrange - 准备一个从字符串解析的 GUID
        Guid value = new Guid("12345678-1234-1234-1234-123456789012");

        // Act - 调用 IsEmpty 方法
        bool result = value.IsEmpty();

        // Assert - 验证结果为 false
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(Guid?)

    /// <summary>
    /// 测试 - IsEmpty 方法 - null 可空 GUID 应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableGuid_WithNull_ReturnsTrue()
    {
        // Arrange - 准备一个 null 值的可空 GUID
        Guid? value = null;

        // Act - 调用 IsEmpty 方法
        bool result = value.IsEmpty();

        // Assert - 验证结果为 true
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 空可空 GUID 应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NullableGuid_WithEmpty_ReturnsTrue()
    {
        // Arrange - 准备一个空值的可空 GUID
        Guid? value = Guid.Empty;

        // Act - 调用 IsEmpty 方法
        bool result = value.IsEmpty();

        // Assert - 验证结果为 true
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 非空可空 GUID 应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NullableGuid_WithNonEmpty_ReturnsFalse()
    {
        // Arrange - 准备一个非空的可空 GUID
        Guid? value = Guid.NewGuid();

        // Act - 调用 IsEmpty 方法
        bool result = value.IsEmpty();

        // Assert - 验证结果为 false
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(StringBuilder)

    /// <summary>
    /// 测试 - IsEmpty 方法 - StringBuilder 为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_StringBuilder_WithNull_ReturnsTrue()
    {
        // Arrange
        StringBuilder value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - StringBuilder 为空时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_StringBuilder_WithEmpty_ReturnsTrue()
    {
        // Arrange
        StringBuilder value = new StringBuilder();

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - StringBuilder 仅包含空白字符时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_StringBuilder_WithWhiteSpace_ReturnsTrue()
    {
        // Arrange
        StringBuilder value = new StringBuilder("   ");

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - StringBuilder 包含内容时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_StringBuilder_WithContent_ReturnsFalse()
    {
        // Arrange
        StringBuilder value = new StringBuilder("Hello");

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(IEnumerable)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 集合为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_WithNullCollection_ReturnsTrue()
    {
        // Arrange
        IEnumerable<int> collection = null;

        // Act
        bool result = collection.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 空集合应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_WithEmptyCollection_ReturnsTrue()
    {
        // Arrange
        var collection = new List<int>();

        // Act
        bool result = collection.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 空数组应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_WithEmptyArray_ReturnsTrue()
    {
        // Arrange
        var array = Array.Empty<string>();

        // Act
        bool result = array.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 IsEmpty 方法 - 延迟执行的LINQ查询返回空结果时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_WithEmptyLinqQuery_ReturnsTrue()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var query = collection.Where(x => x > 10);

        // Act
        bool result = query.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 IsEmpty 方法 - 延迟执行的LINQ查询返回非空结果时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_WithNonEmptyLinqQuery_ReturnsFalse()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var query = collection.Where(x => x > 0);

        // Act
        bool result = query.IsEmpty();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 IsEmpty 方法 - 使用自定义迭代器时应正确判断
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_WithCustomIterator_WorksCorrectly()
    {
        // Arrange
        var emptyIterator = GetEmptyIterator();
        var nonEmptyIterator = GetNonEmptyIterator();

        // Act & Assert
        Assert.True(emptyIterator.IsEmpty());
        Assert.False(nonEmptyIterator.IsEmpty());
    }

    /// <summary>
    /// 测试 IsEmpty 方法 - 与 NotEmpty 方法结果应相反
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_ComparedWithNotEmpty_ShouldBeInversed()
    {
        // Arrange
        IEnumerable<int>[] collections = new IEnumerable<int>[]
        {
            null,
            new List<int>(),
            new List<int> { 1, 2, 3 }
        };

        // Act & Assert
        foreach (var collection in collections)
        {
            bool isEmpty = collection.IsEmpty();
            bool notEmpty = collection.NotEmpty();
            Assert.Equal(!isEmpty, notEmpty);
        }
    }

    /// <summary>
    /// 测试 IsEmpty 方法 - 性能检测
    /// </summary>
    [Fact]
    public void IsEmpty_IEnumerable_PerformanceCheck()
    {
        // Arrange
        var largeCollection = Enumerable.Range(1, 10000).ToList();
        var emptyCollection = new List<int>();
        const int iterations = 10000;

        // Act - 测试空集合性能
        var startEmpty = DateTime.Now;
        for (int i = 0; i < iterations; i++)
        {
            bool _ = emptyCollection.IsEmpty();
        }
        var emptyTime = (DateTime.Now - startEmpty).TotalMilliseconds;

        // Act - 测试非空大集合性能
        var startNonEmpty = DateTime.Now;
        for (int i = 0; i < iterations; i++)
        {
            bool _ = largeCollection.IsEmpty();
        }
        var nonEmptyTime = (DateTime.Now - startNonEmpty).TotalMilliseconds;

        // Assert - 确保性能在可接受范围内
        Assert.True(emptyTime < 500); // 空集合检查应该很快
        Assert.True(nonEmptyTime < 500); // 即使大集合也应该很快，因为只需要检查第一个元素
    }

    /// <summary>
    /// 获取空的自定义迭代器
    /// </summary>
    private IEnumerable<int> GetEmptyIterator()
    {
        yield break;
    }

    /// <summary>
    /// 获取非空的自定义迭代器
    /// </summary>
    private IEnumerable<int> GetNonEmptyIterator()
    {
        yield return 1;
    }

    #endregion

    #region IsEmpty(IDictionary<TKey, TValue>)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 泛型字典为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Dictionary_WithNull_ReturnsTrue()
    {
        // Arrange
        IDictionary<string, int> value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 泛型字典为空时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_Dictionary_WithEmpty_ReturnsTrue()
    {
        // Arrange
        IDictionary<string, int> value = new Dictionary<string, int>();

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 泛型字典包含元素时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_Dictionary_WithItems_ReturnsFalse()
    {
        // Arrange
        IDictionary<string, int> value = new Dictionary<string, int> { { "key", 1 } };

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsEmpty(IDictionary)

    /// <summary>
    /// 测试 - IsEmpty 方法 - 非泛型字典为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NonGenericDictionary_WithNull_ReturnsTrue()
    {
        // Arrange
        IDictionary value = null;

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 非泛型字典为空时应返回 true
    /// </summary>
    [Fact]
    public void IsEmpty_NonGenericDictionary_WithEmpty_ReturnsTrue()
    {
        // Arrange
        IDictionary value = new Hashtable();

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsEmpty 方法 - 非泛型字典包含元素时应返回 false
    /// </summary>
    [Fact]
    public void IsEmpty_NonGenericDictionary_WithItems_ReturnsFalse()
    {
        // Arrange
        IDictionary value = new Hashtable { { "key", 1 } };

        // Act
        bool result = value.IsEmpty();

        // Assert
        Assert.False(result);
    }

    #endregion

    #endregion

    #region IsDefault

    /// <summary>
    /// 测试 - IsDefault 方法 - 引用类型值为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsDefault_ReferenceType_WithNull_ReturnsTrue()
    {
        // Arrange
        string value = null;

        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 引用类型值不为 null 时应返回 false
    /// </summary>
    [Fact]
    public void IsDefault_ReferenceType_WithNonNull_ReturnsFalse()
    {
        // Arrange
        string value = "test";

        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 值类型为默认值时应返回 true
    /// </summary>
    [Theory]
    [InlineData(0)]      // int 的默认值
    [InlineData(0.0)]    // double 的默认值
    [InlineData(false)]  // bool 的默认值
    public void IsDefault_ValueType_WithDefault_ReturnsTrue<T>(T value)
    {
        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 值类型不为默认值时应返回 false
    /// </summary>
    [Theory]
    [InlineData(1)]      // 非默认 int
    [InlineData(0.1)]    // 非默认 double
    [InlineData(true)]   // 非默认 bool
    public void IsDefault_ValueType_WithNonDefault_ReturnsFalse<T>(T value)
    {
        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 结构类型为默认值时应返回 true
    /// </summary>
    [Fact]
    public void IsDefault_StructType_WithDefault_ReturnsTrue()
    {
        // Arrange
        DateTime value = default;
        Guid guid = default;

        // Act
        bool dateResult = value.IsDefault();
        bool guidResult = guid.IsDefault();

        // Assert
        Assert.True(dateResult);
        Assert.True(guidResult);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 结构类型不为默认值时应返回 false
    /// </summary>
    [Fact]
    public void IsDefault_StructType_WithNonDefault_ReturnsFalse()
    {
        // Arrange
        DateTime value = DateTime.Now;
        Guid guid = Guid.NewGuid();

        // Act
        bool dateResult = value.IsDefault();
        bool guidResult = guid.IsDefault();

        // Assert
        Assert.False(dateResult);
        Assert.False(guidResult);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 可空类型为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsDefault_NullableType_WithNull_ReturnsTrue()
    {
        // Arrange
        int? value = null;

        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 可空类型不为 null 时应返回 false（即使值等于基础类型默认值）
    /// </summary>
    [Fact]
    public void IsDefault_NullableType_WithValue_ReturnsFalse()
    {
        // Arrange
        int? value = 0; // 虽然 0 是 int 的默认值，但 int? 的默认值是 null

        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.False(result); // 因为可空类型的默认值是 null，而不是 0
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 自定义类型为默认值时应返回 true
    /// </summary>
    [Fact]
    public void IsDefault_CustomType_WithDefault_ReturnsTrue()
    {
        // Arrange
        CustomStruct value = default;

        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsDefault 方法 - 自定义类型不为默认值时应返回 false
    /// </summary>
    [Fact]
    public void IsDefault_CustomType_WithNonDefault_ReturnsFalse()
    {
        // Arrange
        CustomStruct value = new CustomStruct { Value = 42 };

        // Act
        bool result = value.IsDefault();

        // Assert
        Assert.False(result);
    }

    // 用于测试的自定义结构
    private struct CustomStruct
    {
        public int Value;
    }

    #endregion

    #region IsNull / NotNull

    /// <summary>
    /// 测试 - IsNull 方法 - 传入 null 对象应返回 true
    /// </summary>
    [Fact]
    public void IsNull_WithNullObject_ReturnsTrue()
    {
        // Arrange
        object obj = null;

        // Act
        bool result = obj.IsNull();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsNull 方法 - 传入非 null 对象应返回 false
    /// </summary>
    [Fact]
    public void IsNull_WithNonNullObject_ReturnsFalse()
    {
        // Arrange
        object obj = new object();

        // Act
        bool result = obj.IsNull();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsNull 泛型方法 - 传入 null 字符串应返回 true
    /// </summary>
    [Fact]
    public void IsNull_Generic_WithNullString_ReturnsTrue()
    {
        // Arrange
        string str = null;

        // Act
        bool result = str.IsNull<string>();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsNull 泛型方法 - 传入空字符串应返回 false
    /// </summary>
    [Fact]
    public void IsNull_Generic_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        string str = string.Empty;

        // Act
        bool result = str.IsNull<string>();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsNull 泛型方法 - 传入非 null 字符串应返回 false
    /// </summary>
    [Fact]
    public void IsNull_Generic_WithNonNullString_ReturnsFalse()
    {
        // Arrange
        string str = "test";

        // Act
        bool result = str.IsNull<string>();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsNull 泛型方法 - 传入 null 的自定义类应返回 true
    /// </summary>
    [Fact]
    public void IsNull_Generic_WithNullCustomClass_ReturnsTrue()
    {
        // Arrange
        TestClass obj = null;

        // Act
        bool result = obj.IsNull<TestClass>();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsNull 泛型方法 - 传入非 null 的自定义类应返回 false
    /// </summary>
    [Fact]
    public void IsNull_Generic_WithNonNullCustomClass_ReturnsFalse()
    {
        // Arrange
        TestClass obj = new TestClass();

        // Act
        bool result = obj.IsNull<TestClass>();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsNull 方法 - 传入 null 的可空值类型应返回 true
    /// </summary>
    [Fact]
    public void IsNull_WithNullableValueType_Null_ReturnsTrue()
    {
        // Arrange
        int? nullableInt = null;

        // Act
        bool result = nullableInt.IsNull();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsNull 方法 - 传入非 null 的可空值类型应返回 false
    /// </summary>
    [Fact]
    public void IsNull_WithNullableValueType_NonNull_ReturnsFalse()
    {
        // Arrange
        int? nullableInt = 0;

        // Act
        bool result = nullableInt.IsNull();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - NotNull 方法 - 传入 null 对象应返回 false
    /// </summary>
    [Fact]
    public void NotNull_WithNullObject_ReturnsFalse()
    {
        // Arrange
        object obj = null;

        // Act
        bool result = obj.NotNull();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - NotNull 方法 - 传入非 null 对象应返回 true
    /// </summary>
    [Fact]
    public void NotNull_WithNonNullObject_ReturnsTrue()
    {
        // Arrange
        object obj = new object();

        // Act
        bool result = obj.NotNull();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - NotNull 泛型方法 - 传入 null 字符串应返回 false
    /// </summary>
    [Fact]
    public void NotNull_Generic_WithNullString_ReturnsFalse()
    {
        // Arrange
        string str = null;

        // Act
        bool result = str.NotNull<string>();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - NotNull 泛型方法 - 传入非 null 字符串应返回 true
    /// </summary>
    [Fact]
    public void NotNull_Generic_WithNonNullString_ReturnsTrue()
    {
        // Arrange
        string str = "test";

        // Act
        bool result = str.NotNull<string>();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 用于测试的自定义类
    /// </summary>
    private class TestClass { }

    #endregion
}
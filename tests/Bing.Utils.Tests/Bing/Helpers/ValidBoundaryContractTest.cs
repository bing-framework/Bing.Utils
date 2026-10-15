namespace Bing.Helpers;
/// <summary>
/// Valid 边界契约测试
/// </summary>
[Trait("Bing.Helpers", "Valid.Boundary")]
public class ValidBoundaryContractTest
{
    /// <summary>
    /// 验证空输入会因参数契约而引发异常。
    /// </summary>
    [Fact]
    public void IsLengthStr_NullValue_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Valid.IsLengthStr(null, 1, 10));
        ex.ParamName.ShouldBe("input");
    }
    /// <summary>
    /// 验证字符串长度恰好等于最小值或最大值时不满足严格区间判断。
    /// </summary>
    /// <param name="value">待校验的字符串。</param>
    /// <param name="minLength">允许的最小长度边界。</param>
    /// <param name="maxLength">允许的最大长度边界。</param>
    [Theory]
    [InlineData("ab", 2, 10)]
    [InlineData("abcdefghij", 2, 10)]
    public void IsLengthStr_LengthEqualsBoundary_ReturnsFalse(string value, int minLength, int maxLength)
    {
        var result = Valid.IsLengthStr(value, minLength, maxLength);
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 验证等于最小时间边界的日期时间有效。
    /// </summary>
    [Fact]
    public void IsDateTimeMin_ValueEqualsMin_ReturnsTrue()
    {
        var min = new DateTime(2025, 1, 1, 0, 0, 0);
        var result = Valid.IsDateTimeMin("2025-01-01 00:00:00", min);
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 验证等于最大时间边界的日期时间有效。
    /// </summary>
    [Fact]
    public void IsDateTimeMax_ValueEqualsMax_ReturnsTrue()
    {
        var max = new DateTime(2025, 1, 1, 0, 0, 0);
        var result = Valid.IsDateTimeMax("2025-01-01 00:00:00", max);
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 验证后缀集合为 <see langword="null"/> 时引发参数异常。
    /// </summary>
    [Fact]
    public void IsPostfix_NullPostfixes_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Valid.IsPostfix("file.txt", null));
        ex.ParamName.ShouldBe("value");
    }
}

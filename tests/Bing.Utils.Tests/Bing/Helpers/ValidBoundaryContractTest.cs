namespace Bing.Helpers;
/// <summary>
/// Valid 边界契约测试
/// </summary>
[Trait("Bing.Helpers", "Valid.Boundary")]
public class ValidBoundaryContractTest
{
    [Fact]
    public void IsLengthStr_NullValue_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Valid.IsLengthStr(null, 1, 10));
        ex.ParamName.ShouldBe("input");
    }
    [Theory]
    [InlineData("ab", 2, 10)]
    [InlineData("abcdefghij", 2, 10)]
    public void IsLengthStr_LengthEqualsBoundary_ReturnsFalse(string value, int minLength, int maxLength)
    {
        var result = Valid.IsLengthStr(value, minLength, maxLength);
        result.ShouldBeFalse();
    }
    [Fact]
    public void IsDateTimeMin_ValueEqualsMin_ReturnsTrue()
    {
        var min = new DateTime(2025, 1, 1, 0, 0, 0);
        var result = Valid.IsDateTimeMin("2025-01-01 00:00:00", min);
        result.ShouldBeTrue();
    }
    [Fact]
    public void IsDateTimeMax_ValueEqualsMax_ReturnsTrue()
    {
        var max = new DateTime(2025, 1, 1, 0, 0, 0);
        var result = Valid.IsDateTimeMax("2025-01-01 00:00:00", max);
        result.ShouldBeTrue();
    }
    [Fact]
    public void IsPostfix_NullPostfixes_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Valid.IsPostfix("file.txt", null));
        ex.ParamName.ShouldBe("value");
    }
}

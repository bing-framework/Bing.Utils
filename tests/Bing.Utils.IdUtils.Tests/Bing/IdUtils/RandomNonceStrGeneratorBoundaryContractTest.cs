namespace Bing.IdUtils;

/// <summary>
/// 测试类：覆盖 <see cref="RandomNonceStrGenerator"/> 的边界与契约行为。
/// </summary>
[Trait("IdUtilsUT", "RandomNonceStrGenerator.Boundary")]
public class RandomNonceStrGeneratorBoundaryContractTest
{
    /// <summary>
    /// 测试用例：Create() 无参重载应返回 16 位字母数字串。
    /// </summary>
    [Fact]
    public void Create_DefaultOverload_Returns16AlphaNumeric()
    {
        var result = RandomNonceStrGenerator.Create();

        result.Length.ShouldBe(16);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：Create(forceToAvoidRepetition) 重载应保持默认长度 16。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_ForceAvoidRepetitionOverload_Returns16Length(bool forceToAvoidRepetition)
    {
        var result = RandomNonceStrGenerator.Create(forceToAvoidRepetition);

        result.Length.ShouldBe(16);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：Create(length) 在 length 大于 16 时应返回指定长度。
    /// </summary>
    [Theory]
    [InlineData(17)]
    [InlineData(32)]
    [InlineData(64)]
    public void Create_LengthGreaterThan16_ReturnsExactLength(int length)
    {
        var result = RandomNonceStrGenerator.Create(length);

        result.Length.ShouldBe(length);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：Create(length) 在 length 小于等于 16（含负数）时应回退为 16 位。
    /// </summary>
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    public void Create_LengthLessOrEqual16_ReturnsMinimum16(int length)
    {
        var result = RandomNonceStrGenerator.Create(length);

        result.Length.ShouldBe(16);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }
}

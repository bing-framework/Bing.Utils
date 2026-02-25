namespace Bing.IdUtils;

/// <summary>
/// 测试类：覆盖 `RandomIdGenerator` 的边界与异常契约行为。
/// </summary>
[Trait("IdUtilsUT", "RandomIdGenerator.Boundary")]
public class RandomIdGeneratorBoundaryContractTest
{
    /// <summary>
    /// 测试用例：验证 `Create` 在 `NegativeLength` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void Create_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => RandomIdGenerator.Create(-1, RandomIdGenerator.AllNumbers));
        ex.ParamName.ShouldBe("capacity");
    }

    /// <summary>
    /// 测试用例：验证 `Create` 在 `NullDictionary` 场景下，结果为 `ThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void Create_NullDictionary_ThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => RandomIdGenerator.Create(8, null));
    }

    /// <summary>
    /// 测试用例：验证 `Create` 在 `NullFormat` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void Create_NullFormat_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => RandomIdGenerator.Create((string)null, RandomIdGenerator.AllNumbers));
        ex.ParamName.ShouldBe("format");
    }

    /// <summary>
    /// 测试用例：验证 `Create` 在 `FormatWithoutPlaceholder` 场景下，结果为 `ReturnsLiteral`。
    /// </summary>
    [Fact]
    public void Create_FormatWithoutPlaceholder_ReturnsLiteral()
    {
        const string format = "CONST-ID";

        var result = RandomIdGenerator.Create(format, RandomIdGenerator.AllNumbers);

        result.ShouldBe(format);
    }

    /// <summary>
    /// 测试用例：验证 `Create` 在 `SingleCharacterDictionary` 场景下，结果为 `ReturnsRepeatedSameCharacter`。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(32)]
    public void Create_SingleCharacterDictionary_ReturnsRepeatedSameCharacter(int length)
    {
        var result = RandomIdGenerator.Create(length, "X");

        result.ShouldBe(new string('X', length));
    }

    /// <summary>
    /// 测试用例：Create(format) 在 format 为空字符串时，应返回空字符串。
    /// </summary>
    [Fact]
    public void Create_EmptyFormat_ReturnsEmptyString()
    {
        var result = RandomIdGenerator.Create(string.Empty, RandomIdGenerator.AllNumbers);

        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试用例：Create(format) 在 format 非法占位符场景下，应抛出 FormatException。
    /// </summary>
    [Fact]
    public void Create_InvalidFormat_ThrowsFormatException()
    {
        const string invalidFormat = "ID-{0";

        Should.Throw<FormatException>(() => RandomIdGenerator.Create(invalidFormat, RandomIdGenerator.AllNumbers));
    }
}

namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `StringProwessExtensions` 相关行为。
/// </summary>
[Trait("Bing.Text", "StringProwessExtensions")]
public class StringProwessExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `JoinStringFor` 在 `WithDelimiter` 场景下，结果为 `JoinsAllItems`。
    /// </summary>
    [Fact]
    public void JoinStringFor_WithDelimiter_JoinsAllItems()
    {
        var result = "|".JoinStringFor(new[] { 1, 2, 3 });
        result.ShouldBe("1|2|3");
    }
    /// <summary>
    /// 测试用例：验证 `JoinStringFor` 在 `WithNullDelimiter` 场景下，结果为 `CurrentlyConcatenatesWithoutSeparator`。
    /// </summary>
    [Fact]
    public void JoinStringFor_WithNullDelimiter_CurrentlyConcatenatesWithoutSeparator()
    {
        var result = ((string)null).JoinStringFor(new[] { "a", "b", "c" });
        result.ShouldBe("abc");
    }
    /// <summary>
    /// 测试用例：验证 `JoinStringFor` 在 `WithNullList` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public void JoinStringFor_WithNullList_ReturnsEmptyString()
    {
        IEnumerable<int> list = null;
        var result = ",".JoinStringFor(list);
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `SplitByIndex` 在 `WithDifferentIndexes` 场景下，结果为 `ReturnsCurrentBehavior`。
    /// </summary>
    [Theory]
    [InlineData("ABCDE", -1, "", "ABCDE")]
    [InlineData("ABCDE", 0, "", "ABCDE")]
    [InlineData("ABCDE", 1, "", "ABCDE")]
    [InlineData("ABCDE", 2, "A", "BCDE")]
    [InlineData("ABCDE", 5, "ABCDE", "")]
    [InlineData("ABCDE", 6, "ABCDE", "")]
    public void SplitByIndex_WithDifferentIndexes_ReturnsCurrentBehavior(string input, int index, string expectedLeft, string expectedRight)
    {
        var result = input.SplitByIndex(index);
        result.Item1.ShouldBe(expectedLeft);
        result.Item2.ShouldBe(expectedRight);
    }
    /// <summary>
    /// 测试用例：验证 `SplitByIndex` 在 `WithNullOrEmpty` 场景下，结果为 `ReturnsTwoEmptyStrings`。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SplitByIndex_WithNullOrEmpty_ReturnsTwoEmptyStrings(string input)
    {
        var result = input.SplitByIndex(3);
        result.Item1.ShouldBe(string.Empty);
        result.Item2.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `SplitTyped` 在 `WithCharDelimiter` 场景下，结果为 `ReturnsTypedArray`。
    /// </summary>
    [Fact]
    public void SplitTyped_WithCharDelimiter_ReturnsTypedArray()
    {
        var result = "1,2,3".SplitTyped<int>(',');
        result.ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 测试用例：验证 `SplitTyped` 在 `WithStringDelimiter` 场景下，结果为 `ReturnsTypedArray`。
    /// </summary>
    [Fact]
    public void SplitTyped_WithStringDelimiter_ReturnsTypedArray()
    {
        var result = "10||20||30".SplitTyped<int>("||");
        result.ShouldBe(new[] { 10, 20, 30 });
    }
    /// <summary>
    /// 测试用例：验证 `SplitTyped` 在 `WithNullOrWhiteSpaceInput` 场景下，结果为 `ReturnsEmptyArray`。
    /// </summary>
    [Fact]
    public void SplitTyped_WithNullOrWhiteSpaceInput_ReturnsEmptyArray()
    {
        ((string)null).SplitTyped<int>(',').ShouldBeEmpty();
        "".SplitTyped<int>(',').ShouldBeEmpty();
        "   ".SplitTyped<int>(',').ShouldBeEmpty();
    }
    /// <summary>
    /// 测试用例：验证 `SplitTyped` 在 `WithEmptyEntries` 场景下，结果为 `IgnoresEmptySegments`。
    /// </summary>
    [Fact]
    public void SplitTyped_WithEmptyEntries_IgnoresEmptySegments()
    {
        "1,,2,,,3".SplitTyped<int>(',').ShouldBe(new[] { 1, 2, 3 });
        "1||||2||3".SplitTyped<int>("||").ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 测试用例：验证 `SplitTyped` 在 `WithInvalidValue` 场景下，结果为 `ThrowsFormatException`。
    /// </summary>
    [Fact]
    public void SplitTyped_WithInvalidValue_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => "1,a,3".SplitTyped<int>(','));
    }
}


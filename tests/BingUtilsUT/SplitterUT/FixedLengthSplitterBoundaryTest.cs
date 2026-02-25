using Bing.Text.Splitters;

namespace BingUtilsUT.SplitterUT;

/// <summary>
/// 测试类：覆盖 FixedLengthSplitter 的边界输入与契约行为。
/// </summary>
[Trait("SplitterUT", "FixedLengthSplitter.Boundary")]
public class FixedLengthSplitterBoundaryTest
{
    /// <summary>
    /// 测试用例：固定长度为 0 时，当前行为在切割阶段抛出除零异常。
    /// </summary>
    [Fact]
    public void FixedLength_ZeroLength_SplitToList_CurrentlyThrowsDivideByZeroException()
    {
        var splitter = Splitter.FixedLength(0);

        Should.Throw<DivideByZeroException>(() => splitter.SplitToList("abc"));
    }

    /// <summary>
    /// 测试用例：输入为 null/空白字符串时，应返回空结果。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Split_NullOrWhiteSpaceInput_ReturnsEmpty(string input)
    {
        var splitter = Splitter.FixedLength(2);

        splitter.Split(input).ShouldBeEmpty();
        splitter.SplitToList(input).ShouldBeEmpty();
        splitter.SplitToArray(input).ShouldBeEmpty();
    }

    /// <summary>
    /// 测试用例：固定长度为 1 时，应逐字符切割并保持顺序。
    /// </summary>
    [Fact]
    public void Split_LengthOne_ReturnsEachCharacter()
    {
        var result = Splitter.FixedLength(1).SplitToArray("abc");

        result.ShouldBe(new[] { "a", "b", "c" });
    }

    /// <summary>
    /// 测试用例：Limit 小于等于 0 时，应按无限制处理。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Limit_NonPositiveValue_BehavesAsUnlimited(int limit)
    {
        var result = Splitter.FixedLength(2).Limit(limit).SplitToList("abcdef");

        result.ShouldBe(new[] { "ab", "cd", "ef" });
    }

    /// <summary>
    /// 测试用例：Limit 大于分段数时，应返回全部分段。
    /// </summary>
    [Fact]
    public void Limit_GreaterThanSegmentCount_ReturnsAllSegments()
    {
        var result = Splitter.FixedLength(2).Limit(99).SplitToArray("abcdef");

        result.ShouldBe(new[] { "ab", "cd", "ef" });
    }

    /// <summary>
    /// 测试用例：TrimResults(null) 应回退为默认 Trim 行为。
    /// </summary>
    [Fact]
    public void TrimResults_NullFunc_FallsBackToDefaultTrim()
    {
        var result = Splitter.FixedLength(2).TrimResults(null).SplitToList("a b c ");

        result.ShouldBe(new[] { "a", "b", "c" });
    }

    /// <summary>
    /// 测试用例：FixedLength + KeyValueSeparator 在缺少分隔符时，值应为空字符串。
    /// </summary>
    [Fact]
    public void WithKeyValueSeparator_MissingValueSeparator_ReturnsEmptyValue()
    {
        var result = Splitter.FixedLength(3).WithKeyValueSeparator("=").Split("a=1abc").ToList();

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("a");
        result[0].Value.ShouldBe("1");
        result[1].Key.ShouldBe("abc");
        result[1].Value.ShouldBe(string.Empty);
    }
}

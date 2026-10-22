using Bing.Text.Truncation;
using Shouldly;
using Xunit;

namespace Bing.Text;

/// <summary>
/// 测试按 Unicode 文本元素安全截断的行为。
/// </summary>
[Trait("TextUT", "TextElementTruncator")]
public class TextElementTruncatorTests
{
    /// <summary>
    /// 测试目的：代理对、肤色序列、ZWJ 序列、键帽和旗帜应各自作为一个文本元素保留。
    /// </summary>
    /// <param name="text">待截断文本。</param>
    /// <param name="maxLength">允许的文本元素数量。</param>
    /// <param name="expected">预期截断结果。</param>
    [Theory]
    [InlineData("A😀B", 2, "A.")]
    [InlineData("A👍🏽B", 2, "A.")]
    [InlineData("A👨‍👩‍👧‍👦B", 2, "A.")]
    [InlineData("A1️⃣B", 2, "A.")]
    [InlineData("A🇨🇳B", 2, "A.")]
    [InlineData("AéB", 2, "A.")]
    public void Truncate_PreservesTextElementBoundaries(string text, int maxLength, string expected)
    {
        text.TruncateByTextElements(maxLength).ShouldBe(expected);
    }

    /// <summary>
    /// 验证从右侧截断时，保留下来的组合文本元素不会被拆开。
    /// </summary>
    /// <param name="element">待验证文本元素。</param>
    [Theory]
    [InlineData("😀")]
    [InlineData("👍🏽")]
    [InlineData("👨‍👩‍👧‍👦")]
    [InlineData("1️⃣")]
    [InlineData("🇨🇳")]
    [InlineData("é")]
    public void TruncateFromRight_KeepsWholeTextElement(string element)
    {
        ("A" + element + "BC").TruncateByTextElements(3, truncationString: ".")
            .ShouldBe("A" + element + ".");
    }

    /// <summary>
    /// 验证从左侧截断时，保留下来的组合文本元素不会被拆开。
    /// </summary>
    /// <param name="element">待验证文本元素。</param>
    [Theory]
    [InlineData("😀")]
    [InlineData("👍🏽")]
    [InlineData("👨‍👩‍👧‍👦")]
    [InlineData("1️⃣")]
    [InlineData("🇨🇳")]
    [InlineData("é")]
    public void TruncateFromLeft_KeepsWholeTextElement(string element)
    {
        ("AB" + element + "C").TruncateByTextElements(3, truncationString: ".", from: StringTruncateFrom.Left)
            .ShouldBe("." + element + "C");
    }

    /// <summary>
    /// 验证孤立代理项在两种截断方向下保持原始代码单元。
    /// </summary>
    [Fact]
    public void Truncate_PreservesIsolatedSurrogate()
    {
        const string isolated = "\uD800";
        ("A" + isolated + "BC").TruncateByTextElements(3, truncationString: ".")
            .ShouldBe("A" + isolated + ".");
        ("AB" + isolated + "C").TruncateByTextElements(3, truncationString: ".", from: StringTruncateFrom.Left)
            .ShouldBe("." + isolated + "C");
    }

    /// <summary>
    /// 测试目的：未超出文本元素上限时应原样返回，左侧截断应保留末尾完整元素。
    /// </summary>
    [Fact]
    public void Truncate_ReturnsOriginalOrKeepsRightSideElements()
    {
        "A👍🏽B".TruncateByTextElements(3).ShouldBe("A👍🏽B");
        "A👍🏽BC".TruncateByTextElements(2, from: StringTruncateFrom.Left).ShouldBe(".C");
    }

    /// <summary>
    /// 测试目的：自定义截断字符串和额外空格也应按文本元素数量限制结果。
    /// </summary>
    [Fact]
    public void Truncate_CountsMarkerAndOptionalSpaceAsTextElements()
    {
        "😀😀😀😀".TruncateByTextElements(3, "…", "。", extraSpace: true).ShouldBe("😀 …");
        "😀😀😀".TruncateByTextElements(1, "...", ".").ShouldBe(".");
    }

    /// <summary>
    /// 测试目的：空文本、负数上限和零上限应遵循现有截断器的边界约定。
    /// </summary>
    /// <param name="text">待截断文本。</param>
    /// <param name="maxLength">允许的文本元素数量。</param>
    /// <param name="expected">预期截断结果。</param>
    [Theory]
    [InlineData(null, 2, "")]
    [InlineData("", 2, "")]
    [InlineData("😀", -1, "")]
    [InlineData("😀", 0, "😀")]
    public void Truncate_HandlesBoundaryArguments(string text, int maxLength, string expected)
    {
        text.TruncateByTextElements(maxLength).ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：通用截断扩展方法应能够显式选择文本元素截断器。
    /// </summary>
    [Fact]
    public void GenericTruncate_AcceptsTextElementTruncator()
    {
        "A👍🏽B".Truncate(2, StringTruncators.ByTextElements).ShouldBe("A.");
    }
}

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
    /// 测试目的：验证常见组合字符的截断结果遵循当前运行时的文本元素边界。
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
    public void Truncate_RespectsRuntimeTextElementBoundaries(string text, int maxLength, string expected)
    {
        text.TruncateByTextElements(maxLength).ShouldBe(expected);
    }

    /// <summary>
    /// 验证从右侧截断时，结果遵循当前运行时的文本元素边界。
    /// </summary>
    /// <param name="element">待截断的组合字符。</param>
    /// <param name="expected">当前目标框架的预期结果。</param>
    [Theory]
    [InlineData("😀", "A😀.")]
#if NETCOREAPP3_1
    [InlineData("👍🏽", "A👍.")]
    [InlineData("👨‍👩‍👧‍👦", "A👨.")]
    [InlineData("🇨🇳", "A🇨.")]
#else
    [InlineData("👍🏽", "A👍🏽.")]
    [InlineData("👨‍👩‍👧‍👦", "A👨‍👩‍👧‍👦.")]
    [InlineData("🇨🇳", "A🇨🇳.")]
#endif
    [InlineData("1️⃣", "A1️⃣.")]
    [InlineData("é", "Aé.")]
    public void TruncateFromRight_RespectsRuntimeTextElementBoundaries(string element, string expected)
    {
        ("A" + element + "BC").TruncateByTextElements(3, truncationString: ".")
            .ShouldBe(expected);
    }

    /// <summary>
    /// 验证从左侧截断时，结果遵循当前运行时的文本元素边界。
    /// </summary>
    /// <param name="element">待截断的组合字符。</param>
    /// <param name="expected">当前目标框架的预期结果。</param>
    [Theory]
    [InlineData("😀", ".😀C")]
#if NETCOREAPP3_1
    [InlineData("👍🏽", ".🏽C")]
    [InlineData("👨‍👩‍👧‍👦", ".👦C")]
    [InlineData("🇨🇳", ".🇳C")]
#else
    [InlineData("👍🏽", ".👍🏽C")]
    [InlineData("👨‍👩‍👧‍👦", ".👨‍👩‍👧‍👦C")]
    [InlineData("🇨🇳", ".🇨🇳C")]
#endif
    [InlineData("1️⃣", ".1️⃣C")]
    [InlineData("é", ".éC")]
    public void TruncateFromLeft_RespectsRuntimeTextElementBoundaries(string element, string expected)
    {
        ("AB" + element + "C").TruncateByTextElements(3, truncationString: ".", from: StringTruncateFrom.Left)
            .ShouldBe(expected);
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
    /// 测试目的：原样返回与左侧截断结果应遵循当前运行时的文本元素边界。
    /// </summary>
    [Fact]
    public void Truncate_UsesRuntimeTextElementBoundaries()
    {
#if NETCOREAPP3_1
        "A👍🏽B".TruncateByTextElements(3).ShouldBe("...");
#else
        "A👍🏽B".TruncateByTextElements(3).ShouldBe("A👍🏽B");
#endif
        "A👍🏽B".TruncateByTextElements(4).ShouldBe("A👍🏽B");
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

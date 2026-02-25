namespace Bing.Text;

/// <summary>
/// 测试类：覆盖 `StringProwessExtensions` 在 Unicode 场景下的边界行为。
/// </summary>
[Trait("Bing.Text", "StringProwess.UnicodeBoundary")]
public class StringProwessUnicodeBoundaryTest
{
    /// <summary>
    /// 测试用例：验证 `SplitByIndex` 在 `SplitAtSurrogateBoundary` 场景下，结果为 `SplitsByUtf16CodeUnit`。
    /// </summary>
    [Fact]
    public void SplitByIndex_SplitAtSurrogateBoundary_SplitsByUtf16CodeUnit()
    {
        var text = "😀B";

        var result = text.SplitByIndex(2);

        result.Item1.Length.ShouldBe(1);
        result.Item2.Length.ShouldBe(2);
        char.IsHighSurrogate(result.Item1[0]).ShouldBeTrue();
        char.IsLowSurrogate(result.Item2[0]).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 `JoinStringFor` 在 `ComposedAndDecomposedItems` 场景下，结果为 `PreservesOriginalSequence`。
    /// </summary>
    [Fact]
    public void JoinStringFor_ComposedAndDecomposedItems_PreservesOriginalSequence()
    {
        var items = new[] { "café", "cafe\u0301" };

        var result = "|".JoinStringFor(items);

        result.ShouldBe("café|cafe\u0301");
    }
}

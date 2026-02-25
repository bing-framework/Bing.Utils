using Bing.Collections;

namespace Bing.Utils.Tests.Collections;

/// <summary>
/// 测试类：`Colls` 集合工具方法与扩展方法测试
/// </summary>
[Trait("CollUT", "Colls")]
public class CollsTests
{
    /// <summary>
    /// 测试用例：`Colls.AddRange` 静态方法应按顺序合并两个集合，且不修改输入集合
    /// </summary>
    [Fact]
    public void AddRange_StaticMethod_TwoLists_MergesInOrderWithoutMutatingInputs()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };
        var other = new List<int> { 6, 7, 8, 9, 10 };

        var result = Colls.AddRange(source, other).ToList();

        source.ShouldBe(new[] { 1, 2, 3, 4, 5 });
        other.ShouldBe(new[] { 6, 7, 8, 9, 10 });
        result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
    }

    /// <summary>
    /// 测试用例：`AddRange` 扩展方法在指定 `limit` 时，仅追加限定数量元素并保持顺序
    /// </summary>
    [Fact]
    public void AddRange_ExtensionMethod_WithLimit_AppendsLimitedItemsInOrder()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };
        var other = new List<int> { 6, 7, 8, 9, 10 };

        var result = source.AddRange(other, 3).ToList();

        source.ShouldBe(new[] { 1, 2, 3, 4, 5 });
        other.ShouldBe(new[] { 6, 7, 8, 9, 10 });
        result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
    }
}

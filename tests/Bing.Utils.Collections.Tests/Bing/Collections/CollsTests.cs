using System.Collections.Generic;
using System.Linq;
using Bing.Collections;

namespace Bing.Collections;

/// <summary>
/// 集合批量追加的顺序和输入保留测试。
/// </summary>
[Trait("CollUT", "Colls")]
public class CollsTests
{
    /// <summary>
    /// 验证静态批量追加按顺序合并且保留输入列表。
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
    /// 验证扩展批量追加限制元素数量且保留输入列表。
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

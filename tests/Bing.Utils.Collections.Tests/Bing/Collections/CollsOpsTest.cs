using System.Collections.Generic;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `CollsOps` 相关行为。
/// </summary>
[Trait("CollectionsUT", "Colls.Ops")]
public class CollsOpsTest
{
    /// <summary>
    /// 测试用例：验证 `AddRange` 在 `WithLimit` 场景下，结果为 `ReturnsExpectedSequence`。
    /// </summary>
    [Fact]
    public void AddRange_WithLimit_ReturnsExpectedSequence()
    {
        var source = new[] { 1, 2 };
        var other = new[] { 3, 4, 5 };
        var result = Colls.AddRange(source, other, 2).ToArray();
        result.ShouldBe([1, 2, 3, 4]);
    }
    /// <summary>
    /// 测试用例：验证 `AddIf` 在 `WithFlagFalse` 场景下，结果为 `DoesNotAppendValue`。
    /// </summary>
    [Fact]
    public void AddIf_WithFlagFalse_DoesNotAppendValue()
    {
        var source = new[] { 1, 2 };
        var result = Colls.AddIf(source, 3, false).ToArray();
        result.ShouldBe([1, 2]);
    }
    /// <summary>
    /// 测试用例：验证 `AddIf` 在 `WithPredicateNull` 场景下，结果为 `DoesNotAppendValue`。
    /// </summary>
    [Fact]
    public void AddIf_WithPredicateNull_DoesNotAppendValue()
    {
        var source = new[] { 1, 2 };
        var result = Colls.AddIf(source, 3, (Func<int, bool>)null).ToArray();
        result.ShouldBe([1, 2]);
    }
    /// <summary>
    /// 测试用例：验证 `AddIf` 在 `WhenSourceIsNull` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void AddIf_WhenSourceIsNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Colls.AddIf<int>(null, 1, true).ToArray());
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 测试用例：验证 `AddIfNotExist` 在 `WhenValueAlreadyExists` 场景下，结果为 `DoesNotAppend`。
    /// </summary>
    [Fact]
    public void AddIfNotExist_WhenValueAlreadyExists_DoesNotAppend()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };
        var result = source.AddIfNotExist(2).ToArray();
        result.ShouldBe([1, 2, 3]);
    }
    /// <summary>
    /// 测试用例：验证 `AddIfNotNull` 在 `WithNullValue` 场景下，结果为 `DoesNotAppend`。
    /// </summary>
    [Fact]
    public void AddIfNotNull_WithNullValue_DoesNotAppend()
    {
        IEnumerable<string> source = new[] { "a", "b" };
        var result = source.AddIfNotNull(null).ToArray();
        result.ShouldBe(["a", "b"]);
    }
    /// <summary>
    /// 测试用例：验证 `GetOrAdd` 在 `WhenItemExists` 场景下，结果为 `DoesNotCallFactory`。
    /// </summary>
    [Fact]
    public void GetOrAdd_WhenItemExists_DoesNotCallFactory()
    {
        ICollection<string> source = new List<string> { "a", "b" };
        var callCount = 0;
        var result = Colls.GetOrAdd(source, x => x == "a", () =>
        {
            callCount++;
            return "new";
        });
        result.ShouldBe("a");
        callCount.ShouldBe(0);
    }
    /// <summary>
    /// 测试用例：验证 `RemoveDuplicatesIgnoreCase` 在 `RemovesDuplicatesAndKeepsLastEntry` 场景下的行为。
    /// </summary>
    [Fact]
    public void RemoveDuplicatesIgnoreCase_RemovesDuplicatesAndKeepsLastEntry()
    {
        IList<string> source = new List<string> { "A", "a", "B", "b", "C" };
        var result = Colls.RemoveDuplicatesIgnoreCase(source).ToArray();
        result.ShouldBe(["a", "b", "C"]);
        source.ShouldBe(["a", "b", "C"]);
    }
    /// <summary>
    /// 测试用例：验证 `RemoveIf` 在 `WhenPredicateIsNull` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void RemoveIf_WhenPredicateIsNull_ThrowsArgumentNullException()
    {
        IList<int> source = new List<int> { 1, 2, 3 };
        var ex = Should.Throw<ArgumentNullException>(() => Colls.RemoveIf(source, null).ToArray());
        ex.ParamName.ShouldBe("predicate");
    }
    /// <summary>
    /// 测试用例：验证 `RemoveRangeSafety` 在 `WhenIndexAndCountOutOfRange` 场景下，结果为 `ReturnsOriginalList`。
    /// </summary>
    [Fact]
    public void RemoveRangeSafety_WhenIndexAndCountOutOfRange_ReturnsOriginalList()
    {
        var source = new List<int> { 1, 2, 3 };
        Colls.RemoveRangeSafety(source, -1, 2).ShouldBe(source);
        source.ShouldBe([1, 2, 3]);
        Colls.RemoveRangeSafety(source, 10, 2).ShouldBe(source);
        source.ShouldBe([1, 2, 3]);
    }
    /// <summary>
    /// 测试用例：验证 `Merge` 在 `WithRightNull` 场景下，结果为 `ReturnsSourceOnly`。
    /// </summary>
    [Fact]
    public void Merge_WithRightNull_ReturnsSourceOnly()
    {
        var source = new[] { 1, 2 };
        var result = Colls.Merge(source, (IEnumerable<int>)null).ToArray();
        result.ShouldBe([1, 2]);
    }
}


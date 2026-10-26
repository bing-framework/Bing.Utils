using System.Collections.Generic;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 集合追加、去重、移除和合并测试。
/// </summary>
[Trait("CollectionsUT", "Colls.Ops")]
public class CollsOpsTest
{
    /// <summary>
    /// 验证批量追加只取限定数量的元素。
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
    /// 验证条件为假时不追加元素。
    /// </summary>
    [Fact]
    public void AddIf_WithFlagFalse_DoesNotAppendValue()
    {
        var source = new[] { 1, 2 };
        var result = Colls.AddIf(source, 3, false).ToArray();
        result.ShouldBe([1, 2]);
    }
    /// <summary>
    /// 验证谓词为空时不追加元素。
    /// </summary>
    [Fact]
    public void AddIf_WithPredicateNull_DoesNotAppendValue()
    {
        var source = new[] { 1, 2 };
        var result = Colls.AddIf(source, 3, (Func<int, bool>)null).ToArray();
        result.ShouldBe([1, 2]);
    }
    /// <summary>
    /// 验证条件追加拒绝空源序列并报告参数名。
    /// </summary>
    [Fact]
    public void AddIf_WhenSourceIsNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Colls.AddIf<int>(null, 1, true).ToArray());
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 验证已有元素不会被重复追加。
    /// </summary>
    [Fact]
    public void AddIfNotExist_WhenValueAlreadyExists_DoesNotAppend()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };
        var result = source.AddIfNotExist(2).ToArray();
        result.ShouldBe([1, 2, 3]);
    }
    /// <summary>
    /// 验证空值不会被追加。
    /// </summary>
    [Fact]
    public void AddIfNotNull_WithNullValue_DoesNotAppend()
    {
        IEnumerable<string> source = new[] { "a", "b" };
        var result = source.AddIfNotNull(null).ToArray();
        result.ShouldBe(["a", "b"]);
    }
    /// <summary>
    /// 验证匹配元素存在时返回该元素且不调用工厂。
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
    /// 验证值类型未命中时调用工厂并添加结果。
    /// </summary>
    [Fact]
    public void GetOrAdd_WhenValueTypeIsMissing_CallsFactoryAndAddsResult()
    {
        var source = new List<int> { 1 };
        var callCount = 0;

        var result = Colls.GetOrAdd(source, value => value == 2, () =>
        {
            callCount++;
            return 2;
        });

        result.ShouldBe(2);
        callCount.ShouldBe(1);
        source.ShouldBe([1, 2]);
    }

    /// <summary>
    /// 验证命中默认值时不会误判为未命中。
    /// </summary>
    [Fact]
    public void GetOrAdd_WhenDefaultValueMatches_DoesNotCallFactory()
    {
        var source = new List<int> { 0 };
        var callCount = 0;

        var result = Colls.GetOrAdd(source, value => value == 0, () =>
        {
            callCount++;
            return 42;
        });

        result.ShouldBe(0);
        callCount.ShouldBe(0);
        source.ShouldBe([0]);
    }

    /// <summary>
    /// 验证命中 null 时不会再次添加工厂结果。
    /// </summary>
    [Fact]
    public void GetOrAdd_WhenNullValueMatches_DoesNotCallFactory()
    {
        ICollection<string> source = new List<string> { null! };
        var callCount = 0;

        var result = Colls.GetOrAdd(source, value => value is null, () =>
        {
            callCount++;
            return "new";
        });

        result.ShouldBeNull();
        callCount.ShouldBe(0);
        source.Count.ShouldBe(1);
    }
    /// <summary>
    /// 验证忽略大小写去重时保留最后出现的元素并更新源列表。
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
    /// 验证条件移除拒绝空谓词并报告参数名。
    /// </summary>
    [Fact]
    public void RemoveIf_WhenPredicateIsNull_ThrowsArgumentNullException()
    {
        IList<int> source = new List<int> { 1, 2, 3 };
        var ex = Should.Throw<ArgumentNullException>(() => Colls.RemoveIf(source, null).ToArray());
        ex.ParamName.ShouldBe("predicate");
    }
    /// <summary>
    /// 验证无效起始索引的安全移除保留原列表。
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
    /// 验证安全范围移除准确删除 count 个元素，并将过大的 count 截断到末尾。
    /// </summary>
    [Fact]
    public void RemoveRangeSafety_RemovesRequestedCountAndClampsToEnd()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };

        Colls.RemoveRangeSafety(source, 1, 2).ShouldBe(source);
        source.ShouldBe([1, 4, 5]);

        Colls.RemoveRangeSafety(source, 1, 10).ShouldBe(source);
        source.ShouldBe([1]);
    }
    /// <summary>
    /// 验证合并空的右侧序列时保留源元素。
    /// </summary>
    [Fact]
    public void Merge_WithRightNull_ReturnsSourceOnly()
    {
        var source = new[] { 1, 2 };
        var result = Colls.Merge(source, (IEnumerable<int>)null).ToArray();
        result.ShouldBe([1, 2]);
    }
}


using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 字典更新、级联查找和集合转换测试。
/// </summary>
[Trait("CollectionsUT", "DictsAndCollConv")]
public class DictsAndCollConvTest
{
    /// <summary>
    /// 验证已有字典键通过更新工厂修改值。
    /// </summary>
    [Fact]
    public void AddValueOrUpdate_WhenKeyExists_ShouldUpdate()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };
        Dicts.AddValueOrUpdate(dict, "a", _ => 10, (_, oldValue) => oldValue + 1);
        dict["a"].ShouldBe(2);
    }
    /// <summary>
    /// 验证缺失的字典键通过工厂添加值。
    /// </summary>
    [Fact]
    public void AddValueIfNotExist_WhenMissing_ShouldAdd()
    {
        var dict = new Dictionary<string, int>();
        Dicts.AddValueIfNotExist(dict, "a", _ => 3);
        dict["a"].ShouldBe(3);
    }
    /// <summary>
    /// 验证级联默认值查找拒绝空字典集合。
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_WhenNullCollection_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            Dicts.GetValueOrDefaultCascading<string, int>(null!, "a", -1));
    }
    /// <summary>
    /// 验证缺失字典值只创建一次并供后续查询复用。
    /// </summary>
    [Fact]
    public void GetValueOrAdd_WhenMissing_ShouldCreateOnce()
    {
        var dict = new Dictionary<string, int>();
        var callCount = 0;
        var first = Dicts.GetValueOrAdd(dict, "a", _ =>
        {
            callCount++;
            return 7;
        });
        var second = Dicts.GetValueOrAdd(dict, "a", _ =>
        {
            callCount++;
            return 99;
        });
        first.ShouldBe(7);
        second.ShouldBe(7);
        callCount.ShouldBe(1);
    }
    /// <summary>
    /// 验证相同类型的空只读字典复用单例。
    /// </summary>
    [Fact]
    public void ReadOnlyDicts_Empty_ShouldReturnSingleton()
    {
        var d1 = ReadOnlyDicts.Empty<string, int>();
        var d2 = ReadOnlyDicts.Empty<string, int>();
        d1.Count.ShouldBe(0);
        ReferenceEquals(d1, d2).ShouldBeTrue();
    }
    /// <summary>
    /// 验证缺失键返回指定默认值。
    /// </summary>
    [Fact]
    public void ReadOnlyDicts_GetValueOrDefault_ShouldReturnDefaultWhenMissing()
    {
        IReadOnlyDictionary<string, int> dict = new ReadOnlyDictionary<string, int>(
            new Dictionary<string, int> { ["a"] = 1 });
        ReadOnlyDicts.GetValueOrDefault(dict, "missing", 9).ShouldBe(9);
    }
    /// <summary>
    /// 验证级联查找返回首个匹配字典的值。
    /// </summary>
    [Fact]
    public void ReadOnlyDicts_TryGetValueCascading_ShouldResolveFirstMatch()
    {
        var first = new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
        var second = new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["a"] = 2 });
        var found = ReadOnlyDicts.TryGetValueCascading(new[] { first, second }, "a", out var value);
        found.ShouldBeTrue();
        value.ShouldBe(1);
    }
    /// <summary>
    /// 验证索引序列包含从零开始的索引和值。
    /// </summary>
    [Fact]
    public void CollConv_ToIndexedSequence_ShouldReturnExpectedIndex()
    {
        var indexed = CollConv.ToIndexedSequence(new[] { "x", "y" }).ToArray();
        indexed.Length.ShouldBe(2);
        indexed[0].Key.ShouldBe(0);
        indexed[0].Value.ShouldBe("x");
        indexed[1].Key.ShouldBe(1);
        indexed[1].Value.ShouldBe("y");
    }
    /// <summary>
    /// 验证从枚举器当前位置开始转换时包含当前元素。
    /// </summary>
    [Fact]
    public void CollConv_ToEnumerableAfter_ShouldContinueFromCurrent()
    {
        var enumerator = new[] { 10, 20, 30 }.AsEnumerable().GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        var values = CollConv.ToEnumerableAfter(enumerator).ToArray();
        values.ShouldBe(new[] { 10, 20, 30 });
    }
    /// <summary>
    /// 验证空序列转换为空引用。
    /// </summary>
    [Fact]
    public void CollConv_AsNullWhenEmpty_ShouldReturnNullForEmpty()
    {
        var result = CollConv.AsNullWhenEmpty(Array.Empty<int>());
        result.ShouldBeNull();
    }
    /// <summary>
    /// 验证非空序列保留全部元素。
    /// </summary>
    [Fact]
    public void CollConv_AsNullWhenEmpty_ShouldReturnEnumerableForNonEmpty()
    {
        var result = CollConv.AsNullWhenEmpty(new[] { 1, 2, 3 });
        result.ShouldNotBeNull();
        result!.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 验证带谓词的列表转换筛选匹配元素。
    /// </summary>
    [Fact]
    public void CollConvShortcut_ToList_WithPredicate_ShouldFilter()
    {
        var result = CollConvShortcutExtensions.ToList(new[] { 1, 2, 3, 4 }, x => x % 2 == 0);
        result.ShouldBe(new[] { 2, 4 });
    }
}


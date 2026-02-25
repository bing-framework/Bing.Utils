using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `DictsAndCollConv` 相关行为。
/// </summary>
[Trait("CollectionsUT", "DictsAndCollConv")]
public class DictsAndCollConvTest
{
    /// <summary>
    /// 测试用例：验证 `AddValueOrUpdate` 在 `WhenKeyExists` 场景下，结果为 `ShouldUpdate`。
    /// </summary>
    [Fact]
    public void AddValueOrUpdate_WhenKeyExists_ShouldUpdate()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };
        Dicts.AddValueOrUpdate(dict, "a", _ => 10, (_, oldValue) => oldValue + 1);
        dict["a"].ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `AddValueIfNotExist` 在 `WhenMissing` 场景下，结果为 `ShouldAdd`。
    /// </summary>
    [Fact]
    public void AddValueIfNotExist_WhenMissing_ShouldAdd()
    {
        var dict = new Dictionary<string, int>();
        Dicts.AddValueIfNotExist(dict, "a", _ => 3);
        dict["a"].ShouldBe(3);
    }
    /// <summary>
    /// 测试用例：验证 `GetValueOrDefaultCascading` 在 `WhenNullCollection` 场景下，结果为 `ShouldThrow`。
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_WhenNullCollection_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            Dicts.GetValueOrDefaultCascading<string, int>(null!, "a", -1));
    }
    /// <summary>
    /// 测试用例：验证 `GetValueOrAdd` 在 `WhenMissing` 场景下，结果为 `ShouldCreateOnce`。
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
    /// 测试用例：验证 `ReadOnlyDicts` 在 `Empty` 场景下，结果为 `ShouldReturnSingleton`。
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
    /// 测试用例：验证 `ReadOnlyDicts` 在 `GetValueOrDefault` 场景下，结果为 `ShouldReturnDefaultWhenMissing`。
    /// </summary>
    [Fact]
    public void ReadOnlyDicts_GetValueOrDefault_ShouldReturnDefaultWhenMissing()
    {
        IReadOnlyDictionary<string, int> dict = new ReadOnlyDictionary<string, int>(
            new Dictionary<string, int> { ["a"] = 1 });
        ReadOnlyDicts.GetValueOrDefault(dict, "missing", 9).ShouldBe(9);
    }
    /// <summary>
    /// 测试用例：验证 `ReadOnlyDicts` 在 `TryGetValueCascading` 场景下，结果为 `ShouldResolveFirstMatch`。
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
    /// 测试用例：验证 `CollConv` 在 `ToIndexedSequence` 场景下，结果为 `ShouldReturnExpectedIndex`。
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
    /// 测试用例：验证 `CollConv` 在 `ToEnumerableAfter` 场景下，结果为 `ShouldContinueFromCurrent`。
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
    /// 测试用例：验证 `CollConv` 在 `AsNullWhenEmpty` 场景下，结果为 `ShouldReturnNullForEmpty`。
    /// </summary>
    [Fact]
    public void CollConv_AsNullWhenEmpty_ShouldReturnNullForEmpty()
    {
        var result = CollConv.AsNullWhenEmpty(Array.Empty<int>());
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `CollConv` 在 `AsNullWhenEmpty` 场景下，结果为 `ShouldReturnEnumerableForNonEmpty`。
    /// </summary>
    [Fact]
    public void CollConv_AsNullWhenEmpty_ShouldReturnEnumerableForNonEmpty()
    {
        var result = CollConv.AsNullWhenEmpty(new[] { 1, 2, 3 });
        result.ShouldNotBeNull();
        result!.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 测试用例：验证 `CollConvShortcut` 在 `ToList_WithPredicate` 场景下，结果为 `ShouldFilter`。
    /// </summary>
    [Fact]
    public void CollConvShortcut_ToList_WithPredicate_ShouldFilter()
    {
        var result = CollConvShortcutExtensions.ToList(new[] { 1, 2, 3, 4 }, x => x % 2 == 0);
        result.ShouldBe(new[] { 2, 4 });
    }
}


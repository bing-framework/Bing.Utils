using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `CollConvAndDictConvBoundary` 相关行为。
/// </summary>
[Trait("CollectionsUT", "CollConvAndDictConv")]
public class CollConvAndDictConvBoundaryTest
{
    /// <summary>
    /// 测试用例：验证 `ToEnumerable` 在 `NullEnumerator` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToEnumerable_NullEnumerator_ThrowsArgumentNullException()
    {
        IEnumerator<int> enumerator = null;
        var ex = Should.Throw<ArgumentNullException>(() => CollConv.ToEnumerable(enumerator).ToArray());
        ex.ParamName.ShouldBe("enumerator");
    }
    /// <summary>
    /// 测试用例：验证 `ToEnumerableAfter` 在 `EnumeratorNotStarted` 场景下，结果为 `CurrentlyThrowsInvalidOperationException`。
    /// </summary>
    [Fact]
    public void ToEnumerableAfter_EnumeratorNotStarted_CurrentlyThrowsInvalidOperationException()
    {
        var enumerator = new[] { 1, 2, 3 }.AsEnumerable().GetEnumerator();
        Should.Throw<InvalidOperationException>(() => CollConv.ToEnumerableAfter(enumerator).ToArray());
    }
    /// <summary>
    /// 测试用例：验证 `AsOptionals` 在 `NullSource` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void AsOptionals_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => CollConv.AsOptionals(source).ToArray());
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 测试用例：验证 `AsEnumerableProxy` 在 `NullSource` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void AsEnumerableProxy_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => CollConv.AsEnumerableProxy(source));
        ex.ParamName.ShouldBe("enumerable");
    }
    /// <summary>
    /// 测试用例：验证 `AsEnumerableProxy` 在 `ValidSource` 场景下，结果为 `EnumeratesAllItems`。
    /// </summary>
    [Fact]
    public void AsEnumerableProxy_ValidSource_EnumeratesAllItems()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };
        var proxy = CollConv.AsEnumerableProxy(source);
        proxy.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 测试用例：验证 `AsNullWhenEmpty` 在 `NullSource` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void AsNullWhenEmpty_NullSource_CurrentlyThrowsNullReferenceException()
    {
        IEnumerable<int> source = null;
        Should.Throw<NullReferenceException>(() => CollConv.AsNullWhenEmpty(source));
    }
    /// <summary>
    /// 测试用例：验证 `AsNullWhenEmpty` 在 `NonEmptySource` 场景下，结果为 `ReturnsSingleUseEnumerable`。
    /// </summary>
    [Fact]
    public void AsNullWhenEmpty_NonEmptySource_ReturnsSingleUseEnumerable()
    {
        IEnumerable<int> source = new[] { 5, 6, 7 };
        var result = CollConv.AsNullWhenEmpty(source);
        result.ShouldNotBeNull();
        result.ToArray().ShouldBe(new[] { 5, 6, 7 });
        Should.Throw<InvalidOperationException>(() => result.ToArray());
    }
    /// <summary>
    /// 测试用例：验证 `ShortcutToList` 在 `WithNullPredicate` 场景下，结果为 `ReturnsAllItems`。
    /// </summary>
    [Fact]
    public void ShortcutToList_WithNullPredicate_ReturnsAllItems()
    {
        var result = CollConvShortcutExtensions.ToList(new[] { 1, 2, 3 }, (Func<int, bool>)null);
        result.ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 测试用例：验证 `DictConvCast` 在 `WithNullSource` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void DictConvCast_WithNullSource_ThrowsArgumentNullException()
    {
        IReadOnlyDictionary<string, string> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => DictConv.Cast<string, string, string, string>(source));
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 测试用例：验证 `DictConvCast` 在 `ReturnsLiveProjectionOfSource` 场景下的行为。
    /// </summary>
    [Fact]
    public void DictConvCast_ReturnsLiveProjectionOfSource()
    {
        var source = new Dictionary<string, string> { ["a"] = "1" };
        var casted = DictConv.Cast<string, string, string, string>(source);
        source["b"] = "2";
        casted.Count.ShouldBe(2);
        casted["b"].ShouldBe("2");
    }
    /// <summary>
    /// 测试用例：验证 `DictConvToDictionary` 在 `HashtableNull` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_HashtableNull_CurrentlyThrowsNullReferenceException()
    {
        Hashtable table = null;
        Should.Throw<NullReferenceException>(() => DictConv.ToDictionary<string, int>(table));
    }
    /// <summary>
    /// 测试用例：验证 `DictConvToDictionary` 在 `WithNullComparer` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_WithNullComparer_ThrowsArgumentNullException()
    {
        var source = new[] { new KeyValuePair<string, int>("a", 1) };
        var ex = Should.Throw<ArgumentNullException>(() => DictConv.ToDictionary(source, null));
        ex.ParamName.ShouldBe("equalityComparer");
    }
    /// <summary>
    /// 测试用例：验证 `DictConvToTuple` 在 `WithNullSource` 场景下，结果为 `CurrentBehaviorDependsOnTargetFramework`。
    /// </summary>
    [Fact]
    public void DictConvToTuple_WithNullSource_CurrentBehaviorDependsOnTargetFramework()
    {
        IDictionary<string, int> source = null;
#if NET6_0_OR_GREATER
        Should.Throw<NullReferenceException>(() => DictConv.ToTuple(source).ToArray());
#else
        var ex = Should.Throw<ArgumentNullException>(() => DictConv.ToTuple(source).ToArray());
        ex.ParamName.ShouldBe("source");
#endif
    }
    /// <summary>
    /// 测试用例：验证 `ReadOnlyDictConv` 在 `ToDictionaryByKeySelector` 场景下，结果为 `ReturnsExpectedReadOnlyDictionary`。
    /// </summary>
    [Fact]
    public void ReadOnlyDictConv_ToDictionaryByKeySelector_ReturnsExpectedReadOnlyDictionary()
    {
        var source = new[] { "ab", "cd" };
        var result = ReadOnlyDictConv.ToDictionary(source, x => x[0]);
        result.Count.ShouldBe(2);
        result['a'].ShouldBe("ab");
        result['c'].ShouldBe("cd");
        result.ShouldBeOfType<ReadOnlyDictionary<char, string>>();
    }
    /// <summary>
    /// 测试用例：验证 `ReadOnlyDictConv` 在 `ToDictionaryWithNullKeySelector` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ReadOnlyDictConv_ToDictionaryWithNullKeySelector_ThrowsArgumentNullException()
    {
        var source = new[] { "ab" };
        var ex = Should.Throw<ArgumentNullException>(() => ReadOnlyDictConv.ToDictionary(source, (Func<string, char>)null));
        ex.ParamName.ShouldBe("keySelector");
    }

    /// <summary>
    /// 测试用例：验证 `DictConv.ToDictionary(Hashtable)` 在 `ValidHashtable` 场景下，结果为 `ReturnsTypedSnapshot`。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_ValidHashtable_ReturnsTypedSnapshot()
    {
        var table = new Hashtable
        {
            ["a"] = 1,
            ["b"] = 2
        };

        var result = DictConv.ToDictionary<string, int>(table);
        table["a"] = 9;

        result.Count.ShouldBe(2);
        result["a"].ShouldBe(1);
        result["b"].ShouldBe(2);
    }

    /// <summary>
    /// 测试用例：验证 `DictConv.ToDictionary(Hashtable)` 在 `InvalidKeyCast` 场景下，结果为 `ThrowsInvalidCastException`。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_HashtableInvalidKeyCast_ThrowsInvalidCastException()
    {
        var table = new Hashtable
        {
            [1] = 10
        };

        Should.Throw<InvalidCastException>(() => DictConv.ToDictionary<string, int>(table));
    }

    /// <summary>
    /// 测试用例：验证 `DictConv.ToDictionary(IEnumerable)` 在 `DuplicateKeys` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_KeyValueWithDuplicateKeys_ThrowsArgumentException()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("a", 2)
        };

        Should.Throw<ArgumentException>(() => DictConv.ToDictionary(source));
    }

    /// <summary>
    /// 测试用例：验证 `DictConv.ToDictionary(IEnumerable, comparer)` 在 `CaseInsensitiveComparer` 场景下，结果为 `UsesComparerForLookup`。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_WithComparer_UsesComparerForLookup()
    {
        var source = new List<KeyValuePair<string, int>>
        {
            new("A", 1),
            new("b", 2)
        };

        var result = DictConv.ToDictionary(source, StringComparer.OrdinalIgnoreCase);
        source.Add(new KeyValuePair<string, int>("c", 3));

        result["a"].ShouldBe(1);
        result["B"].ShouldBe(2);
        result.ContainsKey("c").ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 `DictConv.ToTuple` 在 `ValidSource` 场景下，结果为 `ReturnsAllPairs`。
    /// </summary>
    [Fact]
    public void DictConvToTuple_ValidSource_ReturnsAllPairs()
    {
        IDictionary<string, int> source = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2
        };

        var tuples = DictConv.ToTuple(source).ToList();

        tuples.Count.ShouldBe(2);
        tuples.Any(x => x.Item1 == "a" && x.Item2 == 1).ShouldBeTrue();
        tuples.Any(x => x.Item1 == "b" && x.Item2 == 2).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 `DictConv.ToSortedArrayByValue` 在 `Descending` 场景下，结果为 `ReturnsDescendingByValue`。
    /// </summary>
    [Fact]
    public void DictConvToSortedArrayByValue_Descending_ReturnsDescendingByValue()
    {
        var source = new Dictionary<string, int>
        {
            ["a"] = 2,
            ["b"] = 5,
            ["c"] = 1
        };

        var result = DictConv.ToSortedArrayByValue(source, asc: false);

        result.Select(x => x.Value).ShouldBe(new[] { 5, 2, 1 });
    }

    /// <summary>
    /// 测试用例：验证 `DictConv.ToSortedArrayByKey` 在 `UnorderedKeys` 场景下，结果为 `ReturnsAscendingByKey`。
    /// </summary>
    [Fact]
    public void DictConvToSortedArrayByKey_UnorderedKeys_ReturnsAscendingByKey()
    {
        var source = new Dictionary<string, int>
        {
            ["b"] = 2,
            ["a"] = 1,
            ["c"] = 3
        };

        var result = DictConv.ToSortedArrayByKey(source);

        result.Select(x => x.Key).ShouldBe(new[] { "a", "b", "c" });
    }
}


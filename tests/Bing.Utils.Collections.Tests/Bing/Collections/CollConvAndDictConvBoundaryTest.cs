using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 集合与字典转换的空值、快照和排序契约测试。
/// </summary>
[Trait("CollectionsUT", "CollConvAndDictConv")]
public class CollConvAndDictConvBoundaryTest
{
    /// <summary>
    /// 验证枚举器转换拒绝空引用并报告参数名。
    /// </summary>
    [Fact]
    public void ToEnumerable_NullEnumerator_ThrowsArgumentNullException()
    {
        IEnumerator<int> enumerator = null;
        var ex = Should.Throw<ArgumentNullException>(() => CollConv.ToEnumerable(enumerator).ToArray());
        ex.ParamName.ShouldBe("enumerator");
    }
    /// <summary>
    /// 验证尚未开始的枚举器不能从当前位置转换。
    /// </summary>
    [Fact]
    public void ToEnumerableAfter_EnumeratorNotStarted_CurrentlyThrowsInvalidOperationException()
    {
        var enumerator = new[] { 1, 2, 3 }.AsEnumerable().GetEnumerator();
        Should.Throw<InvalidOperationException>(() => CollConv.ToEnumerableAfter(enumerator).ToArray());
    }
    /// <summary>
    /// 验证可选值序列转换拒绝空源并报告参数名。
    /// </summary>
    [Fact]
    public void AsOptionals_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => CollConv.AsOptionals(source).ToArray());
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 验证可枚举代理转换拒绝空源并报告参数名。
    /// </summary>
    [Fact]
    public void AsEnumerableProxy_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => CollConv.AsEnumerableProxy(source));
        ex.ParamName.ShouldBe("enumerable");
    }
    /// <summary>
    /// 验证可枚举代理保留全部源元素。
    /// </summary>
    [Fact]
    public void AsEnumerableProxy_ValidSource_EnumeratesAllItems()
    {
        IEnumerable<int> source = new[] { 1, 2, 3 };
        var proxy = CollConv.AsEnumerableProxy(source);
        proxy.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 验证当前实现对空源引用抛出空引用异常。
    /// </summary>
    [Fact]
    public void AsNullWhenEmpty_NullSource_CurrentlyThrowsNullReferenceException()
    {
        IEnumerable<int> source = null;
        Should.Throw<NullReferenceException>(() => CollConv.AsNullWhenEmpty(source));
    }
    /// <summary>
    /// 验证非空转换结果只能完整枚举一次。
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
    /// 验证列表转换的空谓词保留全部元素。
    /// </summary>
    [Fact]
    public void ShortcutToList_WithNullPredicate_ReturnsAllItems()
    {
        var result = CollConvShortcutExtensions.ToList(new[] { 1, 2, 3 }, (Func<int, bool>)null);
        result.ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 验证字典投影转换拒绝空源并报告参数名。
    /// </summary>
    [Fact]
    public void DictConvCast_WithNullSource_ThrowsArgumentNullException()
    {
        IReadOnlyDictionary<string, string> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => DictConv.Cast<string, string, string, string>(source));
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 验证字典投影反映源字典后续追加的数据。
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
    /// 验证当前哈希表转换对空引用抛出空引用异常。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_HashtableNull_CurrentlyThrowsNullReferenceException()
    {
        Hashtable table = null;
        Should.Throw<NullReferenceException>(() => DictConv.ToDictionary<string, int>(table));
    }
    /// <summary>
    /// 验证字典转换拒绝空比较器并报告参数名。
    /// </summary>
    [Fact]
    public void DictConvToDictionary_WithNullComparer_ThrowsArgumentNullException()
    {
        var source = new[] { new KeyValuePair<string, int>("a", 1) };
        var ex = Should.Throw<ArgumentNullException>(() => DictConv.ToDictionary(source, null));
        ex.ParamName.ShouldBe("equalityComparer");
    }
    /// <summary>
    /// 验证空字典转换为元组时按目标框架抛出对应异常。
    /// </summary>
    /// <remarks>.NET 6 及以上版本抛出空引用异常；其他目标框架抛出参数空值异常并报告源参数名。</remarks>
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
    /// 验证按键选择器创建只读字典。
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
    /// 验证只读字典转换拒绝空键选择器并报告参数名。
    /// </summary>
    [Fact]
    public void ReadOnlyDictConv_ToDictionaryWithNullKeySelector_ThrowsArgumentNullException()
    {
        var source = new[] { "ab" };
        var ex = Should.Throw<ArgumentNullException>(() => ReadOnlyDictConv.ToDictionary(source, (Func<string, char>)null));
        ex.ParamName.ShouldBe("keySelector");
    }

    /// <summary>
    /// 验证哈希表转换生成强类型字典快照。
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
    /// 验证哈希表键类型不兼容时抛出转换异常。
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
    /// 验证重复键的键值对序列不能转换为字典。
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
    /// 验证转换字典使用指定比较器且不受源列表后续追加影响。
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
    /// 验证字典键值对全部转换为元组。
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
    /// 验证按字典值降序生成键值对数组。
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
    /// 验证按字典键升序生成键值对数组。
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


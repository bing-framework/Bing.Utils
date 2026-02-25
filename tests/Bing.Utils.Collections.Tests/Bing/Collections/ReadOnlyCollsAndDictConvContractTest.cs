using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bing.Collections;

/// <summary>
/// 测试类：验证 ReadOnlyColls 与 ReadOnlyDictConv 的契约行为。
/// </summary>
[Trait("CollectionsUT", "ReadOnlyColls.ReadOnlyDictConv.Contract")]
public class ReadOnlyCollsAndDictConvContractTest
{
    /// <summary>
    /// 测试用例：当 source 为空时，Append 应抛出参数异常。
    /// </summary>
    [Fact]
    public void Append_SourceIsNull_ThrowsArgumentNullException()
    {
        IReadOnlyCollection<int> source = null;

        var ex = Should.Throw<ArgumentNullException>(() => ReadOnlyColls.Append(source, 1));

        ex.ParamName.ShouldBe("source");
    }

    /// <summary>
    /// 测试用例：Append 应保持原顺序并在末尾追加元素，且不修改输入集合。
    /// </summary>
    [Fact]
    public void Append_ValidSource_ReturnsCollectionWithAppendedItemAndKeepsSourceUnchanged()
    {
        var source = new List<int> { 1, 2 };

        var result = ReadOnlyColls.Append(source, 3);

        source.ShouldBe(new[] { 1, 2 });
        result.Count.ShouldBe(3);
        result.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 测试用例：扩展方法 Append 与静态方法行为应一致。
    /// </summary>
    [Fact]
    public void Append_ExtensionMethodCalled_ReturnsSameExpectedSequence()
    {
        IReadOnlyCollection<string> source = new[] { "A", "B" };

        var result = source.Append("C");

        result.Count.ShouldBe(3);
        result.ToArray().ShouldBe(new[] { "A", "B", "C" });
    }

    /// <summary>
    /// 测试用例：Empty 应返回同类型单例且为空集合。
    /// </summary>
    [Fact]
    public void Empty_CalledMultipleTimes_ReturnsSameSingletonInstance()
    {
        var first = ReadOnlyColls.Empty<int>();
        var second = ReadOnlyColls.Empty<int>();

        ReferenceEquals(first, second).ShouldBeTrue();
        first.Count.ShouldBe(0);
    }

    /// <summary>
    /// 测试用例：OfList(params) 应返回只读列表并保持元素顺序。
    /// </summary>
    [Fact]
    public void OfList_ParamsValues_ReturnsReadOnlyListInOriginalOrder()
    {
        var result = ReadOnlyColls.OfList(2, 4, 6);

        result.ShouldBe(new[] { 2, 4, 6 });
        var writable = (IList<int>)result;
        Should.Throw<NotSupportedException>(() => writable.Add(8));
    }

    /// <summary>
    /// 测试用例：ToDictionary(键值对) 遇到重复键时应抛出异常。
    /// </summary>
    [Fact]
    public void ToDictionary_KeyValuePairsWithDuplicateKey_ThrowsArgumentException()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("a", 2)
        };

        Should.Throw<ArgumentException>(() => ReadOnlyDictConv.ToDictionary(source));
    }

    /// <summary>
    /// 测试用例：ToDictionary(键值对, comparer) 应使用比较器进行键匹配。
    /// </summary>
    [Fact]
    public void ToDictionary_KeyValuePairsWithComparer_UsesComparerForLookup()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("A", 1),
            new KeyValuePair<string, int>("b", 2)
        };

        var result = ReadOnlyDictConv.ToDictionary(source, StringComparer.OrdinalIgnoreCase);

        result["a"].ShouldBe(1);
        result["B"].ShouldBe(2);
        result.ShouldBeOfType<ReadOnlyDictionary<string, int>>();
    }

    /// <summary>
    /// 测试用例：ToDictionary(source, keySelector, elementSelector) 当 elementSelector 为空时应抛出参数异常。
    /// </summary>
    [Fact]
    public void ToDictionary_SourceWithNullElementSelector_ThrowsArgumentNullException()
    {
        var source = new[] { "x" };

        var ex = Should.Throw<ArgumentNullException>(() =>
            ReadOnlyDictConv.ToDictionary(source, x => x, (Func<string, int>)null));

        ex.ParamName.ShouldBe("elementSelector");
    }

    /// <summary>
    /// 测试用例：ToDictionary(source, keySelector) 结果应为快照，不受源集合后续修改影响。
    /// </summary>
    [Fact]
    public void ToDictionary_KeySelectorOverMutableSource_ResultShouldRemainSnapshot()
    {
        var source = new List<string> { "ab", "cd" };
        var result = ReadOnlyDictConv.ToDictionary(source, x => x[0]);

        source[0] = "zz";

        result['a'].ShouldBe("ab");
        result['c'].ShouldBe("cd");
    }

    /// <summary>
    /// 测试用例：ToDictionary(source, keySelector, comparer) 当 comparer 为空时应抛出参数异常。
    /// </summary>
    [Fact]
    public void ToDictionary_KeySelectorWithNullComparer_ThrowsArgumentNullException()
    {
        var source = new[] { "ab", "cd" };

        var ex = Should.Throw<ArgumentNullException>(() =>
            ReadOnlyDictConv.ToDictionary(source, x => x[0], comparer: null));

        ex.ParamName.ShouldBe("comparer");
    }

    /// <summary>
    /// 测试用例：ToDictionary(source, keySelector, elementSelector, comparer) 当 comparer 为空时应抛出参数异常。
    /// </summary>
    [Fact]
    public void ToDictionary_KeyElementWithNullComparer_ThrowsArgumentNullException()
    {
        var source = new[] { "ab", "cd" };

        var ex = Should.Throw<ArgumentNullException>(() =>
            ReadOnlyDictConv.ToDictionary(source, x => x[0], x => x.Length, comparer: null));

        ex.ParamName.ShouldBe("comparer");
    }

    /// <summary>
    /// 测试用例：ToDictionary(source, keySelector, elementSelector) 遇到重复键时应抛出异常。
    /// </summary>
    [Fact]
    public void ToDictionary_KeyElementWithDuplicateKeys_ThrowsArgumentException()
    {
        var source = new[] { "ax", "ay" };

        Should.Throw<ArgumentException>(() =>
            ReadOnlyDictConv.ToDictionary(source, x => x[0], x => x.Length));
    }

    /// <summary>
    /// 测试用例：ToDictionary(source, keySelector, elementSelector) 返回结果应为只读字典且禁止写入。
    /// </summary>
    [Fact]
    public void ToDictionary_KeyElementSelector_ReturnsReadOnlyDictionary()
    {
        var source = new[] { "ab", "cd" };

        var result = ReadOnlyDictConv.ToDictionary(source, x => x[0], x => x.ToUpperInvariant());

        result['a'].ShouldBe("AB");
        result['c'].ShouldBe("CD");
        result.ShouldBeOfType<ReadOnlyDictionary<char, string>>();
        Should.Throw<NotSupportedException>(() => ((IDictionary<char, string>)result).Add('e', "EF"));
    }
}

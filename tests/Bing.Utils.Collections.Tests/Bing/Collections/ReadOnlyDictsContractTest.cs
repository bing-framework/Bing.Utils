using System;
using System.Collections.Generic;

namespace Bing.Collections;

/// <summary>
/// 只读字典默认值、级联查找和空字典契约测试。
/// </summary>
[Trait("CollectionsUT", "ReadOnlyDicts.Contract")]
public class ReadOnlyDictsContractTest
{
    /// <summary>
    /// 验证空字典返回指定默认值。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_NullDictionary_ReturnsDefaultValue()
    {
        IReadOnlyDictionary<string, int> dictionary = null;

        var result = ReadOnlyDicts.GetValueOrDefault(dictionary, "id", 9);

        result.ShouldBe(9);
    }

    /// <summary>
    /// 验证空字典未指定默认值时返回类型默认值。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_NullDictionaryWithoutExplicitDefault_ReturnsTypeDefault()
    {
        IReadOnlyDictionary<string, int> dictionary = null;

        var result = ReadOnlyDicts.GetValueOrDefault(dictionary, "id");

        result.ShouldBe(0);
    }

    /// <summary>
    /// 验证空字典调用默认值工厂一次并返回计算值。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_NullDictionaryWithCalculator_InvokesCalculatorAndReturnsValue()
    {
        IReadOnlyDictionary<string, int> dictionary = null;
        var invokeCount = 0;

        var result = ReadOnlyDicts.GetValueOrDefault(dictionary, "x", key =>
        {
            invokeCount++;
            key.ShouldBe("x");
            return 11;
        });

        result.ShouldBe(11);
        invokeCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证缺失键调用默认值工厂一次。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_MissingKeyWithCalculator_InvokesCalculatorOnce()
    {
        var invokeCount = 0;
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["a"] = 1
        };

        var result = ReadOnlyDicts.GetValueOrDefault(dictionary, "b", _ =>
        {
            invokeCount++;
            return 42;
        });

        result.ShouldBe(42);
        invokeCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证已有键返回原值且不调用默认值工厂。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_ExistingKeyWithCalculator_DoesNotInvokeCalculator()
    {
        var invokeCount = 0;
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            ["a"] = 7
        };

        var result = ReadOnlyDicts.GetValueOrDefault(dictionary, "a", _ =>
        {
            invokeCount++;
            return 99;
        });

        result.ShouldBe(7);
        invokeCount.ShouldBe(0);
    }

    /// <summary>
    /// 验证缺失键且工厂为空时返回类型默认值。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_MissingKeyAndNullCalculator_ReturnsTypeDefault()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>();

        var result = ReadOnlyDicts.GetValueOrDefault<string, int>(dictionary, "missing", null);

        result.ShouldBe(0);
    }

    /// <summary>
    /// 验证级联查找拒绝空字典集合并报告参数名。
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_NullCollection_ThrowsArgumentNullException()
    {
        IEnumerable<IReadOnlyDictionary<string, int>> coll = null;

        var exception = Should.Throw<ArgumentNullException>(() =>
            ReadOnlyDicts.GetValueOrDefaultCascading(coll, "id", -1));

        exception.ParamName.ShouldBe("dictionaryColl");
    }

    /// <summary>
    /// 验证级联查找未命中时返回指定默认值。
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_MissingKey_ReturnsProvidedDefaultValue()
    {
        IReadOnlyDictionary<string, int> first = new Dictionary<string, int> { ["a"] = 1 };
        IReadOnlyDictionary<string, int> second = new Dictionary<string, int> { ["b"] = 2 };

        var result = ReadOnlyDicts.GetValueOrDefaultCascading(new[] { first, second }, "id", -1);

        result.ShouldBe(-1);
    }

    /// <summary>
    /// 验证级联查找未命中且未指定默认值时返回类型默认值。
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_MissingKeyAndNoExplicitDefault_ReturnsTypeDefault()
    {
        IReadOnlyDictionary<string, int> first = new Dictionary<string, int> { ["a"] = 1 };

        var result = ReadOnlyDicts.GetValueOrDefaultCascading(new[] { first }, "id");

        result.ShouldBe(0);
    }

    /// <summary>
    /// 验证级联查找返回首个匹配的值。
    /// </summary>
    [Fact]
    public void TryGetValueCascading_MultipleDictionaries_ReturnsFirstMatchedValue()
    {
        IReadOnlyDictionary<string, int> first = new Dictionary<string, int> { ["id"] = 10 };
        IReadOnlyDictionary<string, int> second = new Dictionary<string, int> { ["id"] = 20 };

        var found = ReadOnlyDicts.TryGetValueCascading(new[] { first, second }, "id", out var value);

        found.ShouldBeTrue();
        value.ShouldBe(10);
    }

    /// <summary>
    /// 验证级联查找未命中时返回失败及类型默认值。
    /// </summary>
    [Fact]
    public void TryGetValueCascading_MissingKey_ReturnsFalseAndDefault()
    {
        IReadOnlyDictionary<string, int> first = new Dictionary<string, int> { ["a"] = 1 };
        IReadOnlyDictionary<string, int> second = new Dictionary<string, int> { ["b"] = 2 };

        var found = ReadOnlyDicts.TryGetValueCascading(new[] { first, second }, "id", out var value);

        found.ShouldBeFalse();
        value.ShouldBe(0);
    }

    /// <summary>
    /// 验证当前级联查找遍历到空字典时抛出空引用异常。
    /// </summary>
    [Fact]
    public void TryGetValueCascading_CollectionContainsNullDictionary_CurrentlyThrowsNullReferenceException()
    {
        IReadOnlyDictionary<string, int> first = new Dictionary<string, int> { ["a"] = 1 };
        IEnumerable<IReadOnlyDictionary<string, int>> coll = new IReadOnlyDictionary<string, int>[]
        {
            first,
            null
        };

        Should.Throw<NullReferenceException>(() => ReadOnlyDicts.TryGetValueCascading(coll, "id", out _));
    }

    /// <summary>
    /// 验证级联查找命中后停止遍历并跳过后续空字典。
    /// </summary>
    [Fact]
    public void TryGetValueCascading_HitBeforeNullDictionary_ShortCircuitsAndReturnsMatchedValue()
    {
        IReadOnlyDictionary<string, int> first = new Dictionary<string, int> { ["id"] = 7 };
        IEnumerable<IReadOnlyDictionary<string, int>> coll = new IReadOnlyDictionary<string, int>[]
        {
            first,
            null
        };

        var found = ReadOnlyDicts.TryGetValueCascading(coll, "id", out var value);

        found.ShouldBeTrue();
        value.ShouldBe(7);
    }

    /// <summary>
    /// 验证空字典只读且拒绝写入。
    /// </summary>
    [Fact]
    public void Empty_ReadonlyContract_ReturnsReadOnlyDictionaryAndBlocksWrites()
    {
        var empty = ReadOnlyDicts.Empty<string, int>();

        empty.Count.ShouldBe(0);
        empty.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyDictionary<string, int>>();
        Should.Throw<NotSupportedException>(() => ((IDictionary<string, int>)empty).Add("a", 1));
    }
}

using System;
using System.Collections.Generic;

namespace Bing.Collections;

/// <summary>
/// 测试类：覆盖 `ReadOnlyDicts` 相关行为。
/// </summary>
[Trait("CollectionsUT", "ReadOnlyDicts.Contract")]
public class ReadOnlyDictsContractTest
{
    /// <summary>
    /// 测试用例：验证 `GetValueOrDefault` 在 `NullDictionary` 场景下，结果为 `ReturnsDefaultValue`。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_NullDictionary_ReturnsDefaultValue()
    {
        IReadOnlyDictionary<string, int> dictionary = null;

        var result = ReadOnlyDicts.GetValueOrDefault(dictionary, "id", 9);

        result.ShouldBe(9);
    }

    /// <summary>
    /// 测试用例：验证 `GetValueOrDefault` 在 `NullDictionaryWithoutExplicitDefault` 场景下，结果为 `ReturnsTypeDefault`。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_NullDictionaryWithoutExplicitDefault_ReturnsTypeDefault()
    {
        IReadOnlyDictionary<string, int> dictionary = null;

        var result = ReadOnlyDicts.GetValueOrDefault(dictionary, "id");

        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试用例：验证 `GetValueOrDefault` 在 `NullDictionaryWithCalculator` 场景下，结果为 `InvokesCalculatorAndReturnsValue`。
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
    /// 测试用例：验证 `GetValueOrDefault` 在 `MissingKeyWithCalculator` 场景下，结果为 `InvokesCalculatorOnce`。
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
    /// 测试用例：验证 `GetValueOrDefault` 在 `ExistingKeyWithCalculator` 场景下，结果为 `DoesNotInvokeCalculator`。
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
    /// 测试用例：验证 `GetValueOrDefault` 在 `MissingKeyAndNullCalculator` 场景下，结果为 `ReturnsTypeDefault`。
    /// </summary>
    [Fact]
    public void GetValueOrDefault_MissingKeyAndNullCalculator_ReturnsTypeDefault()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>();

        var result = ReadOnlyDicts.GetValueOrDefault<string, int>(dictionary, "missing", null);

        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试用例：验证 `GetValueOrDefaultCascading` 在 `NullCollection` 场景下，结果为 `ThrowsArgumentNullException`。
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
    /// 测试用例：验证 `GetValueOrDefaultCascading` 在 `MissingKey` 场景下，结果为 `ReturnsProvidedDefaultValue`。
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
    /// 测试用例：验证 `GetValueOrDefaultCascading` 在 `MissingKeyAndNoExplicitDefault` 场景下，结果为 `ReturnsTypeDefault`。
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_MissingKeyAndNoExplicitDefault_ReturnsTypeDefault()
    {
        IReadOnlyDictionary<string, int> first = new Dictionary<string, int> { ["a"] = 1 };

        var result = ReadOnlyDicts.GetValueOrDefaultCascading(new[] { first }, "id");

        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试用例：验证 `TryGetValueCascading` 在 `MultipleDictionaries` 场景下，结果为 `ReturnsFirstMatchedValue`。
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
    /// 测试用例：验证 `TryGetValueCascading` 在 `MissingKey` 场景下，结果为 `ReturnsFalseAndDefault`。
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
    /// 测试用例：验证 `TryGetValueCascading` 在 `CollectionContainsNullDictionary` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
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
    /// 测试用例：验证 `TryGetValueCascading` 在 `HitBeforeNullDictionary` 场景下，结果为 `ShortCircuitsAndReturnsMatchedValue`。
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
    /// 测试用例：验证 `Empty` 在 `ReadonlyContract` 场景下，结果为 `ReturnsReadOnlyDictionaryAndBlocksWrites`。
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

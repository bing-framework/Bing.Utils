using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 测试类：DictConvExtensions 字典转换扩展方法（Cast / ToDictionary / ToTuple / ToSortedArray 系列）
/// </summary>
[Trait("CollUT", "DictConvExtensions")]
public class DictConvExtensionsTest
{
    #region ToDictionary(IEnumerable<KeyValuePair>) — KVP 集合转字典

    /// <summary>
    /// 测试目的：从 KeyValuePair 集合转换的字典应包含所有键值对
    /// </summary>
    [Fact]
    public void ToDictionary_FromKvpEnumerable_ContainsAllPairs()
    {
        // Arrange
        var pairs = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("c", 3),
        };

        // Act
        var result = DictConvExtensions.ToDictionary<string, int>(pairs);

        // Assert
        result.Count.ShouldBe(3);
        result["a"].ShouldBe(1);
        result["c"].ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：从空 KVP 集合转换应返回空字典
    /// </summary>
    [Fact]
    public void ToDictionary_FromEmptyKvpEnumerable_ReturnsEmptyDict()
    {
        // Arrange
        var pairs = Array.Empty<KeyValuePair<string, int>>();

        // Act
        var result = DictConvExtensions.ToDictionary<string, int>(pairs);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region ToDictionary(Hashtable) — Hashtable 转字典

    /// <summary>
    /// 测试目的：从 Hashtable 转换应返回包含所有键值对的强类型字典
    /// </summary>
    [Fact]
    public void ToDictionary_FromHashtable_ContainsAllPairs()
    {
        // Arrange
        var hashtable = new Hashtable
        {
            { "key1", 100 },
            { "key2", 200 },
        };

        // Act
        var result = hashtable.ToDictionary<string, int>();

        // Assert
        result.Count.ShouldBe(2);
        result["key1"].ShouldBe(100);
        result["key2"].ShouldBe(200);
    }

    #endregion

    #region ToSortedArrayByKey — 按键排序

    /// <summary>
    /// 测试目的：ToSortedArrayByKey 应返回按键升序排列的 KeyValuePair 列表
    /// </summary>
    [Fact]
    public void ToSortedArrayByKey_UnorderedDict_SortsAscendingByKey()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "c", 3 },
            { "a", 1 },
            { "b", 2 },
        };

        // Act
        var result = dict.ToSortedArrayByKey();

        // Assert
        result.Select(kv => kv.Key).ShouldBe(new[] { "a", "b", "c" });
        result[0].Value.ShouldBe(1);
    }

    /// <summary>
    /// 测试目的：ToSortedArrayByKey 对空字典应返回空列表
    /// </summary>
    [Fact]
    public void ToSortedArrayByKey_EmptyDict_ReturnsEmptyList()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var result = dict.ToSortedArrayByKey();

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：ToSortedArrayByKey 对整数键字典应按数字大小升序排列
    /// </summary>
    [Fact]
    public void ToSortedArrayByKey_IntKeys_SortsNumerically()
    {
        // Arrange
        var dict = new Dictionary<int, string>
        {
            { 5, "five" },
            { 1, "one" },
            { 3, "three" },
        };

        // Act
        var result = dict.ToSortedArrayByKey();

        // Assert
        result.Select(kv => kv.Key).ShouldBe(new[] { 1, 3, 5 });
    }

    #endregion

    #region ToSortedArrayByValue — 按值排序

    /// <summary>
    /// 测试目的：ToSortedArrayByValue(asc=true) 应返回按值升序排列的列表
    /// </summary>
    [Fact]
    public void ToSortedArrayByValue_Ascending_SortsByValueAsc()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "c", 30 },
            { "a", 10 },
            { "b", 20 },
        };

        // Act
        var result = DictConvExtensions.ToSortedArrayByValue(dict, asc: true);

        // Assert
        result.Select(kv => kv.Value).ShouldBe(new[] { 10, 20, 30 });
    }

    /// <summary>
    /// 测试目的：ToSortedArrayByValue(asc=false) 应返回按值降序排列的列表
    /// </summary>
    [Fact]
    public void ToSortedArrayByValue_Descending_SortsByValueDesc()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "a", 10 },
            { "b", 30 },
            { "c", 20 },
        };

        // Act
        var result = DictConvExtensions.ToSortedArrayByValue(dict, asc: false);

        // Assert
        result.Select(kv => kv.Value).ShouldBe(new[] { 30, 20, 10 });
    }

    #endregion

    #region ToTuple — 字典转元组集合

    /// <summary>
    /// 测试目的：ToTuple 应将字典中每个键值对转换为 Tuple&lt;TKey, TValue&gt;
    /// </summary>
    [Fact]
    public void ToTuple_FromDict_ReturnsAllTuples()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "x", 1 },
            { "y", 2 },
        };

        // Act
        var result = DictConvExtensions.ToTuple(dict).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(t => t.Item1 == "x" && t.Item2 == 1);
        result.ShouldContain(t => t.Item1 == "y" && t.Item2 == 2);
    }

    /// <summary>
    /// 测试目的：ToTuple 对空字典应返回空集合
    /// </summary>
    [Fact]
    public void ToTuple_EmptyDict_ReturnsEmptyCollection()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var result = DictConvExtensions.ToTuple(dict).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion
}

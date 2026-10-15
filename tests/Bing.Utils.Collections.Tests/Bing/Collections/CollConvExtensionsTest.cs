using System;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 枚举器、索引序列、排序数组和哈希集合转换测试。
/// </summary>
[Trait("CollUT", "CollConvExtensions")]
public class CollConvExtensionsTest
{
    #region ToEnumerable — IEnumerator 转 IEnumerable

    /// <summary>
    /// 验证ToEnumerable 应将 IEnumerator 转换为可枚举集合并返回所有元素
    /// </summary>
    [Fact]
    public void ToEnumerable_ValidEnumerator_ReturnsAllElements()
    {
        // Arrange
        var enumerator = new List<int> { 1, 2, 3 }.GetEnumerator();

        // Act
        var result = enumerator.ToEnumerable().ToList();

        // Assert
        result.ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 验证ToEnumerable 对 null 枚举器应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void ToEnumerable_NullEnumerator_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerator<int> enumerator = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => enumerator.ToEnumerable().ToArray());
    }

    #endregion

    #region ToEnumerableAfter — 从当前位置起转换

    /// <summary>
    /// 验证从当前位置转换的结果非空且包含当前元素。
    /// </summary>
    [Fact]
    public void ToEnumerableAfter_EnumeratorAdvanced_ReturnsRemainingElements()
    {
        // Arrange: 先将枚举器推进到第 1 个元素（index 0）
        var list = new List<int> { 10, 20, 30 };
        var enumerator = list.GetEnumerator();
        enumerator.MoveNext(); // 现在 Current = 10

        // Act: ToEnumerableAfter 从 Current 开始（包含 Current）
        var result = enumerator.ToEnumerableAfter().ToList();

        // Assert
        result.ShouldContain(10);
        result.Count.ShouldBeGreaterThan(0);
    }

    #endregion

    #region ToIndexedSequence — 带索引序列

    /// <summary>
    /// 验证ToIndexedSequence 应为每个元素附上从 0 开始的索引
    /// </summary>
    [Fact]
    public void ToIndexedSequence_SimpleList_AddsCorrectIndices()
    {
        // Arrange
        var list = new[] { "a", "b", "c" };

        // Act
        var result = list.ToIndexedSequence().ToList();

        // Assert
        result.Count.ShouldBe(3);
        result[0].Key.ShouldBe(0);
        result[0].Value.ShouldBe("a");
        result[1].Key.ShouldBe(1);
        result[1].Value.ShouldBe("b");
        result[2].Key.ShouldBe(2);
        result[2].Value.ShouldBe("c");
    }

    /// <summary>
    /// 验证ToIndexedSequence 对空集合应返回空序列
    /// </summary>
    [Fact]
    public void ToIndexedSequence_EmptyList_ReturnsEmpty()
    {
        // Act
        var result = Array.Empty<string>().ToIndexedSequence().ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 验证ToIndexedSequence 对 null 应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void ToIndexedSequence_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<int> source = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => source.ToIndexedSequence().ToArray());
    }

    #endregion

    #region ToSortedArray — 有序数组

    /// <summary>
    /// 验证ToSortedArray() 对实现 IComparable&lt;T&gt; 的类型应升序排列
    /// </summary>
    [Fact]
    public void ToSortedArray_WithoutComparer_SortsAscending()
    {
        // Arrange
        var list = new[] { 5, 3, 1, 4, 2 };

        // Act
        var result = list.ToSortedArray();

        // Assert
        result.ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    /// <summary>
    /// 验证ToSortedArray(Comparison) 应按照提供的比较函数排列
    /// </summary>
    [Fact]
    public void ToSortedArray_WithComparison_SortsDescending()
    {
        // Arrange: 降序排列
        var list = new[] { 1, 3, 5, 2, 4 };

        // Act
        var result = list.ToSortedArray((a, b) => b.CompareTo(a));

        // Assert
        result.ShouldBe(new[] { 5, 4, 3, 2, 1 });
    }

    /// <summary>
    /// 验证ToSortedArray 对单元素集合应返回该元素
    /// </summary>
    [Fact]
    public void ToSortedArray_SingleElement_ReturnsSingleElement()
    {
        // Act
        var result = new[] { 42 }.ToSortedArray();

        // Assert
        result.ShouldBe(new[] { 42 });
    }

    #endregion

    #region ToHashSet — HashSet 转换

    /// <summary>
    /// 验证未要求预先去重时生成包含全部不同元素的哈希集合。
    /// </summary>
    [Fact]
    public void ToHashSet_WithoutIgnoreDup_CreatesHashSet()
    {
        // Arrange
        var list = new[] { 1, 2, 3 };

        // Act
        var result = list.ToHashSet(false);

        // Assert
        result.ShouldBeOfType<HashSet<int>>();
        result.Count.ShouldBe(3);
    }

    /// <summary>
    /// 验证ToHashSet(ignoreDup=true) 应去除重复元素后创建 HashSet
    /// </summary>
    [Fact]
    public void ToHashSet_IgnoreDup_RemovesDuplicates()
    {
        // Arrange
        var list = new[] { 1, 2, 2, 3, 3, 3 };

        // Act
        var result = list.ToHashSet(true);

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain(1);
        result.ShouldContain(2);
        result.ShouldContain(3);
    }

    /// <summary>
    /// 验证ToHashSet(keyFunc) 应按投影键创建 HashSet&lt;TKey&gt;
    /// </summary>
    [Fact]
    public void ToHashSet_WithKeyFunc_ProjectsToKeys()
    {
        // Arrange
        var list = new[] { "apple", "banana", "cherry" };

        // Act: 将字符串长度作为 key 创建 HashSet
        var result = list.ToHashSet(s => s.Length);

        // Assert: 长度分别为 5, 6, 6 → 只有 5, 6 两种 (6 重复，但 HashSet 去重)
        result.Count.ShouldBe(2);
    }

    /// <summary>
    /// 验证ToHashSet(keyFunc) 对 null keyFunc 应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void ToHashSet_NullKeyFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var list = new[] { "a", "b" };
        Func<string, int> keyFunc = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => list.ToHashSet(keyFunc));
    }

    #endregion

    #region ToHashSetIgnoringDuplicates — 忽略重复转换

    /// <summary>
    /// 验证ToHashSetIgnoringDuplicates 应去除重复项后创建 HashSet
    /// </summary>
    [Fact]
    public void ToHashSetIgnoringDuplicates_WithDuplicates_RemovesAll()
    {
        // Arrange
        var list = new[] { 1, 1, 2, 2, 3 };

        // Act
        var result = list.ToHashSetIgnoringDuplicates();

        // Assert
        result.Count.ShouldBe(3);
    }

    /// <summary>
    /// 验证无重复元素转换后保留原元素数量。
    /// </summary>
    [Fact]
    public void ToHashSetIgnoringDuplicates_NoDuplicates_ReturnsAll()
    {
        // Arrange
        var list = new[] { 10, 20, 30 };

        // Act
        var result = list.ToHashSetIgnoringDuplicates();

        // Assert
        result.Count.ShouldBe(3);
    }

    #endregion
}

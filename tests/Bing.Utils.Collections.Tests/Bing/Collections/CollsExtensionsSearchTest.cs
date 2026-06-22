using System;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 测试类：CollsExtensions.Search — BinarySearch 扩展方法系列
/// </summary>
[Trait("CollUT", "CollsExtensions.Search")]
public class CollsExtensionsSearchTest
{
    // 辅助：有序整数列表用于测试
    private static readonly IList<int> SortedInts = new List<int> { 1, 3, 5, 7, 9, 11 };

    // 辅助类
    private record Item(int Id, string Name);

    private static readonly IList<Item> SortedItems = new List<Item>
    {
        new(10, "a"),
        new(20, "b"),
        new(30, "c"),
        new(40, "d"),
        new(50, "e"),
    };

    #region BinarySearch(source, value) — 基础查找

    /// <summary>
    /// 测试目的：BinarySearch 对有序集合中存在的元素应返回正确索引
    /// </summary>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 2)]
    [InlineData(11, 5)]
    public void BinarySearch_ExistingValue_ReturnsCorrectIndex(int value, int expectedIndex)
    {
        // Act
        var result = SortedInts.BinarySearch(value);

        // Assert
        result.ShouldBe(expectedIndex);
    }

    /// <summary>
    /// 测试目的：BinarySearch 对不存在的值应返回负数（插入点的按位补）
    /// </summary>
    [Fact]
    public void BinarySearch_MissingValue_ReturnsNegative()
    {
        // Act: 6 不在列表中，位于 5(index=2) 和 7(index=3) 之间
        var result = SortedInts.BinarySearch(6);

        // Assert
        result.ShouldBeLessThan(0);
    }

    #endregion

    #region BinarySearch(source, map, value) — 带映射函数

    /// <summary>
    /// 测试目的：BinarySearch 带映射函数应在投影后的有序集合中正确查找
    /// </summary>
    [Fact]
    public void BinarySearch_WithMapFunc_FindsCorrectElement()
    {
        // Arrange: 按 Id 进行二分查找
        var result = SortedItems.BinarySearch(x => x.Id, 30);

        // Assert: Id=30 在索引 2
        result.ShouldBe(2);
    }

    /// <summary>
    /// 测试目的：BinarySearch 带映射函数对不存在的值应返回负数
    /// </summary>
    [Fact]
    public void BinarySearch_WithMapFunc_MissingValue_ReturnsNegative()
    {
        // Act
        var result = SortedItems.BinarySearch(x => x.Id, 25);

        // Assert
        result.ShouldBeLessThan(0);
    }

    #endregion

    #region BinarySearch(source, index, length, value) — 带范围

    /// <summary>
    /// 测试目的：BinarySearch 在指定范围内找到元素应返回正确索引
    /// </summary>
    [Fact]
    public void BinarySearch_WithRange_FindsInRange()
    {
        // Arrange: [1,3,5,7,9,11] 范围 [index=2, length=3] → 包含 [5,7,9]
        // Act: 查找 7
        var result = SortedInts.BinarySearch(2, 3, 7);

        // Assert: 7 在原数组索引 3
        result.ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：BinarySearch 在指定范围内未找到应返回负数
    /// </summary>
    [Fact]
    public void BinarySearch_WithRange_ValueOutsideRange_ReturnsNegative()
    {
        // Arrange: 范围 [0,2] → 包含 [1,3]，查找 9 不在此范围
        var result = SortedInts.BinarySearch(0, 2, 9);

        // Assert
        result.ShouldBeLessThan(0);
    }

    #endregion

    #region BinarySearch(source, index, length, map, value) — 带范围和映射

    /// <summary>
    /// 测试目的：BinarySearch 带范围和映射函数应在投影后的子集中正确查找
    /// </summary>
    [Fact]
    public void BinarySearch_WithRangeAndMap_FindsCorrectElement()
    {
        // Arrange: SortedItems [10,20,30,40,50], 范围 [1,3] → [20,30,40], 查找 Id=30
        var result = SortedItems.BinarySearch(1, 3, x => x.Id, 30);

        // Assert: Id=30 在原数组索引 2
        result.ShouldBe(2);
    }

    #endregion

    #region BinarySearch(source, value, comparer) — 带比较器

    /// <summary>
    /// 测试目的：BinarySearch 带自定义比较器应使用该比较器进行查找
    /// </summary>
    [Fact]
    public void BinarySearch_WithComparer_FindsCorrectElement()
    {
        // Arrange: 使用默认比较器查找 5
        var result = SortedInts.BinarySearch(5, Comparer<int>.Default);

        // Assert
        result.ShouldBe(2);
    }

    /// <summary>
    /// 测试目的：BinarySearch 带自定义比较器对字符串列表应正确排序并查找
    /// </summary>
    [Fact]
    public void BinarySearch_WithStringComparer_FindsElement()
    {
        // Arrange: 有序字符串列表（大小写忽略排序）
        IList<string> sortedStrings = new List<string> { "apple", "banana", "cherry" };

        // Act
        var result = sortedStrings.BinarySearch("banana", StringComparer.OrdinalIgnoreCase);

        // Assert
        result.ShouldBe(1);
    }

    #endregion

    #region BinarySearch(source, map, value, comparer) — 带映射和比较器

    /// <summary>
    /// 测试目的：BinarySearch 带映射函数和比较器应组合使用进行查找
    /// </summary>
    [Fact]
    public void BinarySearch_WithMapAndComparer_FindsCorrectElement()
    {
        // Act: 按 Id 使用 Comparer<int>.Default 查找 Id=20
        var result = SortedItems.BinarySearch(x => x.Id, 20, Comparer<int>.Default);

        // Assert: Id=20 在索引 1
        result.ShouldBe(1);
    }

    #endregion

    #region BinarySearch(source, index, length, value, comparer) — 带范围和比较器

    /// <summary>
    /// 测试目的：BinarySearch 带范围和比较器应在指定范围内正确查找
    /// </summary>
    [Fact]
    public void BinarySearch_WithRangeAndComparer_FindsCorrectElement()
    {
        // Arrange: [1,3,5,7,9,11] 范围 [1, 4] → [3,5,7,9]，查找 7
        var result = SortedInts.BinarySearch(1, 4, 7, Comparer<int>.Default);

        // Assert
        result.ShouldBe(3);
    }

    #endregion

    #region BinarySearch(source, index, length, map, value, comparer) — 全参数重载

    /// <summary>
    /// 测试目的：BinarySearch 全参数重载应在指定范围内结合映射和比较器正确查找
    /// </summary>
    [Fact]
    public void BinarySearch_AllParams_FindsCorrectElement()
    {
        // Arrange: SortedItems [10,20,30,40,50], 范围 [2,3] → [30,40,50]
        // 查找 Id=40，映射 x => x.Id，比较器 Comparer<int>.Default
        var result = SortedItems.BinarySearch(2, 3, x => x.Id, 40, Comparer<int>.Default);

        // Assert: Id=40 在索引 3
        result.ShouldBe(3);
    }

    #endregion
}

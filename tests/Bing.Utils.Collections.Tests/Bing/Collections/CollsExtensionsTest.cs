using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 测试类：CollsExtensions 集合工具扩展方法（查询/操作系列）
/// </summary>
[Trait("CollUT", "CollsExtensions")]
public class CollsExtensionsTest
{
    #region BeContainedIn — 元素包含判断

    /// <summary>
    /// 测试目的：BeContainedIn(params T[]) 在元素存在于数组时应返回 true
    /// </summary>
    [Fact]
    public void BeContainedIn_ParamsArray_ItemPresent_ReturnsTrue()
    {
        // Act
        var result = 2.BeContainedIn(1, 2, 3);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：BeContainedIn(params T[]) 在元素不在数组中时应返回 false
    /// </summary>
    [Fact]
    public void BeContainedIn_ParamsArray_ItemAbsent_ReturnsFalse()
    {
        // Act
        var result = 5.BeContainedIn(1, 2, 3);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：BeContainedIn(IEnumerable&lt;T&gt;) 在集合中存在该元素时应返回 true
    /// </summary>
    [Fact]
    public void BeContainedIn_Enumerable_ItemPresent_ReturnsTrue()
    {
        // Arrange
        var list = new List<string> { "apple", "banana", "cherry" };

        // Act
        var result = "banana".BeContainedIn(list);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：BeContainedIn(IEnumerable&lt;T&gt;) 在集合中不存在该元素时应返回 false
    /// </summary>
    [Fact]
    public void BeContainedIn_Enumerable_ItemAbsent_ReturnsFalse()
    {
        // Arrange
        var list = new List<string> { "apple", "banana" };

        // Act
        var result = "mango".BeContainedIn(list);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：BeContainedIn 对空集合应始终返回 false
    /// </summary>
    [Fact]
    public void BeContainedIn_EmptyCollection_ReturnsFalse()
    {
        // Act
        var result = 1.BeContainedIn(Array.Empty<int>());

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Contains — 条件包含判断

    /// <summary>
    /// 测试目的：Contains(Expression) 在集合中有满足条件的元素时应返回 true
    /// </summary>
    [Fact]
    public void Contains_WithCondition_MatchFound_ReturnsTrue()
    {
        // Arrange
        var list = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = list.Contains((Expression<Func<int, bool>>)(x => x > 3));

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：Contains(Expression) 在集合中没有满足条件的元素时应返回 false
    /// </summary>
    [Fact]
    public void Contains_WithCondition_NoMatch_ReturnsFalse()
    {
        // Arrange
        var list = new[] { 1, 2, 3 };

        // Act
        var result = list.Contains((Expression<Func<int, bool>>)(x => x > 10));

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region ContainsAtLeast — 最少数量判断

    /// <summary>
    /// 测试目的：ContainsAtLeast(count) 当集合元素数量 &gt;= count 时应返回 true
    /// </summary>
    [Fact]
    public void ContainsAtLeast_Collection_EnoughElements_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var result = list.ContainsAtLeast(3);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：ContainsAtLeast(count) 当集合元素数量 &lt; count 时应返回 false
    /// </summary>
    [Fact]
    public void ContainsAtLeast_Collection_NotEnoughElements_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 1, 2 };

        // Act
        var result = list.ContainsAtLeast(5);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：ContainsAtLeast(condition, count) 当满足条件的元素数量 &gt;= count 时应返回 true
    /// </summary>
    [Fact]
    public void ContainsAtLeast_WithCondition_EnoughMatches_ReturnsTrue()
    {
        // Arrange: [1..10] 中有 5 个偶数
        var list = Enumerable.Range(1, 10).AsEnumerable();

        // Act
        var result = list.ContainsAtLeast((Expression<Func<int, bool>>)(x => x % 2 == 0), 5);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：ContainsAtLeast(condition, count) 当满足条件的元素数量 &lt; count 时应返回 false
    /// </summary>
    [Fact]
    public void ContainsAtLeast_WithCondition_NotEnoughMatches_ReturnsFalse()
    {
        // Arrange: [1,2,3] 中只有 1 个偶数
        var list = new[] { 1, 2, 3 }.AsEnumerable();

        // Act
        var result = list.ContainsAtLeast((Expression<Func<int, bool>>)(x => x % 2 == 0), 3);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IndexOf — 索引查找

    /// <summary>
    /// 测试目的：IndexOf 应返回元素在集合中的正确索引
    /// </summary>
    [Fact]
    public void IndexOf_ItemPresent_ReturnsCorrectIndex()
    {
        // Arrange
        var list = new[] { "a", "b", "c", "d" };

        // Act
        var index = list.IndexOf("c");

        // Assert
        index.ShouldBe(2);
    }

    /// <summary>
    /// 测试目的：IndexOf 对集合中第一个元素应返回 0
    /// </summary>
    [Fact]
    public void IndexOf_FirstItem_ReturnsZero()
    {
        // Arrange
        var list = new[] { 10, 20, 30 };

        // Act
        var index = list.IndexOf(10);

        // Assert
        index.ShouldBe(0);
    }

    /// <summary>
    /// 测试目的：IndexOf 对不存在的元素应返回 -1
    /// </summary>
    [Fact]
    public void IndexOf_ItemAbsent_ReturnsMinusOne()
    {
        // Arrange
        var list = new[] { 1, 2, 3 };

        // Act
        var index = list.IndexOf(99);

        // Assert
        index.ShouldBe(-1);
    }

    /// <summary>
    /// 测试目的：IndexOf 使用自定义相等比较器时应正确工作
    /// </summary>
    [Fact]
    public void IndexOf_WithEqualityComparer_ReturnsCorrectIndex()
    {
        // Arrange: 忽略大小写比较
        var list = new[] { "Apple", "Banana", "Cherry" };

        // Act
        var index = list.IndexOf("banana", StringComparer.OrdinalIgnoreCase);

        // Assert
        index.ShouldBe(1);
    }

    #endregion

    #region UniqueCount — 不重复计数

    /// <summary>
    /// 测试目的：UniqueCount 应返回集合中不重复元素的数量
    /// </summary>
    [Fact]
    public void UniqueCount_WithDuplicates_ReturnsDistinctCount()
    {
        // Arrange
        var list = new[] { 1, 2, 2, 3, 3, 3 };

        // Act
        var count = list.UniqueCount();

        // Assert
        count.ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：UniqueCount 对无重复集合应返回集合本身的元素数量
    /// </summary>
    [Fact]
    public void UniqueCount_NoDuplicates_ReturnsOriginalCount()
    {
        // Arrange
        var list = new[] { "a", "b", "c", "d" };

        // Act
        var count = list.UniqueCount();

        // Assert
        count.ShouldBe(4);
    }

    /// <summary>
    /// 测试目的：UniqueCount&lt;T, TResult&gt;(valCalculator) 应按投影值计算不重复数量
    /// </summary>
    [Fact]
    public void UniqueCount_WithProjection_ReturnsUniqueProjectedCount()
    {
        // Arrange: 按字符串长度去重
        var list = new[] { "a", "bb", "cc", "ddd" };

        // Act
        var count = list.UniqueCount(s => s.Length);

        // Assert: 长度 1, 2, 3 → 3 种不同长度
        count.ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：UniqueCount 对空集合应返回 0
    /// </summary>
    [Fact]
    public void UniqueCount_EmptyCollection_ReturnsZero()
    {
        // Act
        var count = Array.Empty<int>().UniqueCount();

        // Assert
        count.ShouldBe(0);
    }

    #endregion

    #region AddIf — 条件添加

    /// <summary>
    /// 测试目的：AddIf(value, true) 应将值添加到集合
    /// </summary>
    [Fact]
    public void AddIf_FlagTrue_AddsValue()
    {
        // Arrange
        var list = new[] { 1, 2, 3 }.AsEnumerable();

        // Act
        var result = list.AddIf(99, true).ToList();

        // Assert
        result.ShouldContain(99);
        result.Count.ShouldBe(4);
    }

    /// <summary>
    /// 测试目的：AddIf(value, false) 不应将值添加到集合
    /// </summary>
    [Fact]
    public void AddIf_FlagFalse_DoesNotAdd()
    {
        // Arrange
        var list = new[] { 1, 2, 3 }.AsEnumerable();

        // Act
        var result = list.AddIf(99, false).ToList();

        // Assert
        result.ShouldNotContain(99);
        result.Count.ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：AddIfNotNull 在值不为 null 时应添加到集合
    /// </summary>
    [Fact]
    public void AddIfNotNull_NonNullValue_AddsValue()
    {
        // Arrange
        var list = new[] { "a", "b" }.AsEnumerable();

        // Act
        var result = list.AddIfNotNull("c").ToList();

        // Assert
        result.ShouldContain("c");
        result.Count.ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：AddIfNotNull 在值为 null 时不应添加到集合
    /// </summary>
    [Fact]
    public void AddIfNotNull_NullValue_DoesNotAdd()
    {
        // Arrange
        var list = new[] { "a", "b" }.AsEnumerable();

        // Act
        var result = list.AddIfNotNull(null).ToList();

        // Assert
        result.Count.ShouldBe(2);
    }

    #endregion

    #region RemoveDuplicates — 去重

    /// <summary>
    /// 测试目的：RemoveDuplicates 应移除集合中的重复项
    /// </summary>
    [Fact]
    public void RemoveDuplicates_WithDuplicates_ReturnsUniqueItems()
    {
        // Arrange
        var list = new List<int> { 1, 2, 2, 3, 3, 3 };

        // Act
        var result = list.RemoveDuplicates().ToList();

        // Assert
        result.ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 测试目的：RemoveDuplicatesIgnoreCase 应忽略大小写移除重复字符串
    /// </summary>
    [Fact]
    public void RemoveDuplicatesIgnoreCase_MixedCase_RemovesDuplicates()
    {
        // Arrange
        var list = new List<string> { "Apple", "apple", "APPLE", "Banana" };

        // Act
        var result = list.RemoveDuplicatesIgnoreCase().ToList();

        // Assert
        result.Count.ShouldBe(2);
    }

    #endregion

    #region RemoveIf — 条件移除

    /// <summary>
    /// 测试目的：RemoveIf 应移除满足条件的元素
    /// </summary>
    [Fact]
    public void RemoveIf_WithCondition_RemovesMatchingItems()
    {
        // Arrange: 移除所有偶数
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var result = list.RemoveIf(x => x % 2 == 0).ToList();

        // Assert
        result.ShouldBe(new[] { 1, 3, 5 });
    }

    #endregion

    #region Merge — 集合合并

    /// <summary>
    /// 测试目的：Merge 应将两个集合合并为一个
    /// </summary>
    [Fact]
    public void Merge_TwoCollections_ReturnsAllElements()
    {
        // Arrange
        var left = new[] { 1, 2, 3 };
        var right = new[] { 4, 5, 6 };

        // Act
        var result = left.Merge(right).ToList();

        // Assert
        result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
    }

    /// <summary>
    /// 测试目的：Merge(limit) 应限制从 right 集合中取出的元素数量（limit 是对 right 的上限）
    /// </summary>
    [Fact]
    public void Merge_WithLimit_LimitsFromRightCount()
    {
        // Arrange: left=[1,2,3], right=[4,5,6], limit=2 → 从 right 最多取 2 个 → [1,2,3,4,5]
        var left = new[] { 1, 2, 3 };
        var right = new[] { 4, 5, 6 };

        // Act
        var result = left.Merge(right, 2).ToList();

        // Assert
        result.Count.ShouldBe(5);
        result.ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    #endregion

    #region OrderByRandom — 随机排序（仅验证行为不变性）

    /// <summary>
    /// 测试目的：OrderByRandom 结果应包含与原集合相同数量的元素
    /// </summary>
    [Fact]
    public void OrderByRandom_ReturnsAllElements()
    {
        // Arrange
        var list = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = list.OrderByRandom().ToList();

        // Assert
        result.Count.ShouldBe(5);
        result.ShouldContain(1);
        result.ShouldContain(3);
        result.ShouldContain(5);
    }

    #endregion
}

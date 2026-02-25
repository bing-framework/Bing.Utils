using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `CollJudge` 相关行为。
/// </summary>
[Trait("CollectionsUT", "CollJudge")]
public class CollJudgeTest
{
    /// <summary>
    /// 测试用例：验证 `IsNullOrEmpty` 在 `EnumerableScenarios` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_EnumerableScenarios_ReturnsExpectedResult()
    {
        IEnumerable nullEnumerable = null;
        IEnumerable emptyEnumerable = Array.Empty<int>();
        IEnumerable valueEnumerable = new[] { "A" };
        CollJudge.IsNullOrEmpty(nullEnumerable).ShouldBeTrue();
        CollJudge.IsNullOrEmpty(emptyEnumerable).ShouldBeTrue();
        CollJudge.IsNullOrEmpty(valueEnumerable).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsNullOrEmpty` 在 `GenericEnumerableScenarios` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_GenericEnumerableScenarios_ReturnsExpectedResult()
    {
        IEnumerable<int> nullEnumerable = null;
        IEnumerable<int> emptyEnumerable = Array.Empty<int>();
        IEnumerable<int> valueEnumerable = new[] { 1 };
        CollJudge.IsNullOrEmpty(nullEnumerable).ShouldBeTrue();
        CollJudge.IsNullOrEmpty(emptyEnumerable).ShouldBeTrue();
        CollJudge.IsNullOrEmpty(valueEnumerable).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsSameCount` 在 `ICollectionScenarios` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Fact]
    public void IsSameCount_ICollectionScenarios_ReturnsExpectedResult()
    {
        ICollection<int> left = new List<int> { 1, 2, 3 };
        ICollection<int> right = new List<int> { 7, 8, 9 };
        ICollection<int> different = new List<int> { 1 };
        ICollection<int> nullCollection = null;
        CollJudge.IsSameCount(left, right).ShouldBeTrue();
        CollJudge.IsSameCount(left, different).ShouldBeFalse();
        CollJudge.IsSameCount(nullCollection, nullCollection).ShouldBeTrue();
        CollJudge.IsSameCount(left, nullCollection).ShouldBeFalse();
        left.IsSameCount(right).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsSameCount` 在 `IQueryableScenarios` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Fact]
    public void IsSameCount_IQueryableScenarios_ReturnsExpectedResult()
    {
        IQueryable<int> left = new[] { 1, 2 }.AsQueryable();
        IQueryable<int> right = new[] { 3, 4 }.AsQueryable();
        IQueryable<int> different = new[] { 1 }.AsQueryable();
        IQueryable<int> nullQueryable = null;
        CollJudge.IsSameCount(left, right).ShouldBeTrue();
        CollJudge.IsSameCount(left, different).ShouldBeFalse();
        CollJudge.IsSameCount(nullQueryable, nullQueryable).ShouldBeTrue();
        CollJudge.IsSameCount(left, nullQueryable).ShouldBeFalse();
        left.IsSameCount(right).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `ContainsAtLeast` 在 `ICollection` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void ContainsAtLeast_ICollection_ReturnsExpectedResult(int count, bool expected)
    {
        ICollection<int> collection = new List<int> { 1, 2 };
        var result = CollJudge.ContainsAtLeast(collection, count);
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `ContainsAtLeast` 在 `NullCollection` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void ContainsAtLeast_NullCollection_ReturnsFalse()
    {
        ICollection<int> collection = null;
        CollJudge.ContainsAtLeast(collection, 1).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `ContainsAtLeast` 在 `IQueryable` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(4, false)]
    public void ContainsAtLeast_IQueryable_ReturnsExpectedResult(int count, bool expected)
    {
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        var result = CollJudge.ContainsAtLeast(queryable, count);
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `ContainsEqualCount` 在 `IQueryableScenarios` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Fact]
    public void ContainsEqualCount_IQueryableScenarios_ReturnsExpectedResult()
    {
        IQueryable<int> left = new[] { 1, 2 }.AsQueryable();
        IQueryable<int> right = new[] { 3, 4 }.AsQueryable();
        IQueryable<int> different = new[] { 3 }.AsQueryable();
        IQueryable<int> nullQueryable = null;
        CollJudge.ContainsEqualCount(left, right).ShouldBeTrue();
        CollJudge.ContainsEqualCount(left, different).ShouldBeFalse();
        CollJudge.ContainsEqualCount(nullQueryable, nullQueryable).ShouldBeTrue();
        CollJudge.ContainsEqualCount(left, nullQueryable).ShouldBeFalse();
    }
}


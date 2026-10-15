using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 集合空值、数量及数量下限判断测试。
/// </summary>
[Trait("CollectionsUT", "CollJudge")]
public class CollJudgeTest
{
    /// <summary>
    /// 验证非泛型序列的空值和空序列判断。
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
    /// 验证泛型序列的空值和空序列判断。
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
    /// 验证集合数量比较及空值处理。
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
    /// 验证可查询序列的数量比较及空值处理。
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
    /// 验证集合是否达到指定数量下限。
    /// </summary>
    /// <param name="count">数量下限。</param>
    /// <param name="expected">预期判断结果。</param>
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
    /// 验证空集合引用不能满足数量下限。
    /// </summary>
    [Fact]
    public void ContainsAtLeast_NullCollection_ReturnsFalse()
    {
        ICollection<int> collection = null;
        CollJudge.ContainsAtLeast(collection, 1).ShouldBeFalse();
    }
    /// <summary>
    /// 验证可查询序列是否达到指定数量下限。
    /// </summary>
    /// <param name="count">数量下限。</param>
    /// <param name="expected">预期判断结果。</param>
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
    /// 验证可查询序列的数量相等判断及空值处理。
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


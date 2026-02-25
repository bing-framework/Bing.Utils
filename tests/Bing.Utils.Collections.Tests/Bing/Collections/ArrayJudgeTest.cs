using System.Collections.Generic;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `ArrayJudge` 相关行为。
/// </summary>
[Trait("CollectionsUT", "ArrayJudge")]
public class ArrayJudgeTest
{
    /// <summary>
    /// 测试用例：验证 `IsIndexInRange` 在 `OneDimensionalArray` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void IsIndexInRange_OneDimensionalArray_ReturnsExpectedResult(int index, bool expected)
    {
        var array = new[] { 10, 20, 30 };
        var result = ArrayJudge.IsIndexInRange(array, index);
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `IsIndexInRange` 在 `MultiDimensionalArrayWithLowerBound` 场景下，结果为 `ReturnsExpectedResult`。
    /// </summary>
    [Fact]
    public void IsIndexInRange_MultiDimensionalArrayWithLowerBound_ReturnsExpectedResult()
    {
        var array = Array.CreateInstance(typeof(int), new[] { 2, 3 }, new[] { 0, 1 });
        ArrayJudge.IsIndexInRange(array, 0, 1).ShouldBeFalse();
        ArrayJudge.IsIndexInRange(array, 1, 1).ShouldBeTrue();
        ArrayJudge.IsIndexInRange(array, 3, 1).ShouldBeTrue();
        ArrayJudge.IsIndexInRange(array, 4, 1).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsIndexInRange` 在 `InvalidDimension` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsIndexInRange_InvalidDimension_ThrowsArgumentOutOfRangeException(int dimension)
    {
        var array = new[] { 1, 2, 3 };
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => ArrayJudge.IsIndexInRange(array, 0, dimension));
        ex.ParamName.ShouldBe("dimension");
    }
}


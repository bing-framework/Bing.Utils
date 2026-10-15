using System.Collections.Generic;
namespace Bing.Collections;
/// <summary>
/// 数组索引边界和维度校验测试。
/// </summary>
[Trait("CollectionsUT", "ArrayJudge")]
public class ArrayJudgeTest
{
    /// <summary>
    /// 验证一维数组的索引范围判断。
    /// </summary>
    /// <param name="index">待判断索引。</param>
    /// <param name="expected">预期判断结果。</param>
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
    /// 验证带非零下界的多维数组按指定维度判断索引。
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
    /// 验证无效维度被拒绝并报告参数名。
    /// </summary>
    /// <param name="dimension">无效的维度序号。</param>
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


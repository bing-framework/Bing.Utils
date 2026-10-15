namespace Bing.Utils.Tests.Bing;

/// <summary>
/// 伪随机数扩展的取值范围、选取和分布测试。
/// </summary>
[Trait("Bing", "RandomExtensions")]
public class RandomExtensionsTests
{
    /// <summary>
    /// 使用固定种子的伪随机数生成器。
    /// </summary>
    private readonly Random _rng = new Random(42); // 固定种子保证确定性

    #region NextBool

    /// <summary>
    /// 验证无参数布尔采样返回布尔值。
    /// </summary>
    [Fact]
    public void NextBool_NoParam_ReturnsBoolValue()
    {
        var value = _rng.NextBool();
        (value == true || value == false).ShouldBeTrue();
    }

    /// <summary>
    /// 验证概率为零或一时的布尔采样边界。
    /// </summary>
    /// <param name="probability">返回真值的概率。</param>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void NextBool_WithProbabilityBoundary_ReturnsCorrect(double probability)
    {
        var r = new Random(1);
        var result = r.NextBool(probability);
        if (probability == 0.0)
            result.ShouldBeFalse();
        else
            result.ShouldBeTrue();
    }

    /// <summary>
    /// 验证固定种子下半概率布尔采样的真值数量位于容差内。
    /// </summary>
    [Fact]
    public void NextBool_Probability0_5_ProducesReasonableDistribution()
    {
        var r = new Random(99);
        var trueCount = Enumerable.Range(0, 1000).Count(_ => r.NextBool(0.5));
        // 概率 0.5，期望约 500，允许偏差 ±150
        trueCount.ShouldBeInRange(350, 650);
    }

    #endregion

    #region NextEnum

    /// <summary>
    /// 验证枚举采样结果为已定义的星期成员。
    /// </summary>
    [Fact]
    public void NextEnum_ValidEnumType_ReturnsDefinedMember()
    {
        var r = new Random(7);
        var value = r.NextEnum<DayOfWeek>();
        Enum.IsDefined(typeof(DayOfWeek), value).ShouldBeTrue();
    }

    /// <summary>
    /// 验证非枚举类型不能进行枚举采样。
    /// </summary>
    [Fact]
    public void NextEnum_NonEnumType_ThrowsInvalidOperationException()
    {
        var r = new Random(1);
        Should.Throw<InvalidOperationException>(() => r.NextEnum<int>());
    }

    #endregion

    #region NextBytes

    /// <summary>
    /// 验证随机字节数组具有指定长度。
    /// </summary>
    [Fact]
    public void NextBytes_ReturnsCorrectLength()
    {
        var bytes = _rng.NextBytes(10);
        bytes.Length.ShouldBe(10);
    }

    /// <summary>
    /// 验证零长度随机字节数组为空。
    /// </summary>
    [Fact]
    public void NextBytes_ZeroLength_ReturnsEmptyArray()
    {
        var bytes = _rng.NextBytes(0);
        bytes.Length.ShouldBe(0);
    }

    #endregion

    #region NextUInt16 / NextInt16

    /// <summary>
    /// 验证无符号短整数采样位于类型范围内。
    /// </summary>
    [Fact]
    public void NextUInt16_ReturnsUShortValue()
    {
        var r = new Random(5);
        var value = r.NextUInt16();
        (value >= ushort.MinValue && value <= ushort.MaxValue).ShouldBeTrue();
    }

    /// <summary>
    /// 验证有符号短整数采样位于类型范围内。
    /// </summary>
    [Fact]
    public void NextInt16_ReturnsShortValue()
    {
        var r = new Random(5);
        var value = r.NextInt16();
        (value >= short.MinValue && value <= short.MaxValue).ShouldBeTrue();
    }

    #endregion

    #region NextLong

    /// <summary>
    /// 验证无参数长整数采样非负。
    /// </summary>
    [Fact]
    public void NextLong_NoParam_ReturnsNonNegative()
    {
        var r = new Random(3);
        var value = r.NextLong();
        value.ShouldBeGreaterThanOrEqualTo(0L);
    }

    /// <summary>
    /// 验证指定上限的长整数采样位于断言范围内。
    /// </summary>
    [Fact]
    public void NextLong_WithMax_ReturnsWithinRange()
    {
        var r = new Random(3);
        var value = r.NextLong(100L);
        value.ShouldBeInRange(0L, 100L);
    }

    /// <summary>
    /// 验证指定上下限的长整数采样位于断言范围内。
    /// </summary>
    [Fact]
    public void NextLong_WithMinMax_ReturnsWithinRange()
    {
        var r = new Random(3);
        var value = r.NextLong(50L, 100L);
        value.ShouldBeInRange(50L, 100L);
    }

    #endregion

    #region NextFloat

    /// <summary>
    /// 验证指定上限的单精度采样位于断言范围内。
    /// </summary>
    [Fact]
    public void NextFloat_WithMax_ReturnsWithinRange()
    {
        var r = new Random(11);
        var value = r.NextFloat(100f);
        value.ShouldBeInRange(0f, 100f);
    }

    /// <summary>
    /// 验证指定上下限的单精度采样位于断言范围内。
    /// </summary>
    [Fact]
    public void NextFloat_WithMinMax_ReturnsWithinRange()
    {
        var r = new Random(11);
        var value = r.NextFloat(10f, 50f);
        value.ShouldBeInRange(10f, 50f);
    }

    #endregion

    #region NextDouble

    /// <summary>
    /// 验证指定上限的双精度采样位于断言范围内。
    /// </summary>
    [Fact]
    public void NextDouble_WithMax_ReturnsWithinRange()
    {
        var r = new Random(13);
        var value = r.NextDouble(100.0);
        value.ShouldBeInRange(0.0, 100.0);
    }

    /// <summary>
    /// 验证指定上下限的双精度采样位于断言范围内。
    /// </summary>
    [Fact]
    public void NextDouble_WithMinMax_ReturnsWithinRange()
    {
        var r = new Random(13);
        var value = r.NextDouble(10.0, 50.0);
        value.ShouldBeInRange(10.0, 50.0);
    }

    #endregion

    #region NextDateTime

    /// <summary>
    /// 验证无参数日期采样位于日期类型范围内。
    /// </summary>
    [Fact]
    public void NextDateTime_NoParam_ReturnsValidDateTime()
    {
        var r = new Random(17);
        var dt = r.NextDateTime();
        (dt >= DateTime.MinValue && dt <= DateTime.MaxValue).ShouldBeTrue();
    }

    /// <summary>
    /// 验证日期采样位于指定日期范围内。
    /// </summary>
    [Fact]
    public void NextDateTime_WithRange_ReturnsWithinRange()
    {
        var r = new Random(17);
        var min = new DateTime(2020, 1, 1);
        var max = new DateTime(2025, 12, 31);
        var dt = r.NextDateTime(min, max);
        dt.ShouldBeInRange(min, max);
    }

    #endregion

    #region OneOf

    /// <summary>
    /// 验证随机选取结果来自给定候选集合。
    /// </summary>
    [Fact]
    public void OneOf_SelectsFromGivenValues()
    {
        var r = new Random(21);
        var options = new[] { "a", "b", "c" };
        var result = r.OneOf(options);
        options.ShouldContain(result);
    }

    /// <summary>
    /// 验证单一候选始终被选中。
    /// </summary>
    [Fact]
    public void OneOf_SingleValue_ReturnsThatValue()
    {
        var r = new Random(1);
        r.OneOf("only").ShouldBe("only");
    }

    #endregion

    #region NormalDouble / NormalFloat

    /// <summary>
    /// 验证双精度正态采样结果为有限数值。
    /// </summary>
    [Fact]
    public void NormalDouble_NoParam_ReturnsFiniteValue()
    {
        var r = new Random(33);
        var value = r.NormalDouble();
        double.IsNaN(value).ShouldBeFalse();
        double.IsInfinity(value).ShouldBeFalse();
    }

    /// <summary>
    /// 验证固定种子下双精度正态样本均值位于容差内。
    /// </summary>
    [Fact]
    public void NormalDouble_WithMeanAndDeviation_CenteredAroundMean()
    {
        var r = new Random(33);
        var samples = Enumerable.Range(0, 1000).Select(_ => r.NormalDouble(100.0, 1.0)).ToList();
        var avg = samples.Average();
        // 均值应接近 100，允许偏差 ±0.5
        avg.ShouldBeInRange(98.0, 102.0);
    }

    /// <summary>
    /// 验证单精度正态采样结果为有限数值。
    /// </summary>
    [Fact]
    public void NormalFloat_NoParam_ReturnsFiniteValue()
    {
        var r = new Random(33);
        var value = r.NormalFloat();
        float.IsNaN(value).ShouldBeFalse();
        float.IsInfinity(value).ShouldBeFalse();
    }

    /// <summary>
    /// 验证固定种子下单精度正态样本均值位于容差内。
    /// </summary>
    [Fact]
    public void NormalFloat_WithMeanAndDeviation_CenteredAroundMean()
    {
        var r = new Random(33);
        var samples = Enumerable.Range(0, 1000).Select(_ => r.NormalFloat(50f, 1f)).ToList();
        var avg = samples.Average();
        avg.ShouldBeInRange(48f, 52f);
    }

    #endregion
}

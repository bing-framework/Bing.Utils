using Bing.Numeric;

namespace Bing.Utils.Tests.Bing.Numeric;

/// <summary>
/// Numbers 数值操作测试 — GetRangeBetween / Is / NearOrPreciseEqual / To / Arithmetic / Extensions
/// </summary>
[Trait("Bing.Numeric", "Numbers")]
public class NumbersExtendedTests
{
    #region GetRangeBetween (int)

    /// <summary>
    /// 验证整数区间包含两个端点及中间值。
    /// </summary>
    [Fact]
    public void GetRangeBetween_Int_NormalRange_ReturnsAllInclusive()
    {
        Numbers.GetRangeBetween(1, 5).ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    /// <summary>
    /// 验证整数区间自动调整颠倒的边界。
    /// </summary>
    [Fact]
    public void GetRangeBetween_Int_SwappedMinMax_AutoSwaps()
    {
        Numbers.GetRangeBetween(5, 1).ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    /// <summary>
    /// 验证相同整数边界仅生成一个元素。
    /// </summary>
    [Fact]
    public void GetRangeBetween_Int_SameMinMax_ReturnsSingleElement()
    {
        Numbers.GetRangeBetween(3, 3).ShouldBe(new[] { 3 });
    }

    #endregion

    #region GetRangeBetween (long)

    /// <summary>
    /// 验证长整数区间包含两个端点及中间值。
    /// </summary>
    [Fact]
    public void GetRangeBetween_Long_NormalRange_ReturnsAllInclusive()
    {
        Numbers.GetRangeBetween(1L, 4L).ShouldBe(new[] { 1L, 2L, 3L, 4L });
    }

    /// <summary>
    /// 验证长整数区间自动调整颠倒的边界。
    /// </summary>
    [Fact]
    public void GetRangeBetween_Long_SwappedMinMax_AutoSwaps()
    {
        Numbers.GetRangeBetween(4L, 1L).ShouldBe(new[] { 1L, 2L, 3L, 4L });
    }

    #endregion

    #region Numbers.Is

    /// <summary>
    /// 验证双精度非数值被识别为 NaN。
    /// </summary>
    [Fact]
    public void IsNaN_Double_NaN_ReturnsTrue()
    {
        Numbers.IsNaN(double.NaN).ShouldBeTrue();
    }

    /// <summary>
    /// 验证普通双精度数值不被识别为 NaN。
    /// </summary>
    [Fact]
    public void IsNaN_Double_RealValue_ReturnsFalse()
    {
        Numbers.IsNaN(1.5).ShouldBeFalse();
    }

    /// <summary>
    /// 验证单精度非数值被识别为 NaN。
    /// </summary>
    [Fact]
    public void IsNaN_Float_NaN_ReturnsTrue()
    {
        Numbers.IsNaN(float.NaN).ShouldBeTrue();
    }

    /// <summary>
    /// 验证零被识别为默认数值。
    /// </summary>
    [Fact]
    public void IsDefaultValue_Zero_ReturnsTrue()
    {
        Numbers.IsDefaultValue(0.0).ShouldBeTrue();
    }

    /// <summary>
    /// 验证非零值不被识别为默认数值。
    /// </summary>
    [Fact]
    public void IsDefaultValue_NonZero_ReturnsFalse()
    {
        Numbers.IsDefaultValue(1.5).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 JSON 安全整数上限仍为安全值。
    /// </summary>
    [Fact]
    public void IsJsonSafeInteger_WithinLimit_ReturnsTrue()
    {
        Numbers.IsJsonSafeInteger(9007199254740991UL).ShouldBeTrue();
    }

    /// <summary>
    /// 验证超过 JSON 安全整数上限的值不安全。
    /// </summary>
    [Fact]
    public void IsJsonSafeInteger_ExceedLimit_ReturnsFalse()
    {
        Numbers.IsJsonSafeInteger(9007199254740992UL).ShouldBeFalse();
    }

    #endregion

    #region IsZeroValue / IsNearZeroValue

    /// <summary>
    /// 验证双精度零值被识别为零。
    /// </summary>
    [Fact]
    public void IsZeroValue_Double_Zero_ReturnsTrue()
    {
        Numbers.IsZeroValue(0.0).ShouldBeTrue();
    }

    /// <summary>
    /// 验证双精度非零值不被识别为零。
    /// </summary>
    [Fact]
    public void IsZeroValue_Double_NonZero_ReturnsFalse()
    {
        Numbers.IsZeroValue(0.5).ShouldBeFalse();
    }

    /// <summary>
    /// 验证单精度零值被识别为零。
    /// </summary>
    [Fact]
    public void IsZeroValue_Float_Zero_ReturnsTrue()
    {
        Numbers.IsZeroValue(0f).ShouldBeTrue();
    }

    /// <summary>
    /// 验证精度范围内的值被识别为接近零。
    /// </summary>
    [Fact]
    public void IsNearZeroValue_WithinPrecision_ReturnsTrue()
    {
        Numbers.IsNearZeroValue(0.0005, 0.001).ShouldBeTrue();
    }

    /// <summary>
    /// 验证精度范围外的值不被识别为接近零。
    /// </summary>
    [Fact]
    public void IsNearZeroValue_OutsidePrecision_ReturnsFalse()
    {
        Numbers.IsNearZeroValue(0.01, 0.001).ShouldBeFalse();
    }

    #endregion

    #region IsNearEqual

    /// <summary>
    /// 验证两个数值在指定容差下的近似相等结果。
    /// </summary>
    /// <param name="a">第一个数值。</param>
    /// <param name="b">第二个数值。</param>
    /// <param name="tolerance">比较容差。</param>
    /// <param name="expected">预期的比较结果。</param>
    [Theory]
    [InlineData(1.0f, 1.0f, 0.01f, true)]
    [InlineData(1.0f, 1.005f, 0.01f, true)]
    [InlineData(1.0f, 1.1f, 0.01f, false)]
    public void IsNearEqual_Float_ReturnsCorrect(float a, float b, float tolerance, bool expected)
    {
        Numbers.IsNearEqual(a, b, tolerance).ShouldBe(expected);
    }

    /// <summary>
    /// 验证两个数值在指定容差下的近似相等结果。
    /// </summary>
    /// <param name="a">第一个数值。</param>
    /// <param name="b">第二个数值。</param>
    /// <param name="tolerance">比较容差。</param>
    /// <param name="expected">预期的比较结果。</param>
    [Theory]
    [InlineData(1.0, 1.0, 0.01, true)]
    [InlineData(1.0, 1.005, 0.01, true)]
    [InlineData(1.0, 1.1, 0.01, false)]
    public void IsNearEqual_Double_ReturnsCorrect(double a, double b, double tolerance, bool expected)
    {
        Numbers.IsNearEqual(a, b, tolerance).ShouldBe(expected);
    }

    /// <summary>
    /// 验证容差范围内的十进制数近似相等。
    /// </summary>
    [Fact]
    public void IsNearEqual_Decimal_WithinTolerance_ReturnsTrue()
    {
        Numbers.IsNearEqual(1.00m, 1.005m, 0.01m).ShouldBeTrue();
    }

    /// <summary>
    /// 验证容差范围外的十进制数不近似相等。
    /// </summary>
    [Fact]
    public void IsNearEqual_Decimal_OutsideTolerance_ReturnsFalse()
    {
        Numbers.IsNearEqual(1.00m, 1.1m, 0.01m).ShouldBeFalse();
    }

    /// <summary>
    /// 验证两个双精度 NaN 值判为近似相等。
    /// </summary>
    [Fact]
    public void IsNearEqual_BothNaN_Double_ReturnsTrue()
    {
        Numbers.IsNearEqual(double.NaN, double.NaN, 0.01).ShouldBeTrue();
    }

    #endregion

    #region IsPreciseEqual

    /// <summary>
    /// 验证相同双精度数值精确相等。
    /// </summary>
    [Fact]
    public void IsPreciseEqual_Double_SameValue_ReturnsTrue()
    {
        Numbers.IsPreciseEqual(1.5, 1.5).ShouldBeTrue();
    }

    /// <summary>
    /// 验证不同双精度数值不精确相等。
    /// </summary>
    [Fact]
    public void IsPreciseEqual_Double_DifferentValues_ReturnsFalse()
    {
        Numbers.IsPreciseEqual(1.5, 1.6).ShouldBeFalse();
    }

    /// <summary>
    /// 验证相同单精度数值精确相等。
    /// </summary>
    [Fact]
    public void IsPreciseEqual_Float_SameValue_ReturnsTrue()
    {
        Numbers.IsPreciseEqual(1.5f, 1.5f).ShouldBeTrue();
    }

    /// <summary>
    /// 验证两个可空双精度空值精确相等。
    /// </summary>
    [Fact]
    public void IsPreciseEqual_NullableDouble_BothNull_ReturnsTrue()
    {
        Numbers.IsPreciseEqual((double?)null, null).ShouldBeTrue();
    }

    /// <summary>
    /// 验证仅一侧为空的可空双精度值不精确相等。
    /// </summary>
    [Fact]
    public void IsPreciseEqual_NullableDouble_OneNull_ReturnsFalse()
    {
        Numbers.IsPreciseEqual((double?)1.5, null).ShouldBeFalse();
    }

    #endregion

    #region Numbers.To

    /// <summary>
    /// 验证单精度数值转换为对应的十进制数。
    /// </summary>
    [Fact]
    public void ToDecimal_FromFloat_ReturnsAccurateDecimal()
    {
        var result = Numbers.ToDecimal(1.5f);
        result.ShouldBe(1.5m);
    }

    /// <summary>
    /// 验证单精度数值按指定精度转换为十进制数。
    /// </summary>
    [Fact]
    public void ToDecimal_FromFloat_WithPrecision_RoundsCorrectly()
    {
        var result = Numbers.ToDecimal(1.567f, 2);
        result.ShouldBe(1.57m);
    }

    /// <summary>
    /// 验证双精度数值转换为对应的十进制数。
    /// </summary>
    [Fact]
    public void ToDecimal_FromDouble_ReturnsAccurateDecimal()
    {
        var result = Numbers.ToDecimal(1.5);
        result.ShouldBe(1.5m);
    }

    /// <summary>
    /// 验证单精度数值转换为对应的双精度数。
    /// </summary>
    [Fact]
    public void ToDouble_FromFloat_ReturnsAccurateDouble()
    {
        var result = Numbers.ToDouble(1.5f);
        result.ShouldBe(1.5);
    }

    /// <summary>
    /// 验证按精度转换的双精度结果位于预期区间。
    /// </summary>
    [Fact]
    public void ToDouble_FromFloat_WithPrecision_RoundsCorrectly()
    {
        var result = Numbers.ToDouble(1.567f, 2);
        result.ShouldBeInRange(1.56, 1.57);
    }

    /// <summary>
    /// 验证双精度转换拒绝无效精度。
    /// </summary>
    [Fact]
    public void ToDouble_FromFloat_InvalidPrecision_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Numbers.ToDouble(1.5f, 16));
    }

    /// <summary>
    /// 验证可空单精度空值转换为双精度 NaN。
    /// </summary>
    [Fact]
    public void ToDouble_FromNullableFloat_Null_ReturnsNaN()
    {
        Numbers.ToDouble((float?)null).ShouldBe(double.NaN);
    }

    #endregion

    #region Numbers — GetDecimalPlaces / GetSumAccurate / GetProductAccurate

    /// <summary>
    /// 验证数值的小数位数。
    /// </summary>
    /// <param name="value">待检查的数值。</param>
    /// <param name="expected">预期的小数位数。</param>
    [Theory]
    [InlineData(1.23, 2)]
    [InlineData(1.5, 1)]
    [InlineData(100.0, 0)]
    public void GetDecimalPlaces_ReturnsCorrect(double value, int expected)
    {
        Numbers.GetDecimalPlaces(value).ShouldBe(expected);
    }

    /// <summary>
    /// 验证 NaN 的小数位数为零。
    /// </summary>
    [Fact]
    public void GetDecimalPlaces_NaN_ReturnsZero()
    {
        Numbers.GetDecimalPlaces(double.NaN).ShouldBe(0);
    }

    /// <summary>
    /// 验证精确求和消除示例中的浮点加法误差。
    /// </summary>
    [Fact]
    public void GetSumAccurate_SimpleValues_ReturnsCorrectSum()
    {
        var result = Numbers.GetSumAccurate(0.1, 0.2);
        result.ShouldBe(0.3);
    }

    /// <summary>
    /// 验证精确求积消除示例中的浮点乘法误差。
    /// </summary>
    [Fact]
    public void GetProductAccurate_SimpleValues_ReturnsCorrectProduct()
    {
        var result = Numbers.GetProductAccurate(0.1, 3.0);
        result.ShouldBe(0.3);
    }

    /// <summary>
    /// 验证使用整数运算求和得到预期结果。
    /// </summary>
    [Fact]
    public void GetSumUsingIntegers_SimpleValues_ReturnsCorrectSum()
    {
        var result = Numbers.GetSumUsingIntegers(0.1, 0.2);
        result.ShouldBe(0.3);
    }

    #endregion

    #region FixZero

    /// <summary>
    /// 验证修正零值后仍返回零。
    /// </summary>
    [Fact]
    public void FixZero_ActualZero_ReturnsZero()
    {
        Numbers.FixZero(0.0).ShouldBe(0.0);
    }

    /// <summary>
    /// 验证修正非零值后保留原值。
    /// </summary>
    [Fact]
    public void FixZero_NonZeroValue_ReturnsOriginal()
    {
        Numbers.FixZero(1.5).ShouldBe(1.5);
    }

    #endregion

    #region NumberExtensions (extension methods)

    /// <summary>
    /// 验证数值扩展将零识别为默认值。
    /// </summary>
    [Fact]
    public void IsDefault_Zero_ReturnsTrue()
    {
        0.0.IsDefault().ShouldBeTrue();
    }

    /// <summary>
    /// 验证单精度数值扩展将零识别为零。
    /// </summary>
    [Fact]
    public void IsZero_FloatNearZero_ReturnsTrue()
    {
        0.0f.IsZero().ShouldBeTrue();
    }

    /// <summary>
    /// 验证双精度数值扩展将零识别为零。
    /// </summary>
    [Fact]
    public void IsZero_DoubleNearZero_ReturnsTrue()
    {
        0.0.IsZero().ShouldBeTrue();
    }

    /// <summary>
    /// 验证数值扩展识别精度范围内的近零值。
    /// </summary>
    [Fact]
    public void IsNearZero_DoubleWithinPrecision_ReturnsTrue()
    {
        0.0005.IsNearZero(0.001).ShouldBeTrue();
    }

    /// <summary>
    /// 验证双精度扩展识别容差范围内的近似相等值。
    /// </summary>
    [Fact]
    public void IsNearEqual_DoubleExtension_ReturnsCorrect()
    {
        1.0.IsNearEqual(1.005, 0.01).ShouldBeTrue();
    }

    /// <summary>
    /// 验证双精度扩展识别精确相等值。
    /// </summary>
    [Fact]
    public void IsPreciseEqual_DoubleExtension_SameValue_ReturnsTrue()
    {
        1.5.IsPreciseEqual(1.5).ShouldBeTrue();
    }

    /// <summary>
    /// 验证单精度扩展转换为对应的十进制数。
    /// </summary>
    [Fact]
    public void ToDecimal_FloatExtension_ReturnsAccurateDecimal()
    {
        1.5f.ToDecimal().ShouldBe(1.5m);
    }

    /// <summary>
    /// 验证单精度扩展转换为对应的双精度数。
    /// </summary>
    [Fact]
    public void ToDouble_FloatExtension_ReturnsAccurateDouble()
    {
        1.5f.ToDouble().ShouldBe(1.5);
    }

    /// <summary>
    /// 验证数值扩展返回示例的小数位数。
    /// </summary>
    [Fact]
    public void DecimalPlaces_Extension_ReturnsCorrect()
    {
        1.23.DecimalPlaces().ShouldBe(2);
    }

    /// <summary>
    /// 验证数值扩展执行精确求和。
    /// </summary>
    [Fact]
    public void SumAccurate_Extension_ReturnsCorrectSum()
    {
        0.1.SumAccurate(0.2).ShouldBe(0.3);
    }

    /// <summary>
    /// 验证数值扩展执行精确求积。
    /// </summary>
    [Fact]
    public void ProductAccurate_Extension_ReturnsCorrectProduct()
    {
        0.1.ProductAccurate(3.0).ShouldBe(0.3);
    }

    /// <summary>
    /// 验证数值扩展使用整数运算求和。
    /// </summary>
    [Fact]
    public void SumUsingIntegers_Extension_ReturnsCorrectSum()
    {
        0.1.SumUsingIntegers(0.2).ShouldBe(0.3);
    }

    /// <summary>
    /// 验证数值扩展修正零值。
    /// </summary>
    [Fact]
    public void FixZero_Extension_ReturnsTrueZero()
    {
        0.0.FixZero().ShouldBe(0.0);
    }

    #endregion
}

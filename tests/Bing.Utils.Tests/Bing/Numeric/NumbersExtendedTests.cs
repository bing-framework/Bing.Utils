using Bing.Numeric;

namespace Bing.Utils.Tests.Bing.Numeric;

/// <summary>
/// Numbers 数值操作测试 — GetRangeBetween / Is / NearOrPreciseEqual / To / Arithmetic / Extensions
/// </summary>
[Trait("Bing.Numeric", "Numbers")]
public class NumbersExtendedTests
{
    #region GetRangeBetween (int)

    [Fact]
    public void GetRangeBetween_Int_NormalRange_ReturnsAllInclusive()
    {
        Numbers.GetRangeBetween(1, 5).ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void GetRangeBetween_Int_SwappedMinMax_AutoSwaps()
    {
        Numbers.GetRangeBetween(5, 1).ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void GetRangeBetween_Int_SameMinMax_ReturnsSingleElement()
    {
        Numbers.GetRangeBetween(3, 3).ShouldBe(new[] { 3 });
    }

    #endregion

    #region GetRangeBetween (long)

    [Fact]
    public void GetRangeBetween_Long_NormalRange_ReturnsAllInclusive()
    {
        Numbers.GetRangeBetween(1L, 4L).ShouldBe(new[] { 1L, 2L, 3L, 4L });
    }

    [Fact]
    public void GetRangeBetween_Long_SwappedMinMax_AutoSwaps()
    {
        Numbers.GetRangeBetween(4L, 1L).ShouldBe(new[] { 1L, 2L, 3L, 4L });
    }

    #endregion

    #region Numbers.Is

    [Fact]
    public void IsNaN_Double_NaN_ReturnsTrue()
    {
        Numbers.IsNaN(double.NaN).ShouldBeTrue();
    }

    [Fact]
    public void IsNaN_Double_RealValue_ReturnsFalse()
    {
        Numbers.IsNaN(1.5).ShouldBeFalse();
    }

    [Fact]
    public void IsNaN_Float_NaN_ReturnsTrue()
    {
        Numbers.IsNaN(float.NaN).ShouldBeTrue();
    }

    [Fact]
    public void IsDefaultValue_Zero_ReturnsTrue()
    {
        Numbers.IsDefaultValue(0.0).ShouldBeTrue();
    }

    [Fact]
    public void IsDefaultValue_NonZero_ReturnsFalse()
    {
        Numbers.IsDefaultValue(1.5).ShouldBeFalse();
    }

    [Fact]
    public void IsJsonSafeInteger_WithinLimit_ReturnsTrue()
    {
        Numbers.IsJsonSafeInteger(9007199254740991UL).ShouldBeTrue();
    }

    [Fact]
    public void IsJsonSafeInteger_ExceedLimit_ReturnsFalse()
    {
        Numbers.IsJsonSafeInteger(9007199254740992UL).ShouldBeFalse();
    }

    #endregion

    #region IsZeroValue / IsNearZeroValue

    [Fact]
    public void IsZeroValue_Double_Zero_ReturnsTrue()
    {
        Numbers.IsZeroValue(0.0).ShouldBeTrue();
    }

    [Fact]
    public void IsZeroValue_Double_NonZero_ReturnsFalse()
    {
        Numbers.IsZeroValue(0.5).ShouldBeFalse();
    }

    [Fact]
    public void IsZeroValue_Float_Zero_ReturnsTrue()
    {
        Numbers.IsZeroValue(0f).ShouldBeTrue();
    }

    [Fact]
    public void IsNearZeroValue_WithinPrecision_ReturnsTrue()
    {
        Numbers.IsNearZeroValue(0.0005, 0.001).ShouldBeTrue();
    }

    [Fact]
    public void IsNearZeroValue_OutsidePrecision_ReturnsFalse()
    {
        Numbers.IsNearZeroValue(0.01, 0.001).ShouldBeFalse();
    }

    #endregion

    #region IsNearEqual

    [Theory]
    [InlineData(1.0f, 1.0f, 0.01f, true)]
    [InlineData(1.0f, 1.005f, 0.01f, true)]
    [InlineData(1.0f, 1.1f, 0.01f, false)]
    public void IsNearEqual_Float_ReturnsCorrect(float a, float b, float tolerance, bool expected)
    {
        Numbers.IsNearEqual(a, b, tolerance).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1.0, 1.0, 0.01, true)]
    [InlineData(1.0, 1.005, 0.01, true)]
    [InlineData(1.0, 1.1, 0.01, false)]
    public void IsNearEqual_Double_ReturnsCorrect(double a, double b, double tolerance, bool expected)
    {
        Numbers.IsNearEqual(a, b, tolerance).ShouldBe(expected);
    }

    [Fact]
    public void IsNearEqual_Decimal_WithinTolerance_ReturnsTrue()
    {
        Numbers.IsNearEqual(1.00m, 1.005m, 0.01m).ShouldBeTrue();
    }

    [Fact]
    public void IsNearEqual_Decimal_OutsideTolerance_ReturnsFalse()
    {
        Numbers.IsNearEqual(1.00m, 1.1m, 0.01m).ShouldBeFalse();
    }

    [Fact]
    public void IsNearEqual_BothNaN_Double_ReturnsTrue()
    {
        Numbers.IsNearEqual(double.NaN, double.NaN, 0.01).ShouldBeTrue();
    }

    #endregion

    #region IsPreciseEqual

    [Fact]
    public void IsPreciseEqual_Double_SameValue_ReturnsTrue()
    {
        Numbers.IsPreciseEqual(1.5, 1.5).ShouldBeTrue();
    }

    [Fact]
    public void IsPreciseEqual_Double_DifferentValues_ReturnsFalse()
    {
        Numbers.IsPreciseEqual(1.5, 1.6).ShouldBeFalse();
    }

    [Fact]
    public void IsPreciseEqual_Float_SameValue_ReturnsTrue()
    {
        Numbers.IsPreciseEqual(1.5f, 1.5f).ShouldBeTrue();
    }

    [Fact]
    public void IsPreciseEqual_NullableDouble_BothNull_ReturnsTrue()
    {
        Numbers.IsPreciseEqual((double?)null, null).ShouldBeTrue();
    }

    [Fact]
    public void IsPreciseEqual_NullableDouble_OneNull_ReturnsFalse()
    {
        Numbers.IsPreciseEqual((double?)1.5, null).ShouldBeFalse();
    }

    #endregion

    #region Numbers.To

    [Fact]
    public void ToDecimal_FromFloat_ReturnsAccurateDecimal()
    {
        var result = Numbers.ToDecimal(1.5f);
        result.ShouldBe(1.5m);
    }

    [Fact]
    public void ToDecimal_FromFloat_WithPrecision_RoundsCorrectly()
    {
        var result = Numbers.ToDecimal(1.567f, 2);
        result.ShouldBe(1.57m);
    }

    [Fact]
    public void ToDecimal_FromDouble_ReturnsAccurateDecimal()
    {
        var result = Numbers.ToDecimal(1.5);
        result.ShouldBe(1.5m);
    }

    [Fact]
    public void ToDouble_FromFloat_ReturnsAccurateDouble()
    {
        var result = Numbers.ToDouble(1.5f);
        result.ShouldBe(1.5);
    }

    [Fact]
    public void ToDouble_FromFloat_WithPrecision_RoundsCorrectly()
    {
        var result = Numbers.ToDouble(1.567f, 2);
        result.ShouldBeInRange(1.56, 1.57);
    }

    [Fact]
    public void ToDouble_FromFloat_InvalidPrecision_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Numbers.ToDouble(1.5f, 16));
    }

    [Fact]
    public void ToDouble_FromNullableFloat_Null_ReturnsNaN()
    {
        Numbers.ToDouble((float?)null).ShouldBe(double.NaN);
    }

    #endregion

    #region Numbers — GetDecimalPlaces / GetSumAccurate / GetProductAccurate

    [Theory]
    [InlineData(1.23, 2)]
    [InlineData(1.5, 1)]
    [InlineData(100.0, 0)]
    public void GetDecimalPlaces_ReturnsCorrect(double value, int expected)
    {
        Numbers.GetDecimalPlaces(value).ShouldBe(expected);
    }

    [Fact]
    public void GetDecimalPlaces_NaN_ReturnsZero()
    {
        Numbers.GetDecimalPlaces(double.NaN).ShouldBe(0);
    }

    [Fact]
    public void GetSumAccurate_SimpleValues_ReturnsCorrectSum()
    {
        var result = Numbers.GetSumAccurate(0.1, 0.2);
        result.ShouldBe(0.3);
    }

    [Fact]
    public void GetProductAccurate_SimpleValues_ReturnsCorrectProduct()
    {
        var result = Numbers.GetProductAccurate(0.1, 3.0);
        result.ShouldBe(0.3);
    }

    [Fact]
    public void GetSumUsingIntegers_SimpleValues_ReturnsCorrectSum()
    {
        var result = Numbers.GetSumUsingIntegers(0.1, 0.2);
        result.ShouldBe(0.3);
    }

    #endregion

    #region FixZero

    [Fact]
    public void FixZero_ActualZero_ReturnsZero()
    {
        Numbers.FixZero(0.0).ShouldBe(0.0);
    }

    [Fact]
    public void FixZero_NonZeroValue_ReturnsOriginal()
    {
        Numbers.FixZero(1.5).ShouldBe(1.5);
    }

    #endregion

    #region NumberExtensions (extension methods)

    [Fact]
    public void IsDefault_Zero_ReturnsTrue()
    {
        0.0.IsDefault().ShouldBeTrue();
    }

    [Fact]
    public void IsZero_FloatNearZero_ReturnsTrue()
    {
        0.0f.IsZero().ShouldBeTrue();
    }

    [Fact]
    public void IsZero_DoubleNearZero_ReturnsTrue()
    {
        0.0.IsZero().ShouldBeTrue();
    }

    [Fact]
    public void IsNearZero_DoubleWithinPrecision_ReturnsTrue()
    {
        0.0005.IsNearZero(0.001).ShouldBeTrue();
    }

    [Fact]
    public void IsNearEqual_DoubleExtension_ReturnsCorrect()
    {
        1.0.IsNearEqual(1.005, 0.01).ShouldBeTrue();
    }

    [Fact]
    public void IsPreciseEqual_DoubleExtension_SameValue_ReturnsTrue()
    {
        1.5.IsPreciseEqual(1.5).ShouldBeTrue();
    }

    [Fact]
    public void ToDecimal_FloatExtension_ReturnsAccurateDecimal()
    {
        1.5f.ToDecimal().ShouldBe(1.5m);
    }

    [Fact]
    public void ToDouble_FloatExtension_ReturnsAccurateDouble()
    {
        1.5f.ToDouble().ShouldBe(1.5);
    }

    [Fact]
    public void DecimalPlaces_Extension_ReturnsCorrect()
    {
        1.23.DecimalPlaces().ShouldBe(2);
    }

    [Fact]
    public void SumAccurate_Extension_ReturnsCorrectSum()
    {
        0.1.SumAccurate(0.2).ShouldBe(0.3);
    }

    [Fact]
    public void ProductAccurate_Extension_ReturnsCorrectProduct()
    {
        0.1.ProductAccurate(3.0).ShouldBe(0.3);
    }

    [Fact]
    public void SumUsingIntegers_Extension_ReturnsCorrectSum()
    {
        0.1.SumUsingIntegers(0.2).ShouldBe(0.3);
    }

    [Fact]
    public void FixZero_Extension_ReturnsTrueZero()
    {
        0.0.FixZero().ShouldBe(0.0);
    }

    #endregion
}

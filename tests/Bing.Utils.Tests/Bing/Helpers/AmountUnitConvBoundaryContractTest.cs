using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 测试类：覆盖 AmountUnitConv 的边界输入与行为契约。
/// </summary>
[Trait("Bing.Helpers", "AmountUnitConv.BoundaryContract")]
public class AmountUnitConvBoundaryContractTest
{
    /// <summary>
    /// 测试用例：ToFen 在负数且超过两位小数时应截断而非四舍五入。
    /// </summary>
    [Theory]
    [InlineData(-1.239, -123)]
    [InlineData(-1.231, -123)]
    [InlineData(-0.009, 0)]
    public void ToFen_NegativeWithMoreThanTwoDecimals_CutsWithoutRounding(decimal yuan, int expectedFen)
    {
        var result = AmountUnitConv.ToFen(yuan);

        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试用例：ToFenLong 与 ToFen 在 int 范围内应保持一致。
    /// </summary>
    [Theory]
    [InlineData(0.00)]
    [InlineData(123.45)]
    [InlineData(-123.45)]
    [InlineData(9999.99)]
    public void ToFenLong_IntRangeInput_ShouldBeConsistentWithToFen(decimal yuan)
    {
        var asInt = AmountUnitConv.ToFen(yuan);
        var asLong = AmountUnitConv.ToFenLong(yuan);

        asLong.ShouldBe(asInt);
    }

    /// <summary>
    /// 测试用例：SumYuanAmounts 应逐项转换为分再求和（而非先求和再截断）。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSumYuanAmountsPerItemTruncationCases))]
    public void SumYuanAmounts_PerItemTruncationContract_ReturnsExpectedTotal(decimal[] amounts, decimal expected)
    {
        var result = AmountUnitConv.SumYuanAmounts(amounts);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：SumFenAmounts 不应修改输入数组。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSumFenAmountsCases))]
    public void SumFenAmounts_InputArray_ShouldNotBeMutated(int[] source, long expected)
    {
        var snapshot = source.ToArray();

        var result = AmountUnitConv.SumFenAmounts(source);

        result.ShouldBe(expected);
        source.ShouldBe(snapshot);
    }

    /// <summary>
    /// 测试用例：ToN2String(culture: null) 应使用当前线程文化。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetCurrentCultureN2Cases))]
    public void ToN2String_NullCulture_UsesCurrentCulture(string cultureName, decimal input)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;

            var result = AmountUnitConv.ToN2String(input, null);
            var expected = input.ToString("N2", culture);

            result.ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// 测试用例：IsValidAmount 在 minValue > maxValue 时应返回 false。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidRangeCases))]
    public void IsValidAmount_MinGreaterThanMax_ReturnsFalse(decimal yuan, decimal minValue, decimal maxValue)
    {
        var result = AmountUnitConv.IsValidAmount(yuan, minValue: minValue, maxValue: maxValue);

        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：SeparateYuanAndFen 对负小额金额应保持当前语义（0 元 + 负分）。
    /// </summary>
    [Theory]
    [InlineData(-0.01, 0, -1)]
    [InlineData(-0.99, 0, -99)]
    public void SeparateYuanAndFen_NegativeSmallAmount_KeepsCurrentSemantics(
        decimal yuan,
        int expectedYuanPart,
        int expectedFenPart)
    {
        var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(yuan);

        yuanPart.ShouldBe(expectedYuanPart);
        fenPart.ShouldBe(expectedFenPart);
    }

    /// <summary>
    /// 测试用例：ToYuan 与 ToFenLong 在大 long 金额场景下应可逆。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRoundTripFenCases))]
    public void ToYuanAndToFenLong_LargeLongValue_ShouldRoundTrip(long fen)
    {
        var yuan = AmountUnitConv.ToYuan(fen);
        var back = AmountUnitConv.ToFenLong(yuan);

        back.ShouldBe(fen);
    }

    /// <summary>
    /// 测试用例：ToFen(decimal?) 在 null 输入时返回 0。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetNullableToFenCases))]
    public void ToFen_NullableInput_ReturnsExpectedFen(decimal? yuan, int expectedFen)
    {
        var result = AmountUnitConv.ToFen(yuan);

        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试数据：SumYuanAmounts 的“逐项截断后再求和”契约样例
    /// </summary>
    public static IEnumerable<object[]> GetSumYuanAmountsPerItemTruncationCases()
    {
        yield return new object[] { new[] { 0.019m, 0.019m }, 0.02m };
        yield return new object[] { new[] { 1.239m, 1.239m }, 2.46m };
        yield return new object[] { new[] { -0.019m, 0.029m }, 0.01m };
    }

    /// <summary>
    /// 测试数据：SumFenAmounts 求和与输入不变性样例
    /// </summary>
    public static IEnumerable<object[]> GetSumFenAmountsCases()
    {
        yield return new object[] { new[] { 1, 2, 3, -1 }, 5L };
        yield return new object[] { Array.Empty<int>(), 0L };
        yield return new object[] { new[] { int.MaxValue, -1, -int.MaxValue }, -1L };
    }

    /// <summary>
    /// 测试数据：当前线程文化影响 ToN2String(culture:null) 的样例
    /// </summary>
    public static IEnumerable<object[]> GetCurrentCultureN2Cases()
    {
        yield return new object[] { "fr-FR", 1234.56m };
        yield return new object[] { "en-US", 1234.56m };
        yield return new object[] { "zh-CN", -1234.5m };
    }

    /// <summary>
    /// 测试数据：minValue > maxValue 的非法金额区间
    /// </summary>
    public static IEnumerable<object[]> GetInvalidRangeCases()
    {
        yield return new object[] { 10m, 100m, 0m };
        yield return new object[] { -1m, 1m, -1m };
        yield return new object[] { 0m, decimal.MaxValue, decimal.MinValue };
    }

    /// <summary>
    /// 测试数据：ToYuan 与 ToFenLong 的往返转换样例
    /// </summary>
    public static IEnumerable<object[]> GetRoundTripFenCases()
    {
        yield return new object[] { 0L };
        yield return new object[] { 1L };
        yield return new object[] { -1L };
        yield return new object[] { 900_000_000_000_001L };
        yield return new object[] { -900_000_000_000_001L };
    }

    /// <summary>
    /// 测试数据：ToFen(decimal?) 的空值与边界样例
    /// </summary>
    public static IEnumerable<object[]> GetNullableToFenCases()
    {
        yield return new object[] { null, 0 };
        yield return new object[] { 0m, 0 };
        yield return new object[] { 1.239m, 123 };
        yield return new object[] { -1.239m, -123 };
    }
}

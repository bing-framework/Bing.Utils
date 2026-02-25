using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Bing.Helpers;

/// <summary>
/// 测试类：覆盖 AmountUnitConv 的金额单位转换、格式化与边界契约。
/// </summary>
[Trait("Bing.Helpers", "AmountUnitConv")]
public class AmountUnitConvTest : TestBase
{
    /// <inheritdoc />
    public AmountUnitConvTest(ITestOutputHelper output) : base(output)
    {
    }

    #region ToYuan 测试

    /// <summary>
    /// 测试用例：ToYuan(int) 在正常与负数分值场景下返回正确元值。
    /// </summary>
    [Theory]
    [InlineData(0, 0.00)]
    [InlineData(1, 0.01)]
    [InlineData(10, 0.10)]
    [InlineData(100, 1.00)]
    [InlineData(12345, 123.45)]
    [InlineData(999999, 9999.99)]
    [InlineData(-100, -1.00)]
    [InlineData(-12345, -123.45)]
    public void ToYuan_IntFen_ReturnsCorrectYuan(int fen, decimal expectedYuan)
    {
        var result = AmountUnitConv.ToYuan(fen);

        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试用例：ToYuan(int?) 在 null 与有效输入场景下返回正确结果。
    /// </summary>
    [Theory]
    [InlineData(null, 0.00)]
    [InlineData(0, 0.00)]
    [InlineData(12345, 123.45)]
    [InlineData(-100, -1.00)]
    public void ToYuan_NullableIntFen_ReturnsCorrectYuan(int? fen, decimal expectedYuan)
    {
        var result = AmountUnitConv.ToYuan(fen);

        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试用例：ToYuan(long) 在大金额输入下返回正确元值。
    /// </summary>
    [Theory]
    [InlineData(0L, 0.00)]
    [InlineData(12345L, 123.45)]
    [InlineData(999999999999L, 9999999999.99)]
    [InlineData(-12345L, -123.45)]
    public void ToYuan_LongFen_ReturnsCorrectYuan(long fen, decimal expectedYuan)
    {
        var result = AmountUnitConv.ToYuan(fen);

        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试用例：ToYuan(long?) 在 null 与有效输入场景下返回正确结果。
    /// </summary>
    [Theory]
    [InlineData(null, 0.00)]
    [InlineData(0L, 0.00)]
    [InlineData(12345L, 123.45)]
    [InlineData(-100L, -1.00)]
    public void ToYuan_NullableLongFen_ReturnsCorrectYuan(long? fen, decimal expectedYuan)
    {
        var result = AmountUnitConv.ToYuan(fen);

        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试用例：ToYuan 在 int 极值输入下不应抛出异常。
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void ToYuan_ExtremeValues_HandlesCorrectly(int fen)
    {
        Should.NotThrow(() =>
        {
            var result = AmountUnitConv.ToYuan(fen);
            result.ShouldNotBe(decimal.MinValue);
            result.ShouldNotBe(decimal.MaxValue);
        });
    }

    #endregion

    #region ToFen 测试

    /// <summary>
    /// 测试用例：ToFen(decimal) 在常规输入下返回正确分值。
    /// </summary>
    [Theory]
    [InlineData(0.00, 0)]
    [InlineData(0.01, 1)]
    [InlineData(0.10, 10)]
    [InlineData(1.00, 100)]
    [InlineData(123.45, 12345)]
    [InlineData(9999.99, 999999)]
    [InlineData(-1.00, -100)]
    [InlineData(-123.45, -12345)]
    public void ToFen_DecimalYuan_ReturnsCorrectFen(decimal yuan, int expectedFen)
    {
        var result = AmountUnitConv.ToFen(yuan);

        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试用例：ToFen(decimal?) 在 null 与有效输入下返回正确分值。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetToFenNullableCases))]
    public void ToFen_NullableDecimalYuan_ReturnsCorrectFen(decimal? yuan, int expectedFen)
    {
        var result = AmountUnitConv.ToFen(yuan);

        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试用例：ToFen 对超过两位小数的金额执行截断而非四舍五入。
    /// </summary>
    [Theory]
    [InlineData(123.456, 12345)]
    [InlineData(123.499, 12349)]
    [InlineData(123.999, 12399)]
    [InlineData(0.009, 0)]
    [InlineData(0.019, 1)]
    public void ToFen_PrecisionHandling_CutsWithoutRounding(decimal yuan, int expectedFen)
    {
        var result = AmountUnitConv.ToFen(yuan);

        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试用例：ToFenLong 在大金额输入下返回正确 long 分值。
    /// </summary>
    [Theory]
    [InlineData(0.00, 0L)]
    [InlineData(123.45, 12345L)]
    [InlineData(9999999999.99, 999999999999L)]
    [InlineData(-123.45, -12345L)]
    public void ToFenLong_DecimalYuan_ReturnsCorrectLongFen(decimal yuan, long expectedFen)
    {
        var result = AmountUnitConv.ToFenLong(yuan);

        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试用例：ToFenLong(decimal?) 在 null 与有效输入下返回正确结果。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetToFenLongNullableCases))]
    public void ToFenLong_NullableDecimalYuan_ReturnsCorrectLongFen(decimal? yuan, long expectedFen)
    {
        var result = AmountUnitConv.ToFenLong(yuan);

        result.ShouldBe(expectedFen);
    }

    #endregion

    #region 往返转换测试

    /// <summary>
    /// 测试用例：分 -> 元 -> 分 的往返转换应保持一致。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(12345)]
    [InlineData(-100)]
    public void RoundTripConversion_FenToYuanToFen_MaintainsConsistency(int originalFen)
    {
        var yuan = AmountUnitConv.ToYuan(originalFen);
        var resultFen = AmountUnitConv.ToFen(yuan);

        resultFen.ShouldBe(originalFen);
    }

    /// <summary>
    /// 测试用例：元 -> 分 -> 元 的往返转换在两位小数场景下应保持一致。
    /// </summary>
    [Theory]
    [InlineData(0.00)]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(123.45)]
    [InlineData(-1.23)]
    public void RoundTripConversion_YuanToFenToYuan_MaintainsConsistency(decimal originalYuan)
    {
        var fen = AmountUnitConv.ToFen(originalYuan);
        var resultYuan = AmountUnitConv.ToYuan(fen);

        resultYuan.ShouldBe(originalYuan);
    }

    #endregion

    #region ToN2String 测试

    /// <summary>
    /// 测试用例：ToN2String 对金额格式化后应返回两位小数文本。
    /// </summary>
    [Theory]
    [InlineData(0, "0.00")]
    [InlineData(123.45, "123.45")]
    [InlineData(1000, "1,000.00")]
    [InlineData(1234567.89, "1,234,567.89")]
    [InlineData(-123.45, "-123.45")]
    public void ToN2String_DecimalInput_ReturnsFormattedString(decimal input, string expected)
    {
        var result = AmountUnitConv.ToN2String(input);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：ToN2String 在指定文化与默认文化下应返回一致格式。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetToN2StringCultureCases))]
    public void ToN2String_WithCulture_ReturnsCorrectFormat(decimal input, string cultureName)
    {
        var culture = cultureName == null ? null : new CultureInfo(cultureName);
        var expected = input.ToString("N2", culture ?? CultureInfo.CurrentCulture);

        var result = AmountUnitConv.ToN2String(input, culture);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：SeparateYuanAndFen 对常规输入应正确拆分元与分。
    /// </summary>
    [Theory]
    [InlineData(0.00, 0, 0)]
    [InlineData(123.45, 123, 45)]
    [InlineData(100.00, 100, 0)]
    [InlineData(0.99, 0, 99)]
    [InlineData(1000.01, 1000, 1)]
    [InlineData(-123.45, -123, -45)]
    public void SeparateYuanAndFen_ValidAmounts_ReturnsCorrectSeparation(decimal yuan, int expectedYuanPart, int expectedFenPart)
    {
        var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(yuan);

        yuanPart.ShouldBe(expectedYuanPart);
        fenPart.ShouldBe(expectedFenPart);
    }

    /// <summary>
    /// 测试用例：SeparateYuanAndFen 在超过两位小数时应按截断语义处理。
    /// </summary>
    [Theory]
    [InlineData(123.456, 123, 45)]
    [InlineData(123.999, 123, 99)]
    public void SeparateYuanAndFen_PrecisionHandling_CutsCorrectly(decimal yuan, int expectedYuanPart, int expectedFenPart)
    {
        var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(yuan);

        yuanPart.ShouldBe(expectedYuanPart);
        fenPart.ShouldBe(expectedFenPart);
    }

    #endregion

    #region IsValidAmount 测试

    /// <summary>
    /// 测试用例：IsValidAmount 在默认区间下返回正确结果。
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(100.50, true)]
    [InlineData(-1, false)]
    public void IsValidAmount_DefaultRange_ReturnsCorrectValidation(decimal amount, bool expected)
    {
        var result = AmountUnitConv.IsValidAmount(amount);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：IsValidAmount 在自定义区间下返回正确结果。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetIsValidAmountCustomRangeCases))]
    public void IsValidAmount_CustomRange_ReturnsCorrectValidation(decimal amount, decimal min, decimal max, bool expected)
    {
        var result = AmountUnitConv.IsValidAmount(amount, min, max);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：IsValidAmount 在 minValue 大于 maxValue 的非法区间下应恒为 false。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetIsValidAmountInvalidRangeCases))]
    public void IsValidAmount_InvalidRange_ReturnsFalse(decimal amount, decimal min, decimal max)
    {
        var result = AmountUnitConv.IsValidAmount(amount, min, max);

        result.ShouldBeFalse();
    }

    #endregion

    #region SumFenAmounts 测试

    /// <summary>
    /// 测试用例：SumFenAmounts 在 null/空/单值/重复值/大样本等场景下返回正确合计，且不修改输入。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSumFenAmountsCases))]
    public void SumFenAmounts_BoundaryAndRepresentativeInputs_ReturnsExpected(int[] amounts, long expected)
    {
        var snapshot = amounts?.ToArray();

        var result = AmountUnitConv.SumFenAmounts(amounts);

        result.ShouldBe(expected);
        if (snapshot != null)
            amounts.ShouldBe(snapshot);
    }

    /// <summary>
    /// 测试用例：SumYuanAmounts 在 null/空/重复值/大样本/截断精度场景下返回正确合计，且不修改输入。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetSumYuanAmountsCases))]
    public void SumYuanAmounts_BoundaryAndRepresentativeInputs_ReturnsExpected(decimal[] amounts, decimal expected)
    {
        var snapshot = amounts?.ToArray();

        var result = AmountUnitConv.SumYuanAmounts(amounts);

        result.ShouldBe(expected);
        if (snapshot != null)
            amounts.ShouldBe(snapshot);
    }

    /// <summary>
    /// 测试用例：私有方法 CutDecimalWithN 在负数小数位输入时抛出 ArgumentOutOfRangeException。
    /// </summary>
    [Fact]
    public void CutDecimalWithN_NegativeDigits_ThrowsArgumentOutOfRangeException()
    {
        var exception = Should.Throw<TargetInvocationException>(() =>
        {
            var method = typeof(AmountUnitConv).GetMethod("CutDecimalWithN",
                BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, new object[] { 123.456m, -1 });
        });

        var innerException = exception.InnerException.ShouldBeOfType<ArgumentOutOfRangeException>();
        innerException.ParamName.ShouldBe("digits");
        innerException.Message.ShouldContain("小数位数不能为负数");
    }

    /// <summary>
    /// 测试用例：极值场景下 ToYuan/ToFenLong 组合不应发生溢出异常。
    /// </summary>
    [Fact]
    public void ExtremeValues_DoNotCauseOverflow()
    {
        Should.NotThrow(() =>
        {
            var maxSafeFen = long.MaxValue / 100;
            var maxSafeYuan = AmountUnitConv.ToYuan(maxSafeFen);
            var backToFen = AmountUnitConv.ToFenLong(maxSafeYuan);
            maxSafeYuan.ShouldBeGreaterThan(0);
            backToFen.ShouldBeGreaterThan(0);
        });
    }

    /// <summary>
    /// 测试用例：高频转换应在合理时间内完成。
    /// </summary>
    [Fact]
    public void PerformanceTest_LargeNumberOfConversions_CompletesInReasonableTime()
    {
        const int iterations = 10000;

        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var fen = i;
                var yuan = AmountUnitConv.ToYuan(fen);
                var backToFen = AmountUnitConv.ToFen(yuan);
                var formatted = AmountUnitConv.ToN2String(yuan);
                backToFen.ShouldBe(fen);
                formatted.ShouldNotBeNull();
            }
        }, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// 测试用例：并发调用转换方法不应相互干扰。
    /// </summary>
    [Fact]
    public void ThreadSafety_ConcurrentCalls_DoNotInterfere()
    {
        const int threadCount = 10;
        const int operationsPerThread = 1000;
        var tasks = new System.Threading.Tasks.Task[threadCount];

        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var testValue = threadId * operationsPerThread + i;
                    var yuan = AmountUnitConv.ToYuan(testValue);
                    var fen = AmountUnitConv.ToFen(yuan);
                    fen.ShouldBe(testValue);
                }
            });
        }

        Should.NotThrow(() => System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(10)));
    }

    #endregion

    #region 场景测试

    /// <summary>
    /// 测试用例：购物车金额汇总与展示场景。
    /// </summary>
    [Fact]
    public void RealWorldScenario_ShoppingCartCalculation_WorksCorrectly()
    {
        var products = new[]
        {
            new { Name = "商品A", Price = 99.99m, Quantity = 2 },
            new { Name = "商品B", Price = 149.50m, Quantity = 1 },
            new { Name = "商品C", Price = 29.90m, Quantity = 3 }
        };

        var totalYuan = 0m;
        foreach (var product in products)
            totalYuan += product.Price * product.Quantity;

        var totalFen = AmountUnitConv.ToFen(totalYuan);
        var formattedTotal = AmountUnitConv.ToN2String(totalYuan);
        var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(totalYuan);

        totalYuan.ShouldBe(439.18m);
        totalFen.ShouldBe(43918);
        formattedTotal.ShouldBe("439.18");
        yuanPart.ShouldBe(439);
        fenPart.ShouldBe(18);

        Output.WriteLine($"购物车总金额: {formattedTotal}");
        Output.WriteLine($"拆分显示: {yuanPart}元{fenPart}分");
    }

    /// <summary>
    /// 测试用例：批量转账金额验证与往返转换场景。
    /// </summary>
    [Fact]
    public void RealWorldScenario_BatchTransferValidation_WorksCorrectly()
    {
        var transfers = new[]
        {
            1000.00m,
            500.50m,
            999.99m,
            0.01m
        };

        foreach (var amount in transfers)
        {
            AmountUnitConv.IsValidAmount(amount).ShouldBeTrue();

            var fenAmount = AmountUnitConv.ToFenLong(amount);
            var displayAmount = AmountUnitConv.ToYuan(fenAmount);

            displayAmount.ShouldBe(amount);
            Output.WriteLine($"转账金额: {amount} -> {fenAmount}分 -> {displayAmount}");
        }

        var totalAmount = AmountUnitConv.SumYuanAmounts(transfers);
        totalAmount.ShouldBe(2500.50m);
        Output.WriteLine($"批量转账总金额: {AmountUnitConv.ToN2String(totalAmount)}");
    }

    #endregion


    #region TestData

    /// <summary>
    /// 测试数据：ToN2String 文化格式输入。
    /// </summary>
    public static IEnumerable<object[]> GetToN2StringCultureCases()
    {
        yield return new object[] { 1234.56m, "en-US" };
        yield return new object[] { 1234.56m, "de-DE" };
        yield return new object[] { 1234.56m, null };
    }

    /// <summary>
    /// 测试数据：ToFen(decimal?) 的 null/边界/代表值输入。
    /// </summary>
    public static IEnumerable<object[]> GetToFenNullableCases()
    {
        yield return new object[] { (decimal?)null, 0 };
        yield return new object[] { 0.00m, 0 };
        yield return new object[] { 123.45m, 12345 };
        yield return new object[] { -1.00m, -100 };
    }

    /// <summary>
    /// 测试数据：ToFenLong(decimal?) 的 null/边界/代表值输入。
    /// </summary>
    public static IEnumerable<object[]> GetToFenLongNullableCases()
    {
        yield return new object[] { (decimal?)null, 0L };
        yield return new object[] { 123.45m, 12345L };
    }

    /// <summary>
    /// 测试数据：SumFenAmounts 边界与代表值集合。
    /// </summary>
    public static IEnumerable<object[]> GetSumFenAmountsCases()
    {
        yield return new object[] { null, 0L };
        yield return new object[] { Array.Empty<int>(), 0L };
        yield return new object[] { new[] { 100 }, 100L };
        yield return new object[] { new[] { 100, 200, 300 }, 600L };
        yield return new object[] { new[] { 100, -50, 200, -30 }, 220L };
        yield return new object[] { new[] { 5, 5, 5, 5 }, 20L };
        yield return new object[] { Enumerable.Repeat(1, 10000).ToArray(), 10000L };
        yield return new object[] { new[] { int.MaxValue, -1 }, (long)int.MaxValue - 1 };
        yield return new object[] { new[] { int.MinValue, 1 }, (long)int.MinValue + 1 };
    }

    /// <summary>
    /// 测试数据：SumYuanAmounts 边界与代表值集合。
    /// </summary>
    public static IEnumerable<object[]> GetSumYuanAmountsCases()
    {
        yield return new object[] { null, 0m };
        yield return new object[] { Array.Empty<decimal>(), 0m };
        yield return new object[] { new[] { 100.50m }, 100.50m };
        yield return new object[] { new[] { 1.00m, 2.00m, 3.00m }, 6.00m };
        yield return new object[] { new[] { 123.45m, 678.90m, 111.11m }, 913.46m };
        yield return new object[] { new[] { 0.01m, 0.02m, 0.03m, 0.04m }, 0.10m };
        yield return new object[] { new[] { 0.019m, 0.019m }, 0.02m };
        yield return new object[] { Enumerable.Repeat(0.01m, 5000).ToArray(), 50.00m };
    }

    /// <summary>
    /// 测试数据：IsValidAmount 自定义合法区间场景。
    /// </summary>
    public static IEnumerable<object[]> GetIsValidAmountCustomRangeCases()
    {
        yield return new object[] { 50m, 0m, 100m, true };
        yield return new object[] { 150m, 0m, 100m, false };
        yield return new object[] { -10m, -50m, 50m, true };
        yield return new object[] { -60m, -50m, 50m, false };
    }

    /// <summary>
    /// 测试数据：IsValidAmount 非法区间（minValue > maxValue）场景。
    /// </summary>
    public static IEnumerable<object[]> GetIsValidAmountInvalidRangeCases()
    {
        yield return new object[] { 0m, 10m, 0m };
        yield return new object[] { -1m, 10m, 0m };
        yield return new object[] { 100m, 10m, 0m };
    }

    #endregion

}

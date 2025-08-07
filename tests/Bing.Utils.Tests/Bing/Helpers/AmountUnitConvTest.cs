using System.Globalization;
using Bing.Tests;

namespace Bing.Helpers;

/// <summary>
/// 金额单位转换工具类测试
/// </summary>
[Trait("Bing.Helpers", "AmountUnitConv")]
public class AmountUnitConvTest:TestBase
{
    /// <inheritdoc />
    public AmountUnitConvTest(ITestOutputHelper output) : base(output)
    {
    }

    #region ToYuan 测试

    /// <summary>
    /// 测试 - ToYuan(int) - 分转元基本功能
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
        // Act
        var result = AmountUnitConv.ToYuan(fen);

        // Assert
        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试 - ToYuan(int?) - 可空分转元
    /// </summary>
    [Theory]
    [InlineData(null, 0.00)]
    [InlineData(0, 0.00)]
    [InlineData(12345, 123.45)]
    [InlineData(-100, -1.00)]
    public void ToYuan_NullableIntFen_ReturnsCorrectYuan(int? fen, decimal expectedYuan)
    {
        // Act
        var result = AmountUnitConv.ToYuan(fen);

        // Assert
        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试 - ToYuan(long) - 长整型分转元
    /// </summary>
    [Theory]
    [InlineData(0L, 0.00)]
    [InlineData(12345L, 123.45)]
    [InlineData(999999999999L, 9999999999.99)]
    [InlineData(-12345L, -123.45)]
    public void ToYuan_LongFen_ReturnsCorrectYuan(long fen, decimal expectedYuan)
    {
        // Act
        var result = AmountUnitConv.ToYuan(fen);

        // Assert
        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试 - ToYuan(long?) - 可空长整型分转元
    /// </summary>
    [Theory]
    [InlineData(null, 0.00)]
    [InlineData(0L, 0.00)]
    [InlineData(12345L, 123.45)]
    [InlineData(-100L, -1.00)]
    public void ToYuan_NullableLongFen_ReturnsCorrectYuan(long? fen, decimal expectedYuan)
    {
        // Act
        var result = AmountUnitConv.ToYuan(fen);

        // Assert
        result.ShouldBe(expectedYuan);
    }

    /// <summary>
    /// 测试 - ToYuan - 极值测试
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void ToYuan_ExtremeValues_HandlesCorrectly(int fen)
    {
        // Act & Assert - 主要确保不抛异常
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
    /// 测试 - ToFen(decimal) - 元转分基本功能
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
        // Act
        var result = AmountUnitConv.ToFen(yuan);

        // Assert
        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试 - ToFen(decimal?) - 可空元转分
    /// </summary>
    [Theory]
    [InlineData(null, 0)]
    [InlineData(0.00, 0)]
    [InlineData(123.45, 12345)]
    [InlineData(-1.00, -100)]
    public void ToFen_NullableDecimalYuan_ReturnsCorrectFen(object yuan, int expectedFen)
    {
        // Act
        var result = AmountUnitConv.ToFen(Conv.ToDecimalOrNull(yuan));

        // Assert
        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试 - ToFen - 精度处理（截取而非四舍五入）
    /// </summary>
    [Theory]
    [InlineData(123.456, 12345)]   // 截取到两位小数
    [InlineData(123.499, 12349)]   // 不进行四舍五入
    [InlineData(123.999, 12399)]   // 不进行四舍五入
    [InlineData(0.009, 0)]         // 小于0.01的金额截取为0
    [InlineData(0.019, 1)]         // 0.01-0.019之间截取为1分
    public void ToFen_PrecisionHandling_CutsWithoutRounding(decimal yuan, int expectedFen)
    {
        // Act
        var result = AmountUnitConv.ToFen(yuan);

        // Assert
        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试 - ToFenLong - 大金额处理
    /// </summary>
    [Theory]
    [InlineData(0.00, 0L)]
    [InlineData(123.45, 12345L)]
    [InlineData(9999999999.99, 999999999999L)]
    [InlineData(-123.45, -12345L)]
    public void ToFenLong_DecimalYuan_ReturnsCorrectLongFen(decimal yuan, long expectedFen)
    {
        // Act
        var result = AmountUnitConv.ToFenLong(yuan);

        // Assert
        result.ShouldBe(expectedFen);
    }

    /// <summary>
    /// 测试 - ToFenLong - 可空类型
    /// </summary>
    [Theory]
    [InlineData(null, 0L)]
    [InlineData(123.45, 12345L)]
    public void ToFenLong_NullableDecimalYuan_ReturnsCorrectLongFen(object yuan, long expectedFen)
    {
        // Act
        var result = AmountUnitConv.ToFenLong(Conv.ToDecimalOrNull(yuan));

        // Assert
        result.ShouldBe(expectedFen);
    }

    #endregion

    #region 往返转换测试

    /// <summary>
    /// 测试 - 往返转换 - 分->元->分应该保持一致
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(12345)]
    [InlineData(-100)]
    public void RoundTripConversion_FenToYuanToFen_MaintainsConsistency(int originalFen)
    {
        // Act
        var yuan = AmountUnitConv.ToYuan(originalFen);
        var resultFen = AmountUnitConv.ToFen(yuan);

        // Assert
        resultFen.ShouldBe(originalFen);
    }

    /// <summary>
    /// 测试 - 往返转换 - 元->分->元应该保持一致（对于两位小数的金额）
    /// </summary>
    [Theory]
    [InlineData(0.00)]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(123.45)]
    [InlineData(-1.23)]
    public void RoundTripConversion_YuanToFenToYuan_MaintainsConsistency(decimal originalYuan)
    {
        // Act
        var fen = AmountUnitConv.ToFen(originalYuan);
        var resultYuan = AmountUnitConv.ToYuan(fen);

        // Assert
        resultYuan.ShouldBe(originalYuan);
    }

    #endregion

    #region ToN2String 测试

    /// <summary>
    /// 测试 - ToN2String - 基本格式化功能
    /// </summary>
    [Theory]
    [InlineData(0, "0.00")]
    [InlineData(123.45, "123.45")]
    [InlineData(1000, "1,000.00")]
    [InlineData(1234567.89, "1,234,567.89")]
    [InlineData(-123.45, "-123.45")]
    public void ToN2String_DecimalInput_ReturnsFormattedString(decimal input, string expected)
    {
        // Act
        var result = AmountUnitConv.ToN2String(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToN2String - 带区域文化参数
    /// </summary>
    [Fact]
    public void ToN2String_WithCulture_ReturnsCorrectFormat()
    {
        // Arrange
        const decimal amount = 1234.56m;
        var usCulture = new CultureInfo("en-US");
        var germanCulture = new CultureInfo("de-DE");

        // Act
        var usResult = AmountUnitConv.ToN2String(amount, usCulture);
        var germanResult = AmountUnitConv.ToN2String(amount, germanCulture);
        var nullCultureResult = AmountUnitConv.ToN2String(amount, null);

        // Assert
        usResult.ShouldBe("1,234.56");
        germanResult.ShouldBe("1.234,56");
        nullCultureResult.ShouldNotBeNull();
        nullCultureResult.ShouldNotBeEmpty();
    }

    #endregion

    #region SeparateYuanAndFen 测试

    /// <summary>
    /// 测试 - SeparateYuanAndFen - 分离元和分
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
        // Act
        var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(yuan);

        // Assert
        yuanPart.ShouldBe(expectedYuanPart);
        fenPart.ShouldBe(expectedFenPart);
    }

    /// <summary>
    /// 测试 - SeparateYuanAndFen - 精度处理
    /// </summary>
    [Theory]
    [InlineData(123.456, 123, 45)]   // 截取处理
    [InlineData(123.999, 123, 99)]   // 不四舍五入
    public void SeparateYuanAndFen_PrecisionHandling_CutsCorrectly(decimal yuan, int expectedYuanPart, int expectedFenPart)
    {
        // Act
        var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(yuan);

        // Assert
        yuanPart.ShouldBe(expectedYuanPart);
        fenPart.ShouldBe(expectedFenPart);
    }

    #endregion

    #region IsValidAmount 测试

    /// <summary>
    /// 测试 - IsValidAmount - 默认范围验证
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(100.50, true)]
    [InlineData(-1, false)]
    //[InlineData(decimal.MaxValue, true)]
    public void IsValidAmount_DefaultRange_ReturnsCorrectValidation(decimal amount, bool expected)
    {
        // Act
        var result = AmountUnitConv.IsValidAmount(amount);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - IsValidAmount - 自定义范围验证
    /// </summary>
    [Theory]
    [InlineData(50, 0, 100, true)]
    [InlineData(150, 0, 100, false)]
    [InlineData(-10, -50, 50, true)]
    [InlineData(-60, -50, 50, false)]
    public void IsValidAmount_CustomRange_ReturnsCorrectValidation(decimal amount, decimal min, decimal max, bool expected)
    {
        // Act
        var result = AmountUnitConv.IsValidAmount(amount, min, max);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region SumFenAmounts 测试

    /// <summary>
    /// 测试 - SumFenAmounts - 分金额求和
    /// </summary>
    [Fact]
    public void SumFenAmounts_ValidArrays_ReturnsCorrectSum()
    {
        // Arrange
        var amounts1 = new[] { 100, 200, 300 };
        var amounts2 = new[] { 12345, 67890, 11111 };
        var singleAmount = new[] { 100 };

        // Act
        var result1 = AmountUnitConv.SumFenAmounts(amounts1);
        var result2 = AmountUnitConv.SumFenAmounts(amounts2);
        var result3 = AmountUnitConv.SumFenAmounts(singleAmount);

        // Assert
        result1.ShouldBe(600L);
        result2.ShouldBe(91346L);
        result3.ShouldBe(100L);
    }

    /// <summary>
    /// 测试 - SumFenAmounts - 空数组和null处理
    /// </summary>
    [Fact]
    public void SumFenAmounts_EmptyAndNullArrays_ReturnsZero()
    {
        // Act
        var nullResult = AmountUnitConv.SumFenAmounts(null);
        var emptyResult = AmountUnitConv.SumFenAmounts();

        // Assert
        nullResult.ShouldBe(0L);
        emptyResult.ShouldBe(0L);
    }

    /// <summary>
    /// 测试 - SumFenAmounts - 包含负数
    /// </summary>
    [Fact]
    public void SumFenAmounts_WithNegativeNumbers_ReturnsCorrectSum()
    {
        // Arrange
        var amounts = new[] { 100, -50, 200, -30 };

        // Act
        var result = AmountUnitConv.SumFenAmounts(amounts);

        // Assert
        result.ShouldBe(220L);
    }

    #endregion

    #region SumYuanAmounts 测试

    /// <summary>
    /// 测试 - SumYuanAmounts - 元金额求和
    /// </summary>
    [Fact]
    public void SumYuanAmounts_ValidArrays_ReturnsCorrectSum()
    {
        // Arrange
        var amounts1 = new[] { 1.00m, 2.00m, 3.00m };
        var amounts2 = new[] { 123.45m, 678.90m, 111.11m };
        var singleAmount = new[] { 100.50m };

        // Act
        var result1 = AmountUnitConv.SumYuanAmounts(amounts1);
        var result2 = AmountUnitConv.SumYuanAmounts(amounts2);
        var result3 = AmountUnitConv.SumYuanAmounts(singleAmount);

        // Assert
        result1.ShouldBe(6.00m);
        result2.ShouldBe(913.46m);
        result3.ShouldBe(100.50m);
    }

    /// <summary>
    /// 测试 - SumYuanAmounts - 空数组和null处理
    /// </summary>
    [Fact]
    public void SumYuanAmounts_EmptyAndNullArrays_ReturnsZero()
    {
        // Act
        var nullResult = AmountUnitConv.SumYuanAmounts(null);
        var emptyResult = AmountUnitConv.SumYuanAmounts();

        // Assert
        nullResult.ShouldBe(0m);
        emptyResult.ShouldBe(0m);
    }

    /// <summary>
    /// 测试 - SumYuanAmounts - 精度测试
    /// </summary>
    [Fact]
    public void SumYuanAmounts_PrecisionTest_MaintainsAccuracy()
    {
        // Arrange
        var amounts = new[] { 0.01m, 0.02m, 0.03m, 0.04m };

        // Act
        var result = AmountUnitConv.SumYuanAmounts(amounts);

        // Assert
        result.ShouldBe(0.10m);
    }

    #endregion

    #region 边界条件和异常测试

    /// <summary>
    /// 测试 - CutDecimalWithN - 负数小数位抛出异常
    /// </summary>
    [Fact]
    public void CutDecimalWithN_NegativeDigits_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Should.Throw<TargetInvocationException>(() =>
        {
            // 通过反射调用私有方法进行测试
            var method = typeof(AmountUnitConv).GetMethod("CutDecimalWithN",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, new object[] { 123.456m, -1 });
        });

        // 验证内部异常是我们期望的类型
        exception.InnerException.ShouldBeOfType<ArgumentOutOfRangeException>();
        exception.InnerException.Message.ShouldContain("小数位数不能为负数");
    }

    /// <summary>
    /// 测试 - 极值处理 - 确保不会溢出
    /// </summary>
    [Fact]
    public void ExtremeValues_DoNotCauseOverflow()
    {
        // Act & Assert - 主要确保不抛异常
        Should.NotThrow(() =>
        {
            // 测试最大安全金额
            var maxSafeFen = long.MaxValue / 100;
            var maxSafeYuan = AmountUnitConv.ToYuan(maxSafeFen);
            var backToFen = AmountUnitConv.ToFenLong(maxSafeYuan);

            maxSafeYuan.ShouldBeGreaterThan(0);
            backToFen.ShouldBeGreaterThan(0);
        });
    }

    /// <summary>
    /// 测试 - 性能测试 - 大量转换操作
    /// </summary>
    [Fact]
    public void PerformanceTest_LargeNumberOfConversions_CompletesInReasonableTime()
    {
        // Arrange
        const int iterations = 10000;

        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var fen = i;
                var yuan = AmountUnitConv.ToYuan(fen);
                var backToFen = AmountUnitConv.ToFen(yuan);
                var formatted = AmountUnitConv.ToN2String(yuan);

                // 简单验证确保操作正常
                backToFen.ShouldBe(fen);
                formatted.ShouldNotBeNull();
            }
        }, TimeSpan.FromSeconds(2)); // 应该在2秒内完成10000次转换
    }

    /// <summary>
    /// 测试 - 线程安全性 - 并发调用不会产生问题
    /// </summary>
    [Fact]
    public void ThreadSafety_ConcurrentCalls_DoNotInterfere()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 1000;
        var tasks = new System.Threading.Tasks.Task[threadCount];

        // Act
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

                    // 验证转换的正确性
                    fen.ShouldBe(testValue);
                }
            });
        }

        // Assert
        Should.NotThrow(() => System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(10)));
    }

    #endregion

    #region 实际业务场景测试

    /// <summary>
    /// 测试 - 实际业务场景 - 购物车金额计算
    /// </summary>
    [Fact]
    public void RealWorldScenario_ShoppingCartCalculation_WorksCorrectly()
    {
        // Arrange - 模拟购物车商品
        var products = new[]
        {
            new { Name = "商品A", Price = 99.99m, Quantity = 2 },
            new { Name = "商品B", Price = 149.50m, Quantity = 1 },
            new { Name = "商品C", Price = 29.90m, Quantity = 3 }
        };

        // Act - 计算总金额
        var totalYuan = 0m;
        foreach (var product in products)
        {
            totalYuan += product.Price * product.Quantity;
        }

        var totalFen = AmountUnitConv.ToFen(totalYuan);
        var formattedTotal = AmountUnitConv.ToN2String(totalYuan);
        var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(totalYuan);

        // Assert
        totalYuan.ShouldBe(439.18m); // 99.99*2 + 149.50*1 + 29.90*3
        totalFen.ShouldBe(43918);
        formattedTotal.ShouldBe("439.18");
        yuanPart.ShouldBe(439);
        fenPart.ShouldBe(18);

        Output.WriteLine($"购物车总金额: {formattedTotal}");
        Output.WriteLine($"分离显示: {yuanPart}元{fenPart}分");
    }

    /// <summary>
    /// 测试 - 实际业务场景 - 批量转账金额验证
    /// </summary>
    [Fact]
    public void RealWorldScenario_BatchTransferValidation_WorksCorrectly()
    {
        // Arrange - 模拟批量转账
        var transfers = new[]
        {
            1000.00m,   // 1000元
            500.50m,    // 500.5元
            999.99m,    // 999.99元
            0.01m       // 0.01元
        };

        // Act & Assert
        foreach (var amount in transfers)
        {
            // 验证金额有效性
            AmountUnitConv.IsValidAmount(amount).ShouldBeTrue();

            // 转换为分进行存储
            var fenAmount = AmountUnitConv.ToFenLong(amount);

            // 从分转换回元进行显示
            var displayAmount = AmountUnitConv.ToYuan(fenAmount);

            // 验证往返转换的一致性
            displayAmount.ShouldBe(amount);

            Output.WriteLine($"转账金额: {amount} -> {fenAmount}分 -> {displayAmount}");
        }

        // 计算总转账金额
        var totalAmount = AmountUnitConv.SumYuanAmounts(transfers);
        totalAmount.ShouldBe(2500.50m);

        Output.WriteLine($"批量转账总金额: {AmountUnitConv.ToN2String(totalAmount)}");
    }

    #endregion
}
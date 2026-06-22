using Bing.Extensions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="DecimalExtensions"/> 单元测试
/// </summary>
public class DecimalExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // Rounding() — 两位小数
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Rounding_TwoDecimals_RoundsCorrectly()
    {
        (1.235m).Rounding().ShouldBe(1.24m);
    }

    [Fact]
    public void Rounding_TwoDecimals_AlreadyRounded()
    {
        (1.23m).Rounding().ShouldBe(1.23m);
    }

    [Fact]
    public void Rounding_Zero_ReturnsZero()
    {
        (0m).Rounding().ShouldBe(0m);
    }

    [Fact]
    public void Rounding_Negative_RoundsCorrectly()
    {
        (-1.235m).Rounding().ShouldBe(-1.24m);
    }

    // ─────────────────────────────────────────────────────────────────
    // Rounding(int) — 指定小数位
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Rounding_FourDecimals_RoundsCorrectly()
    {
        (3.14159m).Rounding(4).ShouldBe(3.1416m);
    }

    [Fact]
    public void Rounding_ZeroDecimals_RoundsToInteger()
    {
        (2.5m).Rounding(0).ShouldBe(2m); // 银行家舍入
    }

    [Fact]
    public void Rounding_ThreeDecimals_RoundsCorrectly()
    {
        (1.2345m).Rounding(3).ShouldBe(1.234m); // 银行家舍入
    }

    // ─────────────────────────────────────────────────────────────────
    // Abs — 单值
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Abs_PositiveValue_ReturnsPositive()
    {
        (5.5m).Abs().ShouldBe(5.5m);
    }

    [Fact]
    public void Abs_NegativeValue_ReturnsPositive()
    {
        (-5.5m).Abs().ShouldBe(5.5m);
    }

    [Fact]
    public void Abs_Zero_ReturnsZero()
    {
        (0m).Abs().ShouldBe(0m);
    }

    // ─────────────────────────────────────────────────────────────────
    // Abs — 集合
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Abs_Collection_ConvertsAllToPositive()
    {
        var values = new[] { -1m, 2m, -3m, 0m };
        var result = values.Abs().ToList();
        result.ShouldBe(new[] { 1m, 2m, 3m, 0m });
    }

    [Fact]
    public void Abs_EmptyCollection_ReturnsEmpty()
    {
        var values = Array.Empty<decimal>();
        values.Abs().ShouldBeEmpty();
    }
}

/// <summary>
/// <see cref="DoubleExtensions"/> 单元测试
/// </summary>
public class DoubleExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // InRange (bool)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void InRange_ValueWithinRange_ReturnsTrue()
    {
        (5.0).InRange(1.0, 10.0).ShouldBeTrue();
    }

    [Fact]
    public void InRange_ValueAtMin_ReturnsTrue()
    {
        (1.0).InRange(1.0, 10.0).ShouldBeTrue();
    }

    [Fact]
    public void InRange_ValueAtMax_ReturnsTrue()
    {
        (10.0).InRange(1.0, 10.0).ShouldBeTrue();
    }

    [Fact]
    public void InRange_ValueBelowMin_ReturnsFalse()
    {
        (0.9).InRange(1.0, 10.0).ShouldBeFalse();
    }

    [Fact]
    public void InRange_ValueAboveMax_ReturnsFalse()
    {
        (10.1).InRange(1.0, 10.0).ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // InRange (with default)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void InRange_WithDefault_ReturnsValueWhenInRange()
    {
        (5.0).InRange(1.0, 10.0, -1.0).ShouldBe(5.0);
    }

    [Fact]
    public void InRange_WithDefault_ReturnsDefaultWhenOutOfRange()
    {
        (99.0).InRange(1.0, 10.0, -1.0).ShouldBe(-1.0);
    }

    // ─────────────────────────────────────────────────────────────────
    // TimeSpan factory
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Days_ReturnsCorrectTimeSpan()
    {
        (2.5).Days().ShouldBe(TimeSpan.FromDays(2.5));
    }

    [Fact]
    public void Hours_ReturnsCorrectTimeSpan()
    {
        (3.0).Hours().ShouldBe(TimeSpan.FromHours(3.0));
    }

    [Fact]
    public void Minutes_ReturnsCorrectTimeSpan()
    {
        (45.0).Minutes().ShouldBe(TimeSpan.FromMinutes(45.0));
    }

    [Fact]
    public void Seconds_ReturnsCorrectTimeSpan()
    {
        (30.0).Seconds().ShouldBe(TimeSpan.FromSeconds(30.0));
    }

    [Fact]
    public void Milliseconds_ReturnsCorrectTimeSpan()
    {
        (500.0).Milliseconds().ShouldBe(TimeSpan.FromMilliseconds(500.0));
    }

    // ─────────────────────────────────────────────────────────────────
    // Round
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Round_TwoDecimalsNoMultiple_RoundsCorrectly()
    {
        (3.14159).Round(2).ShouldBe(3.14);
    }

    [Fact]
    public void Round_WithMultiple100_ScalesAndRounds()
    {
        (0.1234).Round(2, 100).ShouldBe(12.34);
    }

    [Fact]
    public void Round_ZeroDecimals_ReturnsInteger()
    {
        (3.7).Round(0).ShouldBe(4.0);
    }

    // ─────────────────────────────────────────────────────────────────
    // FixValue
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void FixValue_NaN_ReturnsDefaultZero()
    {
        double.NaN.FixValue().ShouldBe(0.0);
    }

    [Fact]
    public void FixValue_NaN_ReturnsCustomDefault()
    {
        double.NaN.FixValue(-1.0).ShouldBe(-1.0);
    }

    [Fact]
    public void FixValue_PositiveInfinity_ReturnsDefault()
    {
        double.PositiveInfinity.FixValue().ShouldBe(0.0);
    }

    [Fact]
    public void FixValue_NegativeInfinity_ReturnsDefault()
    {
        double.NegativeInfinity.FixValue().ShouldBe(0.0);
    }

    [Fact]
    public void FixValue_NormalValue_ReturnsSameValue()
    {
        (3.14).FixValue().ShouldBe(3.14);
    }
}

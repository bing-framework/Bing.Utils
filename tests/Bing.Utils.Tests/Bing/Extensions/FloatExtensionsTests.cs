using Bing.Extensions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="FloatExtensions"/> 单元测试
/// </summary>
public class FloatExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // InRange
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void InRange_InsideRange_ReturnsTrue() =>
        5f.InRange(1f, 10f).ShouldBeTrue();

    [Fact]
    public void InRange_AtMin_ReturnsTrue() =>
        1f.InRange(1f, 10f).ShouldBeTrue();

    [Fact]
    public void InRange_AtMax_ReturnsTrue() =>
        10f.InRange(1f, 10f).ShouldBeTrue();

    [Fact]
    public void InRange_BelowMin_ReturnsFalse() =>
        0f.InRange(1f, 10f).ShouldBeFalse();

    [Fact]
    public void InRange_AboveMax_ReturnsFalse() =>
        11f.InRange(1f, 10f).ShouldBeFalse();

    [Fact]
    public void InRange_WithDefault_InsideRange_ReturnsValue() =>
        5f.InRange(1f, 10f, 99f).ShouldBe(5f);

    [Fact]
    public void InRange_WithDefault_OutsideRange_ReturnsDefault() =>
        15f.InRange(1f, 10f, 99f).ShouldBe(99f);

    // ─────────────────────────────────────────────────────────────────
    // TimeSpan factories
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Days_ReturnsCorrectTimeSpan() =>
        2f.Days().ShouldBe(TimeSpan.FromDays(2));

    [Fact]
    public void Hours_ReturnsCorrectTimeSpan() =>
        3f.Hours().ShouldBe(TimeSpan.FromHours(3));

    [Fact]
    public void Minutes_ReturnsCorrectTimeSpan() =>
        45f.Minutes().ShouldBe(TimeSpan.FromMinutes(45));

    [Fact]
    public void Seconds_ReturnsCorrectTimeSpan() =>
        30f.Seconds().ShouldBe(TimeSpan.FromSeconds(30));

    [Fact]
    public void Milliseconds_ReturnsCorrectTimeSpan() =>
        250f.Milliseconds().ShouldBe(TimeSpan.FromMilliseconds(250));
}

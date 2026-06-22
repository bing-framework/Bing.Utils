using Bing.Drawing;
using Bing.Conversions;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 <see cref="ColorExtensions"/> 相关行为。
/// </summary>
[Trait("Drawing", "ColorExtensions")]
public class ColorExtensionsTest
{
    #region GetGrayScale

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.GetGrayScale"/> 在 `WhiteColor` 场景下，结果为 `ReturnsOne`。
    /// 白色 (255,255,255) 的灰度应为 1.0
    /// </summary>
    [Fact]
    public void GetGrayScale_WhiteColor_ReturnsOne()
    {
        var white = Color.White;
        white.GetGrayScale().ShouldBe(1.0f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.GetGrayScale"/> 在 `BlackColor` 场景下，结果为 `ReturnsZero`。
    /// </summary>
    [Fact]
    public void GetGrayScale_BlackColor_ReturnsZero()
    {
        Color.Black.GetGrayScale().ShouldBe(0.0f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.GetGrayScale"/> 在 `GrayColor` 场景下，结果为 `ReturnsApproximatelyHalf`。
    /// 灰色 (128,128,128) 的灰度约为 0.5
    /// </summary>
    [Fact]
    public void GetGrayScale_GrayColor_ReturnsApproximatelyHalf()
    {
        var gray = Color.FromArgb(128, 128, 128);
        var result = gray.GetGrayScale();
        result.ShouldBeInRange(0.4f, 0.6f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.GetGrayScale"/> 在 `PureRedColor` 场景下，结果为 `UsesWeightedFormula`。
    /// 纯红色 (255,0,0) 灰度 = 0.30 * 255 / 255 ≈ 0.30
    /// </summary>
    [Fact]
    public void GetGrayScale_PureRedColor_UsesWeightedFormula()
    {
        var red = Color.FromArgb(255, 0, 0);
        var result = red.GetGrayScale();
        result.ShouldBe(0.30f, 0.01f);
    }

    #endregion

    #region Blend

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.Blend"/> 在 `Amount1_0` 场景下，结果为 `ReturnsOriginalColor`。
    /// amount=1.0 时完全保留前景色
    /// </summary>
    [Fact]
    public void Blend_Amount1_ReturnsOriginalColor()
    {
        var foreground = Color.FromArgb(255, 200, 100, 50);
        var background = Color.FromArgb(255, 50, 100, 200);
        var result = foreground.Blend(background, 1.0);
        result.R.ShouldBe(foreground.R);
        result.G.ShouldBe(foreground.G);
        result.B.ShouldBe(foreground.B);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.Blend"/> 在 `Amount0` 场景下，结果为 `ReturnsBackgroundColor`。
    /// amount=0 时完全使用背景色
    /// </summary>
    [Fact]
    public void Blend_Amount0_ReturnsBackgroundColor()
    {
        var foreground = Color.FromArgb(255, 200, 100, 50);
        var background = Color.FromArgb(255, 50, 100, 200);
        var result = foreground.Blend(background, 0.0);
        result.R.ShouldBe(background.R);
        result.G.ShouldBe(background.G);
        result.B.ShouldBe(background.B);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.Blend"/> 在 `Amount0_5` 场景下，结果为 `ReturnsMidpoint`。
    /// amount=0.5 时结果应在两色中间
    /// </summary>
    [Fact]
    public void Blend_Amount0_5_ReturnsMidpoint()
    {
        var foreground = Color.FromArgb(255, 200, 0, 0);
        var background = Color.FromArgb(255, 0, 0, 0);
        var result = foreground.Blend(background, 0.5);
        result.R.ShouldBe((byte)100); // 200 * 0.5 + 0 * 0.5 = 100
        result.G.ShouldBe((byte)0);
        result.B.ShouldBe((byte)0);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.Blend"/> 在 `PreservesAlpha` 场景下，结果为 `KeepsOriginalAlpha`。
    /// </summary>
    [Fact]
    public void Blend_PreservesAlpha_KeepsOriginalAlpha()
    {
        var foreground = Color.FromArgb(128, 100, 100, 100);
        var background = Color.FromArgb(255, 0, 0, 0);
        var result = foreground.Blend(background, 0.5);
        result.A.ShouldBe((byte)128);
    }

    #endregion

    #region IsSimilarColors

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.IsSimilarColors"/> 在 `SameColor` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsSimilarColors_SameColor_ReturnsTrue()
    {
        var color = Color.FromArgb(255, 100, 150, 200);
        color.IsSimilarColors(color).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.IsSimilarColors"/> 在 `VeryDifferentColors` 场景下，结果为 `ReturnsFalse`。
    /// 黑色与白色相差巨大
    /// </summary>
    [Fact]
    public void IsSimilarColors_VeryDifferentColors_ReturnsFalse()
    {
        Color.Black.IsSimilarColors(Color.White).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.IsSimilarColors"/> 在 `DifferentAlpha` 场景下，结果为 `ReturnsFalse`。
    /// Alpha 差值超过 1 时应返回 false
    /// </summary>
    [Fact]
    public void IsSimilarColors_DifferentAlpha_ReturnsFalse()
    {
        var colorA = Color.FromArgb(100, 128, 128, 128);
        var colorB = Color.FromArgb(200, 128, 128, 128);
        colorA.IsSimilarColors(colorB).ShouldBeFalse();
    }

    #endregion

    #region ColorDifference

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.ColorDifference"/> 在 `SameColor` 场景下，结果为 `ReturnsZero`。
    /// </summary>
    [Fact]
    public void ColorDifference_SameColor_ReturnsZero()
    {
        var color = Color.FromArgb(255, 100, 150, 200);
        color.ColorDifference(color).ShouldBe(0.0, 0.001);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.ColorDifference"/> 在 `BlackVsWhite` 场景下，结果为 `ReturnsLargeValue`。
    /// 黑白之间差异最大
    /// </summary>
    [Fact]
    public void ColorDifference_BlackVsWhite_ReturnsLargeValue()
    {
        var diff = Color.Black.ColorDifference(Color.White);
        diff.ShouldBeGreaterThan(100.0);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.ColorDifference"/> 在 `IsSymmetric` 场景下，结果为 `SameResultBothWays`。
    /// colorDifference(A,B) 应等于 colorDifference(B,A)
    /// </summary>
    [Fact]
    public void ColorDifference_IsSymmetric_SameResultBothWays()
    {
        var a = Color.FromArgb(255, 200, 100, 50);
        var b = Color.FromArgb(255, 50, 100, 200);
        a.ColorDifference(b).ShouldBe(b.ColorDifference(a), 0.001);
    }

    #endregion

    #region GetDuotoneColor

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.GetDuotoneColor"/> 在 `BlackSourceColor` 场景下，结果为 `ReturnsClr1`。
    /// 黑色灰度=0，双色调结果应接近 clr1
    /// </summary>
    [Fact]
    public void GetDuotoneColor_BlackSource_ReturnsClr1()
    {
        var black = Color.FromArgb(255, 0, 0, 0);
        var clr1 = Color.FromArgb(255, 255, 0, 0); // 红色
        var clr2 = Color.FromArgb(255, 0, 0, 255); // 蓝色
        var result = black.GetDuotoneColor(clr1, clr2);
        result.R.ShouldBe(clr1.R);
        result.G.ShouldBe(clr1.G);
        result.B.ShouldBe(clr1.B);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.GetDuotoneColor"/> 在 `WhiteSourceColor` 场景下，结果为 `ReturnsClr2`。
    /// 白色灰度=1，双色调结果应接近 clr2
    /// </summary>
    [Fact]
    public void GetDuotoneColor_WhiteSource_ReturnsClr2()
    {
        var white = Color.FromArgb(255, 255, 255, 255);
        var clr1 = Color.FromArgb(255, 255, 0, 0); // 红色
        var clr2 = Color.FromArgb(255, 0, 0, 255); // 蓝色
        var result = white.GetDuotoneColor(clr1, clr2);
        result.R.ShouldBe(clr2.R);
        result.G.ShouldBe(clr2.G);
        result.B.ShouldBe(clr2.B);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorExtensions.GetDuotoneColor"/> 在 `PreservesAlpha` 场景下，结果为 `KeepsSourceAlpha`。
    /// </summary>
    [Fact]
    public void GetDuotoneColor_PreservesAlpha()
    {
        var source = Color.FromArgb(128, 0, 0, 0);
        var clr1 = Color.Red;
        var clr2 = Color.Blue;
        var result = source.GetDuotoneColor(clr1, clr2);
        result.A.ShouldBe((byte)128);
    }

    #endregion
}

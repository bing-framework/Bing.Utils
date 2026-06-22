using System.Drawing;
using System.Drawing.Imaging;
using Bing.Drawing;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 <see cref="ColorEffect"/> 相关行为。
/// </summary>
[Trait("Drawing", "ColorEffect")]
public class ColorEffectTest
{
    private readonly ColorEffect _effect = new();

    #region ReplaceColor

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.ReplaceColor"/> 在 `MatchingPixel` 场景下，结果为 `PixelColorChanged`。
    /// </summary>
    [Fact]
    public void ReplaceColor_MatchingPixel_PixelColorChanged()
    {
        using var bitmap = CreateBitmap(1, 1, Color.Red);
        _effect.ReplaceColor(bitmap, Color.Red, Color.Blue);
        var result = bitmap.GetPixel(0, 0);
        result.R.ShouldBe((byte)0);
        result.G.ShouldBe((byte)0);
        result.B.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.ReplaceColor"/> 在 `NonMatchingPixel` 场景下，结果为 `PixelColorUnchanged`。
    /// 像素颜色与替换目标差距超过阈值时不应改变
    /// </summary>
    [Fact]
    public void ReplaceColor_NonMatchingPixel_PixelColorUnchanged()
    {
        using var bitmap = CreateBitmap(1, 1, Color.FromArgb(255, 0, 0));
        _effect.ReplaceColor(bitmap, Color.Blue, Color.Green);
        var result = bitmap.GetPixel(0, 0);
        result.R.ShouldBe((byte)255);
        result.G.ShouldBe((byte)0);
        result.B.ShouldBe((byte)0);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.ReplaceColor"/> 在 `MultiplPixels` 场景下，结果为 `AllMatchingPixelsReplaced`。
    /// 2x2 图像：全部像素均为红色，全部应被替换为蓝色
    /// </summary>
    [Fact]
    public void ReplaceColor_MultiplePixels_AllMatchingPixelsReplaced()
    {
        using var bitmap = CreateBitmap(2, 2, Color.Red);
        _effect.ReplaceColor(bitmap, Color.Red, Color.Blue);
        for (var x = 0; x < 2; x++)
        for (var y = 0; y < 2; y++)
        {
            var px = bitmap.GetPixel(x, y);
            px.B.ShouldBe((byte)255);
        }
    }

    #endregion

    #region SetBlackWhiteEffect

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.SetBlackWhiteEffect"/> 在 `WhitePixelAboveThreshold` 场景下，结果为 `PixelRemainsWhite`。
    /// </summary>
    [Fact]
    public void SetBlackWhiteEffect_WhitePixelAboveThreshold_PixelRemainsWhite()
    {
        using var bitmap = CreateBitmap(1, 1, Color.White);
        _effect.SetBlackWhiteEffect(bitmap, 0.5f);
        var result = bitmap.GetPixel(0, 0);
        result.R.ShouldBe((byte)255);
        result.G.ShouldBe((byte)255);
        result.B.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.SetBlackWhiteEffect"/> 在 `BlackPixelBelowThreshold` 场景下，结果为 `PixelRemainsBlack`。
    /// </summary>
    [Fact]
    public void SetBlackWhiteEffect_BlackPixelBelowThreshold_PixelRemainsBlack()
    {
        using var bitmap = CreateBitmap(1, 1, Color.Black);
        _effect.SetBlackWhiteEffect(bitmap, 0.5f);
        var result = bitmap.GetPixel(0, 0);
        result.R.ShouldBe((byte)0);
        result.G.ShouldBe((byte)0);
        result.B.ShouldBe((byte)0);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.SetBlackWhiteEffect"/> 在 `ThresholdZero` 场景下，结果为 `AllPixelsWhite`。
    /// 阈值为 0 时，任何灰度 >= 0 → 全白
    /// </summary>
    [Fact]
    public void SetBlackWhiteEffect_ThresholdZero_AllPixelsWhite()
    {
        using var bitmap = CreateBitmap(1, 1, Color.Black);
        _effect.SetBlackWhiteEffect(bitmap, 0.0f);
        var result = bitmap.GetPixel(0, 0);
        result.R.ShouldBe((byte)255);
        result.G.ShouldBe((byte)255);
        result.B.ShouldBe((byte)255);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.SetBlackWhiteEffect"/> 在 `ThresholdOne` 场景下，结果为 `OnlyWhitePixelRemainsWhite`。
    /// 阈值为 1 时，只有灰度=1.0 的像素为白色
    /// </summary>
    [Fact]
    public void SetBlackWhiteEffect_ThresholdOne_OnlyWhitePixelRemainsWhite()
    {
        using var bitmapWhite = CreateBitmap(1, 1, Color.White);
        using var bitmapGray = CreateBitmap(1, 1, Color.FromArgb(128, 128, 128));

        _effect.SetBlackWhiteEffect(bitmapWhite, 1.0f);
        _effect.SetBlackWhiteEffect(bitmapGray, 1.0f);

        var white = bitmapWhite.GetPixel(0, 0);
        var gray = bitmapGray.GetPixel(0, 0);

        white.R.ShouldBe((byte)255); // 白色灰度=1.0 >= 1.0 → 白
        gray.R.ShouldBe((byte)0);    // 灰色灰度<1.0 → 黑
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.SetBlackWhiteEffect"/> 保留像素的 Alpha 通道。
    /// </summary>
    [Fact]
    public void SetBlackWhiteEffect_PreservesAlpha()
    {
        using var bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        bitmap.SetPixel(0, 0, Color.FromArgb(128, 255, 255, 255));
        _effect.SetBlackWhiteEffect(bitmap, 0.5f);
        var result = bitmap.GetPixel(0, 0);
        result.A.ShouldBe((byte)128);
    }

    #endregion

    #region SetDuotoneEffect

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.SetDuotoneEffect"/> 在 `BlackSourcePixel` 场景下，结果为 `OutputEqualsColorA`。
    /// 黑色像素灰度=0 → 应返回 colorA
    /// </summary>
    [Fact]
    public void SetDuotoneEffect_BlackSourcePixel_OutputEqualsColorA()
    {
        using var bitmap = CreateBitmap(1, 1, Color.Black);
        var colorA = Color.FromArgb(255, 255, 0, 0); // 红
        var colorB = Color.FromArgb(255, 0, 0, 255); // 蓝
        _effect.SetDuotoneEffect(bitmap, colorA, colorB);
        var result = bitmap.GetPixel(0, 0);
        result.R.ShouldBe(colorA.R);
        result.G.ShouldBe(colorA.G);
        result.B.ShouldBe(colorA.B);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorEffect.SetDuotoneEffect"/> 在 `WhiteSourcePixel` 场景下，结果为 `OutputEqualsColorB`。
    /// 白色像素灰度=1 → 应返回 colorB
    /// </summary>
    [Fact]
    public void SetDuotoneEffect_WhiteSourcePixel_OutputEqualsColorB()
    {
        using var bitmap = CreateBitmap(1, 1, Color.White);
        var colorA = Color.FromArgb(255, 255, 0, 0); // 红
        var colorB = Color.FromArgb(255, 0, 0, 255); // 蓝
        _effect.SetDuotoneEffect(bitmap, colorA, colorB);
        var result = bitmap.GetPixel(0, 0);
        result.R.ShouldBe(colorB.R);
        result.G.ShouldBe(colorB.G);
        result.B.ShouldBe(colorB.B);
    }

    #endregion

    #region Helper

    /// <summary>
    /// 创建指定尺寸和颜色的测试用 Bitmap（Format32bppArgb）
    /// </summary>
    private static Bitmap CreateBitmap(int width, int height, Color fill)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            bitmap.SetPixel(x, y, fill);
        return bitmap;
    }

    #endregion
}

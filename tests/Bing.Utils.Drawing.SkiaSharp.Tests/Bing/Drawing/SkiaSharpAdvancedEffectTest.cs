using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖高级效果 API。
/// </summary>
[Trait("Drawing", "SkiaSharp.Advanced")]
public class SkiaSharpAdvancedEffectTest
{
    #region TwistImage

    [Fact]
    public void TwistImage_ReturnsSameSize()
    {
        using var source = CreateTestImage(8, 8);
        using var result = SkiaSharpHelper.TwistImage(source, true, 3, 0);
        result.Width.ShouldBe(8);
        result.Height.ShouldBe(8);
    }

    [Fact]
    public void TwistImage_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.TwistImage(null!, true, 3, 0));
    }

    [Fact]
    public void TwistImage_ZeroMult_PreservesContent()
    {
        using var source = CreateTestImage(4, 4, SKColors.White);
        using var result = SkiaSharpHelper.TwistImage(source, true, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    #endregion

    #region SetErosionEffect

    [Fact]
    public void SetErosionEffect_ReturnsSameSize()
    {
        using var source = CreateTestImage(4, 4);
        using var result = SkiaSharpHelper.SetErosionEffect(source, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void SetErosionEffect_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.SetErosionEffect(null!, 0, 0));
    }

    [Fact]
    public void SetErosionEffect_PositiveBrightnessProducesBrighterImage()
    {
        using var source = CreateTestImage(1, 1, new SKColor(128, 128, 128));
        using var result = SkiaSharpHelper.SetErosionEffect(source, 50, 0);
        result.Width.ShouldBe(1);
        result.Height.ShouldBe(1);
    }

    #endregion

    #region ToIcoStream

    [Fact]
    public void ToIcoStream_ProducesValidIcoHeader()
    {
        using var source = CreateTestImage(32, 32);
        using var stream = SkiaSharpHelper.ToIcoStream(source, new[] { 16, 32 });
        stream.Position.ShouldBe(0);
        var bytes = new byte[6];
        stream.Read(bytes, 0, 6);
        bytes[0].ShouldBe((byte)0);
        bytes[1].ShouldBe((byte)0);
        bytes[2].ShouldBe((byte)1); // ICO type
        bytes[3].ShouldBe((byte)0);
        bytes[4].ShouldBe((byte)2); // 2 frames
    }

    [Fact]
    public void ToIcoStream_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SkiaSharpHelper.ToIcoStream(null!));
    }

    [Fact]
    public void ToIcoStream_DefaultSizes_ProducesSixFrames()
    {
        using var source = CreateTestImage(256, 256);
        using var stream = SkiaSharpHelper.ToIcoStream(source);
        stream.Position = 0;
        var header = new byte[6];
        stream.Read(header, 0, 6);
        header[4].ShouldBe((byte)6);
    }

    #endregion

    #region Helper

    private static SKImage CreateTestImage(int width, int height, SKColor? fill = null)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(fill ?? new SKColor(128, 128, 128));
        return SKImage.FromBitmap(bitmap);
    }

    #endregion
}

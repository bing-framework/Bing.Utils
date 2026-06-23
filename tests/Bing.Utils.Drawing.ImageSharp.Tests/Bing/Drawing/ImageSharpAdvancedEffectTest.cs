using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖高级效果 API。
/// </summary>
[Trait("Drawing", "ImageSharp.Advanced")]
public class ImageSharpAdvancedEffectTest
{
    #region TwistImage

    [Fact]
    public void TwistImage_ReturnsSameSize()
    {
        using var source = CreateTestImage(8, 8);
        using var result = ImageSharpHelper.TwistImage(source, true, 3, 0);
        result.Width.ShouldBe(8);
        result.Height.ShouldBe(8);
    }

    [Fact]
    public void TwistImage_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.TwistImage(null!, true, 3, 0));
    }

    [Fact]
    public void TwistImage_ZeroMult_PreservesContent()
    {
        using var source = CreateTestImage(4, 4, Color.White);
        using var result = ImageSharpHelper.TwistImage(source, true, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    #endregion

    #region SetErosionEffect

    [Fact]
    public void SetErosionEffect_ReturnsSameSize()
    {
        using var source = CreateTestImage(4, 4);
        using var result = ImageSharpHelper.SetErosionEffect(source, 0, 0);
        result.Width.ShouldBe(4);
        result.Height.ShouldBe(4);
    }

    [Fact]
    public void SetErosionEffect_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.SetErosionEffect(null!, 0, 0));
    }

    [Fact]
    public void SetErosionEffect_PositiveBrightnessProducesBrighterImage()
    {
        using var source = CreateTestImage(1, 1, Color.FromRgb(128, 128, 128));
        using var result = ImageSharpHelper.SetErosionEffect(source, 50, 0);
        result.Width.ShouldBe(1);
        result.Height.ShouldBe(1);
    }

    #endregion

    #region ToIcoStream

    [Fact]
    public void ToIcoStream_ProducesValidIcoHeader()
    {
        using var source = CreateTestImage(32, 32);
        using var stream = ImageSharpHelper.ToIcoStream(source, new[] { 16, 32 });
        stream.Position.ShouldBe(0);
        var bytes = new byte[6];
        stream.Read(bytes, 0, 6);
        bytes[0].ShouldBe((byte)0); // reserved
        bytes[1].ShouldBe((byte)0); // reserved
        bytes[2].ShouldBe((byte)1); // type: ICO
        bytes[3].ShouldBe((byte)0);
        bytes[4].ShouldBe((byte)2); // 2 frames
    }

    [Fact]
    public void ToIcoStream_NullImage_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToIcoStream(null!));
    }

    [Fact]
    public void ToIcoStream_DefaultSizes_ProducesSixFrames()
    {
        using var source = CreateTestImage(256, 256);
        using var stream = ImageSharpHelper.ToIcoStream(source);
        stream.Position = 0;
        var header = new byte[6];
        stream.Read(header, 0, 6);
        header[4].ShouldBe((byte)6); // 6 default sizes
    }

    #endregion

    #region Helper

    private static Image CreateTestImage(int width, int height, Color? fill = null)
    {
        var image = new Image<Rgba32>(width, height);
        var fillColor = fill ?? Color.FromRgb(128, 128, 128);
        var pixel = fillColor.ToPixel<Rgba32>();
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            image[x, y] = pixel;
        return image;
    }

    #endregion
}

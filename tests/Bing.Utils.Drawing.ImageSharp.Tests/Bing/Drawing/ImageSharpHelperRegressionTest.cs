using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 <see cref="ImageSharpHelper"/> 的 Phase 0 回归契约。
/// </summary>
public class ImageSharpHelperRegressionTest
{
    /// <summary>
    /// 测试用例：验证通用 image mime 且大小写混合的 Data URL 可以被解析。
    /// </summary>
    [Fact]
    public void FromDataUrl_GenericMimeWithMixedCase_ReturnsImage()
    {
        using var source = CreateSampleImage();
        var base64 = ImageSharpHelper.ToBase64String(source, PngFormat.Instance);
        var dataUrl = $"data:IMAGE/WEBP;base64,{base64}";

        using var restored = ImageSharpHelper.FromDataUrl(dataUrl);

        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(source.Width);
        restored.Height.ShouldBe(source.Height);
    }

    private static Image<Rgba32> CreateSampleImage()
    {
        var image = new Image<Rgba32>(2, 2);
        image[0, 0] = new Rgba32(255, 0, 0, 255);
        image[1, 0] = new Rgba32(0, 255, 0, 255);
        image[0, 1] = new Rgba32(0, 0, 255, 255);
        image[1, 1] = new Rgba32(255, 255, 255, 255);
        return image;
    }
}
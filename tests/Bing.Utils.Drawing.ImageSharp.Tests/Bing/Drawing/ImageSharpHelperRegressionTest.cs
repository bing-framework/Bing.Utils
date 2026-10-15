using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证图像 Data URL 解析的媒体类型兼容行为。
/// </summary>
public class ImageSharpHelperRegressionTest
{
    /// <summary>
    /// 验证媒体类型大小写混合且与载荷格式不同时仍可加载图像。
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

    /// <summary>
    /// 创建包含红、绿、蓝及白色像素的二乘二图像。
    /// </summary>
    /// <returns>用于编码解析测试的 RGBA 图像，调用方负责释放。</returns>
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

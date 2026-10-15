using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证 <see cref="SkiaSharpHelper"/> 对 Data URL MIME 类型和大小写的回归行为。
/// </summary>
public class SkiaSharpHelperRegressionTest
{
    /// <summary>
    /// 验证 JPEG Data URL 前缀仅包含一次 <c>image/</c>。
    /// </summary>
    [Fact]
    public void ToDataUrl_Jpeg_UsesExactMimePrefix()
    {
        using var source = CreateSampleImage();

        var dataUrl = SkiaSharpHelper.ToDataUrl(source, (SKEncodedImageFormat.Jpeg, 90));

        dataUrl.ShouldStartWith("data:image/jpeg;base64,");
    }

    /// <summary>
    /// 验证带有大小写混合通用图像 MIME 的 Data URL 可被加载。
    /// </summary>
    [Fact]
    public void FromDataUrl_GenericMimeWithMixedCase_ReturnsImage()
    {
        using var source = CreateSampleImage();
        var base64 = SkiaSharpHelper.ToBase64String(source, (SKEncodedImageFormat.Png, 100));
        var dataUrl = $"data:IMAGE/WEBP;base64,{base64}";

        using var restored = SkiaSharpHelper.FromDataUrl(dataUrl);

        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(source.Width);
        restored.Height.ShouldBe(source.Height);
    }

    /// <summary>
    /// 创建包含红、绿、蓝和白色像素的 2×2 测试图像。
    /// </summary>
    /// <returns>包含四种颜色样本的图像。</returns>
    private static SKImage CreateSampleImage()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0, 255));
        bitmap.SetPixel(1, 0, new SKColor(0, 255, 0, 255));
        bitmap.SetPixel(0, 1, new SKColor(0, 0, 255, 255));
        bitmap.SetPixel(1, 1, new SKColor(255, 255, 255, 255));
        return SKImage.FromBitmap(bitmap);
    }
}

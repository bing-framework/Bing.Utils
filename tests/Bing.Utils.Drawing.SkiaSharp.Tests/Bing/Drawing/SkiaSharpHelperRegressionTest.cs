using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 <see cref="SkiaSharpHelper"/> 的 Phase 0 回归契约。
/// </summary>
public class SkiaSharpHelperRegressionTest
{
    /// <summary>
    /// 测试用例：验证 JPEG Data URL 前缀正确，不重复拼接 image/。
    /// </summary>
    [Fact]
    public void ToDataUrl_Jpeg_UsesExactMimePrefix()
    {
        using var source = CreateSampleImage();

        var dataUrl = SkiaSharpHelper.ToDataUrl(source, (SKEncodedImageFormat.Jpeg, 90));

        dataUrl.ShouldStartWith("data:image/jpeg;base64,");
    }

    /// <summary>
    /// 测试用例：验证通用 image mime 且大小写混合的 Data URL 可以被解析。
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
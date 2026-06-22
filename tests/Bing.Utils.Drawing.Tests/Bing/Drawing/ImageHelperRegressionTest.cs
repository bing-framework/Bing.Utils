using System.Drawing.Imaging;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 <see cref="ImageHelper"/> 的 Phase 0 回归契约。
/// </summary>
[Trait("Drawing", "ImageHelper")]
public class ImageHelperRegressionTest
{
    /// <summary>
    /// 测试用例：验证内存位图默认输出 Data URL 时使用可回读的 PNG 格式。
    /// </summary>
    [Fact]
    public void ToDataUrl_InMemoryBitmap_UsesPngAndCanRoundTrip()
    {
        using var bitmap = CreateBitmap();

        var dataUrl = ImageHelper.ToDataUrl(bitmap);

        dataUrl.ShouldStartWith("data:image/png;base64,");

        using var restored = ImageHelper.FromDataUrl(dataUrl);
        restored.Width.ShouldBe(bitmap.Width);
        restored.Height.ShouldBe(bitmap.Height);
    }

    /// <summary>
    /// 测试用例：验证输出流返回后位置已重置到开头，调用方可直接读取。
    /// </summary>
    [Fact]
    public void ToStream_InMemoryBitmap_ReturnsReadableStreamAtBeginning()
    {
        using var bitmap = CreateBitmap();
        using var stream = ImageHelper.ToStream(bitmap);

        stream.Position.ShouldBe(0);
        stream.ReadByte().ShouldBe(0x89);
    }

    /// <summary>
    /// 测试用例：验证通用 image mime 的 Data URL 也能被解析。
    /// </summary>
    [Fact]
    public void FromDataUrl_GenericImageMimeWithPngPayload_ReturnsImage()
    {
        using var bitmap = CreateBitmap();
        var base64 = ImageHelper.ToBase64String(bitmap, ImageFormat.Png);
        var dataUrl = $"data:IMAGE/WEBP;base64,{base64}";

        using var restored = ImageHelper.FromDataUrl(dataUrl);

        restored.Width.ShouldBe(bitmap.Width);
        restored.Height.ShouldBe(bitmap.Height);
    }

    private static Bitmap CreateBitmap()
    {
        var bitmap = new Bitmap(2, 2);
        bitmap.SetPixel(0, 0, Color.Red);
        bitmap.SetPixel(1, 0, Color.Green);
        bitmap.SetPixel(0, 1, Color.Blue);
        bitmap.SetPixel(1, 1, Color.White);
        return bitmap;
    }
}
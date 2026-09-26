using Bing.Drawing.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证 ImageSharp 统一后端的几何、绘制和编码契约。
/// </summary>
public sealed class ImageSharpImageBackendTest
{
    /// <summary>
    /// 验证几何操作返回相互独立且尺寸符合预期的图像表面。
    /// </summary>
    [Fact]
    public void GeometryOperations_ReturnIndependentSurfaces()
    {
        var backend = new ImageSharpImageBackend();
        using var source = backend.Create(4, 3, new RgbColor(10, 20, 30));
        using var resized = backend.Resize(source, 8, 6);
        using var cropped = backend.Crop(resized, 1, 1, 4, 3);
        using var rotated = backend.Rotate(cropped, 90);
        using var flipped = backend.Flip(rotated, horizontal: true, vertical: true);

        source.Width.ShouldBe(4);
        source.Height.ShouldBe(3);
        resized.Width.ShouldBe(8);
        resized.Height.ShouldBe(6);
        cropped.Width.ShouldBe(4);
        cropped.Height.ShouldBe(3);
        rotated.Width.ShouldBe(3);
        rotated.Height.ShouldBe(4);
        flipped.Width.ShouldBe(3);
        flipped.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证后端支持绘制边框及各类标注形状并能编码为 PNG。
    /// </summary>
    [Fact]
    public void Annotations_DrawAllSupportedShapes()
    {
        var backend = new ImageSharpImageBackend();
        using var surface = backend.Create(80, 60, new RgbColor(255, 255, 255));

        backend.Frame(surface, 6, 2, new RgbColor(0, 0, 0));
        backend.Annotate(surface, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Rectangle,
            X = 4,
            Y = 4,
            Width = 20,
            Height = 15,
            FillColor = new RgbColor(255, 0, 0, 100)
        }, 1);
        backend.Annotate(surface, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Ellipse,
            X = 30,
            Y = 4,
            Width = 20,
            Height = 15
        }, 1);
        backend.Annotate(surface, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Line,
            X = 4,
            Y = 30,
            X2 = 30,
            Y2 = 50
        }, 1);
        backend.Annotate(surface, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Arrow,
            X = 35,
            Y = 30,
            X2 = 70,
            Y2 = 50
        }, 1);

        var bytes = backend.Encode(surface, ImageOutputFormat.Png, 90, new RgbColor(255, 255, 255));
        bytes.Length.ShouldBeGreaterThan(8);
        bytes[0].ShouldBe((byte)0x89);
        bytes[1].ShouldBe((byte)0x50);
        bytes[2].ShouldBe((byte)0x4E);
        bytes[3].ShouldBe((byte)0x47);
    }

    /// <summary>
    /// 验证 JPEG 编码会使用背景色合成透明像素。
    /// </summary>
    [Fact]
    public void JpegEncoding_CompositesTransparentPixelsOnBackground()
    {
        var backend = new ImageSharpImageBackend();
        using var surface = backend.Create(2, 2, new RgbColor(255, 0, 0, 0));

        var bytes = backend.Encode(surface, ImageOutputFormat.Jpeg, 90, new RgbColor(0, 255, 0));
        using var decoded = Image.Load<Rgba32>(bytes);

        decoded[0, 0].R.ShouldBeLessThan((byte)20);
        decoded[0, 0].G.ShouldBeGreaterThan((byte)220);
        decoded[0, 0].B.ShouldBeLessThan((byte)20);
        decoded[0, 0].A.ShouldBe(byte.MaxValue);
    }

    /// <summary>
    /// 验证文本测量缺少外部字体文件时抛出参数异常。
    /// </summary>
    [Fact]
    public void TextMeasurement_RequiresExternalFontFile()
    {
        var backend = new ImageSharpImageBackend();

        Should.Throw<ArgumentException>(() => backend.MeasureText(new ImageTextOptions
        {
            Text = "text",
            FontPath = string.Empty,
            FontSize = 12
        }));
    }
}

using System;
using System.IO;
using Bing.Drawing;
using Bing.Drawing.Internal;
using SkiaSharp;

namespace Bing.Drawing.Internal;

/// <summary>
/// 验证 Skia 图像后端的几何、绘制和编码契约。
/// </summary>
public sealed class SkiaImageBackendTest
{
    /// <summary>
    /// 验证几何操作返回相互独立且尺寸符合预期的图像表面。
    /// </summary>
    [Fact]
    public void GeometryOperations_CreateIndependentSurfaces()
    {
        var backend = new SkiaImageBackend();
        using var source = backend.Create(8, 4, new RgbColor(255, 0, 0));
        using var resized = backend.Resize(source, 4, 2);
        using var cropped = backend.Crop(source, 2, 1, 4, 2);
        using var rotated = backend.Rotate(source, 90);
        using var flipped = backend.Flip(source, true, true);

        resized.Width.ShouldBe(4);
        resized.Height.ShouldBe(2);
        cropped.Width.ShouldBe(4);
        cropped.Height.ShouldBe(2);
        rotated.Width.ShouldBe(4);
        rotated.Height.ShouldBe(8);
        flipped.Width.ShouldBe(8);
        flipped.Height.ShouldBe(4);

        source.Width.ShouldBe(8);
        source.Height.ShouldBe(4);
    }

    /// <summary>
    /// 验证对角旋转使用向上取整后的边界尺寸。
    /// </summary>
    [Fact]
    public void Rotate_UsesCeilingBoundsAtDiagonalAngle()
    {
        var backend = new SkiaImageBackend();
        using var source = backend.Create(10, 10, new RgbColor(255, 0, 0));

        using var rotated = backend.Rotate(source, 45);

        rotated.Width.ShouldBe(15);
        rotated.Height.ShouldBe(15);
    }

    /// <summary>
    /// 验证关闭自动方向时保留 JPEG 原始尺寸，启用时应用方向信息。
    /// </summary>
    [Fact]
    public void Process_AutoOrientFalsePreservesRawJpegDimensions()
    {
        var backend = new SkiaImageBackend();
        var source = CreateOrientedJpeg(12, 8, 6);

        using var loaded = backend.Load(source);
        loaded.Width.ShouldBe(12);
        loaded.Height.ShouldBe(8);

        var raw = SkiaSharpHelper.Process(source, new ImageProcessOptions { AutoOrient = false });
        raw.Width.ShouldBe(12);
        raw.Height.ShouldBe(8);

        var oriented = SkiaSharpHelper.Process(source);
        oriented.Width.ShouldBe(8);
        oriented.Height.ShouldBe(12);
    }

    /// <summary>
    /// 验证绘制操作修改目标图像并可编码为 PNG。
    /// </summary>
    [Fact]
    public void DrawingOperations_MutateTargetAndEncodePng()
    {
        var backend = new SkiaImageBackend();
        using var target = backend.Create(24, 24, new RgbColor(255, 255, 255));
        using var overlay = backend.Create(6, 6, new RgbColor(255, 0, 0));

        backend.DrawImage(target, overlay, 2, 2, 1);
        backend.Frame(target, 3, 2, new RgbColor(0, 0, 255));
        backend.Annotate(target, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Rectangle,
            X = 8,
            Y = 8,
            Width = 8,
            Height = 6,
            Color = new RgbColor(0, 255, 0),
            FillColor = new RgbColor(0, 255, 0, 64)
        }, 1);
        backend.Annotate(target, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Ellipse,
            X = 10,
            Y = 10,
            Width = 4,
            Height = 4,
            Color = new RgbColor(0, 0, 0)
        }, 1);
        backend.Annotate(target, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Line,
            X = 0,
            Y = 12,
            X2 = 23,
            Y2 = 12,
            Color = new RgbColor(0, 0, 0)
        }, 1);
        backend.Annotate(target, new ImageAnnotation
        {
            Kind = ImageAnnotationKind.Arrow,
            X = 2,
            Y = 20,
            X2 = 18,
            Y2 = 20,
            Color = new RgbColor(0, 0, 0)
        }, 1);

        var encoded = backend.Encode(target, ImageOutputFormat.Png, 100, new RgbColor(0, 0, 0));
        using var image = SKImage.FromEncodedData(encoded);
        using var bitmap = SKBitmap.FromImage(image);

        encoded.Length.ShouldBeGreaterThan(0);
        bitmap.GetPixel(3, 3).Red.ShouldBeGreaterThan((byte)200);
        bitmap.GetPixel(0, 12).Red.ShouldBeLessThan((byte)100);
    }

    /// <summary>
    /// 验证 JPEG 编码会使用背景色合成透明像素。
    /// </summary>
    [Fact]
    public void Encode_JpegFlattensTransparentPixelsWithBackground()
    {
        var backend = new SkiaImageBackend();
        using var source = backend.Create(2, 2, new RgbColor(255, 0, 0, 0));

        var encoded = backend.Encode(source, ImageOutputFormat.Jpeg, 100, new RgbColor(0, 0, 255));
        using var image = SKImage.FromEncodedData(encoded);
        using var bitmap = SKBitmap.FromImage(image);
        var pixel = bitmap.GetPixel(0, 0);

        pixel.Alpha.ShouldBe((byte)255);
        pixel.Blue.ShouldBeGreaterThan((byte)180);
        pixel.Red.ShouldBeLessThan((byte)100);
    }

    /// <summary>
    /// 验证 PNG 编码保留透明像素。
    /// </summary>
    [Fact]
    public void Encode_PngPreservesTransparency()
    {
        var backend = new SkiaImageBackend();
        using var source = backend.Create(2, 2, new RgbColor(255, 0, 0, 0));

        var encoded = backend.Encode(source, ImageOutputFormat.Png, 100, new RgbColor(0, 0, 255));
        using var image = SKImage.FromEncodedData(encoded);
        using var bitmap = SKBitmap.FromImage(image);

        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证圆角边框保持角部透明且边框位于图像范围内。
    /// </summary>
    [Fact]
    public void Frame_RoundedCornersRemainTransparentAndBorderStaysInside()
    {
        var backend = new SkiaImageBackend();
        using var target = backend.Create(12, 12, new RgbColor(255, 255, 255));

        backend.Frame(target, 4, 2, new RgbColor(0, 0, 0));
        var encoded = backend.Encode(target, ImageOutputFormat.Png, 100, new RgbColor(0, 0, 0));
        using var image = SKImage.FromEncodedData(encoded);
        using var bitmap = SKBitmap.FromImage(image);

        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)0);
        bitmap.GetPixel(6, 0).Alpha.ShouldBe((byte)255);
        bitmap.GetPixel(1, 1).Red.ShouldBeLessThan((byte)100);
    }

    /// <summary>
    /// 验证较宽边框仍保持圆角透明区域。
    /// </summary>
    [Fact]
    public void Frame_LargeBorderPreservesRoundedTransparency()
    {
        var backend = new SkiaImageBackend();
        using var target = backend.Create(12, 12, new RgbColor(255, 255, 255));

        backend.Frame(target, 4, 12, new RgbColor(0, 0, 0));
        var encoded = backend.Encode(target, ImageOutputFormat.Png, 100, new RgbColor(0, 0, 0));
        using var image = SKImage.FromEncodedData(encoded);
        using var bitmap = SKBitmap.FromImage(image);

        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)0);
        bitmap.GetPixel(6, 6).Alpha.ShouldBe((byte)255);
        bitmap.GetPixel(6, 6).Red.ShouldBeLessThan((byte)100);
    }

    /// <summary>
    /// 创建带有指定 EXIF 方向的测试 JPEG 图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="orientation">要写入的 EXIF 方向。</param>
    /// <returns>带有指定方向元数据的 JPEG 字节数组。</returns>
    private static byte[] CreateOrientedJpeg(int width, int height, ushort orientation)
    {
        var backend = new SkiaImageBackend();
        using var source = backend.Create(width, height, new RgbColor(255, 0, 0));
        var jpeg = backend.Encode(source, ImageOutputFormat.Jpeg, 100, new RgbColor(255, 255, 255));
        return InjectExifOrientation(jpeg, orientation);
    }

    /// <summary>
    /// 将 EXIF 方向段插入 JPEG 数据。
    /// </summary>
    /// <param name="jpeg">原始 JPEG 数据。</param>
    /// <param name="orientation">要写入的 EXIF 方向。</param>
    /// <returns>插入 EXIF 段后的 JPEG 数据。</returns>
    private static byte[] InjectExifOrientation(byte[] jpeg, ushort orientation)
    {
        var exif = new byte[]
        {
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            (byte)'I', (byte)'I', 42, 0, 8, 0, 0, 0,
            1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0,
            (byte)orientation, 0, 0, 0, 0, 0, 0, 0
        };
        var length = checked(exif.Length + 2);
        using var result = new MemoryStream();
        result.WriteByte(0xff);
        result.WriteByte(0xd8);
        result.WriteByte(0xff);
        result.WriteByte(0xe1);
        result.WriteByte((byte)(length >> 8));
        result.WriteByte((byte)length);
        result.Write(exif, 0, exif.Length);
        result.Write(jpeg, 2, jpeg.Length - 2);
        return result.ToArray();
    }

    /// <summary>
    /// 验证加载无效图像数据时抛出内容异常。
    /// </summary>
    [Fact]
    public void Load_InvalidBytes_ThrowsInvalidDataException()
    {
        var backend = new SkiaImageBackend();

        Should.Throw<InvalidDataException>(() => backend.Load(new byte[] { 1, 2, 3 }));
    }

    /// <summary>
    /// 验证文本测量缺少外部字体文件时抛出文件异常。
    /// </summary>
    [Fact]
    public void MeasureText_RequiresExternalFontFile()
    {
        var backend = new SkiaImageBackend();

        Should.Throw<FileNotFoundException>(() => backend.MeasureText(new ImageTextOptions
        {
            Text = "A",
            FontPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ttf"),
            FontSize = 16
        }));
    }
}

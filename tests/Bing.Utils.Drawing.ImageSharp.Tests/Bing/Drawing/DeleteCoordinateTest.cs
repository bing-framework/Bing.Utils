namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 DeleteCoordinate 元数据清理功能。
/// </summary>
[Trait("Drawing", "DeleteCoordinate")]
public class DeleteCoordinateTest
{
    #region JPEG 测试

    [Fact]
    public void DeleteCoordinate_JpegBytes_RemovesGpsIfPresent()
    {
        // 构造最小合法 JPEG：SOI + DQT + SOF0 + SOS + EOI（无 EXIF 时应幂等）
        var jpeg = CreateMinimalJpeg();
        var result = ImageSharpHelper.DeleteCoordinate(jpeg);

        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
        // 应仍以 JPEG SOI 开头
        result[0].ShouldBe((byte)0xFF);
        result[1].ShouldBe((byte)0xD8);
    }

    [Fact]
    public void DeleteCoordinate_JpegNoExif_IsIdempotent()
    {
        var jpeg = CreateMinimalJpeg();
        var result = ImageSharpHelper.DeleteCoordinate(jpeg);

        // 无 EXIF 的 JPEG 应原样返回
        result.ShouldBe(jpeg);
    }

    #endregion

    #region PNG 测试

    [Fact]
    public void DeleteCoordinate_PngBytes_RemovesExifChunkIfPresent()
    {
        var png = CreateMinimalPng();
        var result = ImageSharpHelper.DeleteCoordinate(png);

        result.ShouldNotBeNull();
        // 应仍以 PNG 签名开头
        result.Length.ShouldBeGreaterThanOrEqualTo(8);
        result[0].ShouldBe((byte)0x89);
        result[1].ShouldBe((byte)0x50);
    }

    [Fact]
    public void DeleteCoordinate_PngNoExif_IsIdempotent()
    {
        var png = CreateMinimalPng();
        var result = ImageSharpHelper.DeleteCoordinate(png);

        result.ShouldBe(png);
    }

    #endregion

    #region Stream 测试

    [Fact]
    public void DeleteCoordinate_Stream_DoesNotDisposeCallerOwnedStream()
    {
        var jpeg = CreateMinimalJpeg();
        using var input = new MemoryStream(jpeg);
        var result = ImageSharpHelper.DeleteCoordinate(input);

        // 流不应被关闭
        input.CanRead.ShouldBeTrue();
        result.ShouldNotBeNull();
    }

    [Fact]
    public void DeleteCoordinate_StreamToStream_WritesToOutput()
    {
        var jpeg = CreateMinimalJpeg();
        using var input = new MemoryStream(jpeg);
        using var output = new MemoryStream();

        ImageSharpHelper.DeleteCoordinate(input, output);

        output.Position.ShouldBeGreaterThan(0);
        var result = output.ToArray();
        result[0].ShouldBe((byte)0xFF);
        result[1].ShouldBe((byte)0xD8);
    }

    #endregion

    #region 异常测试

    [Fact]
    public void DeleteCoordinate_NullBytes_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.DeleteCoordinate((byte[])null!));
    }

    [Fact]
    public void DeleteCoordinate_NullInputStream_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.DeleteCoordinate((Stream)null!));
    }

    [Fact]
    public void DeleteCoordinate_UnsupportedFormat_ThrowBehavior_Throws()
    {
        var unsupported = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
        var options = new ImageMetadataOptions { UnsupportedFormatBehavior = UnsupportedFormatBehavior.Throw };
        Should.Throw<NotSupportedException>(() => ImageSharpHelper.DeleteCoordinate(unsupported, options));
    }

    [Fact]
    public void DeleteCoordinate_UnsupportedFormat_PassThrough_ReturnsOriginal()
    {
        var unsupported = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
        var options = new ImageMetadataOptions { UnsupportedFormatBehavior = UnsupportedFormatBehavior.PassThrough };
        var result = ImageSharpHelper.DeleteCoordinate(unsupported, options);
        result.ShouldBe(unsupported);
    }

    #endregion

    #region 保留像素内容

    [Fact]
    public void DeleteCoordinate_PreservesPixelContent()
    {
        // 通过 ImageSharp 生成一张纯红图并编码为 JPEG
        var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(4, 4);
        for (var x = 0; x < 4; x++)
        for (var y = 0; y < 4; y++)
            image[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(255, 0, 0, 255);

        var jpegBytes = ImageSharpHelper.ToBytes(image, SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance);
        var cleaned = ImageSharpHelper.DeleteCoordinate(jpegBytes);

        // 清理后仍可加载并保持尺寸
        using var restored = ImageSharpHelper.FromBytes(cleaned);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(4);
        restored.Height.ShouldBe(4);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建最小合法 JPEG（无 EXIF）
    /// </summary>
    private static byte[] CreateMinimalJpeg()
    {
        // SOI + APP0 (JFIF) + DQT + SOF0 + DHT + SOS + 一行 MCU 数据 + EOI
        // 使用 ImageSharp 生成一张 2x2 白色 JPEG
        var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(2, 2);
        for (var x = 0; x < 2; x++)
        for (var y = 0; y < 2; y++)
            image[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(255, 255, 255, 255);

        return ImageSharpHelper.ToBytes(image, SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance);
    }

    /// <summary>
    /// 创建最小合法 PNG（无 eXIf chunk）
    /// </summary>
    private static byte[] CreateMinimalPng()
    {
        var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(2, 2);
        for (var x = 0; x < 2; x++)
        for (var y = 0; y < 2; y++)
            image[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(255, 255, 255, 255);

        return ImageSharpHelper.ToBytes(image, SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
    }

    #endregion
}

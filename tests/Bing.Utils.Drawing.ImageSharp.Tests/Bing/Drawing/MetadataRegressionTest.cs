using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Bing.Drawing.Internal;

namespace Bing.Drawing;

/// <summary>
/// 覆盖编码元数据清理和跨格式 ICC 迁移的回归契约。
/// </summary>
    [Trait("Drawing", "MetadataRegression")]
public sealed class MetadataRegressionTest
{
    /// <summary>
    /// 验证 JPEG 删除 GPS 信息时保持图像扫描数据和输入字节不变。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_Jpeg_RemovesGpsWithoutChangingScanOrInput()
    {
        var jpeg = InjectJpegSegments(CreateJpeg(), BuildJpegSegment(0xE1, CreateExifPayload()));
        var original = (byte[])jpeg.Clone();
        var before = JpegMetadataSanitizer.Parse(jpeg);

        var cleaned = ImageSharpHelper.DeleteCoordinate(jpeg);

        jpeg.SequenceEqual(original).ShouldBeTrue();
        var after = JpegMetadataSanitizer.Parse(cleaned);
        cleaned.Skip(after.TailStart).SequenceEqual(jpeg.Skip(before.TailStart)).ShouldBeTrue();

        var exif = after.Segments.First(segment => JpegMetadataSanitizer.IsExifSegment(cleaned, segment));
        var payload = exif.GetPayload(cleaned);
        for (var i = 6 + 38; i < payload.Length; i++)
            payload[i].ShouldBe((byte)0);
        payload[6 + 22].ShouldBe((byte)0);
        ImageSharpHelper.DeleteCoordinate(cleaned).ShouldBe(cleaned);
    }

    /// <summary>
    /// 验证删除完整 EXIF 时可独立保留或移除 ICC 配置文件。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_Jpeg_EntireExifAndIccAreIndependent()
    {
        var profile = new byte[] { 1, 2, 3, 4, 5, 6 };
        var segments = new List<byte[]> { BuildJpegSegment(0xE1, CreateExifPayload()), BuildIccSegment(profile) };
        var jpeg = InjectJpegSegments(CreateJpeg(), segments.ToArray());

        var keepIcc = ImageSharpHelper.DeleteCoordinate(jpeg, new ImageMetadataOptions
        {
            RemoveEntireExif = true,
            PreserveIccProfile = true
        });
        var keepDoc = JpegMetadataSanitizer.Parse(keepIcc);
        keepDoc.Segments.Any(segment => JpegMetadataSanitizer.IsExifSegment(keepIcc, segment)).ShouldBeFalse();
        JpegMetadataSanitizer.GetIccProfile(keepIcc, keepDoc).SequenceEqual(profile).ShouldBeTrue();

        var dropIcc = ImageSharpHelper.DeleteCoordinate(jpeg, new ImageMetadataOptions
        {
            RemoveEntireExif = true,
            PreserveIccProfile = false
        });
        var dropDoc = JpegMetadataSanitizer.Parse(dropIcc);
        dropDoc.Segments.Any(segment => JpegMetadataSanitizer.IsIccSegment(dropIcc, segment)).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 PNG 校验和无效时删除坐标抛出内容异常。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_Png_InvalidCrc_ThrowsInvalidDataException()
    {
        var png = CreatePng();
        var corrupt = (byte[])png.Clone();
        corrupt[FindPngChunkDataOffset(corrupt, "IDAT")] ^= 0x01;

        Should.Throw<InvalidDataException>(() => ImageSharpHelper.DeleteCoordinate(corrupt));
    }

    /// <summary>
    /// 验证图像识别能够读取尺寸、方向、帧数和格式。
    /// </summary>
    [Fact]
    public void Identify_ReadsDimensionsOrientationAndStaticFrameCount()
    {
        var jpeg = InjectJpegSegments(CreateJpeg(), BuildJpegSegment(0xE1, CreateExifPayload()));
        var info = Internal.EncodedImageInspector.Identify(jpeg);

        info.Width.ShouldBe(2);
        info.Height.ShouldBe(2);
        info.FrameCount.ShouldBe(1);
        info.Orientation.ShouldBe(6);
        info.Format.ShouldBe(ImageOutputFormat.Jpeg);
    }

    /// <summary>
    /// 验证跨 JPEG 与 PNG 格式复制元数据时保留 ICC 配置文件。
    /// </summary>
    [Fact]
    public void CopyMetadata_CrossFormat_PreservesIccProfile()
    {
        var profile = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var sourceJpeg = InjectJpegSegments(CreateJpeg(), BuildIccSegment(profile));
        var encodedPng = Internal.EncodedImageInspector.CopyMetadata(sourceJpeg, CreatePng(), true, true, true);
        var pngDocument = PngMetadataSanitizer.Parse(encodedPng);
        pngDocument.Chunks.Any(chunk => chunk.Type == "iCCP").ShouldBeTrue();

        var encodedJpeg = Internal.EncodedImageInspector.CopyMetadata(encodedPng, CreateJpeg(), true, true, true);
        var jpegDocument = JpegMetadataSanitizer.Parse(encodedJpeg);
        JpegMetadataSanitizer.GetIccProfile(encodedJpeg, jpegDocument).SequenceEqual(profile).ShouldBeTrue();
    }

    /// <summary>
    /// 验证保留元数据时复制 JPEG 未知应用段。
    /// </summary>
    [Fact]
    public void CopyMetadata_Jpeg_PreservesUnknownApplicationSegmentWhenMetadataIsKept()
    {
        var payload = Encoding.ASCII.GetBytes("custom-app13");
        var source = InjectJpegSegments(CreateJpeg(), BuildJpegSegment(0xED, payload));

        var encoded = Internal.EncodedImageInspector.CopyMetadata(source, CreateJpeg(), false, true, true);
        var document = JpegMetadataSanitizer.Parse(encoded);
        var segment = document.Segments.Single(item => item.Marker == 0xED);

        segment.GetPayload(encoded).SequenceEqual(payload).ShouldBeTrue();
    }

    /// <summary>
    /// 验证保留元数据时复制 PNG 未知辅助数据块。
    /// </summary>
    [Fact]
    public void CopyMetadata_Png_PreservesUnknownAncillaryChunkWhenMetadataIsKept()
    {
        var payload = new byte[] { 3, 1, 4, 1, 5 };
        var source = InjectPngChunk(CreatePng(), "IHDR", "vpAg", payload);

        var encoded = Internal.EncodedImageInspector.CopyMetadata(source, CreatePng(), false, true, true);
        var document = PngMetadataSanitizer.Parse(encoded);
        var chunk = document.Chunks.Single(item => item.Type == "vpAg");

        chunk.GetData(encoded).SequenceEqual(payload).ShouldBeTrue();
    }

    /// <summary>
    /// 验证包含多张图片的 JPEG 不支持统一处理。
    /// </summary>
    [Fact]
    public void Identify_JpegWithMultiplePicturesRejectsUnifiedProcessing()
    {
        var source = InjectJpegSegments(CreateJpeg(), BuildJpegSegment(0xE2, CreateMpfPayload(2)));

        Should.Throw<NotSupportedException>(() => ImageSharpHelper.Process(source));
    }

    /// <summary>
    /// 创建用于元数据测试的示例 JPEG 图像。
    /// </summary>
    /// <returns>示例 JPEG 字节数组。</returns>
    private static byte[] CreateJpeg()
    {
        using var image = new Image<Rgba32>(2, 2);
        image[0, 0] = new Rgba32(255, 0, 0, 255);
        image[1, 0] = new Rgba32(0, 255, 0, 255);
        image[0, 1] = new Rgba32(0, 0, 255, 255);
        image[1, 1] = new Rgba32(255, 255, 255, 255);
        return ImageSharpHelper.ToBytes(image, JpegFormat.Instance);
    }

    /// <summary>
    /// 创建用于元数据测试的示例 PNG 图像。
    /// </summary>
    /// <returns>示例 PNG 字节数组。</returns>
    private static byte[] CreatePng()
    {
        using var image = new Image<Rgba32>(2, 2);
        return ImageSharpHelper.ToBytes(image, PngFormat.Instance);
    }

    /// <summary>
    /// 将 JPEG 段插入图像起始标记之后。
    /// </summary>
    /// <param name="jpeg">原始 JPEG 数据。</param>
    /// <param name="segments">要插入的 JPEG 段。</param>
    /// <returns>插入段后的 JPEG 数据。</returns>
    private static byte[] InjectJpegSegments(byte[] jpeg, params byte[][] segments)
    {
        using var output = new MemoryStream();
        output.Write(jpeg, 0, 2);
        foreach (var segment in segments)
        {
            if (segment is not null)
                output.Write(segment, 0, segment.Length);
        }
        output.Write(jpeg, 2, jpeg.Length - 2);
        return output.ToArray();
    }

    /// <summary>
    /// 在指定 PNG 数据块之后插入新的数据块。
    /// </summary>
    /// <param name="png">原始 PNG 数据。</param>
    /// <param name="afterChunk">插入位置前的数据块类型。</param>
    /// <param name="type">新数据块类型。</param>
    /// <param name="payload">新数据块负载。</param>
    /// <returns>插入数据块后的 PNG 数据。</returns>
    private static byte[] InjectPngChunk(byte[] png, string afterChunk, string type, byte[] payload)
    {
        var document = PngMetadataSanitizer.Parse(png);
        var injected = PngMetadataSanitizer.BuildChunk(type, payload);
        using var output = new MemoryStream();
        output.Write(png, 0, 8);
        foreach (var chunk in document.Chunks)
        {
            var raw = chunk.GetRaw(png);
            output.Write(raw, 0, raw.Length);
            if (chunk.Type == afterChunk)
                output.Write(injected, 0, injected.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// 构造带有指定标记和负载的 JPEG 段。
    /// </summary>
    /// <param name="marker">JPEG 段标记。</param>
    /// <param name="payload">段负载。</param>
    /// <returns>构造出的 JPEG 段。</returns>
    private static byte[] BuildJpegSegment(byte marker, byte[] payload)
    {
        var result = new byte[payload.Length + 4];
        result[0] = 0xFF;
        result[1] = marker;
        var length = payload.Length + 2;
        result[2] = (byte)(length >> 8);
        result[3] = (byte)length;
        Array.Copy(payload, 0, result, 4, payload.Length);
        return result;
    }

    /// <summary>
    /// 构造包含 ICC 配置文件的 JPEG 段。
    /// </summary>
    /// <param name="profile">ICC 配置文件数据。</param>
    /// <returns>包含 ICC 配置文件的 JPEG 段。</returns>
    private static byte[] BuildIccSegment(byte[] profile)
    {
        var prefix = Encoding.ASCII.GetBytes("ICC_PROFILE\0");
        var payload = new byte[prefix.Length + 2 + profile.Length];
        Array.Copy(prefix, 0, payload, 0, prefix.Length);
        payload[prefix.Length] = 1;
        payload[prefix.Length + 1] = 1;
        Array.Copy(profile, 0, payload, prefix.Length + 2, profile.Length);
        return BuildJpegSegment(0xE2, payload);
    }

    /// <summary>
    /// 查找 PNG 指定数据块的负载偏移量。
    /// </summary>
    /// <param name="png">PNG 数据。</param>
    /// <param name="expectedType">要查找的数据块类型。</param>
    /// <returns>数据块负载的起始偏移量。</returns>
    private static int FindPngChunkDataOffset(byte[] png, string expectedType)
    {
        var position = 8;
        while (position + 12 <= png.Length)
        {
            var length = ReadUInt32BigEndian(png, position);
            var type = Encoding.ASCII.GetString(png, position + 4, 4);
            if (string.Equals(type, expectedType, StringComparison.Ordinal))
                return position + 8;
            var next = checked(position + 12 + checked((int)length));
            if (next > png.Length)
                break;
            position = next;
        }

        throw new InvalidOperationException("测试 PNG 缺少目标 chunk。");
    }

    /// <summary>
    /// 读取大端序的无符号 32 位整数。
    /// </summary>
    /// <param name="data">包含整数的数据。</param>
    /// <param name="offset">整数起始偏移量。</param>
    /// <returns>读取到的无符号整数。</returns>
    private static uint ReadUInt32BigEndian(byte[] data, int offset)
    {
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
            (data[offset + 2] << 8) | data[offset + 3]);
    }

    /// <summary>
    /// 构造包含方向和 GPS 信息的 EXIF 负载。
    /// </summary>
    /// <returns>包含测试元数据的 EXIF 负载。</returns>
    private static byte[] CreateExifPayload()
    {
        var tiff = new byte[56];
        tiff[0] = (byte)'I';
        tiff[1] = (byte)'I';
        tiff[2] = 42;
        WriteUInt32(tiff, 4, 8);
        tiff[8] = 2;

        // IFD0: Orientation=6 and GPS IFD at offset 38.
        WriteUInt16(tiff, 10, 0x0112);
        WriteUInt16(tiff, 12, 3);
        WriteUInt32(tiff, 14, 1);
        WriteUInt16(tiff, 18, 6);
        WriteUInt16(tiff, 22, 0x8825);
        WriteUInt16(tiff, 24, 4);
        WriteUInt32(tiff, 26, 1);
        WriteUInt32(tiff, 30, 38);

        // GPS IFD: LatitudeRef is inline and is cleared with the GPS directory.
        tiff[38] = 1;
        tiff[40] = 1;
        tiff[42] = 2;
        tiff[44] = 2;
        tiff[48] = (byte)'N';

        var payload = new byte[6 + tiff.Length];
        Encoding.ASCII.GetBytes("Exif\0\0").CopyTo(payload, 0);
        Array.Copy(tiff, 0, payload, 6, tiff.Length);
        return payload;
    }

    /// <summary>
    /// 构造包含指定图片数量的 MPF 负载。
    /// </summary>
    /// <param name="imageCount">MPF 声明的图片数量。</param>
    /// <returns>包含指定图片数量的 MPF 负载。</returns>
    private static byte[] CreateMpfPayload(uint imageCount)
    {
        var tiff = new byte[26];
        tiff[0] = (byte)'I';
        tiff[1] = (byte)'I';
        tiff[2] = 42;
        WriteUInt32(tiff, 4, 8);
        WriteUInt16(tiff, 8, 1);
        WriteUInt16(tiff, 10, 0xB001);
        WriteUInt16(tiff, 12, 4);
        WriteUInt32(tiff, 14, 1);
        WriteUInt32(tiff, 18, imageCount);
        WriteUInt32(tiff, 22, 0);

        var payload = new byte[4 + tiff.Length];
        Encoding.ASCII.GetBytes("MPF\0").CopyTo(payload, 0);
        Array.Copy(tiff, 0, payload, 4, tiff.Length);
        return payload;
    }

    /// <summary>
    /// 以小端序写入无符号 16 位整数。
    /// </summary>
    /// <param name="data">写入目标数据。</param>
    /// <param name="offset">写入起始偏移量。</param>
    /// <param name="value">要写入的数值。</param>
    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// 以小端序写入无符号 32 位整数。
    /// </summary>
    /// <param name="data">写入目标数据。</param>
    /// <param name="offset">写入起始偏移量。</param>
    /// <param name="value">要写入的数值。</param>
    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }
}

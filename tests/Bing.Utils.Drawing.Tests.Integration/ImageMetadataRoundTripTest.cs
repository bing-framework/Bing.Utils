using Bing.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using System.Text;

namespace Bing.Utils.Drawing.Tests.Integration;

/// <summary>
/// 使用真实 GPS 坐标、方向及系统 sRGB 配置验证元数据清理和跨格式编码。
/// </summary>
public class ImageMetadataRoundTripTest
{
    /// <summary>
    /// 验证不同后端和输出格式下 GPS、方向及 ICC 元数据可独立处理并保持图像可解码。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    /// <param name="jpeg">是否使用 JPEG 编码；为 false 时使用 PNG 编码。</param>
    [Theory]
    [InlineData(0, false), InlineData(1, false), InlineData(2, false)]
    [InlineData(0, true), InlineData(1, true), InlineData(2, true)]
    public void Metadata_RealGpsAndIcc_AreIndependentAndSurviveEncoding(int backend, bool jpeg)
    {
        var profilePath = Environment.GetEnvironmentVariable("BING_TEST_ICC") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "spool", "drivers", "color", "sRGB Color Space Profile.icm");
        File.Exists(profilePath).ShouldBeTrue("设置 BING_TEST_ICC 为有效的 sRGB ICC 文件。");
        var profile = File.ReadAllBytes(profilePath);
        using var input = new Image<Rgba32>(12, 8, new Rgba32(100, 150, 200));
        input.Metadata.IccProfile = new IccProfile(profile);
        input.Metadata.ExifProfile = new ExifProfile();
        input.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)6);
        input.Metadata.ExifProfile.SetValue(ExifTag.ImageDescription, "metadata regression");
        input.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, "N");
        input.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitude, new[] { new Rational(31), new Rational(12), new Rational(30) });
        input.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitudeRef, "E");
        input.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitude, new[] { new Rational(121), new Rational(28), new Rational(15) });
        using var stream = new MemoryStream();
        input.Save(stream, jpeg ? (IImageEncoder)new JpegEncoder() : new PngEncoder());
        // ImageSharp 2.1 的 PNG 元数据接口不提供 ICC 往返，独立写入标准 iCCP。
        var source = jpeg ? stream.ToArray() : AddPngIcc(stream.ToArray(), profile);
        var snapshot = source.ToArray();
        using var decodedSource = Image.Load<Rgba32>(source);
        decodedSource.Metadata.ExifProfile.GetValue(ExifTag.GPSLatitude).Value.Length.ShouldBe(3);
        var expectedIcc = ReadIcc(source);
        expectedIcc.ShouldNotBeEmpty();

        byte[] Clean(ImageMetadataOptions options) => backend switch
        {
            0 => ImageHelper.DeleteCoordinate(source, options),
            1 => ImageSharpHelper.DeleteCoordinate(source, options),
            _ => SkiaSharpHelper.DeleteCoordinate(source, options)
        };
        var gpsBytes = Clean(new ImageMetadataOptions());
        using var gpsOnly = Image.Load<Rgba32>(gpsBytes);
        gpsOnly.Metadata.ExifProfile.GetValue(ExifTag.GPSLatitude).ShouldBeNull();
        gpsOnly.Metadata.ExifProfile.GetValue(ExifTag.GPSLongitude).ShouldBeNull();
        gpsOnly.Metadata.ExifProfile.GetValue(ExifTag.Orientation).Value.ShouldBe((ushort)6);
        gpsOnly.Metadata.ExifProfile.GetValue(ExifTag.ImageDescription).Value.ShouldBe("metadata regression");
        ReadIcc(gpsBytes).ShouldBe(expectedIcc);
        for (var y = 0; y < input.Height; y++) for (var x = 0; x < input.Width; x++)
            gpsOnly[x, y].ShouldBe(decodedSource[x, y]);

        var noExifBytes = Clean(new ImageMetadataOptions { RemoveEntireExif = true });
        using var noExif = Image.Load<Rgba32>(noExifBytes);
        noExif.Metadata.ExifProfile.ShouldBeNull(); ReadIcc(noExifBytes).ShouldBe(expectedIcc);
        var noIccBytes = Clean(new ImageMetadataOptions { RemoveGps = false, PreserveIccProfile = false });
        using var noIcc = Image.Load<Rgba32>(noIccBytes);
        ReadIcc(noIccBytes).ShouldBeEmpty(); noIcc.Metadata.ExifProfile.GetValue(ExifTag.GPSLatitude).ShouldNotBeNull();

        var options = new ImageProcessOptions { Format = jpeg ? ImageOutputFormat.Png : ImageOutputFormat.Jpeg };
        var processed = backend switch { 0 => ImageHelper.Process(source, options), 1 => ImageSharpHelper.Process(source, options), _ => SkiaSharpHelper.Process(source, options) };
        using var uploaded = Image.Load<Rgba32>(processed.Bytes);
        uploaded.Width.ShouldBe(8); uploaded.Height.ShouldBe(12);
        uploaded.Metadata.ExifProfile.ShouldBeNull(); ReadIcc(processed.Bytes).ShouldBe(expectedIcc);
        source.ShouldBe(snapshot);
    }

    /// <summary>
    /// 向 PNG 数据插入标准 iCCP 数据块。
    /// </summary>
    /// <param name="png">原始 PNG 数据。</param>
    /// <param name="profile">ICC 配置文件数据。</param>
    /// <returns>插入 iCCP 数据块后的 PNG 数据。</returns>
    private static byte[] AddPngIcc(byte[] png, byte[] profile)
    {
        using var payload = new MemoryStream();
        payload.Write(Encoding.ASCII.GetBytes("sRGB\0\0"));
        payload.WriteByte(0x78); payload.WriteByte(0x9c);
        using (var deflate = new DeflateStream(payload, CompressionLevel.Optimal, true)) deflate.Write(profile);
        uint a = 1, b = 0;
        foreach (var value in profile) { a = (a + value) % 65521; b = (b + a) % 65521; }
        var adler = (b << 16) | a;
        payload.WriteByte((byte)(adler >> 24)); payload.WriteByte((byte)(adler >> 16)); payload.WriteByte((byte)(adler >> 8)); payload.WriteByte((byte)adler);
        var bytes = payload.ToArray();
        using var chunk = new MemoryStream();
        void U32(uint n) { chunk.WriteByte((byte)(n >> 24)); chunk.WriteByte((byte)(n >> 16)); chunk.WriteByte((byte)(n >> 8)); chunk.WriteByte((byte)n); }
        U32((uint)bytes.Length); chunk.Write(Encoding.ASCII.GetBytes("iCCP")); chunk.Write(bytes);
        uint crc = 0xffffffff;
        foreach (var chunkByte in chunk.ToArray().Skip(4))
        {
            crc ^= chunkByte;
            for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
        }
        U32(~crc);
        return png.Take(33).Concat(chunk.ToArray()).Concat(png.Skip(33)).ToArray();
    }

    /// <summary>
    /// 从 JPEG 或 PNG 数据中读取 ICC 配置文件。
    /// </summary>
    /// <param name="encoded">已编码的图像数据。</param>
    /// <returns>读取到的 ICC 配置文件；不存在时返回空数组。</returns>
    private static byte[] ReadIcc(byte[] encoded)
    {
        if (encoded[0] == 0xff)
        {
            using var image = Image.Load<Rgba32>(encoded);
            return image.Metadata.IccProfile?.ToByteArray() ?? Array.Empty<byte>();
        }
        for (var offset = 8; offset + 12 <= encoded.Length;)
        {
            var length = (encoded[offset] << 24) | (encoded[offset + 1] << 16) | (encoded[offset + 2] << 8) | encoded[offset + 3];
            if (Encoding.ASCII.GetString(encoded, offset + 4, 4) == "iCCP")
            {
                var compressed = Array.IndexOf(encoded, (byte)0, offset + 8, length) + 2;
                using var input = new MemoryStream(encoded, compressed + 2, offset + 8 + length - compressed - 6);
                using var zlib = new DeflateStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream(); zlib.CopyTo(output); return output.ToArray();
            }
            offset += 12 + length;
        }
        return Array.Empty<byte>();
    }
}

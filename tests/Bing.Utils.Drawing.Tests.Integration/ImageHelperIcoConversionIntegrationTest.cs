using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Bing.Drawing;

namespace Bing.Utils.Drawing.Tests.Integration;

/// <summary>
/// 使用真实 PNG 数据验证原生 ICO 容器头和帧数据。
/// </summary>
public sealed class ImageHelperIcoConversionIntegrationTest
{
    /// <summary>
    /// 验证大尺寸 PNG 转换为 ICO 时写入完整头信息并生成可解码帧。
    /// </summary>
    [Fact]
    public void ToIcoStream_256By256LargePng_WritesFullLengthOffsetAndDecodableFrame()
    {
        using var source = CreateDeterministicNoiseBitmap(256, 256);
        using var ico = ImageHelper.ToIcoStream(source, new Size(256, 256));

        var bytes = ico.ToArray();
        bytes.Length.ShouldBeGreaterThan(22 + ushort.MaxValue);
        bytes[0].ShouldBe((byte)0);
        bytes[1].ShouldBe((byte)0);
        bytes[2].ShouldBe((byte)1);
        bytes[3].ShouldBe((byte)0);
        bytes[4].ShouldBe((byte)1);
        bytes[5].ShouldBe((byte)0);
        bytes[6].ShouldBe((byte)0);
        bytes[7].ShouldBe((byte)0);

        var dataSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(14, 4));
        var dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(18, 4));
        dataOffset.ShouldBe(22u);
        dataSize.ShouldBe((uint)(bytes.Length - dataOffset));

        using var frameStream = new MemoryStream(bytes, checked((int)dataOffset), checked((int)dataSize), writable: false);
        using var frame = Image.FromStream(frameStream);
        frame.Width.ShouldBe(256);
        frame.Height.ShouldBe(256);

        using var iconStream = new MemoryStream(bytes, writable: false);
        using var icon = new Icon(iconStream);
        using var iconBitmap = icon.ToBitmap();
        iconBitmap.Width.ShouldBe(256);
        iconBitmap.Height.ShouldBe(256);
    }

    /// <summary>
    /// 验证并发生成不同尺寸的 ICO 时各自保留独立的头信息和帧尺寸。
    /// </summary>
    [Fact]
    public async Task ToIcoStream_ConcurrentDifferentSizes_KeepIndependentHeaders()
    {
        using var source = CreateDeterministicNoiseBitmap(64, 64);
        var sourceBytes = ImageHelper.ToBytes(source, ImageFormat.Png);
        var expectedSizes = Enumerable.Range(0, 64)
            .Select(index => (index % 4) switch
            {
                0 => 16,
                1 => 32,
                2 => 64,
                _ => 128
            })
            .ToArray();

        var tasks = expectedSizes.Select(expectedSize => Task.Run(() =>
        {
            using var input = ImageHelper.FromBytes(sourceBytes);
            using var ico = ImageHelper.ToIcoStream(input, new Size(expectedSize, expectedSize));
            var bytes = ico.ToArray();
            var width = bytes[6] == 0 ? 256 : bytes[6];
            var height = bytes[7] == 0 ? 256 : bytes[7];
            var dataSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(14, 4));
            var dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(18, 4));
            dataOffset.ShouldBe(22u);
            dataSize.ShouldBe((uint)(bytes.Length - 22));

            using var frameStream = new MemoryStream(bytes, checked((int)dataOffset), checked((int)dataSize), writable: false);
            using var frame = Image.FromStream(frameStream);
            return (expectedSize, width, height, frame.Width, frame.Height);
        })).ToArray();

        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            result.width.ShouldBe(result.expectedSize);
            result.height.ShouldBe(result.expectedSize);
            result.Item4.ShouldBe(result.expectedSize);
            result.Item5.ShouldBe(result.expectedSize);
        }
    }

    /// <summary>
    /// 创建具有确定性像素内容的测试位图。
    /// </summary>
    /// <param name="width">位图宽度。</param>
    /// <param name="height">位图高度。</param>
    /// <returns>填充确定性像素内容的位图。</returns>
    private static Bitmap CreateDeterministicNoiseBitmap(int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        var state = 0x13579BDFu;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            var r = (byte)(state >> 24);
            state = unchecked(state * 1664525u + 1013904223u);
            var g = (byte)(state >> 24);
            state = unchecked(state * 1664525u + 1013904223u);
            var b = (byte)(state >> 24);
            bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
        }

        return bitmap;
    }
}

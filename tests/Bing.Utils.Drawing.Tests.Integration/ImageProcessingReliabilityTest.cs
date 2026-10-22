using System.IO.Compression;
using System.Linq;
using System.Text;
using Bing.Drawing;
using Bing.Drawing.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Utils.Drawing.Tests.Integration;

/// <summary>
/// 验证统一图像流程的流所有权、几何布局、动画拒绝及 ICC 解压边界。
/// </summary>
public sealed class ImageProcessingReliabilityTest
{
    /// <summary>
    /// 验证不可定位流读取失败时按 <c>leaveOpen</c> 契约处理资源。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_NonSeekableReadFailure_RespectsLeaveOpen(int backend)
    {
        using (var keepOpen = new ThrowingReadStream(SolidPng(new Rgba32(20, 30, 40))))
        {
            Should.Throw<IOException>(() => Process(backend, keepOpen, leaveOpen: true));
            keepOpen.IsDisposed.ShouldBeFalse();
        }

        var closeOnFailure = new ThrowingReadStream(SolidPng(new Rgba32(20, 30, 40)));
        Should.Throw<IOException>(() => Process(backend, closeOnFailure, leaveOpen: false));
        closeOnFailure.IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 验证旋转 90 度后尺寸交换且像素位置正确。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_Rotation90_ChangesDimensionsAndMovesPixels(int backend)
    {
        var source = RotationPng();
        var result = Process(backend, source, new ImageProcessOptions { Rotation = 90 });

        result.Width.ShouldBe(2);
        result.Height.ShouldBe(4);
        using var output = Image.Load<Rgba32>(result.Bytes);
        output[1, 0].ShouldBe(new Rgba32(255, 0, 0, 255));
    }

    /// <summary>
    /// 验证网格拼接按单元位置放置每个输入图像。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Compose_GridLayoutPlacesEachSourceInItsCell(int backend)
    {
        var sources = new[]
        {
            SolidPng(new Rgba32(255, 0, 0)),
            SolidPng(new Rgba32(0, 255, 0)),
            SolidPng(new Rgba32(0, 0, 255)),
            SolidPng(new Rgba32(255, 255, 0))
        };
        var options = new ImageComposeOptions
        {
            Layout = ImageComposeLayout.Grid,
            Columns = 2,
            CellWidth = 10,
            CellHeight = 8,
            Spacing = 2,
            Padding = 1,
            Mode = ImageResizeMode.Cover
        };

        var result = backend switch
        {
            0 => ImageHelper.Compose(sources, options),
            1 => ImageSharpHelper.Compose(sources, options),
            _ => SkiaSharpHelper.Compose(sources, options)
        };

        result.Width.ShouldBe(24);
        result.Height.ShouldBe(20);
        using var output = Image.Load<Rgba32>(result.Bytes);
        output[5, 5].ShouldBe(new Rgba32(255, 0, 0, 255));
        output[17, 5].ShouldBe(new Rgba32(0, 255, 0, 255));
        output[5, 15].ShouldBe(new Rgba32(0, 0, 255, 255));
        output[17, 15].ShouldBe(new Rgba32(255, 255, 0, 255));
    }

    /// <summary>
    /// 验证包含多帧的 APNG 会被统一处理拒绝。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_ApngWithMultipleFrames_IsRejected(int backend)
    {
        var animationControl = BuildPngChunk("acTL", new byte[] { 0, 0, 0, 2, 0, 0, 0, 0 });
        var animated = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(1, 2, 3)), animationControl);

        Should.Throw<NotSupportedException>(() => Process(backend, animated));
    }

    /// <summary>
    /// 验证缺少动画控制块的 APNG 控制数据会被拒绝。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_ApngControlChunkWithoutAnimationControl_IsRejected(int backend)
    {
        var frameControl = BuildPngChunk("fcTL", new byte[26]);
        var animated = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(1, 2, 3)), frameControl);

        Should.Throw<NotSupportedException>(() => Process(backend, animated));
    }

    /// <summary>
    /// 验证缺少动画控制块的 APNG 帧数据会被拒绝。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_ApngDataChunkWithoutAnimationControl_IsRejected(int backend)
    {
        var frameData = BuildPngChunk("fdAT", new byte[] { 0, 0, 0, 1 });
        var animated = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(1, 2, 3)), frameData);

        Should.Throw<NotSupportedException>(() => Process(backend, animated));
    }

    /// <summary>
    /// 验证带单帧控制块的 APNG 识别会被拒绝。
    /// </summary>
    [Fact]
    public void Identify_ApngSingleFrameControlIsRejected()
    {
        var animationControl = BuildPngChunk("acTL", new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 });
        var animated = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(1, 2, 3)), animationControl);

        Should.Throw<NotSupportedException>(() => ImageSharpHelper.Identify(animated));
    }

    /// <summary>
    /// 验证复制 PNG 元数据时将调色板前的辅助数据块保持在合法位置。
    /// </summary>
    [Fact]
    public void CopyMetadata_PngPlacesPrePaletteAncillaryBeforePlte()
    {
        var source = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(10, 20, 30)), BuildPngChunk("sBIT", new byte[] { 8, 8, 8, 8 }));
        var target = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(10, 20, 30)), BuildPngChunk("PLTE", new byte[] { 0, 0, 0 }));

        var encoded = EncodedImageInspector.CopyMetadata(source, target, false, true, true);
        var document = PngMetadataSanitizer.Parse(encoded);
        var sbitIndex = document.Chunks.Select((chunk, index) => (chunk.Type, index)).Single(item => item.Type == "sBIT").index;
        var plteIndex = document.Chunks.Select((chunk, index) => (chunk.Type, index)).Single(item => item.Type == "PLTE").index;

        sbitIndex.ShouldBeLessThan(plteIndex);
    }

    /// <summary>
    /// 验证 ICC 配置文件的无效压缩数据会被拒绝。
    /// </summary>
    [Fact]
    public void CopyMetadata_RejectsCorruptIccZlib()
    {
        // Header and an empty deflate block are structurally valid; Adler-32 is intentionally wrong.
        var corruptZlib = new byte[] { 0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x02 };
        var source = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(10, 20, 30)), BuildIccChunk(corruptZlib));

        Should.Throw<InvalidDataException>(() => ImageSharpHelper.Identify(source));
        Should.Throw<InvalidDataException>(() => EncodedImageInspector.CopyMetadata(
            source, SolidPng(new Rgba32(10, 20, 30)), true, true, true));
    }

    /// <summary>
    /// 验证 ICC 解压结果超过限制时会被拒绝。
    /// </summary>
    [Fact]
    public void CopyMetadata_RejectsIccExpansionBeyondBound()
    {
        var profile = new byte[16 * 1024 * 1024 + 1];
        var source = InsertPngChunkBeforeIdat(SolidPng(new Rgba32(10, 20, 30)), BuildIccChunk(CreateZlib(profile)));

        Should.Throw<InvalidDataException>(() => ImageSharpHelper.Identify(source));
        Should.Throw<InvalidDataException>(() => EncodedImageInspector.CopyMetadata(
            source, SolidPng(new Rgba32(10, 20, 30)), true, true, true));
    }

    /// <summary>
    /// 通过指定后端处理字节数组并返回统一结果。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    /// <param name="source">源图像字节数组。</param>
    /// <param name="options">图像处理选项。</param>
    /// <returns>统一的图像处理结果。</returns>
    private static ImageProcessResult Process(int backend, byte[] source, ImageProcessOptions? options = null)
    {
        return backend switch
        {
            0 => ImageHelper.Process(source, options),
            1 => ImageSharpHelper.Process(source, options),
            _ => SkiaSharpHelper.Process(source, options)
        };
    }

    /// <summary>
    /// 通过指定后端处理输入流并控制流的释放行为。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    /// <param name="source">源图像流。</param>
    /// <param name="leaveOpen">是否在处理完成后保留输入流打开。</param>
    /// <returns>统一的图像处理结果。</returns>
    private static ImageProcessResult Process(int backend, Stream source, bool leaveOpen)
    {
        return backend switch
        {
            0 => ImageHelper.Process(source, leaveOpen: leaveOpen),
            1 => ImageSharpHelper.Process(source, leaveOpen: leaveOpen),
            _ => SkiaSharpHelper.Process(source, leaveOpen: leaveOpen)
        };
    }

    /// <summary>
    /// 创建用于旋转测试的 PNG 图像。
    /// </summary>
    /// <returns>旋转测试用 PNG 字节数组。</returns>
    private static byte[] RotationPng()
    {
        using var image = new Image<Rgba32>(4, 2, new Rgba32(0, 0, 0, 255));
        image[0, 0] = new Rgba32(255, 0, 0, 255);
        image[3, 0] = new Rgba32(0, 255, 0, 255);
        image[0, 1] = new Rgba32(0, 0, 255, 255);
        image[3, 1] = new Rgba32(255, 255, 0, 255);
        return EncodePng(image);
    }

    /// <summary>
    /// 创建指定颜色的 PNG 图像。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    /// <returns>指定颜色的 PNG 字节数组。</returns>
    private static byte[] SolidPng(Rgba32 color)
    {
        using var image = new Image<Rgba32>(4, 4, color);
        return EncodePng(image);
    }

    /// <summary>
    /// 将 ImageSharp 图像编码为 PNG 字节数组。
    /// </summary>
    /// <param name="image">待编码的图像。</param>
    /// <returns>PNG 字节数组。</returns>
    private static byte[] EncodePng(Image<Rgba32> image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    /// <summary>
    /// 构造包含压缩 ICC 数据的 PNG 数据块。
    /// </summary>
    /// <param name="zlib">压缩后的 ICC 数据。</param>
    /// <returns>构造出的 PNG 数据块。</returns>
    private static byte[] BuildIccChunk(byte[] zlib)
    {
        var keyword = Encoding.ASCII.GetBytes("ICC Profile");
        var payload = new byte[keyword.Length + 2 + zlib.Length];
        Array.Copy(keyword, 0, payload, 0, keyword.Length);
        payload[keyword.Length] = 0;
        payload[keyword.Length + 1] = 0;
        Array.Copy(zlib, 0, payload, keyword.Length + 2, zlib.Length);
        return BuildPngChunk("iCCP", payload);
    }

    /// <summary>
    /// 在 PNG 的 IDAT 数据块之前插入指定数据块。
    /// </summary>
    /// <param name="png">原始 PNG 数据。</param>
    /// <param name="chunkToInsert">要插入的数据块。</param>
    /// <returns>插入数据块后的 PNG 数据。</returns>
    private static byte[] InsertPngChunkBeforeIdat(byte[] png, byte[] chunkToInsert)
    {
        using var output = new MemoryStream(png.Length + chunkToInsert.Length);
        output.Write(png, 0, 8);
        var position = 8;
        var inserted = false;
        while (position + 12 <= png.Length)
        {
            var length = ReadUInt32(png, position);
            var rawLength = checked(12 + checked((int)length));
            var type = Encoding.ASCII.GetString(png, position + 4, 4);
            if (!inserted && string.Equals(type, "IDAT", StringComparison.Ordinal))
            {
                output.Write(chunkToInsert, 0, chunkToInsert.Length);
                inserted = true;
            }

            output.Write(png, position, rawLength);
            position = checked(position + rawLength);
        }

        if (!inserted || position != png.Length)
            throw new InvalidDataException("测试 PNG 缺少完整的 IDAT chunk。");
        return output.ToArray();
    }

    /// <summary>
    /// 构造带有指定类型、负载和校验和的 PNG 数据块。
    /// </summary>
    /// <param name="type">数据块类型。</param>
    /// <param name="payload">数据块负载。</param>
    /// <returns>构造出的 PNG 数据块。</returns>
    private static byte[] BuildPngChunk(string type, byte[] payload)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        var result = new byte[12 + payload.Length];
        WriteUInt32(result, 0, (uint)payload.Length);
        Array.Copy(typeBytes, 0, result, 4, 4);
        Array.Copy(payload, 0, result, 8, payload.Length);
        WriteUInt32(result, 8 + payload.Length, ComputeCrc(result, 4, 4 + payload.Length));
        return result;
    }

    /// <summary>
    /// 使用 zlib 格式压缩数据。
    /// </summary>
    /// <param name="data">待压缩的数据。</param>
    /// <returns>zlib 格式的压缩数据。</returns>
    private static byte[] CreateZlib(byte[] data)
    {
        using var compressed = new MemoryStream();
        using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, true))
            deflate.Write(data, 0, data.Length);

        var raw = compressed.ToArray();
        using var zlib = new MemoryStream(raw.Length + 6);
        zlib.WriteByte(0x78);
        zlib.WriteByte(0x9C);
        zlib.Write(raw, 0, raw.Length);
        var adler = Adler32(data);
        WriteUInt32(zlib, adler);
        return zlib.ToArray();
    }

    /// <summary>
    /// 计算数据的 Adler-32 校验值。
    /// </summary>
    /// <param name="data">待计算的数据。</param>
    /// <returns>Adler-32 校验值。</returns>
    private static uint Adler32(byte[] data)
    {
        const uint modulo = 65521;
        uint a = 1;
        uint b = 0;
        foreach (var value in data)
        {
            a = (a + value) % modulo;
            b = (b + a) % modulo;
        }
        return (b << 16) | a;
    }

    /// <summary>
    /// 计算指定范围的 CRC-32 校验值。
    /// </summary>
    /// <param name="data">待计算的数据。</param>
    /// <param name="offset">计算范围起始偏移量。</param>
    /// <param name="length">计算范围长度。</param>
    /// <returns>CRC-32 校验值。</returns>
    private static uint ComputeCrc(byte[] data, int offset, int length)
    {
        uint crc = 0xFFFFFFFF;
        for (var i = 0; i < length; i++)
        {
            crc ^= data[offset + i];
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// 读取大端序的无符号 32 位整数。
    /// </summary>
    /// <param name="data">包含整数的数据。</param>
    /// <param name="offset">整数起始偏移量。</param>
    /// <returns>读取到的无符号整数。</returns>
    private static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
            (data[offset + 2] << 8) | data[offset + 3]);
    }

    /// <summary>
    /// 以大端序写入无符号 32 位整数。
    /// </summary>
    /// <param name="data">写入目标数据。</param>
    /// <param name="offset">写入起始偏移量。</param>
    /// <param name="value">要写入的数值。</param>
    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    /// <summary>
    /// 以大端序向流写入无符号 32 位整数。
    /// </summary>
    /// <param name="stream">写入目标流。</param>
    /// <param name="value">要写入的数值。</param>
    private static void WriteUInt32(Stream stream, uint value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    /// <summary>
    /// 在指定读取次数后抛出异常的测试流。
    /// </summary>
    private sealed class ThrowingReadStream : Stream
    {
        /// <summary>
        /// 包装实际读取数据的内存流。
        /// </summary>
        private readonly MemoryStream _inner;
        /// <summary>
        /// 触发模拟读取失败前允许的读取次数。
        /// </summary>
        private int _readsBeforeFailure = 1;

        /// <summary>
        /// 初始化 <see cref="ThrowingReadStream" /> 类的新实例。
        /// </summary>
        /// <param name="bytes">要读取的字节数组。</param>
        internal ThrowingReadStream(byte[] bytes) => _inner = new MemoryStream(bytes);

        /// <summary>
        /// 获取流是否已释放。
        /// </summary>
        internal bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public override bool CanRead => !IsDisposed;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_readsBeforeFailure-- == 0)
                throw new IOException("模拟不可定位流读取失败。");
            return _inner.Read(buffer, offset, Math.Min(count, 7));
        }

        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}

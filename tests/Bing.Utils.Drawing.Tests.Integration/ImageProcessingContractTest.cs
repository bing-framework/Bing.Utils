using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bing.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Utils.Drawing.Tests.Integration;

/// <summary>
/// 覆盖统一图像管线中容易被后端旧 API 掩盖的公共契约。
/// </summary>
public sealed class ImageProcessingContractTest
{
    /// <summary>
    /// 验证统一输出格式与后端原生输出格式的能力集合分别符合契约。
    /// </summary>
    [Fact]
    public void BackendCapabilitiesSeparateUnifiedAndNativeFormats()
    {
        var unified = new[] { ImageOutputFormat.Png, ImageOutputFormat.Jpeg };
        ImageHelper.SupportedOutputFormats.SequenceEqual(unified).ShouldBeTrue();
        ImageSharpHelper.SupportedOutputFormats.SequenceEqual(unified).ShouldBeTrue();
        SkiaSharpHelper.SupportedOutputFormats.SequenceEqual(unified).ShouldBeTrue();

        ImageHelper.SupportedNativeOutputFormats.SequenceEqual(unified).ShouldBeTrue();
        ImageSharpHelper.SupportedNativeOutputFormats.SequenceEqual(new[] { ImageOutputFormat.Png, ImageOutputFormat.Jpeg, ImageOutputFormat.WebP }).ShouldBeTrue();
        SkiaSharpHelper.SupportedNativeOutputFormats.SequenceEqual(new[] { ImageOutputFormat.Png, ImageOutputFormat.Jpeg, ImageOutputFormat.WebP }).ShouldBeTrue();
    }

    /// <summary>
    /// 验证纵向拼接按输入顺序排列图像并计算正确尺寸。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Compose_VerticalLayoutPlacesSourcesInOrder(int backend)
    {
        var sources = new[]
        {
            Solid(new Rgba32(255, 0, 0), 6, 4),
            Solid(new Rgba32(0, 0, 255), 6, 4)
        };
        var result = Compose(backend, sources, new ImageComposeOptions
        {
            Layout = ImageComposeLayout.Vertical,
            CellWidth = 6,
            CellHeight = 4,
            Spacing = 2,
            Padding = 1
        });

        result.Width.ShouldBe(8);
        result.Height.ShouldBe(12);
        using var image = Image.Load<Rgba32>(result.Bytes);
        image[3, 3].ShouldBe(new Rgba32(255, 0, 0, 255));
        image[3, 8].ShouldBe(new Rgba32(0, 0, 255, 255));
    }

    /// <summary>
    /// 验证文本标注可通过统一处理管线绘制。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_TextAnnotationDrawsThroughCommonPipeline(int backend)
    {
        var fontPath = FindTestFont();
        var options = new ImageProcessOptions
        {
            Annotations = new List<ImageAnnotation>
            {
                new()
                {
                    Kind = ImageAnnotationKind.Text,
                    X = 4,
                    Y = 4,
                    Text = new ImageTextOptions
                    {
                        Text = "A",
                        FontPath = fontPath,
                        FontSize = 20,
                        Color = new RgbColor(0, 0, 0)
                    }
                }
            }
        };

        using var image = Image.Load<Rgba32>(Process(backend, Solid(new Rgba32(255, 255, 255), 80, 40), options).Bytes);
        var hasInk = false;
        for (var y = 4; y < image.Height; y++)
        {
            for (var x = 4; x < image.Width; x++)
            {
                if (image[x, y].R < 220 && image[x, y].G < 220 && image[x, y].B < 220)
                    hasInk = true;
            }
        }
        hasInk.ShouldBeTrue();
    }

    /// <summary>
    /// 验证水平和垂直翻转会移动像素到对应位置。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_HorizontalAndVerticalFlipMovePixels(int backend)
    {
        var source = FourColorGrid();

        using (var horizontal = Image.Load<Rgba32>(Process(backend, source, new ImageProcessOptions { FlipHorizontal = true }).Bytes))
        {
            horizontal[0, 0].ShouldBe(new Rgba32(0, 0, 255, 255));
            horizontal[1, 0].ShouldBe(new Rgba32(0, 255, 0, 255));
            horizontal[2, 0].ShouldBe(new Rgba32(255, 0, 0, 255));
            horizontal[0, 1].ShouldBe(new Rgba32(255, 0, 255, 255));
            horizontal[1, 1].ShouldBe(new Rgba32(0, 255, 255, 255));
            horizontal[2, 1].ShouldBe(new Rgba32(255, 255, 0, 255));
        }

        using (var vertical = Image.Load<Rgba32>(Process(backend, source, new ImageProcessOptions { FlipVertical = true }).Bytes))
        {
            vertical[0, 0].ShouldBe(new Rgba32(255, 255, 0, 255));
            vertical[1, 0].ShouldBe(new Rgba32(0, 255, 255, 255));
            vertical[2, 0].ShouldBe(new Rgba32(255, 0, 255, 255));
            vertical[0, 1].ShouldBe(new Rgba32(255, 0, 0, 255));
            vertical[1, 1].ShouldBe(new Rgba32(0, 255, 0, 255));
            vertical[2, 1].ShouldBe(new Rgba32(0, 0, 255, 255));
        }
    }

    /// <summary>
    /// 验证不保留输入流时成功处理会关闭输入流。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_SuccessWithLeaveOpenFalse_ClosesInput(int backend)
    {
        var stream = new TrackingReadStream(Solid(new Rgba32(20, 30, 40), 12, 8));
        var result = Process(backend, stream, leaveOpen: false);

        result.Width.ShouldBe(12);
        result.Height.ShouldBe(8);
        stream.IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 验证超过最大尺寸限制的输入会被拒绝。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_RejectsMaxDimension(int backend)
    {
        Should.Throw<InvalidDataException>(() => Process(backend, Solid(new Rgba32(20, 30, 40), 12, 8), new ImageProcessOptions
        {
            Limits = new ImageProcessingLimits { MaxDimension = 8 }
        }));
    }

    /// <summary>
    /// 验证超过最大拼接图像数的请求会被拒绝。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Compose_RejectsMaxComposeImages(int backend)
    {
        var sources = new[]
        {
            Solid(new Rgba32(255, 0, 0), 4, 4),
            Solid(new Rgba32(0, 0, 255), 4, 4)
        };
        Should.Throw<ArgumentOutOfRangeException>(() => Compose(backend, sources, new ImageComposeOptions
        {
            Output = new ImageProcessOptions
            {
                Limits = new ImageProcessingLimits { MaxComposeImages = 1 }
            }
        }));
    }

    /// <summary>
    /// 通过指定后端处理字节数组并返回统一结果。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    /// <param name="source">源图像字节数组。</param>
    /// <param name="options">图像处理选项。</param>
    /// <returns>统一的图像处理结果。</returns>
    private static ImageProcessResult Process(int backend, byte[] source, ImageProcessOptions options = null)
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
    /// 通过指定后端拼接图像并返回统一结果。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    /// <param name="sources">待拼接的图像字节数组。</param>
    /// <param name="options">图像拼接选项。</param>
    /// <returns>统一的图像处理结果。</returns>
    private static ImageProcessResult Compose(int backend, IReadOnlyList<byte[]> sources, ImageComposeOptions options)
    {
        return backend switch
        {
            0 => ImageHelper.Compose(sources, options),
            1 => ImageSharpHelper.Compose(sources, options),
            _ => SkiaSharpHelper.Compose(sources, options)
        };
    }

    /// <summary>
    /// 创建包含六种颜色区域的测试图像。
    /// </summary>
    /// <returns>包含颜色网格的 PNG 字节数组。</returns>
    private static byte[] FourColorGrid()
    {
        using var image = new Image<Rgba32>(3, 2);
        image[0, 0] = new Rgba32(255, 0, 0, 255);
        image[1, 0] = new Rgba32(0, 255, 0, 255);
        image[2, 0] = new Rgba32(0, 0, 255, 255);
        image[0, 1] = new Rgba32(255, 255, 0, 255);
        image[1, 1] = new Rgba32(0, 255, 255, 255);
        image[2, 1] = new Rgba32(255, 0, 255, 255);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    /// <summary>
    /// 创建指定颜色和尺寸的 PNG 图像。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>指定颜色的 PNG 字节数组。</returns>
    private static byte[] Solid(Rgba32 color, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, color);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    /// <summary>
    /// 查找用于文本标注测试的字体文件。
    /// </summary>
    /// <returns>可用字体文件路径。</returns>
    private static string FindTestFont()
    {
        var configured = Environment.GetEnvironmentVariable("BING_TEST_FONT");
        var candidates = new[]
        {
            configured,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msyh.ttc"),
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
            "/usr/share/fonts/opentype/dejavu/DejaVuSans.ttf"
        };
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return candidate;
        }
        throw new FileNotFoundException("没有找到测试字体，请设置 BING_TEST_FONT。", configured);
    }

    /// <summary>
    /// 提供可跟踪释放状态的只读测试流。
    /// </summary>
    private sealed class TrackingReadStream : Stream
    {
        /// <summary>
        /// 包装实际读取数据的内存流。
        /// </summary>
        private readonly MemoryStream _inner;

        /// <summary>
        /// 初始化 <see cref="TrackingReadStream" /> 类的新实例。
        /// </summary>
        /// <param name="bytes">要读取的字节数组。</param>
        internal TrackingReadStream(byte[] bytes) => _inner = new MemoryStream(bytes);

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
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, Math.Min(count, 7));
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

using System.Threading;
using Bing.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Utils.Drawing.Tests.Integration;

/// <summary>
/// 验证三个图像后端的公共类型身份、文件保存和图像处理行为。
/// </summary>
public class ImageProcessingTest
{
    /// <summary>
    /// 通过指定后端处理图像并返回统一结果。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    /// <param name="bytes">源图像字节数组。</param>
    /// <param name="options">图像处理选项。</param>
    /// <returns>统一的图像处理结果。</returns>
    private static ImageProcessResult Process(int backend, byte[] bytes, ImageProcessOptions options = null) => backend switch
    {
        0 => ImageHelper.Process(bytes, options), 1 => ImageSharpHelper.Process(bytes, options), _ => SkiaSharpHelper.Process(bytes, options)
    };
    /// <summary>
    /// 创建带有指定 EXIF 方向的示例 PNG 图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="orientation">要写入的 EXIF 方向。</param>
    /// <returns>带有指定方向元数据的 PNG 字节数组。</returns>
    private static byte[] Sample(int width = 12, int height = 8, ushort orientation = 1)
    {
        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) image[x, y] = new Rgba32((byte)(x * 255 / width), (byte)(y * 255 / height), 60, 255);
        image.Metadata.ExifProfile = new ExifProfile(); image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation);
        using var ms = new MemoryStream(); image.Save(ms, new PngEncoder()); return ms.ToArray();
    }
    /// <summary>
    /// 获取所有后端和 EXIF 方向的参数组合。
    /// </summary>
    public static IEnumerable<object[]> Orientations => Enumerable.Range(0, 3).SelectMany(b => Enumerable.Range(1, 8).Select(o => new object[] { b, o }));

    /// <summary>
    /// 验证三个后端均能规范化全部 EXIF 方向并更新输出方向。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    /// <param name="orientation">要验证的 EXIF 方向。</param>
    [Theory, MemberData(nameof(Orientations))]
    public void Process_NormalizesAllExifOrientations(int backend, int orientation)
    {
        var source = Sample(12, 8, (ushort)orientation);
        var result = Process(backend, source);
        result.Width.ShouldBe(orientation >= 5 ? 8 : 12); result.Height.ShouldBe(orientation >= 5 ? 12 : 8);
        using var output = Image.Load<Rgba32>(result.Bytes);
        using var input = Image.Load<Rgba32>(source);
        // 原图左上角像素在八种 EXIF 方向下的实际位置。
        var corner = orientation switch { 2 => (11, 0), 3 => (11, 7), 4 => (0, 7), 5 => (0, 0), 6 => (7, 0), 7 => (7, 11), 8 => (0, 11), _ => (0, 0) };
        output[corner.Item1, corner.Item2].ShouldBe(input[0, 0]);
        var info = ImageSharpHelper.Identify(result.Bytes); info.Orientation.ShouldBe(1);
    }

    /// <summary>
    /// 验证裁剪和缩放后输出尺寸正确且不修改输入数据。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_CropResizeAndDefaultPng_DoNotModifyInput(int backend)
    {
        var bytes = Sample(); var snapshot = bytes.ToArray();
        var result = Process(backend, bytes, new ImageProcessOptions { Crop = new ImageCropOptions { X = -2, Y = 0, Width = 8, Height = 8 }, Resize = new ImageResizeOptions { Width = 3, Height = 4 } });
        bytes.ShouldBe(snapshot); result.Width.ShouldBe(3); result.Height.ShouldBe(4);
        result.MimeType.ShouldBe("image/png"); result.Bytes.Take(4).ShouldBe(new byte[] { 137, 80, 78, 71 });
    }

    /// <summary>
    /// 验证 JPEG 输出使用白色背景并报告正确尺寸和质量。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_JpegUsesWhiteBackground_AndReportsSize(int backend)
    {
        using var image = new Image<Rgba32>(16, 16); using var ms = new MemoryStream(); image.Save(ms, new PngEncoder());
        var result = Process(backend, ms.ToArray(), new ImageProcessOptions { Format = ImageOutputFormat.Jpeg });
        using var output = Image.Load<Rgba32>(result.Bytes);
        output[8, 8].R.ShouldBeGreaterThan((byte)245); output[8, 8].G.ShouldBeGreaterThan((byte)245); output[8, 8].B.ShouldBeGreaterThan((byte)245);
        result.MimeType.ShouldBe("image/jpeg"); result.SizeBytes.ShouldBe(result.Bytes.LongLength); result.Quality.ShouldBe(85);
    }

    /// <summary>
    /// 验证无法达到目标大小时按严格模式抛出异常并报告结果状态。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_CompressionHasExplicitUnreachableResult(int backend)
    {
        var options = new ImageProcessOptions { Format = ImageOutputFormat.Jpeg, TargetSizeBytes = 1 };
        var result = Process(backend, Sample(), options);
        result.TargetSizeReached.ShouldBeFalse(); result.Width.ShouldBe(12); result.Height.ShouldBe(8);
        options.StrictTargetSize = true;
        Should.Throw<InvalidOperationException>(() => Process(backend, Sample(), options));
        options.TargetSizeBytes = 100_000;
        Process(backend, Sample(), options).TargetSizeReached.ShouldBeTrue();
    }

    /// <summary>
    /// 验证压缩会在降低质量后继续缩小图像尺寸以达到目标大小。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_CompressionActuallyReducesDimensionsAfterQuality(int backend)
    {
        using var source = new Image<Rgba32>(512, 384);
        var random = new Random(42);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
            source[x, y] = new Rgba32((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
        using var stream = new MemoryStream(); source.Save(stream, new PngEncoder());
        var bytes = stream.ToArray();
        var full = Process(backend, bytes, new ImageProcessOptions { Format = ImageOutputFormat.Jpeg });
        var compressed = Process(backend, bytes, new ImageProcessOptions { Format = ImageOutputFormat.Jpeg, TargetSizeBytes = full.SizeBytes / 8 });
        compressed.Width.ShouldBeLessThan(full.Width); compressed.Height.ShouldBeLessThan(full.Height);
        compressed.TargetSizeReached.ShouldBeTrue(); compressed.SizeBytes.ShouldBeLessThanOrEqualTo(full.SizeBytes / 8);
        compressed.Width.ShouldBeGreaterThanOrEqualTo(64); compressed.Height.ShouldBeGreaterThanOrEqualTo(64);
    }

    /// <summary>
    /// 验证 Cover 缩放支持九个锚点并从对应区域取样。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_CoverUsesAllNineAnchors(int backend)
    {
        var colors = new[] { new Rgba32(255, 0, 0), new Rgba32(0, 255, 0), new Rgba32(0, 0, 255) };
        foreach (var vertical in new[] { false, true })
        {
            using var source = new Image<Rgba32>(vertical ? 12 : 18, vertical ? 18 : 12);
            for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
                source[x, y] = colors[(vertical ? y : x) / 6];
            using var stream = new MemoryStream(); source.Save(stream, new PngEncoder());
            for (var anchor = 0; anchor < 9; anchor++)
            {
                var result = Process(backend, stream.ToArray(), new ImageProcessOptions
                {
                    Resize = new ImageResizeOptions { Width = vertical ? 12 : 6, Height = vertical ? 6 : 12, Mode = ImageResizeMode.Cover, Anchor = (ImageAnchor)anchor }
                });
                using var output = Image.Load<Rgba32>(result.Bytes);
                output[2, 2].ShouldBe(colors[vertical ? anchor / 3 : anchor % 3]);
            }
        }
    }

    /// <summary>
    /// 验证图像水印遵守锚点、边距和透明度设置。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_ImageWatermarkRespectsAnchorMarginAndOpacity(int backend)
    {
        var options = new ImageProcessOptions { Watermarks = new List<ImageWatermarkOptions>
        {
            new() { ImageBytes = Solid(new Rgba32(255, 0, 0), 4, 2), Anchor = ImageAnchor.BottomRight, Margin = 1, Opacity = 0.5f }
        }};
        using var output = Image.Load<Rgba32>(Process(backend, Solid(new Rgba32(255, 255, 255), 12, 8), options).Bytes);
        output[0, 0].ShouldBe(new Rgba32(255, 255, 255));
        output[10, 6].R.ShouldBe((byte)255);
        ((int)output[10, 6].G).ShouldBeInRange(120, 135);
        ((int)output[10, 6].B).ShouldBeInRange(120, 135);
        output[11, 7].ShouldBe(new Rgba32(255, 255, 255));
    }

    /// <summary>
    /// 验证处理限制和不支持的输出格式会被拒绝。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_RejectsLimitsAndUnsupportedFormat(int backend)
    {
        Should.Throw<InvalidDataException>(() => Process(backend, Sample(), new ImageProcessOptions { Limits = new ImageProcessingLimits { MaxPixels = 10 } }));
        Should.Throw<InvalidDataException>(() => Process(backend, Sample(), new ImageProcessOptions { Limits = new ImageProcessingLimits { MaxInputBytes = 4 } }));
        Should.Throw<NotSupportedException>(() => Process(backend, Sample(), new ImageProcessOptions { Format = ImageOutputFormat.WebP }));
        Should.Throw<ArgumentException>(() => Process(backend, Sample(), new ImageProcessOptions { Crop = new ImageCropOptions { X = 100, Width = 2, Height = 2 } }));
    }

    /// <summary>
    /// 验证拼接使用公共类型并保持输入顺序。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Compose_UsesCommonTypesAndInputOrder(int backend)
    {
        var sources = new[] { Solid(new Rgba32(255, 0, 0)), Solid(new Rgba32(0, 0, 255)) };
        var options = new ImageComposeOptions { Layout = ImageComposeLayout.Horizontal, CellWidth = 16, CellHeight = 16, Spacing = 2, Padding = 1 };
        var result = backend switch { 0 => ImageHelper.Compose(sources, options), 1 => ImageSharpHelper.Compose(sources, options), _ => SkiaSharpHelper.Compose(sources, options) };
        result.Width.ShouldBe(36); result.Height.ShouldBe(18);
        using var image = Image.Load<Rgba32>(result.Bytes); image[5, 5].R.ShouldBe((byte)255); image[25, 5].B.ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证边框和各类标注都会在输出图像中产生可见像素。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_FrameAndAnnotationsHaveVisiblePixels(int backend)
    {
        var options = new ImageProcessOptions { CornerRadius = 4, BorderWidth = 1, BorderColor = new RgbColor(255, 0, 0), Annotations = new List<ImageAnnotation>
        {
            new() { Kind = ImageAnnotationKind.Rectangle, X = 5, Y = 5, Width = 4, Height = 4, Color = new RgbColor(0, 255, 0), FillColor = new RgbColor(0, 255, 0) },
            new() { Kind = ImageAnnotationKind.Line, X = 2, Y = 12, X2 = 12, Y2 = 12, Color = new RgbColor(0, 0, 255) },
            new() { Kind = ImageAnnotationKind.Ellipse, X = 10, Y = 2, Width = 4, Height = 4, Color = new RgbColor(255, 0, 0), FillColor = new RgbColor(255, 0, 0) },
            new() { Kind = ImageAnnotationKind.Arrow, X = 2, Y = 14, X2 = 10, Y2 = 14, StrokeWidth = 1, Color = new RgbColor(0, 0, 255) }
        }};
        using var image = Image.Load<Rgba32>(Process(backend, Solid(new Rgba32(255, 255, 255)), options).Bytes);
        image[0, 0].A.ShouldBe((byte)0); image[7, 7].G.ShouldBe((byte)255); image[7, 7].R.ShouldBe((byte)0);
        image[8, 0].R.ShouldBe((byte)255); image[8, 0].G.ShouldBe((byte)0);
        image[12, 4].R.ShouldBeGreaterThan((byte)200); image[12, 4].G.ShouldBeLessThan((byte)100);
        ((int)image[5, 12].B - image[5, 12].R).ShouldBeGreaterThan(30);
        ((int)image[5, 14].B - image[5, 14].R).ShouldBeGreaterThan(30);
    }

    /// <summary>
    /// 验证显式中文字体能够绘制中文，并拒绝缺少字形的文本。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_ExplicitChineseFontAndMissingGlyph(int backend)
    {
        var path = Environment.GetEnvironmentVariable("BING_TEST_FONT") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msyh.ttc");
        File.Exists(path).ShouldBeTrue("设置 BING_TEST_FONT 为包含中文的字体文件。");
        var text = new ImageTextOptions { Text = "图片", FontPath = path, FontSize = 20, Color = new RgbColor(0, 0, 0) };
        var options = new ImageProcessOptions { Watermarks = new List<ImageWatermarkOptions> { new() { Text = text, Anchor = ImageAnchor.Center, Margin = 0 } } };
        using var output = Image.Load<Rgba32>(Process(backend, Solid(new Rgba32(255, 255, 255), 100, 50), options).Bytes);
        var hasInk = false; for (var y = 0; y < output.Height; y++) for (var x = 0; x < output.Width; x++) hasInk |= output[x, y].R < 128;
        hasInk.ShouldBeTrue(); text.Text = char.ConvertFromUtf32(0x10ffff);
        Should.Throw<ArgumentException>(() => Process(backend, Sample(), options));
    }

    /// <summary>
    /// 验证流所有权和取消令牌行为符合处理契约。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Process_StreamOwnershipAndCancellation(int backend)
    {
        var bytes = Sample(); using var stream = new NonSeekableStream(bytes);
        ImageProcessResult Run(Stream s, bool leaveOpen, CancellationToken token) => backend switch
        { 0 => ImageHelper.Process(s, leaveOpen: leaveOpen, cancellationToken: token), 1 => ImageSharpHelper.Process(s, leaveOpen: leaveOpen, cancellationToken: token), _ => SkiaSharpHelper.Process(s, leaveOpen: leaveOpen, cancellationToken: token) };
        Run(stream, true, default).Width.ShouldBe(12); stream.IsDisposed.ShouldBeFalse();
        var cancelled = new NonSeekableStream(bytes);
        Should.Throw<OperationCanceledException>(() => Run(cancelled, false, new CancellationToken(true))); cancelled.IsDisposed.ShouldBeTrue();
        var malformed = new NonSeekableStream(new byte[] { 1, 2 });
        Should.Throw<Exception>(() => Run(malformed, false, default)); malformed.IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 验证保存失败或取消时保留原文件，并在成功时写入新结果。
    /// </summary>
    /// <param name="backend">要使用的图像后端编号。</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Save_SamePathFailureAndCancellationPreserveExistingFile(int backend)
    {
        var directory = Path.Combine(Path.GetTempPath(), "bing-image-tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "image.png");
        try
        {
            var original = Sample(); File.WriteAllBytes(path, original);
            ImageProcessResult Save(ImageProcessOptions options, CancellationToken token = default) => backend switch
            { 0 => ImageHelper.Save(path, path, options, token), 1 => ImageSharpHelper.Save(path, path, options, token), _ => SkiaSharpHelper.Save(path, path, options, token) };
            Should.Throw<InvalidOperationException>(() => Save(new ImageProcessOptions { TargetSizeBytes = 1, StrictTargetSize = true })); File.ReadAllBytes(path).ShouldBe(original);
            Should.Throw<OperationCanceledException>(() => Save(new ImageProcessOptions(), new CancellationToken(true))); File.ReadAllBytes(path).ShouldBe(original);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Should.Throw<IOException>(() => Save(new ImageProcessOptions())); File.ReadAllBytes(path).ShouldBe(original);
            }
            Directory.GetFiles(directory, ".bing-image-*.tmp").ShouldBeEmpty();
            Save(new ImageProcessOptions { Resize = new ImageResizeOptions { Width = 6, Height = 4 } }).Width.ShouldBe(6);
            ImageSharpHelper.Identify(File.ReadAllBytes(path)).Width.ShouldBe(6);
        }
        finally { Directory.Delete(directory, true); }
        Directory.Exists(directory).ShouldBeFalse();
    }

    /// <summary>
    /// 创建指定颜色和尺寸的 PNG 图像。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>指定颜色的 PNG 字节数组。</returns>
    private static byte[] Solid(Rgba32 color, int width = 16, int height = 16)
    {
        using var image = new Image<Rgba32>(width, height, color); using var stream = new MemoryStream(); image.Save(stream, new PngEncoder()); return stream.ToArray();
    }

    /// <summary>
    /// 提供不可定位读取能力并跟踪释放状态的测试流。
    /// </summary>
    private sealed class NonSeekableStream : Stream
    {
        /// <summary>
        /// 包装实际读取数据的内存流。
        /// </summary>
        private readonly MemoryStream _inner;
        /// <summary>
        /// 获取流是否已释放。
        /// </summary>
        internal bool IsDisposed { get; private set; }
        /// <summary>
        /// 初始化 <see cref="NonSeekableStream" /> 类的新实例。
        /// </summary>
        /// <param name="bytes">要读取的字节数组。</param>
        internal NonSeekableStream(byte[] bytes) => _inner = new MemoryStream(bytes);
        /// <inheritdoc />
        public override bool CanRead => !IsDisposed;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
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
        protected override void Dispose(bool disposing) { IsDisposed = true; if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }
}

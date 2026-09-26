using System.Threading;

namespace Bing.Drawing.Internal;

/// <summary>
/// 执行后端无关的图像处理管线。
/// </summary>
/// <remarks>
/// 管线统一控制处理顺序、尺寸限额和有界压缩策略。
/// </remarks>
internal static class ImagePipeline
{
    /// <summary>
    /// 从输入流读取受大小限制的图像数据。
    /// </summary>
    /// <param name="input">输入流。</param>
    /// <param name="limits">输入资源限制。</param>
    /// <param name="leaveOpen">是否保持输入流打开。</param>
    /// <param name="token">用于取消读取的令牌。</param>
    /// <returns>读取到的图像数据。</returns>
    internal static byte[] Read(Stream input, ImageProcessingLimits limits, bool leaveOpen, CancellationToken token)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        try
        {
            ValidateLimits(limits);
            if (!input.CanRead) throw new ArgumentException("输入流不可读。", nameof(input));
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            while (true)
            {
                token.ThrowIfCancellationRequested();
                // 到达限制后仅多读一个字节，不能无界缓存不可信输入。
                var count = input.Read(buffer, 0, (int)Math.Min(buffer.Length, limits.MaxInputBytes - output.Length + 1));
                if (count == 0) return output.ToArray();
                if (output.Length + count > limits.MaxInputBytes) throw new InvalidDataException("图像输入超过字节限制。");
                output.Write(buffer, 0, count);
            }
        }
        finally { if (!leaveOpen) input.Dispose(); }
    }

    /// <summary>
    /// 识别图像格式、尺寸、帧数和方向。
    /// </summary>
    /// <param name="source">编码图像数据。</param>
    /// <param name="limits">资源限制；未指定时使用默认限制。</param>
    /// <returns>识别到的图像信息。</returns>
    internal static ImageInfo Identify(byte[] source, ImageProcessingLimits? limits = null)
    {
        limits ??= new ImageProcessingLimits();
        ValidateLimits(limits);
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.LongLength > limits.MaxInputBytes) throw new InvalidDataException("图像输入超过字节限制。");
        var info = EncodedImageInspector.Identify(source);
        CheckSize(info.Width, info.Height, limits);
        if (info.FrameCount != 1) throw new NotSupportedException("统一图像流程仅支持单帧静态图片。");
        return info;
    }

    /// <summary>
    /// 按处理选项处理单张图像。
    /// </summary>
    /// <param name="backend">图像后端。</param>
    /// <param name="source">编码源图像数据。</param>
    /// <param name="options">处理选项；未指定时使用默认选项。</param>
    /// <param name="token">用于取消处理的令牌。</param>
    /// <returns>编码后的处理结果。</returns>
    internal static ImageProcessResult Process(IImageBackend backend, byte[] source, ImageProcessOptions? options, CancellationToken token)
    {
        options ??= new ImageProcessOptions();
        Validate(options);
        token.ThrowIfCancellationRequested();
        var info = Identify(source, options.Limits);
        CheckWatermarkBudget(options, source.LongLength, (long)info.Width * info.Height);
        using var original = backend.Load(source);
        if (original.Width != info.Width || original.Height != info.Height) throw new InvalidDataException("解码尺寸与头部不一致。");
        ImageSurface? oriented = null;
        try
        {
            oriented = Orient(backend, original, options.AutoOrient ? info.Orientation : 1);
            return Finish(backend, oriented, source, options, token);
        }
        finally { oriented?.Dispose(); }
    }

    /// <summary>
    /// 将多张图像合成为一张拼图。
    /// </summary>
    /// <param name="backend">图像后端。</param>
    /// <param name="sources">编码源图像数据列表。</param>
    /// <param name="options">拼图选项；未指定时使用默认选项。</param>
    /// <param name="token">用于取消处理的令牌。</param>
    /// <returns>编码后的拼图结果。</returns>
    internal static ImageProcessResult Compose(IImageBackend backend, IReadOnlyList<byte[]> sources, ImageComposeOptions? options, CancellationToken token)
    {
        if (sources == null) throw new ArgumentNullException(nameof(sources));
        options ??= new ImageComposeOptions();
        Validate(options.Output);
        var limits = options.Output.Limits;
        if (sources.Count < 1 || sources.Count > limits.MaxComposeImages) throw new ArgumentOutOfRangeException(nameof(sources));
        if (!Enum.IsDefined(typeof(ImageComposeLayout), options.Layout) || !Enum.IsDefined(typeof(ImageResizeMode), options.Mode)) throw new ArgumentException("拼图布局无效。");
        if (options.CellWidth <= 0 || options.CellHeight <= 0 || options.Spacing < 0 || options.Padding < 0 || options.Columns <= 0) throw new ArgumentOutOfRangeException(nameof(options));
        long bytes = 0, pixels = 0;
        var infos = new ImageInfo[sources.Count];
        for (var i = 0; i < sources.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            infos[i] = Identify(sources[i], limits);
            bytes = checked(bytes + sources[i].LongLength);
            pixels = checked(pixels + (long)infos[i].Width * infos[i].Height);
            if (bytes > limits.MaxInputBytes || pixels > limits.MaxPixels) throw new InvalidDataException("拼图累计输入超过限制。");
        }
        var columns = options.Layout == ImageComposeLayout.Horizontal ? sources.Count : options.Layout == ImageComposeLayout.Vertical ? 1 : Math.Min(options.Columns, sources.Count);
        CheckWatermarkBudget(options.Output, bytes, pixels);
        var rows = (sources.Count + columns - 1) / columns;
        var width = checked(columns * (long)options.CellWidth + (columns - 1L) * options.Spacing + 2L * options.Padding);
        var height = checked(rows * (long)options.CellHeight + (rows - 1L) * options.Spacing + 2L * options.Padding);
        CheckSize(width, height, limits);
        using var canvas = backend.Create((int)width, (int)height, options.Background);
        for (var i = 0; i < sources.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            using var loaded = backend.Load(sources[i]);
            using var oriented = Orient(backend, loaded, options.Output.AutoOrient ? infos[i].Orientation : 1);
            using var fitted = Fit(backend, oriented, new ImageResizeOptions { Width = options.CellWidth, Height = options.CellHeight, Mode = options.Mode }, limits);
            backend.DrawImage(canvas, fitted, options.Padding + i % columns * (options.CellWidth + options.Spacing) + (options.CellWidth - fitted.Width) / 2,
                options.Padding + i / columns * (options.CellHeight + options.Spacing) + (options.CellHeight - fitted.Height) / 2, 1);
        }
        return Finish(backend, canvas, null, options.Output, token);
    }

    /// <summary>
    /// 执行图像编辑阶段并编码输出。
    /// </summary>
    /// <param name="backend">图像后端。</param>
    /// <param name="source">待处理图像。</param>
    /// <param name="encodedSource">原始编码数据；拼图时为 null。</param>
    /// <param name="options">处理选项。</param>
    /// <param name="token">用于取消处理的令牌。</param>
    /// <returns>编码后的处理结果。</returns>
    private static ImageProcessResult Finish(IImageBackend backend, ImageSurface source, byte[]? encodedSource, ImageProcessOptions options, CancellationToken token)
    {
        ImageSurface current = backend.Crop(source, 0, 0, source.Width, source.Height);
        try
        {
            void Replace(ImageSurface next) { current.Dispose(); current = next; }
            token.ThrowIfCancellationRequested();
            if (options.Crop != null)
            {
                var r = options.Crop;
                if (r.Width <= 0 || r.Height <= 0) throw new ArgumentOutOfRangeException(nameof(options.Crop));
                var x = Math.Max(0, r.X); var y = Math.Max(0, r.Y);
                var right = Math.Min(current.Width, (long)r.X + r.Width); var bottom = Math.Min(current.Height, (long)r.Y + r.Height);
                if (right <= x || bottom <= y) throw new ArgumentException("裁剪区域与图像没有交集。");
                Replace(backend.Crop(current, x, y, (int)(right - x), (int)(bottom - y)));
            }
            if (options.Resize != null) Replace(Fit(backend, current, options.Resize, options.Limits));
            var angle = (options.Rotation % 360 + 360) % 360;
            if (angle != 0)
            {
                var radians = angle * Math.PI / 180;
                CheckSize((long)Math.Ceiling(current.Width * Math.Abs(Math.Cos(radians)) + current.Height * Math.Abs(Math.Sin(radians)) - 1e-9),
                    (long)Math.Ceiling(current.Height * Math.Abs(Math.Cos(radians)) + current.Width * Math.Abs(Math.Sin(radians)) - 1e-9), options.Limits);
                Replace(backend.Rotate(current, angle));
            }
            if (options.FlipHorizontal || options.FlipVertical) Replace(backend.Flip(current, options.FlipHorizontal, options.FlipVertical));
            backend.Frame(current, options.CornerRadius, options.BorderWidth, options.BorderColor);
            foreach (var watermark in options.Watermarks)
            {
                token.ThrowIfCancellationRequested();
                DrawWatermark(backend, current, watermark, options.Limits);
            }
            foreach (var annotation in options.Annotations)
            {
                token.ThrowIfCancellationRequested(); ValidateAnnotation(annotation); backend.Annotate(current, annotation, 1);
            }
            return Encode(backend, current, encodedSource, options, token);
        }
        finally { current.Dispose(); }
    }

    /// <summary>
    /// 根据 EXIF 方向值纠正图像方向。
    /// </summary>
    /// <param name="backend">图像后端。</param>
    /// <param name="image">待纠正图像。</param>
    /// <param name="orientation">EXIF 方向值。</param>
    /// <returns>方向纠正后的图像表面。</returns>
    private static ImageSurface Orient(IImageBackend backend, ImageSurface image, int orientation)
    {
        switch (orientation)
        {
            case 2: return backend.Flip(image, true, false);
            case 3: return backend.Rotate(image, 180);
            case 4: return backend.Flip(image, false, true);
            case 5:
                using (var flipped = backend.Flip(image, true, false)) return backend.Rotate(flipped, 270);
            case 6: return backend.Rotate(image, 90);
            case 7:
                using (var flipped = backend.Flip(image, true, false)) return backend.Rotate(flipped, 90);
            case 8: return backend.Rotate(image, 270);
            default: return backend.Crop(image, 0, 0, image.Width, image.Height);
        }
    }

    /// <summary>
    /// 按缩放选项调整图像尺寸。
    /// </summary>
    /// <param name="backend">图像后端。</param>
    /// <param name="image">待缩放图像。</param>
    /// <param name="resize">缩放选项。</param>
    /// <param name="limits">资源限制。</param>
    /// <returns>缩放后的图像表面。</returns>
    private static ImageSurface Fit(IImageBackend backend, ImageSurface image, ImageResizeOptions resize, ImageProcessingLimits limits)
    {
        CheckSize(resize.Width, resize.Height, limits);
        if (!Enum.IsDefined(typeof(ImageResizeMode), resize.Mode) || !Enum.IsDefined(typeof(ImageAnchor), resize.Anchor)) throw new ArgumentException("缩放模式或定位无效。");
        int width, height;
        if (resize.Mode == ImageResizeMode.Stretch)
        {
            width = resize.AllowEnlarge ? resize.Width : Math.Min(image.Width, resize.Width);
            height = resize.AllowEnlarge ? resize.Height : Math.Min(image.Height, resize.Height);
        }
        else
        {
            var factor = resize.Mode == ImageResizeMode.Cover ? Math.Max((double)resize.Width / image.Width, (double)resize.Height / image.Height) : Math.Min((double)resize.Width / image.Width, (double)resize.Height / image.Height);
            if (!resize.AllowEnlarge) factor = Math.Min(1, factor);
            width = Math.Max(1, (int)Math.Round(image.Width * factor)); height = Math.Max(1, (int)Math.Round(image.Height * factor));
        }
        CheckSize(width, height, limits);
        var result = backend.Resize(image, width, height);
        if (resize.Mode != ImageResizeMode.Cover) return result;
        using (result)
        {
            var w = Math.Min(width, resize.Width); var h = Math.Min(height, resize.Height);
            var offset = Anchor(resize.Anchor, width - w, height - h);
            return backend.Crop(result, offset.X, offset.Y, w, h);
        }
    }

    /// <summary>
    /// 在图像上绘制水印。
    /// </summary>
    /// <param name="backend">图像后端。</param>
    /// <param name="image">目标图像。</param>
    /// <param name="watermark">水印选项。</param>
    /// <param name="limits">资源限制。</param>
    private static void DrawWatermark(IImageBackend backend, ImageSurface image, ImageWatermarkOptions watermark, ImageProcessingLimits limits)
    {
        if (watermark == null || (watermark.ImageBytes == null) == (watermark.Text == null)) throw new ArgumentException("水印必须且只能指定图片或文字。");
        if (!Finite(watermark.Opacity) || watermark.Opacity < 0 || watermark.Opacity > 1 || watermark.Margin < 0) throw new ArgumentOutOfRangeException(nameof(watermark));
        if (!Enum.IsDefined(typeof(ImageAnchor), watermark.Anchor)) throw new ArgumentOutOfRangeException(nameof(watermark.Anchor));
        if (watermark.Text != null)
        {
            ValidateText(watermark.Text);
            var size = backend.MeasureText(watermark.Text);
            var offset = Anchor(watermark.Anchor, image.Width - size.Width - 2 * watermark.Margin, image.Height - size.Height - 2 * watermark.Margin);
            backend.Annotate(image, new ImageAnnotation { Kind = ImageAnnotationKind.Text, Text = watermark.Text, X = watermark.Margin + offset.X, Y = watermark.Margin + offset.Y }, watermark.Opacity);
        }
        else
        {
            Identify(watermark.ImageBytes!, limits);
            using var loaded = backend.Load(watermark.ImageBytes!);
            var w = watermark.Width ?? loaded.Width; var h = watermark.Height ?? loaded.Height;
            CheckSize(w, h, limits);
            using var overlay = backend.Resize(loaded, w, h);
            var offset = Anchor(watermark.Anchor, image.Width - w - 2 * watermark.Margin, image.Height - h - 2 * watermark.Margin);
            backend.DrawImage(image, overlay, watermark.Margin + offset.X, watermark.Margin + offset.Y, watermark.Opacity);
        }
    }

    /// <summary>
    /// 根据定位方式计算剩余空间中的偏移量。
    /// </summary>
    /// <param name="anchor">定位方式。</param>
    /// <param name="x">可用横向空间。</param>
    /// <param name="y">可用纵向空间。</param>
    /// <returns>计算出的横向和纵向偏移量。</returns>
    private static (int X, int Y) Anchor(ImageAnchor anchor, int x, int y) =>
        ((int)anchor % 3 == 0 ? 0 : (int)anchor % 3 == 1 ? x / 2 : x, (int)anchor / 3 == 0 ? 0 : (int)anchor / 3 == 1 ? y / 2 : y);

    /// <summary>
    /// 以有界尝试次数编码图像并满足目标大小约束。
    /// </summary>
    /// <param name="backend">图像后端。</param>
    /// <param name="image">待编码图像。</param>
    /// <param name="source">原始编码数据；没有时不迁移元数据。</param>
    /// <param name="options">处理选项。</param>
    /// <param name="token">用于取消编码的令牌。</param>
    /// <returns>最符合约束的编码结果。</returns>
    private static ImageProcessResult Encode(IImageBackend backend, ImageSurface image, byte[]? source, ImageProcessOptions options, CancellationToken token)
    {
        ImageProcessResult? best = null;
        var width = image.Width; var height = image.Height; var attempts = 0;
        for (var round = 0; round <= 6 && attempts < 28; round++)
        {
            using var candidate = backend.Resize(image, width, height);
            var quality = options.Quality;
            while (attempts++ < 28)
            {
                token.ThrowIfCancellationRequested();
                var bytes = backend.Encode(candidate, options.Format, quality, options.JpegBackground);
                if (source != null) bytes = EncodedImageInspector.CopyMetadata(source, bytes, options.RemoveMetadata, options.PreserveIccProfile, options.AutoOrient);
                token.ThrowIfCancellationRequested();
                var result = new ImageProcessResult { Bytes = bytes, Format = options.Format, Width = width, Height = height,
                    Quality = options.Format == ImageOutputFormat.Jpeg ? quality : (int?)null,
                    TargetSizeReached = !options.TargetSizeBytes.HasValue || bytes.LongLength <= options.TargetSizeBytes.Value };
                if (result.TargetSizeReached && result.SizeBytes <= options.Limits.MaxInputBytes) return result;
                if (best == null || result.SizeBytes < best.SizeBytes) best = result;
                if (options.Format != ImageOutputFormat.Jpeg || quality == options.MinimumQuality || !options.TargetSizeBytes.HasValue) break;
                quality = Math.Max(options.MinimumQuality, quality - 15);
            }
            var factor = Math.Max(0.8, Math.Max((double)Math.Min(options.MinimumDimension, image.Width) / width,
                (double)Math.Min(options.MinimumDimension, image.Height) / height));
            var nextW = Math.Max(1, (int)Math.Ceiling(width * factor)); var nextH = Math.Max(1, (int)Math.Ceiling(height * factor));
            if (nextW >= width || nextH >= height) break;
            width = nextW; height = nextH;
        }
        if (best!.SizeBytes > options.Limits.MaxInputBytes) throw new InvalidDataException("编码输出超过字节限制。");
        if (options.StrictTargetSize) throw new InvalidOperationException("在指定质量和尺寸约束内无法达到目标文件大小。");
        return best!;
    }

    /// <summary>
    /// 验证水印加入后的累计资源占用。
    /// </summary>
    /// <param name="options">处理选项。</param>
    /// <param name="bytes">当前累计字节数。</param>
    /// <param name="pixels">当前累计像素数。</param>
    private static void CheckWatermarkBudget(ImageProcessOptions options, long bytes, long pixels)
    {
        foreach (var watermark in options.Watermarks)
        {
            if (watermark == null) throw new ArgumentException("水印不能为 null。");
            if (watermark.ImageBytes == null) continue;
            var info = Identify(watermark.ImageBytes, options.Limits);
            bytes = checked(bytes + watermark.ImageBytes.LongLength);
            pixels = checked(pixels + (long)info.Width * info.Height);
            if (bytes > options.Limits.MaxInputBytes || pixels > options.Limits.MaxPixels) throw new InvalidDataException("图像及水印累计输入超过限制。");
        }
    }

    /// <summary>
    /// 验证图像处理资源限制。
    /// </summary>
    /// <param name="limits">待验证的资源限制。</param>
    internal static void ValidateLimits(ImageProcessingLimits limits)
    {
        if (limits == null) throw new ArgumentNullException(nameof(limits));
        if (limits.MaxInputBytes <= 0 || limits.MaxInputBytes >= int.MaxValue || limits.MaxPixels <= 0 || limits.MaxDimension <= 0 || limits.MaxFrames < 1 || limits.MaxComposeImages < 1) throw new ArgumentOutOfRangeException(nameof(limits));
    }
    /// <summary>
    /// 验证图像尺寸符合资源限制。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="limits">资源限制。</param>
    internal static void CheckSize(long width, long height, ImageProcessingLimits limits)
    {
        if (width <= 0 || height <= 0 || width > limits.MaxDimension || height > limits.MaxDimension || width > limits.MaxPixels / height) throw new InvalidDataException("图像尺寸超过允许范围。");
    }
    /// <summary>
    /// 判断浮点值是否为有限值。
    /// </summary>
    /// <param name="value">待判断的浮点值。</param>
    /// <returns>值为有限数时返回 true，否则返回 false。</returns>
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    /// <summary>
    /// 验证图像处理选项。
    /// </summary>
    /// <param name="options">待验证的处理选项。</param>
    private static void Validate(ImageProcessOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        ValidateLimits(options.Limits);
        if (options.Format != ImageOutputFormat.Png && options.Format != ImageOutputFormat.Jpeg) throw new NotSupportedException("统一流程目前支持 PNG 和 JPEG 编码。");
        if (options.Quality < 1 || options.Quality > 100 || options.MinimumQuality < 1 || options.MinimumQuality > options.Quality || options.MinimumDimension < 1 || options.TargetSizeBytes <= 0 || options.BorderWidth < 0 || !Finite(options.CornerRadius) || options.CornerRadius < 0) throw new ArgumentOutOfRangeException(nameof(options));
        if (options.Watermarks == null || options.Annotations == null) throw new ArgumentException("水印和标注列表不能为空引用。");
    }
    /// <summary>
    /// 验证文字和字体文件选项。
    /// </summary>
    /// <param name="text">文字样式选项。</param>
    internal static void ValidateText(ImageTextOptions text)
    {
        if (text == null || string.IsNullOrEmpty(text.Text) || string.IsNullOrWhiteSpace(text.FontPath) || !Finite(text.FontSize) || text.FontSize <= 0) throw new ArgumentException("文字、字体路径和正数字号必须指定。");
        if (!File.Exists(text.FontPath)) throw new FileNotFoundException("字体文件不存在。", text.FontPath);
        FontGlyphValidator.Validate(text.FontPath, text.Text);
    }
    /// <summary>
    /// 验证图像标注选项。
    /// </summary>
    /// <param name="a">待验证的标注。</param>
    private static void ValidateAnnotation(ImageAnnotation a)
    {
        if (a == null || !Enum.IsDefined(typeof(ImageAnnotationKind), a.Kind) || !Finite(a.X) || !Finite(a.Y) || !Finite(a.X2) || !Finite(a.Y2) || !Finite(a.Width) || !Finite(a.Height) || !Finite(a.StrokeWidth) || a.StrokeWidth <= 0) throw new ArgumentException("标注参数无效。");
        if ((a.Kind == ImageAnnotationKind.Rectangle || a.Kind == ImageAnnotationKind.Ellipse) && (a.Width <= 0 || a.Height <= 0)) throw new ArgumentException("标注尺寸必须大于零。");
        if (a.Kind == ImageAnnotationKind.Text) ValidateText(a.Text!);
    }
}

using Bing.Drawing;
using SkiaSharp;

namespace Bing.Ocr.SkiaSharp;

/// <summary>单图预处理；在解码前检查格式和尺寸，每次调用独立拥有原生资源。</summary>
public sealed class SkiaOcrImagePreprocessor : IOcrImagePreprocessor
{
    /// <inheritdoc />
    public PreparedOcrImage Prepare(byte[] encodedImage, OcrOptions options, OcrServiceOptions limits,
        CancellationToken cancellationToken = default)
    {
        if (encodedImage == null) throw new ArgumentNullException(nameof(encodedImage));
        var request = (options ?? throw new ArgumentNullException(nameof(options))).Snapshot();
        var budget = (limits ?? throw new ArgumentNullException(nameof(limits))).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        if (encodedImage.Length == 0 || encodedImage.Length > budget.MaxInputBytes)
            throw new OcrInputException("input_too_large_or_empty", "编码图片为空或超过限制。");
        try { return PrepareCore(encodedImage, request, budget, cancellationToken); }
        catch (OcrInputException) { throw; }
        catch (NotSupportedException ex) { throw new OcrInputException("unsupported_image", "仅支持静态 PNG、JPEG 和 BMP。", ex); }
        catch (InvalidDataException ex) { throw new OcrInputException("invalid_image", "图片内容无效。", ex); }
        catch (OverflowException ex) { throw new OcrInputException("image_dimensions_exceeded", "图片尺寸溢出。", ex); }
    }

    private static PreparedOcrImage PrepareCore(byte[] encoded, OcrOptions options, OcrServiceOptions limits, CancellationToken token)
    {
        var bmp = encoded.Length >= 2 && encoded[0] == 'B' && encoded[1] == 'M';
        ImageInfo? header = null;
        if (!bmp)
            header = SkiaSharpHelper.Identify(encoded, new ImageProcessingLimits
            {
                MaxInputBytes = limits.MaxInputBytes, MaxPixels = limits.MaxPixels, MaxDimension = limits.MaxDimension
            });
        using var data = SKData.CreateCopy(encoded);
        using var codec = SKCodec.Create(data) ?? throw new OcrInputException("invalid_image", "图片解码器无法打开输入。");
        if (codec.EncodedFormat != SKEncodedImageFormat.Png && codec.EncodedFormat != SKEncodedImageFormat.Jpeg &&
            codec.EncodedFormat != SKEncodedImageFormat.Bmp)
            throw new OcrInputException("unsupported_image", "仅支持静态 PNG、JPEG 和 BMP。");
        if (codec.FrameCount > 1 || (header != null && header.FrameCount > 1))
            throw new OcrInputException("multiple_frames", "首版不支持多帧图片。");
        var width = codec.Info.Width; var height = codec.Info.Height;
        limits.CheckSize(width, height);
        if (header != null && (header.Width != width || header.Height != height))
            throw new OcrInputException("invalid_image", "头部尺寸与解码器不一致。");
        token.ThrowIfCancellationRequested();
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new OcrInputException("invalid_image", "图片像素不完整或损坏。");
        using var raw = SKImage.FromBitmap(bitmap);
        SKImage current = Orient(raw, header?.Orientation ?? (int)codec.EncodedOrigin);
        try
        {
            var originalWidth = current.Width; var originalHeight = current.Height;
            var forward = new OcrTransform();
            var warnings = new List<string>();
            token.ThrowIfCancellationRequested();
            if (options.Region is OcrRectangle r)
            {
                if ((long)r.X + r.Width > current.Width || (long)r.Y + r.Height > current.Height)
                    throw new OcrInputException("region_out_of_bounds", "识别区域超出完整显示图。");
                Replace(ref current, SkiaSharpHelper.Crop(current, new SKRectI(r.X, r.Y, r.X + r.Width, r.Y + r.Height)));
                forward = new OcrTransform(offsetX: -r.X, offsetY: -r.Y);
            }
            token.ThrowIfCancellationRequested();
            if (options.RotationDegrees != 0)
            {
                var rotation = options.RotationDegrees == 90 ? new OcrTransform(0, -1, 1, 0, current.Height, 0)
                    : options.RotationDegrees == 180 ? new OcrTransform(-1, 0, 0, -1, current.Width, current.Height)
                    : new OcrTransform(0, 1, -1, 0, 0, current.Width);
                Replace(ref current, SkiaSharpHelper.Rotate(current, options.RotationDegrees));
                forward = forward.Then(rotation);
            }
            if (options.Scale != 1)
            {
                var targetWidth = Math.Max(1, Math.Round(current.Width * options.Scale, MidpointRounding.AwayFromZero));
                var targetHeight = Math.Max(1, Math.Round(current.Height * options.Scale, MidpointRounding.AwayFromZero));
                if (!OcrPoint.IsFinite(targetWidth) || !OcrPoint.IsFinite(targetHeight) ||
                    targetWidth > limits.MaxDimension || targetHeight > limits.MaxDimension)
                    throw new OcrInputException("image_dimensions_exceeded", "缩放结果超过尺寸限制。");
                limits.CheckSize((int)targetWidth, (int)targetHeight);
                var scale = new OcrTransform(targetWidth / current.Width, 0, 0, targetHeight / current.Height);
                Replace(ref current, SkiaSharpHelper.Resize(current, (int)targetWidth, (int)targetHeight, false, true));
                forward = forward.Then(scale); warnings.Add("image_scaled");
            }
            token.ThrowIfCancellationRequested();
            // 白色底图合成透明区域，避免透明 PNG 在不同引擎呈现不同背景。
            using var output = new SKBitmap(current.Width, current.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(output))
            using (var paint = new SKPaint())
            {
                canvas.Clear(SKColors.White);
                using var filter = options.Preprocessing == OcrPreprocessingPreset.Scan
                    ? SKColorFilter.CreateColorMatrix(new float[] {
                        .2126f,.7152f,.0722f,0,0, .2126f,.7152f,.0722f,0,0,
                        .2126f,.7152f,.0722f,0,0, 0,0,0,1,0 }) : null;
                paint.ColorFilter = filter;
                canvas.DrawImage(current, 0, 0, paint); canvas.Flush();
            }
            using var outputImage = SKImage.FromBitmap(output);
            using var png = outputImage.Encode(SKEncodedImageFormat.Png, 100)
                ?? throw new OcrInputException("encode_failed", "无法编码预处理图片。");
            token.ThrowIfCancellationRequested();
            if (png.Size > limits.MaxPreparedBytes)
                throw new OcrInputException("prepared_image_too_large", "预处理 PNG 超过编码大小限制。");
            return new PreparedOcrImage(png.ToArray(), current.Width, current.Height, originalWidth, originalHeight,
                forward.Inverse(), warnings);
        }
        finally { current.Dispose(); }
    }

    private static void Replace(ref SKImage current, SKImage replacement) { current.Dispose(); current = replacement; }

    private static SKImage Orient(SKImage source, int orientation)
    {
        switch (orientation)
        {
            case 2: return SkiaSharpHelper.FlipHorizontal(source);
            case 3: return SkiaSharpHelper.Rotate(source, 180);
            case 4: return SkiaSharpHelper.FlipVertical(source);
            case 5:
                using (var flipped = SkiaSharpHelper.FlipHorizontal(source)) return SkiaSharpHelper.Rotate(flipped, 270);
            case 6: return SkiaSharpHelper.Rotate(source, 90);
            case 7:
                using (var flipped = SkiaSharpHelper.FlipHorizontal(source)) return SkiaSharpHelper.Rotate(flipped, 90);
            case 8: return SkiaSharpHelper.Rotate(source, 270);
            default: return SkiaSharpHelper.Crop(source, new SKRectI(0, 0, source.Width, source.Height));
        }
    }
}

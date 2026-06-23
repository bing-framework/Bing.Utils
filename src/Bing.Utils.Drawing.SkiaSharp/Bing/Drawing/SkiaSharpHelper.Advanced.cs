using SkiaSharp;

namespace Bing.Drawing;

// 图片操作辅助类 - 高级效果
public static partial class SkiaSharpHelper
{
    #region ToIcoStream(转换为ICO流)

    /// <summary>
    /// 将图像转换为多尺寸 ICO 流
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="sizes">ICO 尺寸列表。默认 16/32/48/64/128/256</param>
    /// <returns>ICO 格式的内存流，调用方负责释放</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Stream ToIcoStream(SKImage image, int[]? sizes = null)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        sizes = sizes ?? Internal.IcoContainerWriter.GetStandardIcoSizes();
        var frames = new List<(byte[] PngBytes, int Width, int Height)>();

        foreach (var size in sizes)
        {
            using var resized = Resize(image, size, size, false, true);
            var bytes = ToBytes(resized, (SKEncodedImageFormat.Png, 100));
            frames.Add((bytes, size, size));
        }

        var icoBytes = Internal.IcoContainerWriter.WriteIco(frames);
        var ms = new MemoryStream(icoBytes);
        return ms;
    }

    #endregion

    #region TwistImage(扭曲效果)

    /// <summary>
    /// 正弦曲线 Wave 扭曲效果
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="isTwist">是否纵向扭曲</param>
    /// <param name="shapeMultValue">波形幅度倍数，越大扭曲程度越高</param>
    /// <param name="shapePhase">波形起始相位，取值 [0, 2*PI]</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage TwistImage(SKImage image, bool isTwist, double shapeMultValue, double shapePhase)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        using var source = SKBitmap.FromImage(image);
        var width = source.Width;
        var height = source.Height;

        using var output = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(new SKColor(255, 255, 255, 255));

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var (destX, destY) = Internal.TwistGeometryProcessor.MapTwist(
                    x, y, width, height, isTwist, shapeMultValue, shapePhase);

                if (destX >= 0 && destX < width && destY >= 0 && destY < height)
                    output.SetPixel(destX, destY, source.GetPixel(x, y));
            }
        }

        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion

    #region SetErosionEffect(冲蚀效果)

    /// <summary>
    /// 设置冲蚀效果（模拟 PPT 冲蚀）：先调对比度，再调亮度，再与白色混合。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="brightness">改变亮度的百分比。范围：-100..100</param>
    /// <param name="contrast">改变对比度的百分比。范围：-100..100</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage SetErosionEffect(SKImage image, float brightness, float contrast)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        // 1. 对比度
        var contrastAmount = Internal.ErosionEffectHelper.GetNearlyAmount(contrast);
        using var withContrast = ApplyColorMatrix(image, CreateContrastMatrix(contrastAmount));

        // 2. 亮度
        var brightnessAmount = Internal.ErosionEffectHelper.GetNearlyAmount(brightness) / 2;
        using var withBrightness = ApplyColorMatrix(withContrast, CreateBrightnessMatrix(brightnessAmount));

        // 3. 混白色
        using var source = SKBitmap.FromImage(withBrightness);
        using var output = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < source.Width; x++)
        {
            for (var y = 0; y < source.Height; y++)
            {
                var color = source.GetPixel(x, y);
                var (r, g, b) = Internal.ErosionEffectHelper.BlendWithWhite(color.Red, color.Green, color.Blue, 0.5f);
                output.SetPixel(x, y, new SKColor(r, g, b, color.Alpha));
            }
        }

        return CopyTrackedFormat(image, SKImage.FromBitmap(output));
    }

    #endregion
}

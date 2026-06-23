using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace Bing.Drawing;

// 图片操作辅助类 - 高级效果
public static partial class ImageSharpHelper
{
    #region ToIcoStream(转换为ICO流)

    /// <summary>
    /// 将图像转换为多尺寸 ICO 流
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="sizes">ICO 尺寸列表。默认 16/32/48/64/128/256</param>
    /// <returns>ICO 格式的内存流，调用方负责释放</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Stream ToIcoStream(Image image, int[]? sizes = null)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        sizes = sizes ?? Internal.IcoContainerWriter.GetStandardIcoSizes();
        var frames = new List<(byte[] PngBytes, int Width, int Height)>();

        foreach (var size in sizes)
        {
            using var resized = Resize(image, size, size, false, true);
            var bytes = ToBytes(resized, PngFormat.Instance);
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
    public static Image TwistImage(Image image, bool isTwist, double shapeMultValue, double shapePhase)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var output = image.CloneAs<Rgba32>();
        var width = output.Width;
        var height = output.Height;

        // 创建白色背景的新图像
        var result = new Image<Rgba32>(width, height, new Rgba32(255, 255, 255, 255));

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var (destX, destY) = Internal.TwistGeometryProcessor.MapTwist(
                    x, y, width, height, isTwist, shapeMultValue, shapePhase);

                if (destX >= 0 && destX < width && destY >= 0 && destY < height)
                    result[destX, destY] = output[x, y];
            }
        }

        output.Dispose();
        return CopyTrackedFormat(image, result);
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
    public static Image SetErosionEffect(Image image, float brightness, float contrast)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        // 1. 对比度
        var contrastAmount = Internal.ErosionEffectHelper.GetNearlyAmount(contrast);
        var output = ApplyColorMatrix(image, CreateContrastMatrix(contrastAmount));

        // 2. 亮度
        var brightnessAmount = Internal.ErosionEffectHelper.GetNearlyAmount(brightness) / 2;
        var withBrightness = ApplyColorMatrix(output, CreateBrightnessMatrix(brightnessAmount));
        if (!ReferenceEquals(output, image))
            output.Dispose();

        // 3. 混白色
        var rgba = withBrightness.CloneAs<Rgba32>();
        if (!ReferenceEquals(withBrightness, image))
            withBrightness.Dispose();

        for (var x = 0; x < rgba.Width; x++)
        {
            for (var y = 0; y < rgba.Height; y++)
            {
                var color = rgba[x, y];
                var (r, g, b) = Internal.ErosionEffectHelper.BlendWithWhite(color.R, color.G, color.B, 0.5f);
                rgba[x, y] = new Rgba32(r, g, b, color.A);
            }
        }

        return CopyTrackedFormat(image, rgba);
    }

    #endregion
}

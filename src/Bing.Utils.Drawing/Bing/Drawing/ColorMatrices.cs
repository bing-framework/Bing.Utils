using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Bing.Drawing;

/// <summary>
/// 颜色转换矩阵
/// </summary>
public static class ColorMatrices
{
    /// <summary>
    /// 在图像上执行颜色矩阵的应用
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="colorMatrix">颜色处理矩阵</param>
    public static void ApplyMatrix(Image image, ColorMatrix colorMatrix)
    {
        using var graphics = Graphics.FromImage(image);
        using var imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        var imageWidth = image.Width;
        var imageHeight = image.Height;
        var imageRect = new Rectangle(0, 0, imageWidth, imageHeight);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.DrawImage(image, imageRect, 0, 0, imageWidth, imageHeight, GraphicsUnit.Pixel, imageAttributes);
    }

    /// <summary>
    /// 使用给定的数量创建亮度过滤器矩阵。
    /// <para>
    /// 使用算法<see href="https://cs.chromium.org/chromium/src/cc/paint/render_surface_filters.cc"/>
    /// </para>
    /// </summary>
    /// <param name="amount">转化比例，必须大于或等于 0。</param>
    /// <remarks>
    /// 值为 0 将创建一个完全黑色的图像。值为 1 时输入保持不变。
    /// 其他值是效果的线性乘数。允许超过 1 的值，从而提供更明亮的结果。
    /// </remarks>
    public static ColorMatrix CreateBrightnessFilter(float amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be >= 0");
        return new ColorMatrix
        {
            Matrix00 = amount,
            Matrix11 = amount,
            Matrix22 = amount,
            Matrix33 = 1F
        };
    }

    /// <summary>
    /// 使用给定的数量创建灰度滤波器矩阵。
    /// <para>
    /// 使用算法<see href="https://en.wikipedia.org/wiki/Luma_%28video%29#Rec._601_luma_versus_Rec._709_luma_coefficients"/>
    /// </para>
    /// </summary>
    /// <param name="amount">转化比例，必须大于等于 0 且小于等于 1。</param>
    public static ColorMatrix CreateGrayScaleFilter(float amount)
    {
        if (amount < 0 || amount > 1)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be in range 0..1");

        amount = 1F - amount;

        var matrix = new ColorMatrix();
        matrix.Matrix00 = .299F + (.701F * amount);
        matrix.Matrix10 = .587F - (.587F * amount);
        matrix.Matrix20 = 1F - (matrix.Matrix00 + matrix.Matrix10);

        matrix.Matrix01 = .299F - (.299F * amount);
        matrix.Matrix11 = .587F + (.2848F * amount);
        matrix.Matrix21 = 1F - (matrix.Matrix01 + matrix.Matrix11);

        matrix.Matrix02 = .299F - (.299F * amount);
        matrix.Matrix12 = .587F - (.587F * amount);
        matrix.Matrix22 = 1F - (matrix.Matrix02 + matrix.Matrix12);
        matrix.Matrix33 = 1F;

        return matrix;
    }

    /// <summary>
    /// 使用给定的数量创建对比度过滤器矩阵。
    /// <para>
    /// 使用算法<see href="https://cs.chromium.org/chromium/src/cc/paint/render_surface_filters.cc"/>
    /// </para>
    /// </summary>
    /// <param name="amount">转化比例，必须大于或等于 0。</param>
    /// <remarks>
    /// 值为 0 将创建一个完全灰色的图像。值为 1 时输入保持不变。
    /// 其他值是效果的线性乘数。允许超过 1 的值，从而提供具有更高对比度的结果。
    /// </remarks>
    public static ColorMatrix CreateContrastFilter(float amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be >= 0");

        var contrast = (-.5F * amount) + .5F;

        return new ColorMatrix
        {
            Matrix00 = amount,
            Matrix11 = amount,
            Matrix22 = amount,
            Matrix33 = 1F,
            Matrix40 = contrast,
            Matrix41 = contrast,
            Matrix42 = contrast
        };
    }

    /// <summary>
    /// 使用给定的数量创建饱和度过滤器矩阵。
    /// <para>
    /// 使用算法<see href="https://cs.chromium.org/chromium/src/cc/paint/render_surface_filters.cc"/>
    /// </para>
    /// </summary>
    /// <param name="amount">转化比例，必须大于或等于 0。</param>
    /// <remarks>
    /// 0 值是完全不饱和的。值为 1 时输入保持不变。
    /// 其他值是效果的线性乘数。允许超过 1 的值，提供超饱和结果。
    /// </remarks>
    public static ColorMatrix CreateSaturationFilter(float amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be >= 0");

        var matrix = new ColorMatrix();
        matrix.Matrix00 = .213F + (.787F * amount);
        matrix.Matrix10 = .715F - (.715F * amount);
        matrix.Matrix20 = 1F - (matrix.Matrix00 + matrix.Matrix10);

        matrix.Matrix01 = .213F - (.213F * amount);
        matrix.Matrix11 = .715F + (.285F * amount);
        matrix.Matrix21 = 1F - (matrix.Matrix01 + matrix.Matrix11);

        matrix.Matrix02 = .213F - (.213F * amount);
        matrix.Matrix12 = .715F - (.715F * amount);
        matrix.Matrix22 = 1F - (matrix.Matrix02 + matrix.Matrix12);
        matrix.Matrix33 = 1F;

        return matrix;
    }
}
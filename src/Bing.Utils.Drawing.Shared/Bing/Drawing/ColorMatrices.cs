namespace Bing.Drawing;

/// <summary>
/// 颜色转换矩阵工厂
/// </summary>
public static class ColorMatrices
{
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
    public static float[,] CreateBrightnessFilter(float amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be >= 0");
        return new float[,]
        {
            { amount, 0, 0, 0, 0 },
            { 0, amount, 0, 0, 0 },
            { 0, 0, amount, 0, 0 },
            { 0, 0, 0, 1F, 0 },
            { 0, 0, 0, 0, 1F }
        };
    }

    /// <summary>
    /// 使用给定的数量创建灰度滤波器矩阵。
    /// <para>
    /// 使用算法<see href="https://en.wikipedia.org/wiki/Luma_%28video%29#Rec._601_luma_versus_Rec._709_luma_coefficients"/>
    /// </para>
    /// </summary>
    /// <param name="amount">转化比例，必须大于等于 0 且小于等于 1。</param>
    public static float[,] CreateGrayScaleFilter(float amount)
    {
        if (amount < 0 || amount > 1)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be in range 0..1");

        var internalAmount = 1F - amount;

        var matrix00 = .299F + (.701F * internalAmount);
        var matrix10 = .587F - (.587F * internalAmount);
        var matrix20 = 1F - (matrix00 + matrix10);

        var matrix01 = .299F - (.299F * internalAmount);
        var matrix11 = .587F + (.2848F * internalAmount);
        var matrix21 = 1F - (matrix01 + matrix11);

        var matrix02 = .299F - (.299F * internalAmount);
        var matrix12 = .587F - (.587F * internalAmount);
        var matrix22 = 1F - (matrix02 + matrix12);

        return new float[,]
        {
            { matrix00, matrix01, matrix02, 0, 0 },
            { matrix10, matrix11, matrix12, 0, 0 },
            { matrix20, matrix21, matrix22, 0, 0 },
            { 0, 0, 0, 1F, 0 },
            { 0, 0, 0, 0, 1F }
        };
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
    public static float[,] CreateContrastFilter(float amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be >= 0");

        var contrast = (-.5F * amount) + .5F;
        return new float[,]
        {
            { amount, 0, 0, 0, 0 },
            { 0, amount, 0, 0, 0 },
            { 0, 0, amount, 0, 0 },
            { 0, 0, 0, 1F, 0 },
            { contrast, contrast, contrast, 0, 1F }
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
    public static float[,] CreateSaturationFilter(float amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Threshold must be >= 0");

        var matrix00 = .213F + (.787F * amount);
        var matrix10 = .715F - (.715F * amount);
        var matrix20 = 1F - (matrix00 + matrix10);

        var matrix01 = .213F - (.213F * amount);
        var matrix11 = .715F + (.285F * amount);
        var matrix21 = 1F - (matrix01 + matrix11);

        var matrix02 = .213F - (.213F * amount);
        var matrix12 = .715F - (.715F * amount);
        var matrix22 = 1F - (matrix02 + matrix12);

        return new float[,]
        {
            { matrix00, matrix01, matrix02, 0, 0 },
            { matrix10, matrix11, matrix12, 0, 0 },
            { matrix20, matrix21, matrix22, 0, 0 },
            { 0, 0, 0, 1F, 0 },
            { 0, 0, 0, 0, 1F }
        };
    }
}

namespace Bing.Drawing.Internal;

/// <summary>
/// 图像几何辅助。纯数学，不依赖任何图像后端。
/// </summary>
internal static class ImageGeometryHelper
{
    /// <summary>
    /// 将角度规范化到 [0, 360) 范围。
    /// </summary>
    /// <param name="angle">以度为单位的角度。</param>
    /// <returns>规范化后的角度。</returns>
    internal static float NormalizeAngle(int angle)
    {
        var normalized = angle % 360;
        if (normalized < 0)
            normalized += 360;
        return normalized;
    }

    /// <summary>
    /// 验证阈值是否处于 [0, 1] 范围。
    /// </summary>
    /// <param name="threshold">待验证的阈值。</param>
    internal static void ValidateThreshold(float threshold)
    {
        if (threshold < 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "阈值必须为0-1之间的浮点数");
    }

    /// <summary>
    /// 验证数值参数是否为非负数。
    /// </summary>
    /// <param name="amount">待验证的数值。</param>
    /// <param name="paramName">参数名，用于构造范围异常。</param>
    internal static void ValidateAmount(float amount, string paramName)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(paramName, "参数必须大于或等于0");
    }

    /// <summary>
    /// 验证颜色矩阵不为 null 且尺寸为 5x5。
    /// </summary>
    /// <param name="matrix">待验证的颜色矩阵。</param>
    internal static void ValidateColorMatrix(float[,] matrix)
    {
        if (matrix is null)
            throw new ArgumentNullException(nameof(matrix));
        if (matrix.GetLength(0) != 5 || matrix.GetLength(1) != 5)
            throw new ArgumentException("颜色矩阵必须为5x5", nameof(matrix));
    }

    /// <summary>
    /// 计算等比缩放后的尺寸。
    /// </summary>
    /// <param name="sourceWidth">源图像宽度。</param>
    /// <param name="sourceHeight">源图像高度。</param>
    /// <param name="targetWidth">目标边界宽度。</param>
    /// <param name="targetHeight">目标边界高度。</param>
    /// <param name="keepAspectRatio">是否保持宽高比。</param>
    /// <param name="allowEnlarge">是否允许放大源图像。</param>
    /// <returns>计算得到的宽度和高度。</returns>
    internal static (int Width, int Height) CalculateResizeSize(
        int sourceWidth, int sourceHeight, int targetWidth, int targetHeight,
        bool keepAspectRatio, bool allowEnlarge)
    {
        if (!keepAspectRatio)
        {
            return (
                allowEnlarge ? targetWidth : Math.Min(targetWidth, sourceWidth),
                allowEnlarge ? targetHeight : Math.Min(targetHeight, sourceHeight));
        }

        var scaleX = targetWidth / (double)sourceWidth;
        var scaleY = targetHeight / (double)sourceHeight;
        var scale = Math.Min(scaleX, scaleY);
        if (!allowEnlarge)
            scale = Math.Min(scale, 1d);

        var width = Math.Max(1, (int)Math.Round(sourceWidth * scale, MidpointRounding.AwayFromZero));
        var height = Math.Max(1, (int)Math.Round(sourceHeight * scale, MidpointRounding.AwayFromZero));
        return (width, height);
    }

    /// <summary>
    /// 规范化裁剪区域（左上角和宽高表示法）。
    /// </summary>
    /// <param name="left">裁剪区域左边界。</param>
    /// <param name="top">裁剪区域上边界。</param>
    /// <param name="width">裁剪区域宽度。</param>
    /// <param name="height">裁剪区域高度。</param>
    /// <param name="imageWidth">图像宽度。</param>
    /// <param name="imageHeight">图像高度。</param>
    /// <returns>裁剪区域与图像边界相交后的左、上坐标及宽、高。</returns>
    internal static (int Left, int Top, int Width, int Height) NormalizeCropRectangle(
        int left, int top, int width, int height, int imageWidth, int imageHeight)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "裁剪区域宽度必须大于0");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "裁剪区域高度必须大于0");

        var clampedLeft = DrawingCompatibilityHelper.Clamp(left, 0, imageWidth);
        var clampedTop = DrawingCompatibilityHelper.Clamp(top, 0, imageHeight);
        var clampedRight = DrawingCompatibilityHelper.Clamp(left + width, 0, imageWidth);
        var clampedBottom = DrawingCompatibilityHelper.Clamp(top + height, 0, imageHeight);

        if (clampedRight <= clampedLeft || clampedBottom <= clampedTop)
            throw new ArgumentOutOfRangeException(nameof(width), "裁剪区域超出图片边界");

        return (clampedLeft, clampedTop, clampedRight - clampedLeft, clampedBottom - clampedTop);
    }

    /// <summary>
    /// 对归一化输入应用 5x5 颜色矩阵，并将结果转换为 RGBA 分量。
    /// </summary>
    /// <param name="r">输入红色分量。</param>
    /// <param name="g">输入绿色分量。</param>
    /// <param name="b">输入蓝色分量。</param>
    /// <param name="a">输入透明度分量。</param>
    /// <param name="matrix">5x5 颜色变换矩阵。</param>
    /// <returns>经矩阵变换并限制在 0 到 255 的 RGBA 分量。</returns>
    internal static (byte R, byte G, byte B, byte A) ApplyColorMatrix(
        byte r, byte g, byte b, byte a, float[,] matrix)
    {
        var nr = r / 255f;
        var ng = g / 255f;
        var nb = b / 255f;
        var na = a / 255f;

        var resultR = nr * matrix[0, 0] + ng * matrix[1, 0] + nb * matrix[2, 0] + na * matrix[3, 0] + matrix[4, 0];
        var resultG = nr * matrix[0, 1] + ng * matrix[1, 1] + nb * matrix[2, 1] + na * matrix[3, 1] + matrix[4, 1];
        var resultB = nr * matrix[0, 2] + ng * matrix[1, 2] + nb * matrix[2, 2] + na * matrix[3, 2] + matrix[4, 2];
        var resultA = nr * matrix[0, 3] + ng * matrix[1, 3] + nb * matrix[2, 3] + na * matrix[3, 3] + matrix[4, 3];

        return (
            DrawingCompatibilityHelper.ClampToByte(resultR * 255f),
            DrawingCompatibilityHelper.ClampToByte(resultG * 255f),
            DrawingCompatibilityHelper.ClampToByte(resultB * 255f),
            DrawingCompatibilityHelper.ClampToByte(resultA * 255f));
    }

    /// <summary>
    /// 计算加权灰度值。
    /// </summary>
    /// <param name="r">红色分量。</param>
    /// <param name="g">绿色分量。</param>
    /// <param name="b">蓝色分量。</param>
    /// <returns>归一化到 0 到 1 的加权灰度值。</returns>
    internal static float GetGrayScale(byte r, byte g, byte b)
    {
        return (0.30f * r + 0.59f * g + 0.11f * b) / 255f;
    }

    /// <summary>
    /// 计算两个颜色的加权距离。
    /// </summary>
    /// <param name="r1">第一个颜色的红色分量。</param>
    /// <param name="g1">第一个颜色的绿色分量。</param>
    /// <param name="b1">第一个颜色的蓝色分量。</param>
    /// <param name="r2">第二个颜色的红色分量。</param>
    /// <param name="g2">第二个颜色的绿色分量。</param>
    /// <param name="b2">第二个颜色的蓝色分量。</param>
    /// <returns>两个颜色的加权距离。</returns>
    internal static double ColorDifference(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var m = (r1 + r2) / 2d;
        var r = Math.Pow(r1 - r2, 2);
        var g = Math.Pow(g1 - g2, 2);
        var b = Math.Pow(b1 - b2, 2);
        return Math.Sqrt((2 + m / 256d) * r + 4 * g + (2 + (255 - m) / 256d) * b);
    }

    /// <summary>
    /// 判断两个颜色是否满足近似判断。
    /// </summary>
    /// <param name="a1">第一个颜色的透明度分量。</param>
    /// <param name="r1">第一个颜色的红色分量。</param>
    /// <param name="g1">第一个颜色的绿色分量。</param>
    /// <param name="b1">第一个颜色的蓝色分量。</param>
    /// <param name="a2">第二个颜色的透明度分量。</param>
    /// <param name="r2">第二个颜色的红色分量。</param>
    /// <param name="g2">第二个颜色的绿色分量。</param>
    /// <param name="b2">第二个颜色的蓝色分量。</param>
    /// <param name="accuracy">允许的颜色差异阈值。</param>
    /// <returns>颜色满足相似度判断时为 true；否则为 false。</returns>
    internal static bool IsSimilarColors(
        byte a1, byte r1, byte g1, byte b1,
        byte a2, byte r2, byte g2, byte b2,
        int accuracy)
    {
        if (Math.Abs(a1 - a2) > 1)
            return false;

        var offsetR = r1 - r2;
        var offsetG = g1 - g2;
        var offsetB = b1 - b2;

        if (offsetB == offsetG && offsetR == offsetB)
        {
            if (Math.Abs(offsetR) > 1)
                return ColorDifference(r1, g1, b1, r2, g2, b2) <= accuracy / 3d;
        }

        return ColorDifference(r1, g1, b1, r2, g2, b2) <= accuracy;
    }
}

namespace Bing.Drawing.Internal;

/// <summary>
/// 灰度图像缓冲区工具
/// </summary>
internal static class GrayImageBuffer
{
    /// <summary>
    /// 将 RGB 分量转换为灰度值。
    /// </summary>
    /// <param name="r">红色分量。</param>
    /// <param name="g">绿色分量。</param>
    /// <param name="b">蓝色分量。</param>
    /// <returns>计算得到的灰度值。</returns>
    internal static byte ToGray(byte r, byte g, byte b)
    {
        return (byte)((r * 19595 + g * 38469 + b * 7472) >> 16);
    }

    /// <summary>
    /// 将灰度值二维数组二值化。
    /// 约定：前景 = 0，背景 = 255。灰度值大于阈值设为背景，否则设为前景。
    /// </summary>
    /// <param name="grayBytes">灰度值二维数组</param>
    /// <param name="threshold">灰度阈值</param>
    /// <returns>新数组（二值化结果）</returns>
    internal static byte[,] Binarize(byte[,] grayBytes, byte threshold)
    {
        int width = grayBytes.GetLength(0), height = grayBytes.GetLength(1);
        var result = new byte[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
                result[x, y] = grayBytes[x, y] > threshold ? (byte)255 : (byte)0;
        }

        return result;
    }

    /// <summary>
    /// 将灰度值二维数组前景色加黑。
    /// 灰度值小于阈值的像素全部设为前景（0）。
    /// </summary>
    /// <param name="grayBytes">灰度值二维数组</param>
    /// <param name="threshold">灰度阈值</param>
    /// <returns>新数组</returns>
    internal static byte[,] DeepenForeground(byte[,] grayBytes, byte threshold)
    {
        int width = grayBytes.GetLength(0), height = grayBytes.GetLength(1);
        var result = Copy(grayBytes);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (result[x, y] < threshold)
                    result[x, y] = 0;
            }
        }

        return result;
    }

    /// <summary>
    /// 将指定灰度范围设置为白色。
    /// </summary>
    /// <param name="grayBytes">源灰度矩阵。</param>
    /// <param name="minGray">范围下限。</param>
    /// <param name="maxGray">范围上限。</param>
    /// <returns>处理后的灰度矩阵。</returns>
    internal static byte[,] ClearGrayRange(byte[,] grayBytes, byte minGray, byte maxGray)
    {
        int width = grayBytes.GetLength(0), height = grayBytes.GetLength(1);
        var result = Copy(grayBytes);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (result[x, y] >= minGray && result[x, y] <= maxGray)
                    result[x, y] = 255;
            }
        }

        return result;
    }

    /// <summary>
    /// 去除空白边界，获取有效图形区域。
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundGray">前景阈值（小于此值视为前景）</param>
    /// <returns>裁剪后的新数组</returns>
    internal static byte[,] TrimToContent(byte[,] binBytes, byte foregroundGray)
    {
        int width = binBytes.GetLength(0), height = binBytes.GetLength(1);
        int x1 = width, y1 = height, x2 = 0, y2 = 0;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (binBytes[x, y] >= foregroundGray)
                    continue;
                if (x1 > x) x1 = x;
                if (y1 > y) y1 = y;
                if (x2 < x) x2 = x;
                if (y2 < y) y2 = y;
            }
        }

        if (x2 < x1 || y2 < y1)
            return new byte[0, 0];

        return SubMatrix(binBytes, x1, y1, x2 - x1 + 1, y2 - y1 + 1);
    }

    /// <summary>
    /// 将灰度矩阵边缘设置为白色。
    /// </summary>
    /// <param name="grayBytes">源灰度矩阵。</param>
    /// <param name="border">边框宽度。</param>
    /// <returns>处理后的灰度矩阵副本。</returns>
    internal static byte[,] ClearBorder(byte[,] grayBytes, int border)
    {
        int width = grayBytes.GetLength(0), height = grayBytes.GetLength(1);
        var result = Copy(grayBytes);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (x < border || y < border || x > width - 1 - border || y > height - 1 - border)
                    result[x, y] = 255;
            }
        }

        return result;
    }

    /// <summary>
    /// 为灰度矩阵添加指定颜色的边框。
    /// </summary>
    /// <param name="grayBytes">源灰度矩阵。</param>
    /// <param name="border">边框宽度。</param>
    /// <param name="borderColor">边框颜色。</param>
    /// <returns>添加边框后的灰度矩阵。</returns>
    internal static byte[,] AddBorder(byte[,] grayBytes, int border, byte borderColor)
    {
        int width = grayBytes.GetLength(0), height = grayBytes.GetLength(1);
        int newWidth = width + border * 2, newHeight = height + border * 2;
        var result = new byte[newWidth, newHeight];
        for (var x = 0; x < newWidth; x++)
        {
            for (var y = 0; y < newHeight; y++)
                result[x, y] = borderColor;
        }

        DrawOnto(result, grayBytes, border, border);
        return result;
    }

    /// <summary>
    /// 按偏移将源灰度矩阵绘制到目标矩阵。
    /// </summary>
    /// <param name="target">接收绘制结果的目标矩阵。</param>
    /// <param name="source">要绘制的源矩阵。</param>
    /// <param name="offsetX">水平方向偏移量。</param>
    /// <param name="offsetY">垂直方向偏移量。</param>
    internal static void DrawOnto(byte[,] target, byte[,] source, int offsetX, int offsetY)
    {
        int sw = source.GetLength(0), sh = source.GetLength(1);
        int tw = target.GetLength(0), th = target.GetLength(1);
        for (var x = 0; x < sw; x++)
        {
            for (var y = 0; y < sh; y++)
            {
                var tx = offsetX + x;
                var ty = offsetY + y;
                if (tx >= 0 && tx < tw && ty >= 0 && ty < th)
                    target[tx, ty] = source[x, y];
            }
        }
    }

    /// <summary>
    /// 复制灰度矩阵。
    /// </summary>
    /// <param name="source">要复制的灰度矩阵。</param>
    /// <returns>源矩阵的深拷贝。</returns>
    internal static byte[,] Copy(byte[,] source)
    {
        int w = source.GetLength(0), h = source.GetLength(1);
        var result = new byte[w, h];
        for (var x = 0; x < w; x++)
        {
            for (var y = 0; y < h; y++)
                result[x, y] = source[x, y];
        }

        return result;
    }

    /// <summary>
    /// 截取灰度矩阵的子矩阵。
    /// </summary>
    /// <param name="source">源灰度矩阵。</param>
    /// <param name="x1">起始横坐标。</param>
    /// <param name="y1">起始纵坐标。</param>
    /// <param name="width">子矩阵宽度。</param>
    /// <param name="height">子矩阵高度。</param>
    /// <returns>截取后的灰度矩阵。</returns>
    internal static byte[,] SubMatrix(byte[,] source, int x1, int y1, int width, int height)
    {
        int sw = source.GetLength(0), sh = source.GetLength(1);
        if (x1 + width > sw)
            width = sw - x1;
        if (y1 + height > sh)
            height = sh - y1;
        if (width <= 0 || height <= 0)
            return new byte[0, 0];

        var result = new byte[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
                result[x, y] = source[x1 + x, y1 + y];
        }

        return result;
    }
}

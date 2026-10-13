namespace Bing.Drawing.Internal;

/// <summary>
/// 二值化图像处理
/// </summary>
internal static class BinaryImageProcessor
{
    /// <summary>
    /// 判断像素是否属于前景。
    /// </summary>
    /// <param name="value">像素灰度值。</param>
    /// <param name="threshold">前景判定阈值。</param>
    /// <returns>像素灰度值小于阈值时为 true；否则为 false。</returns>
    internal static bool IsForeground(byte value, byte threshold)
    {
        return value < threshold;
    }

    /// <summary>
    /// 判断像素是否属于背景。
    /// </summary>
    /// <param name="value">像素灰度值。</param>
    /// <param name="threshold">背景判定阈值。</param>
    /// <returns>像素灰度值大于或等于阈值时为 true；否则为 false。</returns>
    internal static bool IsBackground(byte value, byte threshold)
    {
        return value >= threshold;
    }

    /// <summary>
    /// 统计 8 邻域内的前景像素数量
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="x">x 坐标</param>
    /// <param name="y">y 坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    internal static int CountForegroundNeighbors(byte[,] binBytes, int x, int y, int width, int height, byte foregroundThreshold)
    {
        var count = 0;
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                var nx = x + dx;
                var ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height
                    && IsForeground(binBytes[nx, ny], foregroundThreshold))
                    count++;
            }
        }

        return count;
    }
}

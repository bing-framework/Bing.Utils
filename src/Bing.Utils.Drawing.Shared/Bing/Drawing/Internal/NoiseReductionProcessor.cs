namespace Bing.Drawing.Internal;

/// <summary>
/// 噪声消除处理器
/// </summary>
internal static class NoiseReductionProcessor
{
    /// <summary>
    /// 去除附近噪声。基于 8 邻域统计，前景邻域数小于阈值则清除。
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    /// <param name="minNeighborCount">最小前景邻域数阈值，低于此值的前景像素将被清除</param>
    /// <returns>新数组</returns>
    internal static byte[,] ClearNoiseByNeighborCount(byte[,] binBytes, byte foregroundThreshold, int minNeighborCount)
    {
        int width = binBytes.GetLength(0), height = binBytes.GetLength(1);
        var result = GrayImageBuffer.Copy(binBytes);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (BinaryImageProcessor.IsBackground(result[x, y], foregroundThreshold))
                    continue;
                // 边框像素视为背景
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    result[x, y] = 255;
                    continue;
                }

                var neighborCount = BinaryImageProcessor.CountForegroundNeighbors(
                    binBytes, x, y, width, height, foregroundThreshold);
                if (neighborCount < minNeighborCount)
                    result[x, y] = 255;
            }
        }

        return result;
    }

    /// <summary>
    /// 去除区域噪声。基于连通域面积，面积小于阈值的连通域全部清除。
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    /// <param name="minAreaSize">最小连通域面积阈值</param>
    /// <returns>新数组</returns>
    internal static byte[,] ClearNoiseByArea(byte[,] binBytes, byte foregroundThreshold, int minAreaSize)
    {
        var components = ConnectedComponentProcessor.LabelConnectedComponents(binBytes, foregroundThreshold);
        var result = GrayImageBuffer.Copy(binBytes);
        foreach (var kvp in components)
        {
            if (kvp.Value.Count < minAreaSize)
            {
                foreach (var point in kvp.Value)
                    result[point.X, point.Y] = 255;
            }
        }

        return result;
    }
}

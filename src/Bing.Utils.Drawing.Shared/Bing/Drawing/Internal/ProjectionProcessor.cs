namespace Bing.Drawing.Internal;

/// <summary>
/// 投影处理器
/// </summary>
internal static class ProjectionProcessor
{
    /// <summary>
    /// 统计纵向投影（每列的前景像素数）
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    /// <returns>长度为 width 的数组</returns>
    internal static int[] VerticalProjection(byte[,] binBytes, byte foregroundThreshold)
    {
        int width = binBytes.GetLength(0), height = binBytes.GetLength(1);
        var projection = new int[width];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (BinaryImageProcessor.IsForeground(binBytes[x, y], foregroundThreshold))
                    projection[x]++;
            }
        }

        return projection;
    }

    /// <summary>
    /// 统计横向投影（每行的前景像素数）
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    /// <returns>长度为 height 的数组</returns>
    internal static int[] HorizontalProjection(byte[,] binBytes, byte foregroundThreshold)
    {
        int width = binBytes.GetLength(0), height = binBytes.GetLength(1);
        var projection = new int[height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (BinaryImageProcessor.IsForeground(binBytes[x, y], foregroundThreshold))
                    projection[y]++;
            }
        }

        return projection;
    }

    /// <summary>
    /// 根据纵向投影切分区间
    /// </summary>
    /// <param name="projection">纵向投影</param>
    /// <param name="minCount">最小有效前景数（低于此值忽略）</param>
    /// <returns>切分区间的列表 (startX, endX)</returns>
    internal static List<(int Start, int End)> FindVerticalSegments(int[] projection, int minCount)
    {
        var segments = new List<(int Start, int End)>();
        var inSegment = false;
        var segmentStart = 0;
        for (var x = 0; x < projection.Length; x++)
        {
            if (!inSegment)
            {
                if (projection[x] > minCount)
                {
                    inSegment = true;
                    segmentStart = x;
                }
            }
            else
            {
                if (projection[x] <= minCount)
                {
                    inSegment = false;
                    segments.Add((segmentStart, x - 1));
                }
            }
        }

        if (inSegment)
            segments.Add((segmentStart, projection.Length - 1));

        return segments;
    }

    /// <summary>
    /// 根据纵向投影和最小宽度切分图像
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    /// <param name="minFontWidth">最小字符宽度（0 表示自动）</param>
    /// <param name="minLines">最小有效投影行数</param>
    /// <returns>切分后的子图列表</returns>
    internal static List<byte[,]> SplitByVerticalProjection(byte[,] binBytes, byte foregroundThreshold, int minFontWidth, int minLines)
    {
        var projection = VerticalProjection(binBytes, foregroundThreshold);
        var segments = FindVerticalSegments(projection, minLines);
        var result = new List<byte[,]>();

        foreach (var segment in segments)
        {
            var segmentWidth = segment.End - segment.Start + 1;
            if (minFontWidth > 0 && segmentWidth <= minFontWidth)
                continue;

            var subMatrix = GrayImageBuffer.SubMatrix(binBytes, segment.Start, 0, segmentWidth, binBytes.GetLength(1));
            var trimmed = GrayImageBuffer.TrimToContent(subMatrix, foregroundThreshold);
            if (trimmed.GetLength(0) > 0 && trimmed.GetLength(1) > 0)
                result.Add(trimmed);
        }

        return result;
    }

    /// <summary>
    /// 将二值化数组转换为特征码字符串（调试用）
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    /// <param name="breakLine">是否在每行后换行</param>
    /// <returns>特征码字符串</returns>
    internal static string ToCodeString(byte[,] binBytes, byte foregroundThreshold, bool breakLine)
    {
        int width = binBytes.GetLength(0), height = binBytes.GetLength(1);
        var code = string.Empty;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                code += BinaryImageProcessor.IsForeground(binBytes[x, y], foregroundThreshold) ? "1" : "0";
            if (breakLine)
                code += "\r\n";
        }

        return code;
    }
}

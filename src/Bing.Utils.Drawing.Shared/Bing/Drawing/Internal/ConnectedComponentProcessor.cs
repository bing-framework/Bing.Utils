namespace Bing.Drawing.Internal;

/// <summary>
/// 连通域处理器。使用 4 邻域标记。
/// </summary>
internal static class ConnectedComponentProcessor
{
    /// <summary>
    /// 标记所有 4 邻域连通域。
    /// </summary>
    /// <param name="binBytes">二值化数组</param>
    /// <param name="foregroundThreshold">前景阈值（小于该值视为前景）</param>
    /// <returns>组件 ID 到像素列表的映射</returns>
    internal static Dictionary<int, List<MatrixPoint>> LabelConnectedComponents(byte[,] binBytes, byte foregroundThreshold)
    {
        int width = binBytes.GetLength(0), height = binBytes.GetLength(1);
        var visited = new bool[width, height];
        var components = new Dictionary<int, List<MatrixPoint>>();
        var componentId = 0;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (visited[x, y])
                    continue;
                if (BinaryImageProcessor.IsBackground(binBytes[x, y], foregroundThreshold))
                {
                    visited[x, y] = true;
                    continue;
                }

                var points = FloodFill4(binBytes, visited, x, y, width, height, foregroundThreshold);
                if (points.Count > 0)
                    components[componentId++] = points;
            }
        }

        return components;
    }

    /// <summary>
    /// 4 邻域泛洪填充，返回填充的像素列表
    /// </summary>
    internal static List<MatrixPoint> FloodFill4(byte[,] binBytes, bool[,] visited, int startX, int startY, int width, int height, byte foregroundThreshold)
    {
        var points = new List<MatrixPoint>();
        var stack = new Stack<MatrixPoint>();
        stack.Push(new MatrixPoint(startX, startY));

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height)
                continue;
            if (visited[p.X, p.Y])
                continue;
            if (BinaryImageProcessor.IsBackground(binBytes[p.X, p.Y], foregroundThreshold))
            {
                visited[p.X, p.Y] = true;
                continue;
            }

            visited[p.X, p.Y] = true;
            points.Add(p);

            stack.Push(new MatrixPoint(p.X - 1, p.Y));
            stack.Push(new MatrixPoint(p.X + 1, p.Y));
            stack.Push(new MatrixPoint(p.X, p.Y - 1));
            stack.Push(new MatrixPoint(p.X, p.Y + 1));
        }

        return points;
    }

    /// <summary>
    /// 4 邻域泛洪填充（替换灰度值方式），用于就地标记
    /// </summary>
    /// <param name="binBytes">二值化数组（就地修改）</param>
    /// <param name="startX">起始 x</param>
    /// <param name="startY">起始 y</param>
    /// <param name="replacementGray">替换灰度值</param>
    /// <returns>填充的像素列表</returns>
    internal static List<MatrixPoint> FloodFillReplace(byte[,] binBytes, int startX, int startY, byte replacementGray)
    {
        int width = binBytes.GetLength(0), height = binBytes.GetLength(1);
        if (startX < 0 || startX >= width || startY < 0 || startY >= height)
            return new List<MatrixPoint>();

        var targetGray = binBytes[startX, startY];
        if (targetGray == replacementGray)
            return new List<MatrixPoint>();

        var points = new List<MatrixPoint>();
        var stack = new Stack<MatrixPoint>();
        stack.Push(new MatrixPoint(startX, startY));

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height)
                continue;
            if (binBytes[p.X, p.Y] != targetGray)
                continue;

            binBytes[p.X, p.Y] = replacementGray;
            points.Add(p);

            stack.Push(new MatrixPoint(p.X - 1, p.Y));
            stack.Push(new MatrixPoint(p.X + 1, p.Y));
            stack.Push(new MatrixPoint(p.X, p.Y - 1));
            stack.Push(new MatrixPoint(p.X, p.Y + 1));
        }

        return points;
    }

    /// <summary>
    /// 从连通域结果中获取所有组件的像素总数
    /// </summary>
    internal static int GetTotalComponentPixels(Dictionary<int, List<MatrixPoint>> components)
    {
        var total = 0;
        foreach (var kvp in components)
            total += kvp.Value.Count;
        return total;
    }
}

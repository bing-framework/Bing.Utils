namespace Bing.Drawing;

/// <summary>
/// 二值化矩阵公共兼容层。
/// <para>
/// 维度语义统一为 <c>[width, height]</c>，坐标 <c>(x, y)</c> 原点左上。
/// 前景 = 小于阈值，背景 = 大于等于阈值。
/// </para>
/// <para>
/// 本类提供 <c>byte[,]</c> 与一维 <c>byte[]</c> 之间的转换辅助，以及常用的矩阵操作。
/// 核心算法位于 Internal 层，不建议将 Internal 处理器直接暴露为公共 API。
/// </para>
/// </summary>
public static class BinaryMatrixHelper
{
    #region 一维/二维转换

    /// <summary>
    /// 将一维字节数组转换为二维矩阵 [width, height]
    /// </summary>
    /// <param name="buffer">一维字节数组</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static byte[,] FromFlatArray(byte[] buffer, int width, int height)
    {
        if (buffer is null)
            throw new ArgumentNullException(nameof(buffer));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (buffer.Length != width * height)
            throw new ArgumentException($"缓冲区长度 {buffer.Length} 与指定尺寸 {width}*{height}={width * height} 不匹配");

        var result = new byte[width, height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                result[x, y] = buffer[y * width + x];
        }

        return result;
    }

    /// <summary>
    /// 将二维矩阵 [width, height] 转换为一维字节数组（行优先）
    /// </summary>
    /// <param name="matrix">二维矩阵</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static byte[] ToFlatArray(byte[,] matrix)
    {
        if (matrix is null)
            throw new ArgumentNullException(nameof(matrix));

        var width = matrix.GetLength(0);
        var height = matrix.GetLength(1);
        var result = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                result[y * width + x] = matrix[x, y];
        }

        return result;
    }

    #endregion

    #region ClearBorder

    /// <summary>
    /// 去除图片边框（设为背景 255）
    /// </summary>
    /// <param name="source">输入矩阵</param>
    /// <param name="border">边框宽度</param>
    /// <returns>新矩阵</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[,] ClearBorder(byte[,] source, int border)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (border < 0)
            throw new ArgumentOutOfRangeException(nameof(border));

        return Internal.GrayImageBuffer.ClearBorder(source, border);
    }

    #endregion

    #region AddBorder

    /// <summary>
    /// 添加图片边框（默认白色背景）
    /// </summary>
    /// <param name="source">输入矩阵</param>
    /// <param name="border">边框宽度</param>
    /// <param name="background">背景灰度值，默认 255</param>
    /// <returns>新矩阵（尺寸为原尺寸 + border*2）</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[,] AddBorder(byte[,] source, int border, byte background = 255)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (border < 0)
            throw new ArgumentOutOfRangeException(nameof(border));

        return Internal.GrayImageBuffer.AddBorder(source, border, background);
    }

    #endregion

    #region Clone

    /// <summary>
    /// 从原矩阵中截取子矩阵
    /// </summary>
    /// <param name="source">输入矩阵</param>
    /// <param name="x">起始 X 坐标</param>
    /// <param name="y">起始 Y 坐标</param>
    /// <param name="width">截取宽度</param>
    /// <param name="height">截取高度</param>
    /// <returns>新矩阵</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[,] Clone(byte[,] source, int x, int y, int width, int height)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (x < 0 || y < 0 || width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException("坐标和尺寸必须为非负整数且宽高大于0");

        return Internal.GrayImageBuffer.SubMatrix(source, x, y, width, height);
    }

    #endregion

    #region DrawTo

    /// <summary>
    /// 将小图矩阵绘制到大图矩阵上
    /// </summary>
    /// <param name="source">小图矩阵（源）</param>
    /// <param name="target">大图矩阵（目标，就地修改）</param>
    /// <param name="offsetX">目标偏移 X</param>
    /// <param name="offsetY">目标偏移 Y</param>
    /// <returns>修改后的目标矩阵</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static byte[,] DrawTo(byte[,] source, byte[,] target, int offsetX, int offsetY)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        var sw = source.GetLength(0);
        var sh = source.GetLength(1);
        var tw = target.GetLength(0);
        var th = target.GetLength(1);

        if (offsetX + sw > tw)
            throw new ArgumentException("目标矩阵宽度无法容纳源矩阵宽度");
        if (offsetY + sh > th)
            throw new ArgumentException("目标矩阵高度无法容纳源矩阵高度");

        Internal.GrayImageBuffer.DrawOnto(target, source, offsetX, offsetY);
        return target;
    }

    #endregion

    #region FloodFill

    /// <summary>
    /// 泛水填充算法，将相连通的区域使用指定灰度值填充
    /// </summary>
    /// <param name="source">输入矩阵（就地修改）</param>
    /// <param name="startX">起始 X 坐标</param>
    /// <param name="startY">起始 Y 坐标</param>
    /// <param name="replacementGray">替换灰度值</param>
    /// <returns>修改后的矩阵</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte[,] FloodFill(byte[,] source, int startX, int startY, byte replacementGray)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        Internal.ConnectedComponentProcessor.FloodFillReplace(source, startX, startY, replacementGray);
        return source;
    }

    #endregion

    #region ToCodeString

    /// <summary>
    /// 将二值化数组转换为特征码字符串（调试用途）。
    /// 前景显示为 '1'，背景显示为 '0'。
    /// </summary>
    /// <param name="source">输入矩阵</param>
    /// <param name="foregroundThreshold">前景阈值，小于此值视为前景</param>
    /// <param name="breakLine">是否每行末尾换行</param>
    /// <returns>特征码字符串</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToCodeString(byte[,] source, byte foregroundThreshold = 128, bool breakLine = false)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return Internal.ProjectionProcessor.ToCodeString(source, foregroundThreshold, breakLine);
    }

    #endregion
}

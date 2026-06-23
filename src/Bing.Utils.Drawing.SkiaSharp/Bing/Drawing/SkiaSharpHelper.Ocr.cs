using SkiaSharp;

namespace Bing.Drawing;

// 图片操作辅助类 - OCR 预处理
public static partial class SkiaSharpHelper
{
    #region ToGrayArray2D(转换为灰度二维数组)

    /// <summary>
    /// 将图像转换为灰度二维数组 [width, height]。灰度公式：0.299*R + 0.587*G + 0.114*B
    /// </summary>
    /// <param name="image">图片</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static byte[,] ToGrayArray2D(SKImage image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        using var bitmap = SKBitmap.FromImage(image);
        var width = bitmap.Width;
        var height = bitmap.Height;
        var result = new byte[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var color = bitmap.GetPixel(x, y);
                result[x, y] = Internal.GrayImageBuffer.ToGray(color.Red, color.Green, color.Blue);
            }
        }

        return result;
    }

    #endregion

    #region ToBinaryArray2D(转换为二值化二维数组)

    /// <summary>
    /// 将图像转换为二值化二维数组 [width, height]。
    /// 约定：前景 = 0，背景 = 255。灰度值大于阈值设为背景。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">二值化阈值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static byte[,] ToBinaryArray2D(SKImage image, byte threshold)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var gray = ToGrayArray2D(image);
        return Internal.GrayImageBuffer.Binarize(gray, threshold);
    }

    #endregion

    #region CreateImageFromGrayArray(从灰度数组创建图像)

    /// <summary>
    /// 从灰度二维数组 [width, height] 创建图像
    /// </summary>
    /// <param name="grayBytes">灰度二维数组</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage CreateImageFromGrayArray(byte[,] grayBytes)
    {
        if (grayBytes is null)
            throw new ArgumentNullException(nameof(grayBytes));

        var width = grayBytes.GetLength(0);
        var height = grayBytes.GetLength(1);
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var v = grayBytes[x, y];
                bitmap.SetPixel(x, y, new SKColor(v, v, v, 255));
            }
        }

        return SKImage.FromBitmap(bitmap);
    }

    /// <summary>
    /// 从二值化二维数组 [width, height] 创建图像。
    /// 前景(0)显示为黑色，背景(255)显示为白色。
    /// </summary>
    /// <param name="binBytes">二值化二维数组</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage CreateImageFromBinaryArray(byte[,] binBytes)
    {
        if (binBytes is null)
            throw new ArgumentNullException(nameof(binBytes));

        var width = binBytes.GetLength(0);
        var height = binBytes.GetLength(1);
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var v = binBytes[x, y];
                bitmap.SetPixel(x, y, new SKColor(v, v, v, 255));
            }
        }

        return SKImage.FromBitmap(bitmap);
    }

    #endregion

    #region Binaryzation(图像二值化)

    /// <summary>
    /// 将图像二值化，返回新图像。
    /// 灰度值大于阈值的像素设为白色，否则设为黑色。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">二值化阈值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage Binaryzation(SKImage image, byte threshold)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bin = ToBinaryArray2D(image, threshold);
        return CreateImageFromBinaryArray(bin);
    }

    #endregion

    #region DeepenForeground(前景加黑)

    /// <summary>
    /// 将图像前景加黑。灰度值小于阈值的像素全部设为纯黑。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">前景阈值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage DeepenForeground(SKImage image, byte threshold = 200)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var gray = ToGrayArray2D(image);
        var result = Internal.GrayImageBuffer.DeepenForeground(gray, threshold);
        return CreateImageFromGrayArray(result);
    }

    #endregion

    #region ClearGrayRange(去除指定范围灰度)

    /// <summary>
    /// 将图像中 [minGray, maxGray] 范围内的灰度值设为白色背景。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="minGray">最小灰度</param>
    /// <param name="maxGray">最大灰度</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage ClearGrayRange(SKImage image, byte minGray, byte maxGray)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var gray = ToGrayArray2D(image);
        var result = Internal.GrayImageBuffer.ClearGrayRange(gray, minGray, maxGray);
        return CreateImageFromGrayArray(result);
    }

    #endregion

    #region ClearNoiseByNeighborCount(邻域去噪)

    /// <summary>
    /// 基于 8 邻域统计去除噪声。前景邻域数小于阈值的像素将被清除为背景。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">二值化阈值</param>
    /// <param name="minNeighborCount">最小前景邻域数</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage ClearNoiseByNeighborCount(SKImage image, byte threshold, int minNeighborCount)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bin = ToBinaryArray2D(image, threshold);
        var result = Internal.NoiseReductionProcessor.ClearNoiseByNeighborCount(bin, threshold, minNeighborCount);
        return CreateImageFromBinaryArray(result);
    }

    #endregion

    #region ClearNoiseByArea(连通域去噪)

    /// <summary>
    /// 基于连通域面积去除噪声。面积小于阈值的连通域全部清除为背景。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">二值化阈值</param>
    /// <param name="minAreaSize">最小连通域面积</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage ClearNoiseByArea(SKImage image, byte threshold, int minAreaSize)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bin = ToBinaryArray2D(image, threshold);
        var result = Internal.NoiseReductionProcessor.ClearNoiseByArea(bin, threshold, minAreaSize);
        return CreateImageFromBinaryArray(result);
    }

    #endregion

    #region TrimToContent(去除空白边界)

    /// <summary>
    /// 去除图像的空白边界，获取有效内容区域。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="foregroundThreshold">前景阈值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static SKImage TrimToContent(SKImage image, byte foregroundThreshold = 128)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bin = ToBinaryArray2D(image, foregroundThreshold);
        var trimmed = Internal.GrayImageBuffer.TrimToContent(bin, foregroundThreshold);
        return CreateImageFromBinaryArray(trimmed);
    }

    #endregion

    #region GetVerticalProjection(纵向投影)

    /// <summary>
    /// 获取图像的纵向投影（每列的前景像素数）。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">前景阈值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static int[] GetVerticalProjection(SKImage image, byte threshold = 128)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bin = ToBinaryArray2D(image, threshold);
        return Internal.ProjectionProcessor.VerticalProjection(bin, threshold);
    }

    #endregion

    #region GetHorizontalProjection(横向投影)

    /// <summary>
    /// 获取图像的横向投影（每行的前景像素数）。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">前景阈值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static int[] GetHorizontalProjection(SKImage image, byte threshold = 128)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bin = ToBinaryArray2D(image, threshold);
        return Internal.ProjectionProcessor.HorizontalProjection(bin, threshold);
    }

    #endregion

    #region SplitByVerticalProjection(按纵向投影切分)

    /// <summary>
    /// 根据纵向投影切分图像为多个子图。
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="threshold">前景阈值</param>
    /// <param name="minFontWidth">最小字符宽度（0 表示自动）</param>
    /// <param name="minLines">最小有效投影行数</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static List<SKImage> SplitByVerticalProjection(SKImage image, byte threshold = 128, int minFontWidth = 0, int minLines = 0)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bin = ToBinaryArray2D(image, threshold);
        var splits = Internal.ProjectionProcessor.SplitByVerticalProjection(bin, threshold, minFontWidth, minLines);
        var result = new List<SKImage>();
        foreach (var split in splits)
        {
            if (split.GetLength(0) > 0 && split.GetLength(1) > 0)
                result.Add(CreateImageFromBinaryArray(split));
        }

        return result;
    }

    #endregion
}

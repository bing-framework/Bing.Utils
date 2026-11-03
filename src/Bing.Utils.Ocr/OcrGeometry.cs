namespace Bing.Ocr;

/// <summary>以左上角为原点、向右和向下递增的浮点像素坐标。</summary>
public readonly struct OcrPoint
{
    /// <summary>创建有限数值坐标。</summary>
    public OcrPoint(double x, double y)
    {
        if (!IsFinite(x) || !IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(x), "坐标必须为有限数值。");
        X = x; Y = y;
    }
    /// <summary>横坐标。</summary>
    public double X { get; }
    /// <summary>纵坐标。</summary>
    public double Y { get; }
    internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

/// <summary>EXIF 校正完整图片内的整数裁切区域，右边界与下边界不包含在内。</summary>
public sealed class OcrRectangle
{
    /// <summary>创建位置非负、尺寸为正的矩形。</summary>
    public OcrRectangle(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        X = x; Y = y; Width = width; Height = height;
    }
    /// <summary>左边界。</summary>
    public int X { get; }
    /// <summary>上边界。</summary>
    public int Y { get; }
    /// <summary>宽度。</summary>
    public int Width { get; }
    /// <summary>高度。</summary>
    public int Height { get; }
}

/// <summary>可逆二维仿射变换；x'=M11*x+M12*y+OffsetX，y'=M21*x+M22*y+OffsetY。</summary>
public sealed class OcrTransform
{
    /// <summary>创建可逆变换；默认值为单位变换。</summary>
    public OcrTransform(double m11 = 1, double m12 = 0, double m21 = 0, double m22 = 1, double offsetX = 0, double offsetY = 0)
    {
        if (new[] { m11, m12, m21, m22, offsetX, offsetY }.Any(x => !OcrPoint.IsFinite(x)))
            throw new ArgumentException("变换参数必须为有限数值。");
        var determinant = m11 * m22 - m12 * m21;
        if (!OcrPoint.IsFinite(determinant) || determinant == 0) throw new ArgumentException("变换必须可逆。");
        M11 = m11; M12 = m12; M21 = m21; M22 = m22; OffsetX = offsetX; OffsetY = offsetY;
    }
    /// <summary>第一行第一列。</summary>
    public double M11 { get; }
    /// <summary>第一行第二列。</summary>
    public double M12 { get; }
    /// <summary>第二行第一列。</summary>
    public double M21 { get; }
    /// <summary>第二行第二列。</summary>
    public double M22 { get; }
    /// <summary>横向平移。</summary>
    public double OffsetX { get; }
    /// <summary>纵向平移。</summary>
    public double OffsetY { get; }
    /// <summary>将点映射到目标坐标系。</summary>
    public OcrPoint Apply(OcrPoint point) => new(M11 * point.X + M12 * point.Y + OffsetX, M21 * point.X + M22 * point.Y + OffsetY);
    /// <summary>返回逆变换。</summary>
    public OcrTransform Inverse()
    {
        var d = M11 * M22 - M12 * M21;
        return new(M22 / d, -M12 / d, -M21 / d, M11 / d,
            (M12 * OffsetY - M22 * OffsetX) / d, (M21 * OffsetX - M11 * OffsetY) / d);
    }
    /// <summary>先执行当前变换，再执行 next。</summary>
    public OcrTransform Then(OcrTransform next)
    {
        if (next == null) throw new ArgumentNullException(nameof(next));
        return new(next.M11 * M11 + next.M12 * M21, next.M11 * M12 + next.M12 * M22,
            next.M21 * M11 + next.M22 * M21, next.M21 * M12 + next.M22 * M22,
            next.M11 * OffsetX + next.M12 * OffsetY + next.OffsetX, next.M21 * OffsetX + next.M22 * OffsetY + next.OffsetY);
    }
}

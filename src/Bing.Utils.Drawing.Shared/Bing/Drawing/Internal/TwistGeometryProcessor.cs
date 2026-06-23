namespace Bing.Drawing.Internal;

/// <summary>
/// 扭曲几何处理器。正弦曲线 Wave 扭曲。
/// </summary>
internal static class TwistGeometryProcessor
{
    /// <summary>
    /// 计算扭曲后的目标像素坐标。
    /// </summary>
    /// <param name="srcX">源像素 X</param>
    /// <param name="srcY">源像素 Y</param>
    /// <param name="width">画布宽度</param>
    /// <param name="height">画布高度</param>
    /// <param name="isTwist">true=纵向扭曲，false=横向扭曲</param>
    /// <param name="shapeMultValue">波形幅度倍数</param>
    /// <param name="shapePhase">波形起始相位</param>
    /// <returns>目标像素坐标</returns>
    internal static (int DestX, int DestY) MapTwist(
        int srcX, int srcY, int width, int height,
        bool isTwist, double shapeMultValue, double shapePhase)
    {
        double baseAxisLen = isTwist ? (double)height : (double)width;
        if (baseAxisLen <= 0)
            return (srcX, srcY);

        double dx = isTwist
            ? (2 * Math.PI * srcY) / baseAxisLen
            : (2 * Math.PI * srcX) / baseAxisLen;
        dx += shapePhase;
        double dy = Math.Sin(dx);

        var destX = isTwist ? srcX + (int)(dy * shapeMultValue) : srcX;
        var destY = isTwist ? srcY : srcY + (int)(dy * shapeMultValue);

        return (destX, destY);
    }
}

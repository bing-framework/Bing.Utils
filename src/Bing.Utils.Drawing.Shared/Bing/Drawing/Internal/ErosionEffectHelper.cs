namespace Bing.Drawing.Internal;

/// <summary>
/// 冲蚀效果辅助。模拟 PPT 冲蚀效果：先调对比度，再调亮度，再混白色。
/// </summary>
internal static class ErosionEffectHelper
{
    /// <summary>
    /// 将百分比参数转换为接近 PPT 效果的缩放因子。
    /// </summary>
    /// <param name="percentage">百分比参数，范围 -100..100</param>
    /// <returns>缩放因子</returns>
    internal static float GetNearlyAmount(float percentage)
    {
        var amount = (percentage + 100) / 100f;
        if (percentage > 0)
        {
            var x = (percentage - 60) / 10f;
            var y = 2 + (x > 0 ? x : 0);
            amount = (float)Math.Pow(amount, y);
        }

        return amount;
    }

    /// <summary>
    /// 混合前景色与白色背景。
    /// </summary>
    /// <param name="r">红色</param>
    /// <param name="g">绿色</param>
    /// <param name="b">蓝色</param>
    /// <param name="amount">保留比例（0..1）</param>
    /// <returns>混合后的 (r, g, b)</returns>
    internal static (byte R, byte G, byte B) BlendWithWhite(byte r, byte g, byte b, float amount)
    {
        var blendR = (byte)Math.Round(r * amount + 255 * (1 - amount), MidpointRounding.AwayFromZero);
        var blendG = (byte)Math.Round(g * amount + 255 * (1 - amount), MidpointRounding.AwayFromZero);
        var blendB = (byte)Math.Round(b * amount + 255 * (1 - amount), MidpointRounding.AwayFromZero);
        return (blendR, blendG, blendB);
    }
}

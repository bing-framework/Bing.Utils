using System;
using System.Drawing;

// ReSharper disable once CheckNamespace
namespace Bing.Drawing;

/// <summary>
/// 颜色(<see cref="Color"/>) 扩展
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// 获取颜色的灰度值
    /// </summary>
    /// <param name="color">颜色</param>
    public static float GetGrayScale(this Color color)
    {
        return (0.30f * color.R + 0.59f * color.G + 0.11f * color.B) / 255;
    }

    /// <summary>
    /// 获取双色调效果颜色
    /// </summary>
    /// <param name="sourceColor">原始颜色</param>
    /// <param name="clr1">决定双色调效果的颜色A</param>
    /// <param name="clr2">决定双色调效果的颜色B</param>
    public static Color GetDuotoneColor(this Color sourceColor, Color clr1, Color clr2)
    {
        var grayScale = GetGrayScale(sourceColor);
        var r = clr1.R * (1 - grayScale) + clr2.R * grayScale;
        var g = clr1.G * (1 - grayScale) + clr2.G * grayScale;
        var b = clr1.B * (1 - grayScale) + clr2.B * grayScale;
        return Color.FromArgb(sourceColor.A, (byte)r, (byte)g, (byte)b);
    }

    /// <summary>
    /// 混合颜色
    /// </summary>
    /// <param name="color">背景颜色</param>
    /// <param name="backColor">其它混合背景颜色</param>
    /// <param name="amount">保留多少颜色</param>
    public static Color Blend(this Color color, Color backColor, double amount)
    {
        var r = (byte)((color.R * amount) + backColor.R * (1 - amount));
        var g = (byte)((color.G * amount) + backColor.G * (1 - amount));
        var b = (byte)((color.B * amount) + backColor.B * (1 - amount));
        return Color.FromArgb(color.A, r, g, b);
    }

    /// <summary>
    /// 是否是近似颜色
    /// </summary>
    /// <param name="x">颜色A</param>
    /// <param name="y">颜色B</param>
    /// <param name="accuracy">允许的误差值。默认：36%</param>
    public static bool IsSimilarColors(this Color x, Color y, int accuracy = 36)
    {
        var offsetA = x.A - y.A;
        var offsetR = x.R - y.R;
        var offsetG = x.G - y.G;
        var offsetB = x.B - y.B;

        if (Math.Abs(offsetA) > 1)
            return false;

        if (offsetB == offsetG && offsetR == offsetB)
        {
            if (Math.Abs(offsetR) > 1)
                return ColorDifference(x, y) <= accuracy / 3d;
        }

        var difference = ColorDifference(x, y);
        return difference <= accuracy;
    }

    /// <summary>
    /// 颜色差异。在RGB空间上通过公式计算出加权的欧式距离
    /// </summary>
    /// <param name="x">颜色A</param>
    /// <param name="y">颜色B</param>
    public static double ColorDifference(this Color x, Color y)
    {
        var m = (x.R + y.R) / 2.0;
        var r = Math.Pow(x.R - y.R, 2);
        var g = Math.Pow(x.G - y.G, 2);
        var b = Math.Pow(x.B - y.B, 2);

        return Math.Sqrt((2 + m / 256) * r + 4 * g + (2 + (255 - m) / 256) * b);
    }
}
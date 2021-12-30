using System;
using System.Drawing;

namespace Bing.Conversions
{
    /// <summary>
    /// 颜色转换
    /// </summary>
    public static class ColorConv
    {
        /// <summary>
        /// RGB转HSB
        /// </summary>
        /// <param name="color">颜色</param>
        public static float[] RgbToHsb(Color color)
        {
            // 获取色相
            var hue = color.GetHue();
            // 获取饱和度
            var saturation = color.GetSaturation();
            // 获取亮度
            var brightness = color.GetBrightness();
            return new[] { hue, saturation, brightness };
        }

        /// <summary>
        /// HSV(HSB)转RGB
        /// </summary>
        /// <param name="hue">色相</param>
        /// <param name="saturation">改变图像饱和度。范围：0-1</param>
        /// <param name="value">改变图像对比度。范围：0-1</param>
        public static Color HsbToRgb(double hue, double saturation, double value)
        {
            if (saturation == 0)
            {
                var defaultColor = (int)(value * 255);
                return Color.FromArgb(defaultColor, defaultColor, defaultColor);
            }

            var max = value < 0.5 ? value * (1 + saturation) : value * (1 - saturation) + saturation;
            var min = value * 2 - max;

            hue /= 360;
            return Color.FromArgb(
                GetSingleChannelColor(min, max, hue + 1.0 / 3),
                GetSingleChannelColor(min, max, hue),
                GetSingleChannelColor(min, max, hue - 1.0 / 3));
        }

        /// <summary>
        /// 获取单通道颜色
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="hue">色相</param>
        private static int GetSingleChannelColor(double min, double max, double hue)
        {
            double singleColor;
            hue = (hue + 1) % 1;
            if (hue < 0)
                hue += 1;
            if (hue * 6 < 1)
                singleColor = min + (max - min) * 6 * hue;
            else if (hue * 2 < 1)
                singleColor = max;
            else if (hue * 3 < 2)
                singleColor = min + (max - min) * 6 * (2.0 / 3 - hue);
            else
                singleColor = min;
            return (int)(singleColor * 255);
        }

        /// <summary>
        /// 将SRgb颜色转换为线性RGB颜色
        /// </summary>
        /// <param name="sRgb">(单通道)颜色值。范围：0-1</param>
        /// <remarks>https://en.wikipedia.org/wiki/SRGB#The_forward_transformation_.28CIE_xyY_or_CIE_XYZ_to_sRGB.29</remarks>
        public static double SRgbToLinearRgb(double sRgb)
        {
            if (sRgb <= 0.04045)
                return sRgb / 12.92;
            return Math.Pow((sRgb + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// 将线性RGB颜色转换为SRgb颜色
        /// </summary>
        /// <param name="linearRgb">(单通道)颜色值。范围：0-1</param>
        public static double LinearRgbToSRgb(double linearRgb)
        {
            if (linearRgb < 0.0031308)
                return 12.92 * linearRgb;

            return Math.Pow(linearRgb, 1.0 / 2.4) * 1.055 - 0.055;
        }

        /// <summary>
        /// 转换为16进制颜色
        /// </summary>
        /// <param name="color">颜色</param>
        public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        /// <summary>
        /// 转换为RGB颜色
        /// </summary>
        /// <param name="color">颜色</param>
        public static string ToRgb(Color color) => $"RGB({color.R},{color.G},{color.B})";

        /// <summary>
        /// 将RGB颜色转换为16进制颜色
        /// </summary>
        /// <param name="r">红色</param>
        /// <param name="g">绿色</param>
        /// <param name="b">蓝色</param>
        public static string RgbToHex(int r, int g, int b) => ToHex(Color.FromArgb(r, g, b));
    }
}

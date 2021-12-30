using System;
using System.Drawing;

namespace Bing.Drawing
{
    /// <summary>
    /// 颜色效果
    /// </summary>
    public class ColorEffect
    {
        /// <summary>
        /// 将图片 <paramref name="bitmap"/> 上指定的颜色 <paramref name="colorA"/> 替换为颜色 <paramref name="colorB"/>
        /// </summary>
        /// <param name="bitmap">图片</param>
        /// <param name="colorA">要被替换的颜色</param>
        /// <param name="colorB">要将 <paramref name="colorA"/> 替换的颜色</param>
        public void ReplaceColor(Bitmap bitmap, Color colorA, Color colorB)
        {
            bitmap.PerPixelProcess(color =>
            {
                // 如果当前的颜色和颜色colorA近似，则进行替换
                var isSimilar = color.IsSimilarColors(colorA);
                return isSimilar ? colorB : color;
            });
        }

        /// <summary>
        /// 设置黑白图效果
        /// </summary>
        /// <param name="bitmap">图片</param>
        /// <param name="threshold">像素灰度大于该阈值设为白色，否则为黑色。范围：0-1</param>
        public void SetBlackWhiteEffect(Bitmap bitmap, float threshold)
        {
            bitmap.PerPixelProcess(color =>
            {
                // 如果当前的颜色灰度大于等于该阈值设为白色，否则为黑色
                var rgb = color.GetGrayScale() >= threshold ? Color.White : Color.Black;
                // 此处需要注意不能改变原始像素的Alpha值
                return Color.FromArgb(color.A, rgb);
            });
        }

        /// <summary>
        /// 对图片 <paramref name="bitmap"/> 设置(DuotoneEffect)双色调效果
        /// </summary>
        /// <param name="bitmap">图片</param>
        /// <param name="colorA">决定双色调效果的颜色A</param>
        /// <param name="colorB">决定双色调效果的颜色B</param>
        public void SetDuotoneEffect(Bitmap bitmap, Color colorA, Color colorB)
        {
            bitmap.PerPixelProcess(color => color.GetDuotoneColor(colorA, colorB));
        }

        /// <summary>
        /// 设置冲蚀效果
        /// </summary>
        /// <param name="bitmap">图片</param>
        /// <param name="brightness">改变图像亮度的百分比。范围：-100..100。</param>
        /// <param name="contrast">改变图像对比度的百分比。范围：-100..100。</param>
        public void SetErosionEffect(Bitmap bitmap, float brightness, float contrast)
        {
            // 1、修改图像对比度
            contrast = GetNearlyAmount(contrast);
            bitmap.SetContrast(contrast);

            // 2、修改图像亮度
            brightness = GetNearlyAmount(brightness) / 2;
            bitmap.SetBrightness(brightness);

            // 3、图片逐像素进行混色
            bitmap.PerPixelProcess(color => color.Blend(Color.White, 0.5));
        }

        /// <summary>
        /// 获取和PPT效果近似的转化比例（0~69内使用2次方，70~79内使用3次方，80~89内使用4次方，90~99内使用5次方）
        /// </summary>
        /// <param name="percentage">转化百分比</param>
        private float GetNearlyAmount(float percentage)
        {
            var amount = (percentage + 100) / 100;
            if (percentage > 0)
            {
                var x = (percentage - 60) / 10;
                var y = 2 + (x > 0 ? x : 0);
                amount = (float)Math.Pow(amount, y);
            }

            return amount;
        }
    }
}

﻿using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类
/// </summary>
public static partial class ImageHelper
{
    #region MakeThumbnail(生成缩略图)

    /// <summary>
    /// 生成缩略图
    /// </summary>
    /// <param name="sourceImage">源图</param>
    /// <param name="width">缩略图宽度</param>
    /// <param name="height">缩略图高度</param>
    /// <param name="mode">缩略图方式</param>
    public static Image MakeThumbnail(Image sourceImage, int width, int height, ThumbnailMode mode)
    {
        var towidth = width;
        var toheight = height;

        var x = 0;
        var y = 0;
        var ow = sourceImage.Width;
        var oh = sourceImage.Height;

        switch (mode)
        {
            case ThumbnailMode.FixedBoth:
                break;

            case ThumbnailMode.FixedW:
                toheight = oh * width / ow;
                break;

            case ThumbnailMode.FixedH:
                towidth = ow * height / oh;
                break;

            case ThumbnailMode.Cut:
                if (ow / (double)oh > towidth / (double)toheight)
                {
                    oh = sourceImage.Height;
                    ow = sourceImage.Height * towidth / toheight;
                    y = 0;
                    x = (sourceImage.Width - ow) / 2;
                }
                else
                {
                    ow = sourceImage.Width;
                    oh = sourceImage.Width * height / towidth;
                    x = 0;
                    y = (sourceImage.Height - oh) / 2;
                }
                break;
        }
        //1、新建一个BMP图片
        var bitmap = new Bitmap(towidth, toheight);
        //2、新建一个画板
        var g = Graphics.FromImage(bitmap);
        try
        {
            //3、设置高质量插值法
            g.InterpolationMode = InterpolationMode.High;
            //4、设置高质量，低速度呈现平滑程度
            g.SmoothingMode = SmoothingMode.HighQuality;
            //5、清空画布并以透明背景色填充
            g.Clear(Color.Transparent);
            //6、在指定位置并且按指定大小绘制原图片的指定部分
            g.DrawImage(sourceImage, new Rectangle(0, 0, towidth, toheight), new Rectangle(x, y, ow, oh),
                GraphicsUnit.Pixel);
            return bitmap;
        }
        catch (Exception e)
        {
            throw e;
        }
        finally
        {
            g.Dispose();
        }
    }

    /// <summary>
    /// 生成缩略图
    /// </summary>
    /// <param name="imgBytes">源文件字节数组</param>
    /// <param name="width">缩略图宽度</param>
    /// <param name="height">缩略图高度</param>
    /// <param name="mode">缩略图方式</param>
    public static Image MakeThumbnail(byte[] imgBytes, int width, int height, ThumbnailMode mode)
    {
        using (var sourceImage = FromBytes(imgBytes))
        {
            return MakeThumbnail(sourceImage, width, height, mode);
        }
    }

    /// <summary>
    /// 生成缩略图
    /// </summary>
    /// <param name="sourceImagePath">文件路径</param>
    /// <param name="thumbnailPath">缩略图文件生成路径</param>
    /// <param name="width">缩略图宽度</param>
    /// <param name="height">缩略图高度</param>
    /// <param name="mode">缩略图方式</param>
    public static void MakeThumbnail(string sourceImagePath, string thumbnailPath, int width, int height,
        ThumbnailMode mode)
    {
        using (var sourceImage = Image.FromFile(sourceImagePath))
        {
            using (var resultImage = MakeThumbnail(sourceImage, width, height, mode))
            {
                resultImage.Save(thumbnailPath, ImageFormat.Jpeg);
            }
        }
    }

    #endregion

    #region ScaleImage(缩放图像)

    /// <summary>
    /// 缩放图像，以使其适合宽度/高度
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public static Image ScaleImage(Image image, int width, int height)
    {
        if (image == null || width <= 0 || height <= 0)
            return null;
        var newWidth = image.Width * height / image.Height;
        var newHeight = image.Height * width / image.Width;

        var bmp = new Bitmap(width, height);
        var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        // 调试时，取消以下代码注释
        //g.FillRectangle(Brushes.Aqua, 0, 0, bmp.Width - 1, bmp.Height - 1);
        if (newWidth > width)
        {
            var x = (bmp.Width - width) / 2;
            var y = (bmp.Height - newHeight) / 2;
            g.DrawImage(image, x, y, width, newHeight);
        }
        else
        {
            var x = bmp.Width / 2 - newWidth / 2;
            var y = bmp.Height / 2 - height / 2;
            g.DrawImage(image, x, y, newWidth, height);
        }
        // 调试时，取消以下代码注释
        //g.DrawRectangle(new Pen(Color.Red, 1), 0, 0, bmp.Width - 1, bmp.Height - 1);
        return bmp;
    }

    #endregion

    #region TextWatermark(文字水印)

    //public static string TextWatermark(string path, string letter, int size, Color color, ImageLocationMode mode)
    //{
    //    if (string.IsNullOrWhiteSpace(path))
    //    {
    //        return string.Empty;
    //    }

    //    var extName = Path.GetExtension(path)?.ToLower();
    //    if (extName == ".jpg" || extName == ".bmp" || extName == ".jpeg")
    //    {
    //        var time = DateTime.Now;
    //        var fileName = time.ToString("yyyyMMddHHmmss.fff");
    //        var img = Image.FromFile(path);
    //        var g = Graphics.FromImage(img);
    //        var coors=GetLocation(mode,img,size)
    //    }
    //}

    ///// <summary>
    ///// 获取水印位置
    ///// </summary>
    ///// <param name="mode">水印位置</param>
    ///// <param name="img">图片</param>
    ///// <param name="width">宽度</param>
    ///// <param name="height">高度</param>
    ///// <returns></returns>
    //private static ArrayList GetLocation(ImageLocationMode mode, Image img, int width, int height)
    //{
    //    var coords = new ArrayList();
    //    var x = 0;
    //    var y = 0;

    //    switch (mode)
    //    {
    //        case ImageLocationMode.LeftTop:
    //            x = 10;
    //            y = 10;
    //            break;
    //        case ImageLocationMode.Top:
    //            x = img.Width / 2 - waterImg.Width / 2;
    //            y = img.Height - waterImg.Height;
    //            break;
    //        case ImageLocationMode.RightTop:
    //            x = img.Width - waterImg.Width;
    //            y = 10;
    //            break;
    //        case ImageLocationMode.LeftCenter:
    //            x = 10;
    //            y = img.Height / 2 - waterImg.Height / 2;
    //            break;
    //        case ImageLocationMode.Center:
    //            x = img.Width / 2 - waterImg.Width / 2;
    //            y = img.Height / 2 - waterImg.Height / 2;
    //            break;
    //        case ImageLocationMode.RightCenter:
    //            x = img.Width - waterImg.Width;
    //            y = img.Height / 2 - waterImg.Height / 2;
    //            break;
    //        case ImageLocationMode.LeftBottom:
    //            x = 10;
    //            y = img.Height - waterImg.Height;
    //            break;
    //        case ImageLocationMode.Bottom:
    //            x = img.Width / 2 - waterImg.Width / 2;
    //            y = img.Height - waterImg.Height;
    //            break;
    //        case ImageLocationMode.RightBottom:
    //            x = img.Width - waterImg.Width;
    //            y = img.Height - waterImg.Height;
    //            break;
    //    }
    //    coords.Add(x);
    //    coords.Add(y);
    //    return coords;
    //}

    #endregion

    #region DeleteCoordinate(删除图片中的经纬度信息)

    /// <summary>
    /// 删除图片中的经纬度信息，覆盖原图像
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void DeleteCoordinate(string filePath)
    {
        using (var ms = new MemoryStream(File.ReadAllBytes(filePath)))
        {
            using (var image = Image.FromStream(ms))
            {
                DeleteCoordinate(image);
                image.Save(filePath);
            }
        }
    }

    /// <summary>
    /// 删除图片中的经纬度信息，并另存为
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="savePath">保存文件路径</param>
    public static void DeleteCoordinate(string filePath, string savePath)
    {
        using (var ms = new MemoryStream(File.ReadAllBytes(filePath)))
        {
            using (var image = Image.FromStream(ms))
            {
                DeleteCoordinate(image);
                image.Save(savePath);
            }
        }
    }

    /// <summary>
    /// 删除图片中的经纬度信息
    /// </summary>
    /// <param name="image">图片</param>
    public static void DeleteCoordinate(Image image)
    {
        /*PropertyItem 中对应属性
         * ID	Property tag
           0x0000	PropertyTagGpsVer
           0x0001	PropertyTagGpsLatitudeRef
           0x0002	PropertyTagGpsLatitude
           0x0003	PropertyTagGpsLongitudeRef
           0x0004	PropertyTagGpsLongitude
           0x0005	PropertyTagGpsAltitudeRef
           0x0006	PropertyTagGpsAltitude
         */
        var ids = new[] { 0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006 };
        foreach (var id in ids)
        {
            if (image.PropertyIdList.Contains(id))
            {
                image.RemovePropertyItem(id);
            }
        }
    }

    #endregion

    #region BrightnessHandle(亮度处理)

    /// <summary>
    /// 亮度处理
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="val">增加或减少的光暗值</param>
    /// <returns></returns>
    public static Bitmap BrightnessHandle(Bitmap bitmap, int width, int height, int val)
    {
        Bitmap bm = new Bitmap(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pixel = bitmap.GetPixel(x, y);
                // 红绿蓝三值
                var resultR = pixel.R + val;
                var resultG = pixel.G + val;
                var resultB = pixel.B + val;
                bm.SetPixel(x, y, Color.FromArgb(resultR, resultG, resultB));
            }
        }
        return bm;
    }

    #endregion

    #region FilterColor(滤色处理)

    /// <summary>
    /// 滤色处理
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap FilterColor(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                bitmap.SetPixel(x, y, Color.FromArgb(0, pixel.G, pixel.B));
            }
        }
        return bitmap;
    }

    #endregion

    #region LeftRightTurn(左右翻转)

    /// <summary>
    /// 左右翻转
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap LeftRightTurn(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        for (var y = height - 1; y >= 0; y--)
        {
            for (int x = width - 1, z = 0; x >= 0; x--)
            {
                Color pixel = bitmap.GetPixel(x, y);
                bitmap.SetPixel(z++, y, Color.FromArgb(pixel.R, pixel.G, pixel.B));
            }
        }
        return bitmap;
    }

    #endregion

    #region TopBottomTurn(上下翻转)

    /// <summary>
    /// 上下翻转
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap TopBottomTurn(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        for (var x = 0; x < width; x++)
        {
            for (int y = height - 1, z = 0; y >= 0; y--)
            {
                Color pixel = bitmap.GetPixel(x, y);
                bitmap.SetPixel(x, z++, Color.FromArgb(pixel.R, pixel.G, pixel.B));
            }
        }
        return bitmap;
    }

    #endregion

    #region ToBlackWhiteImage(转换为黑白图片)

    /// <summary>
    /// 转换为黑白图片
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap ToBlackWhiteImage(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                int result = (pixel.R + pixel.G + pixel.B) / 3;
                bitmap.SetPixel(x, y, Color.FromArgb(result, result, result));
            }
        }
        return bitmap;
    }

    #endregion

    #region TwistImage(扭曲图片，滤镜效果)

    /// <summary>
    /// 正弦曲线Wave扭曲图片
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <param name="isTwist">是否扭曲，true:扭曲,false:不扭曲</param>
    /// <param name="shapeMultValue">波形的幅度倍数，越大扭曲的程度越高，默认为3</param>
    /// <param name="shapePhase">波形的起始相位，取值区间[0-2*PI]</param>
    /// <returns></returns>
    public static Bitmap TwistImage(Bitmap bitmap, bool isTwist, double shapeMultValue, double shapePhase)
    {
        Bitmap destBitmap = new Bitmap(bitmap.Width, bitmap.Height);
        // 将位图背景填充为白色
        Graphics g = Graphics.FromImage(destBitmap);
        g.FillRectangle(new SolidBrush(Color.White), 0, 0, destBitmap.Width, destBitmap.Height);
        g.Dispose();
        double dBaseAxisLen = isTwist ? (double)destBitmap.Height : (double)destBitmap.Width;
        for (var i = 0; i < destBitmap.Width; i++)
        {
            for (var j = 0; j < destBitmap.Height; j++)
            {
                double dx = 0;
                dx = isTwist
                    ? (2 * Math.PI * (double)j) / dBaseAxisLen
                    : (2 * Math.PI * (double)i) / dBaseAxisLen;
                dx += shapePhase;
                double dy = Math.Sin(dx);
                // 取当前点的颜色
                int nOldX = 0, nOldY = 0;
                nOldX = isTwist ? i + (int)(dy * shapeMultValue) : i;
                nOldY = isTwist ? j : j + (int)(dy * shapeMultValue);
                Color color = bitmap.GetPixel(i, j);
                if (nOldX >= 0 && nOldX <= destBitmap.Width && nOldY >= 0 && nOldY <= destBitmap.Height)
                {
                    destBitmap.SetPixel(nOldX, nOldY, color);
                }
            }
        }
        return destBitmap;
    }

    #endregion

    #region Rotate(图片旋转)

    /// <summary>
    /// 图片旋转，使图像绕中心点旋转一定角度
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <param name="angle">旋转的角度，正值为逆时针方向</param>
    /// <returns></returns>
    public static Bitmap Rotate(Bitmap bitmap, int angle)
    {
        angle = angle % 360;
        // 弧度转换
        double radian = angle * Math.PI / 180.0;
        double cos = Math.Cos(radian);
        double sin = Math.Sin(radian);
        // 原图的宽和高
        int w1 = bitmap.Width;
        int h1 = bitmap.Height;
        // 旋转后的宽和高
        int w2 = (int)(Math.Max(Math.Abs(w1 * cos - h1 * sin), Math.Abs(w1 * cos + h1 * sin)));
        int h2 = (int)(Math.Max(Math.Abs(w1 * sin - h1 * cos), Math.Abs(w1 * sin + h1 * cos)));
        // 目标位图
        Bitmap newBmp = new Bitmap(w2, h2);
        Graphics graphics = Graphics.FromImage(newBmp);
        graphics.InterpolationMode = InterpolationMode.Bilinear;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        // 计算偏移量
        Point offset = new Point((w2 - w1) / 2, (h2 - h1) / 2);
        // 构造图像显示区域：使原始图像与目标图像中心点一致
        Rectangle rect = new Rectangle(offset.X, offset.Y, w1, h1);
        Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        graphics.TranslateTransform(center.X, center.Y);
        graphics.RotateTransform(360 - angle);
        // 恢复图像在水平和垂直方向的平移
        graphics.TranslateTransform(-center.X, -center.Y);
        graphics.DrawImage(bitmap, rect);
        // 重置绘图的所有变换
        graphics.ResetTransform();
        graphics.Save();
        graphics.Dispose();
        return newBmp;
    }

    #endregion

    #region Gray(图片灰度化)

    /// <summary>
    /// 图片灰度化
    /// </summary>
    /// <param name="bitmap">图片</param>
    public static Bitmap Gray(Bitmap bitmap)
    {
        for (var i = 0; i < bitmap.Width; i++)
        {
            for (var j = 0; j < bitmap.Height; j++)
            {
                Color pixel = bitmap.GetPixel(i, j);
                byte r = pixel.R;
                byte g = pixel.G;
                byte b = pixel.B;

                // Gray = 0.299*R + 0.587*G + 0.114*B 灰度计算公式
                if (r + b + g != 0)
                {
                    byte gray = (byte)((r * 19595 + g * 38469 + b * 7472) >> 16);
                    bitmap.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                }
                else
                {
                    bitmap.SetPixel(i, j, Color.White);
                }
            }
        }
        return bitmap;
    }

    #endregion

    #region Plate(底片效果)

    /// <summary>
    /// 底片效果
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap Plate(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        for (var j = 0; j < height; j++)
        {
            for (var i = 0; i < width; i++)
            {
                Color pixel = bitmap.GetPixel(i, j);
                int r = 255 - pixel.R;
                int g = 255 - pixel.G;
                int b = 255 - pixel.B;
                bitmap.SetPixel(i, j, Color.FromArgb(r, g, b));
            }
        }
        return bitmap;
    }

    #endregion

    #region Emboss(浮雕效果)

    /// <summary>
    /// 浮雕效果
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap Emboss(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        for (var j = 0; j < height; j++)
        {
            for (var i = 0; i < width; i++)
            {
                Color pixel1 = bitmap.GetPixel(i, j);
                Color pixel2 = bitmap.GetPixel(i + 1, j + 1);
                int r = Math.Abs(pixel1.R - pixel2.R + 128);
                int g = Math.Abs(pixel1.G - pixel2.G + 128);
                int b = Math.Abs(pixel1.B - pixel2.B + 128);
                r = r > 255 ? 255 : r;
                r = r < 0 ? 0 : r;
                g = g > 255 ? 255 : g;
                g = g < 0 ? 0 : g;
                b = b > 255 ? 255 : b;
                b = b < 0 ? 0 : b;
                bitmap.SetPixel(i, j, Color.FromArgb(r, g, b));
            }
        }
        return bitmap;
    }

    #endregion

    #region Soften(柔化效果)

    /// <summary>
    /// 柔化效果
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap Soften(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        //高斯模板
        int[] gauss = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = 0;
                int r = 0, g = 0, b = 0;
                for (int col = -1; col <= 1; col++)
                {
                    for (int row = -1; row <= 1; row++)
                    {
                        Color pixel = bitmap.GetPixel(i + row, j + col);
                        r += pixel.R * gauss[index];
                        g += pixel.G * gauss[index];
                        b += pixel.B * gauss[index];
                        index++;
                    }
                }
                r /= 16;
                g /= 16;
                b /= 16;
                // 处理颜色值溢出
                r = r > 255 ? 255 : r;
                r = r < 0 ? 0 : r;
                g = g > 255 ? 255 : g;
                g = g < 0 ? 0 : g;
                b = b > 255 ? 255 : b;
                b = b < 0 ? 0 : b;
                bitmap.SetPixel(i - 1, j - 1, Color.FromArgb(r, g, b));
            }
        }
        return bitmap;
    }

    #endregion

    #region Sharpen(锐化效果)

    /// <summary>
    /// 锐化效果
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap Sharpen(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        // 拉普拉斯模板
        int[] laplacian = { -1, -1, -1, -1, 9, -1, -1, -1, -1 };
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = 0;
                int r = 0, g = 0, b = 0;
                for (int col = -1; col <= 1; col++)
                {
                    for (int row = -1; row <= 1; row++)
                    {
                        Color pixel = bitmap.GetPixel(i + row, j + col);
                        r += pixel.R * laplacian[index];
                        g += pixel.G * laplacian[index];
                        b += pixel.B * laplacian[index];
                        index++;
                    }
                }
                r /= 16;
                g /= 16;
                b /= 16;
                // 处理颜色值溢出
                r = r > 255 ? 255 : r;
                r = r < 0 ? 0 : r;
                g = g > 255 ? 255 : g;
                g = g < 0 ? 0 : g;
                b = b > 255 ? 255 : b;
                b = b < 0 ? 0 : b;
                bitmap.SetPixel(i - 1, j - 1, Color.FromArgb(r, g, b));
            }
        }
        return bitmap;
    }

    #endregion

    #region Atomizing(雾化效果)

    /// <summary>
    /// 雾化效果
    /// </summary>
    /// <param name="bitmap">图片</param>
    /// <returns></returns>
    public static Bitmap Atomizing(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                Random rnd = new Random();
                int k = rnd.Next(123456);
                // 像素块大小
                int dx = i + k % 19;
                int dy = j + k % 19;
                if (dx >= width)
                {
                    dx = width - 1;
                }
                if (dy >= height)
                {
                    dy = height - 1;
                }
                Color pixel = bitmap.GetPixel(dx, dy);
                bitmap.SetPixel(i, j, pixel);
            }
        }
        return bitmap;
    }

    #endregion
}
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Bing.Drawing
{
    /// <summary>
    /// 图片操作辅助类 - 转换
    /// </summary>
    public static partial class ImageHelper
    {
        #region ToBytes(将图像转换为字节数组)

        /// <summary>
        /// 将图像转换为字节数组
        /// </summary>
        /// <param name="bitmap">图像</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[] ToBytes(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            using (var newBitmap = new Bitmap(bitmap))
            {
                using (var ms = new MemoryStream())
                {
                    var format = newBitmap.RawFormat;
                    if (ImageFormat.MemoryBmp.Equals(format))
                        format = ImageFormat.Bmp;
                    newBitmap.Save(ms, format);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// 将图像转换成字节数组
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="format">图像格式</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[] ToBytes(Image image, ImageFormat format)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            using var ms = new MemoryStream();
            image.Save(ms, format);
            return ms.ToArray();
        }

        #endregion

        #region ToStream(转换为内存流)

        /// <summary>
        /// 将图片转换为内存流，需要释放资源
        /// </summary>
        /// <param name="image">图片</param>
        public static Stream ToStream(Image image)
        {
            var ms = new MemoryStream();
            image.Save(ms, image.RawFormat);
            return ms;
        }

        /// <summary>
        /// 将图像转换为内存流，需要释放资源
        /// </summary>
        /// <param name="bitmap">图像</param>
        public static Stream ToStream(Bitmap bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, bitmap.RawFormat);
            return ms;
        }

        #endregion

        #region ToBase64String(转换为Base64字符串)

        /// <summary>
        /// 将图像转换为base64字符串
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="appendPrefix">是否追加前缀</param>
        public static string ToBase64String(Image image, bool appendPrefix = false) => ToBase64String(image, image.RawFormat, appendPrefix);

        /// <summary>
        /// 将图像转换为base64字符串
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="imageFormat">图像格式</param>
        /// <param name="appendPrefix">是否追加前缀</param>
        public static string ToBase64String(Image image, ImageFormat imageFormat, bool appendPrefix = false)
        {
            using var ms = new MemoryStream();
            image.Save(ms, imageFormat);
            var bytes = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(bytes, 0, (int)ms.Length);
            var result = Convert.ToBase64String(bytes);
            if (appendPrefix)
                result = $"data:image/{imageFormat.ToString().ToLower()};base64,{result}";
            return result;
        }

        /// <summary>
        /// 将图像转换为Base64字符串
        /// </summary>
        /// <param name="bitmap">图像</param>
        /// <param name="appendPrefix">是否追加前缀</param>
        public static string ToBase64String(Bitmap bitmap, bool appendPrefix = false) => ToBase64String(bitmap, bitmap.RawFormat, appendPrefix);

        /// <summary>
        /// 将图像转换为Base64字符串
        /// </summary>
        /// <param name="bitmap">图像</param>
        /// <param name="imageFormat">图片格式</param>
        /// <param name="appendPrefix">是否追加前缀</param>
        public static string ToBase64String(Bitmap bitmap, ImageFormat imageFormat, bool appendPrefix = false)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, imageFormat);
            var result = Convert.ToBase64String(ms.ToArray());
            if (appendPrefix)
                result = $"data:image/{imageFormat.ToString().ToLower()};base64,{result}";
            return result;
        }

        #endregion

        #region ToIcoStream(将图像转换为ICO流)

        /// <summary>
        /// PNG-ICON 文件头
        /// </summary>
        // ReSharper disable once IdentifierTypo
        private static readonly byte[] Pngiconheader = { 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 24, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// 将图像转换为ICO流
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="size">大小</param>
        public static MemoryStream ToIcoStream(Image image, Size size)
        {
            using var bmp = new Bitmap(image, size);
            byte[] png;
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                png = ms.ToArray();
            }

            var outMs = new MemoryStream();
            Pngiconheader[6] = (byte)size.Width;
            Pngiconheader[7] = (byte)size.Height;
            Pngiconheader[14] = (byte)(png.Length & 255);
            Pngiconheader[15] = (byte)(png.Length / 256);
            Pngiconheader[18] = (byte)(Pngiconheader.Length);

            outMs.Write(Pngiconheader, 0, Pngiconheader.Length);
            outMs.Write(png, 0, png.Length);
            outMs.Position = 0;
            return outMs;
        }

        #endregion
    }
}

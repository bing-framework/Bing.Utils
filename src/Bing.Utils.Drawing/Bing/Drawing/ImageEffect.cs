using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Bing.Drawing
{
    /// <summary>
    /// 图片效果
    /// </summary>
    public class ImageEffect
    {
        /// <summary>
        /// 根据原始图片 <paramref name="bitmap"/> 创建带有柔化边缘的图片
        /// </summary>
        /// <param name="bitmap">源图片</param>
        /// <param name="radius">柔化半径。单位：像素</param>
        public Bitmap CreateSoftEdgeBitmap(Bitmap bitmap, float radius)
        {
            var cols = bitmap.Width;
            var rows = bitmap.Height;

            // 克隆一个32位ARgb的图片，用于读取Alpha通道
            var image = bitmap.Clone(new Rectangle(0, 0, cols, rows), PixelFormat.Format32bppArgb);
            SetSoftEdgeEffect(image, radius);
            return image;
        }

        /// <summary>
        /// 为原始图片 <paramref name="bitmap"/> 设置柔化边缘的效果
        /// </summary>
        /// <param name="bitmap">源图片（必须是32位带Alpha通道的图片）</param>
        /// <param name="radius">柔化半径。单位：像素</param>
        public void SetSoftEdgeEffect(Bitmap bitmap, float radius)
        {
            var pixelFormat = bitmap.PixelFormat;
            if (pixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException($"Unsupported image pixel format {nameof(pixelFormat)} is used.");

            // 锁定图片并拷贝图片像素
            var cols = bitmap.Width;
            var rows = bitmap.Height;
            var rect = new Rectangle(0, 0, cols, rows);
            var channels = Image.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
            var total = cols * rows * channels;
            var data = new byte[total];
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var iPtr = bitmapData.Scan0;
            Marshal.Copy(iPtr, data, 0, total);

            // 通过算法设置柔化边缘效果
            SetSoftEdgeEffect(data, cols, rows, channels, radius);

            Marshal.Copy(data, 0, iPtr, total);
            bitmap.UnlockBits(bitmapData);
        }

        /// <summary>
        /// 设置柔化边缘效果
        /// </summary>
        /// <param name="data">图像原始数据</param>
        /// <param name="cols">图像宽度</param>
        /// <param name="rows">图像高度</param>
        /// <param name="channels">图像通道数</param>
        /// <param name="radius">柔化半径。单位：像素</param>
        private void SetSoftEdgeEffect(byte[] data, int cols, int rows, int channels, float radius)
        {
            // 创建并提供Alpha蒙层来进行腐蚀和模糊
            var mask = CreateSoftEdgeAlphaMask(data, cols, rows, channels);

            var offsetX = (int)Math.Round(radius / 4.0);
            var offsetY = (int)Math.Round(radius / 4.0);
            var size = new Size(offsetX, offsetY);

            // 腐蚀操作
            mask = AlphaErode(mask, size, 3);
            // 模糊操作
            mask = AlphaBlur(mask, size, 3);

            // 应用Alpha蒙层数据
            ApplySoftEdgeAlphaMask(data, mask, cols, rows, channels);
        }

        /// <summary>
        /// 创建Alpha通道蒙层数据
        /// </summary>
        /// <param name="data">图像原始数据</param>
        /// <param name="cols">图像宽度</param>
        /// <param name="rows">图像高度</param>
        /// <param name="channels">图像通道数</param>
        private byte[,] CreateSoftEdgeAlphaMask(byte[] data, int cols, int rows, int channels)
        {
            // 根据宽高设置一个蒙层数组
            var masks = new byte[cols, rows];

            // 需要考虑大小端
            var isLittleEndian = BitConverter.IsLittleEndian;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var indexOffset = (row * cols + col) * channels;
                    var alpha = isLittleEndian ? data[indexOffset + 3] : data[indexOffset + 0];
                    masks[col, row] = alpha == 0 ? (byte)0 : byte.MaxValue;
                }
            }

            return masks;
        }

        /// <summary>
        /// 应用Alpha通道蒙层数据
        /// </summary>
        /// <param name="data">图像原始数据</param>
        /// <param name="mask">图像Alpha蒙层</param>
        /// <param name="cols">图像宽度</param>
        /// <param name="rows">图像高度</param>
        /// <param name="channels">图像通道数</param>
        private void ApplySoftEdgeAlphaMask(byte[] data, byte[,] mask, int cols, int rows, int channels)
        {
            // 需要考虑大小端
            var isLittleEndian = BitConverter.IsLittleEndian;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var indexOffset = (row * cols + col) * channels;
                    var index = isLittleEndian ? indexOffset + 3 : indexOffset;

                    // 根据蒙层设置Alpha
                    var alpha = (byte)(mask[col, row] / 255.0d * data[index]);
                    data[index] = alpha;
                }
            }
        }

        /// <summary>
        /// 对Alpha蒙层进行腐蚀操作
        /// </summary>
        /// <param name="sourceMask">输入蒙层数据</param>
        /// <param name="size">腐蚀操作卷积核大小</param>
        /// <param name="iteration">连续腐蚀次数</param>
        /// <returns>输出蒙层数据</returns>
        private byte[,] AlphaErode(byte[,] sourceMask, Size size, uint iteration)
        {
            var offsetX = size.Width;
            var offsetY = size.Height;

            var cols = sourceMask.GetLength(0);
            var rows = sourceMask.GetLength(1);
            var erodeMask = new byte[cols, rows];

            for (var i = 0; i < iteration; i++)
            {
                var target = new byte[cols, rows];
                var mask = sourceMask;

                // 下面的卷积操作会尽可能减少不必要的运算过程
                Parallel.For(offsetY, rows - offsetY, row =>
                {
                    var isNeedInitialize = true;
                    var blackPointCols = new List<int>();

                    for (var col = offsetX; col < cols - offsetX; col++)
                    {
                        var minCol = col - offsetX;
                        var maxCol = col + offsetX;
                        var minRow = row - offsetY;
                        var maxRow = row + offsetY;

                        if (isNeedInitialize)
                        {
                            for (var x = minCol; x <= maxCol; x++)
                            {
                                for (var y = minRow; y < maxRow; y++)
                                {
                                    if (mask[x, y] == 0)
                                    {
                                        blackPointCols.Add(x);
                                        break;
                                    }
                                }
                            }

                            isNeedInitialize = false;
                        }
                        else
                        {
                            blackPointCols.Remove(minCol - 1);
                            for (var y = minRow; y < maxRow; y++)
                            {
                                if (mask[maxCol, y] == 0)
                                {
                                    blackPointCols.Add(maxCol);
                                    break;
                                }
                            }
                        }

                        if (blackPointCols.Count == 0)
                        {
                            target[col, row] = byte.MaxValue;
                        }
                    }
                });

                sourceMask = target;
                erodeMask = target;
            }

            return erodeMask;
        }

        /// <summary>
        /// 对Alpha蒙层进行模糊操作（使用归一化框过滤器模糊图像，是一种简单的模糊函数，是计算每个像素中对应的平均值）
        /// </summary>
        /// <param name="sourceMask">输入蒙层数据</param>
        /// <param name="size">模糊操作卷积核大小</param>
        /// <param name="iteration">连续模糊次数</param>
        /// <returns>输出蒙层数据</returns>
        private byte[,] AlphaBlur(byte[,] sourceMask, Size size, uint iteration)
        {
            var offsetX = size.Width;
            var offsetY = size.Height;

            var cols = sourceMask.GetLength(0);
            var rows = sourceMask.GetLength(1);
            var blurMask = new byte[cols, rows];

            for (var i = 0; i < iteration; i++)
            {
                var target = new byte[cols, rows];
                var mask = sourceMask;

                // 下面的卷积操作会尽可能减少不必要的运算过程
                Parallel.For(0, rows, row =>
                {
                    var isNeedInitialize = true;
                    var valueCache = new Dictionary<int, int>();

                    for (var col = 0; col < cols; col++)
                    {
                        var minCol = col - offsetX;
                        var maxCol = col + offsetX;
                        var minRow = row - offsetY;
                        var maxRow = row + offsetY;

                        var count = (offsetX * 2 + 1) * (offsetY * 2 + 1);
                        if (count == 0) count = 1;

                        if (isNeedInitialize)
                        {
                            for (var x = minCol; x <= maxCol; x++)
                            {
                                var value = 0;
                                if (x > 0 && x < cols)
                                {
                                    for (var y = minRow; y < maxRow; y++)
                                    {
                                        if (y > 0 && y < rows)
                                        {
                                            value += mask[x, y];
                                        }
                                    }
                                }

                                valueCache.Add(x, value);
                            }

                            isNeedInitialize = false;
                        }
                        else
                        {
                            var value = 0;
                            valueCache.Remove(minCol - 1);
                            if (maxCol > 0 && maxCol < cols)
                            {
                                for (var y = minRow; y < maxRow; y++)
                                {
                                    if (y > 0 && y < rows)
                                    {
                                        value += mask[maxCol, y];
                                    }
                                }
                            }

                            valueCache.Add(maxCol, value);
                        }

                        var targetValue = valueCache.Values.Sum() / (double)count;
                        target[col, row] = (byte)Math.Round(targetValue);
                    }
                });

                sourceMask = target;
                blurMask = target;
            }

            return blurMask;
        }
    }
}

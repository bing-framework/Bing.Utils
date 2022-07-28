using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bing.Collections
{
    /// <summary>
    /// 数组复制选项
    /// </summary>
    public enum ArrayCopyOptions
    {
        /// <summary>
        /// 根据长度
        /// </summary>
        Length = 0,

        /// <summary>
        /// 根据起始的索引值
        /// </summary>
        SinceIndex = 1
    }

    /// <summary>
    /// 数组 操作
    /// </summary>
    public static partial class Arrays
    {
        /// <summary>
        /// 克隆一个一维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[] Copy<T>(T[] bytes)
        {
            var newBytes = new T[bytes.Length];
            Array.Copy(bytes, newBytes, bytes.Length);
            return newBytes;
        }

        /// <summary>
        /// 克隆一个二维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,] Copy<T>(T[,] bytes)
        {
            int width = bytes.GetLength(0),
                height = bytes.GetLength(1);
            var newBytes = new T[width, height];
            Array.Copy(bytes, newBytes, bytes.Length);
            return newBytes;
        }

        /// <summary>
        /// 克隆一个三维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,] Copy<T>(T[,,] bytes)
        {
            int x = bytes.GetLength(0),
                y = bytes.GetLength(1),
                z = bytes.GetLength(2);
            var newBytes = new T[x, y, z];
            Array.Copy(bytes, newBytes, bytes.Length);
            return newBytes;
        }

        /// <summary>
        /// 克隆一个四维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,] Copy<T>(T[,,,] bytes)
        {
            int x = bytes.GetLength(0),
                y = bytes.GetLength(1),
                z = bytes.GetLength(2),
                m = bytes.GetLength(3);
            var newBytes = new T[x, y, z, m];
            Array.Copy(bytes, newBytes, bytes.Length);
            return newBytes;
        }

        /// <summary>
        /// 克隆一个五维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,,] Copy<T>(T[,,,,] bytes)
        {
            int x = bytes.GetLength(0),
                y = bytes.GetLength(1),
                z = bytes.GetLength(2),
                m = bytes.GetLength(3),
                n = bytes.GetLength(4);
            var newBytes = new T[x, y, z, m, n];
            Array.Copy(bytes, newBytes, bytes.Length);
            return newBytes;
        }

        /// <summary>
        /// 克隆一个六维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,,,] Copy<T>(T[,,,,,] bytes)
        {
            int x = bytes.GetLength(0),
                y = bytes.GetLength(1),
                z = bytes.GetLength(2),
                m = bytes.GetLength(3),
                n = bytes.GetLength(4),
                o = bytes.GetLength(5);
            var newBytes = new T[x, y, z, m, n, o];
            Array.Copy(bytes, newBytes, bytes.Length);
            return newBytes;
        }

        /// <summary>
        /// 克隆一个七维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,,,,] Copy<T>(T[,,,,,,] bytes)
        {
            int x = bytes.GetLength(0),
                y = bytes.GetLength(1),
                z = bytes.GetLength(2),
                m = bytes.GetLength(3),
                n = bytes.GetLength(4),
                o = bytes.GetLength(5),
                p = bytes.GetLength(6);
            var newBytes = new T[x, y, z, m, n, o, p];
            Array.Copy(bytes, newBytes, bytes.Length);
            return newBytes;
        }
    }

    /// <summary>
    /// 数组扩展
    /// </summary>
    public static partial class ArraysExtensions
    {
        /// <summary>
        /// 克隆一个一维数组。<br />
        /// 从指定的源索引开始从 <see cref="T:System.Array" /> 克隆一维数组并将它们粘贴到另一个 <see cref="T:System.Array" /> 从指定的源索引开始 目的地索引。 长度和索引指定为 32 位整数。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        /// <param name="numericVal">数值。根据 <see cref="ArrayCopyOptions"/> 决定数值的使用方式</param>
        /// <param name="options">数组复制选项。默认值：<see cref="ArrayCopyOptions.Length"/></param>
        public static T[] Copy<T>(this T[] bytes, int numericVal, ArrayCopyOptions options = ArrayCopyOptions.Length)
        {
            var arrayLength = options switch
            {
                ArrayCopyOptions.Length => numericVal,
                ArrayCopyOptions.SinceIndex => bytes.Length - numericVal,
                _ => numericVal
            };
            if (arrayLength <= 0)
                return Array.Empty<T>();

            var array = new T[arrayLength];
            switch (options)
            {
                case ArrayCopyOptions.Length:
                    Array.Copy(bytes, array, numericVal);
                    break;
                case ArrayCopyOptions.SinceIndex:
                    Array.Copy(bytes, numericVal, array, 0, arrayLength);
                    break;
                default:
                    Array.Copy(bytes, array, numericVal);
                    break;
            }

            return array;
        }

        /// <summary>
        /// 克隆一个一维数组。<br />
        /// 从指定的源索引开始从 <see cref="T:System.Array" /> 克隆一维数组并将它们粘贴到另一个 <see cref="T:System.Array" /> 从指定的源索引开始 目的地索引。 长度和索引指定为 64 位整数。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        /// <param name="numericVal">数值。根据 <see cref="ArrayCopyOptions"/> 决定数值的使用方式</param>
        /// <param name="options">数组复制选项。默认值：<see cref="ArrayCopyOptions.Length"/></param>
        public static T[] Copy<T>(this T[] bytes, long numericVal, ArrayCopyOptions options = ArrayCopyOptions.Length)
        {
            var arrayLength = options switch
            {
                ArrayCopyOptions.Length => numericVal,
                ArrayCopyOptions.SinceIndex => bytes.Length - numericVal,
                _ => numericVal
            };
            if (arrayLength <= 0)
                return Array.Empty<T>();

            var array = new T[arrayLength];
            switch (options)
            {
                case ArrayCopyOptions.Length:
                    Array.Copy(bytes, array, numericVal);
                    break;
                case ArrayCopyOptions.SinceIndex:
                    Array.Copy(bytes, numericVal, array, 0, arrayLength);
                    break;
                default:
                    Array.Copy(bytes, array, numericVal);
                    break;
            }

            return array;
        }

        /// <summary>
        /// 克隆一个一维数组。<br />
        /// 从指定的源索引开始从 <see cref="T:System.Array" /> 克隆一维数组。 如果复制没有完全成功，则保证所有更改都将被撤消。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        /// <param name="sourceIndex">源索引</param>
        /// <param name="length">长度</param>
        public static T[] Copy<T>(this T[] bytes, int sourceIndex, int length)
        {
            var array = (bytes.Length - sourceIndex - length) switch
            {
                <= 0 => new T[bytes.Length - sourceIndex],
                > 0 => new T[length]
            };

            Array.Copy(bytes, sourceIndex, array, 0, length);

            return array;
        }

        /// <summary>
        /// 克隆一个一维数组。<br />
        /// 从指定的源索引开始从 <see cref="T:System.Array" /> 克隆一维数组。 如果复制没有完全成功，则保证所有更改都将被撤消。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        /// <param name="sourceIndex">源索引</param>
        /// <param name="length">长度</param>
        public static T[] Copy<T>(this T[] bytes, long sourceIndex, long length)
        {
            var array = (bytes.Length - sourceIndex - length) switch
            {
                <= 0 => new T[bytes.Length - sourceIndex],
                > 0 => new T[length]
            };

            Array.Copy(bytes, sourceIndex, array, 0, length);

            return array;
        }

        /// <summary>
        /// 克隆一个一维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[] Copy<T>(this T[] bytes) => Arrays.Copy(bytes);

        /// <summary>
        /// 克隆一个二维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,] Copy<T>(this T[,] bytes) => Arrays.Copy(bytes);

        /// <summary>
        /// 克隆一个三维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,] Copy<T>(this T[,,] bytes) => Arrays.Copy(bytes);

        /// <summary>
        /// 克隆一个四维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,] Copy<T>(this T[,,,] bytes) => Arrays.Copy(bytes);

        /// <summary>
        /// 克隆一个五维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,,] Copy<T>(this T[,,,,] bytes) => Arrays.Copy(bytes);

        /// <summary>
        /// 克隆一个六维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,,,] Copy<T>(this T[,,,,,] bytes) => Arrays.Copy(bytes);

        /// <summary>
        /// 克隆一个七维数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="bytes">数组</param>
        public static T[,,,,,,] Copy<T>(this T[,,,,,,] bytes) => Arrays.Copy(bytes);
    }

    /// <summary>
    /// 数组捷径扩展
    /// </summary>
    public static partial class ArraysShortcutExtensions
    {
        /// <summary>
        /// 克隆。<br />
        /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
        /// </summary>
        /// <param name="sourceArray">源数组</param>
        /// <param name="destinationArray">目标数组</param>
        /// <param name="length">长度</param>
        public static void Copy(this Array sourceArray, Array destinationArray, int length) => Array.Copy(sourceArray, destinationArray, length);

        /// <summary>
        /// 克隆。<br />
        /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
        /// </summary>
        /// <param name="sourceArray">源数组</param>
        /// <param name="sourceIndex">源数组索引</param>
        /// <param name="destinationArray">目标数组</param>
        /// <param name="destinationIndex">目标数组索引</param>
        /// <param name="length">长度</param>
        public static void Copy(this Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length) => Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);

        /// <summary>
        /// 克隆。<br />
        /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
        /// </summary>
        /// <param name="sourceArray">源数组</param>
        /// <param name="destinationArray">目标数组</param>
        /// <param name="length">长度</param>
        public static void Copy(this Array sourceArray, Array destinationArray, long length) => Array.Copy(sourceArray, destinationArray, length);

        /// <summary>
        /// 克隆。<br />
        /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
        /// </summary>
        /// <param name="sourceArray">源数组</param>
        /// <param name="sourceIndex">源数组索引</param>
        /// <param name="destinationArray">目标数组</param>
        /// <param name="destinationIndex">目标数组索引</param>
        /// <param name="length">长度</param>
        public static void Copy(this Array sourceArray, long sourceIndex, Array destinationArray, long destinationIndex, long length) => Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);

        /// <summary>
        /// 克隆。<br />
        /// 从指定源索引开始的 <see cref="T:System.Array" /> 复制一系列元素，并将它们粘贴到另一个 <see cref="T:System.Array" /> 从指定目标索引开始 . 如果复制没有完全成功，则保证所有更改都将被撤消。
        /// </summary>
        /// <param name="sourceArray">源数组</param>
        /// <param name="sourceIndex">源数组索引</param>
        /// <param name="destinationArray">目标数组</param>
        /// <param name="destinationIndex">目标数组索引</param>
        /// <param name="length">长度</param>
        public static void ConstrainedCopy(this Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length) => Array.ConstrainedCopy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);

        /// <summary>
        /// 块复制。<br />
        /// 将指定数量的字节从以特定偏移量开始的源数组复制到以特定偏移量开始的目标数组。
        /// </summary>
        /// <param name="src">源数组</param>
        /// <param name="srcOffset">源数组偏移量</param>
        /// <param name="dst">目标数组</param>
        /// <param name="dstOffset">目标数组偏移量</param>
        /// <param name="count">计数</param>
        public static void BlockCopy(this Array src, int srcOffset, Array dst, int dstOffset, int count) => Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);

    }
}

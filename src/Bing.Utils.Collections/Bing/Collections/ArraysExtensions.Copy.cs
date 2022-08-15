using System;

namespace Bing.Collections
{
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
}
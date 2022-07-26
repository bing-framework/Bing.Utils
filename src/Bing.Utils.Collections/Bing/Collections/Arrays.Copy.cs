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
    }
}

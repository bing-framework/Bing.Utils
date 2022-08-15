using System;

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
}

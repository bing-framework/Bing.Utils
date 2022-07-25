using System;
using System.Runtime.CompilerServices;

namespace Bing.Collections
{
    /// <summary>
    /// 数组 操作
    /// </summary>
    public static partial class Arrays
    {
        /// <summary>
        /// 空数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Empty<T>() => InternalArray.ForEmpty<T>();
    }
}
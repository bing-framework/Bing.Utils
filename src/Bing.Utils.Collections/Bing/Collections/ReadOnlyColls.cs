using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Bing.Collections.Internals;

namespace Bing.Collections
{
    /// <summary>
    /// 只读集合帮助类
    /// </summary>
    internal static class ReadOnlyCollsHelper
    {
        /// <summary>
        /// 包装成只读集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ReadOnlyCollection<T> WrapInReadOnlyCollection<T>(IList<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return new ReadOnlyCollection<T>(source);
        }

        /// <summary>
        /// 附加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="items">附加项</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Append<T>(IEnumerable<T> source, params T[] items)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return items is null ? source : source.Concat(items);
        }
    }

    /// <summary>
    /// 只读集合工具
    /// </summary>
    public static partial class ReadOnlyColls
    {
        /// <summary>
        /// 附加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="item">附加项</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IReadOnlyCollection<T> Append<T>(IReadOnlyCollection<T> source, T item)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return new AppendedReadOnlyCollection<T>(source, item);
        }

        /// <summary>
        /// 获取一个空的只读集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        public static ReadOnlyCollection<T> Empty<T>() => EmptyReadOnlyCollectionSingleton<T>.Instance;

        /// <summary>
        /// 空的只读集合单例
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        private static class EmptyReadOnlyCollectionSingleton<T>
        {
            /// <summary>
            /// 实例
            /// </summary>
            internal static readonly ReadOnlyCollection<T> Instance = new(Array.Empty<T>());
        }

        //public static IReadOnlyList<T> OfList<T>(params T[] @params)
        //{
        //}

        //public static IReadOnlyList<T> OfList<T>(params IEnumerable<T>[] listParams)
        //{

        //}

        //public static IReadOnlyList<T> OfList<T>(IEnumerable<T> list, params IEnumerable<T>[] listParams)
        //{

        //}
    }

    public static class ReadOnlyCollsExtensions
    {
        /// <summary>
        /// 附加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="item">附加项</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyCollection<T> Append<T>(this IReadOnlyCollection<T> source, T item) => ReadOnlyColls.Append(source, item);
    }
}
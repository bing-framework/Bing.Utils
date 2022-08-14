using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        /// 追加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="items">追加项</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Append<T>(IEnumerable<T> source, params T[] items)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return items is null ? source : source.Concat(items);
        }
    }

    public class ReadOnlyColls
    {

    }
}
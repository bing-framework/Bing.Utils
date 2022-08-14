using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Bing.Collections.Internals;

namespace Bing.Collections
{
    /// <summary>
    /// 集合转换器
    /// </summary>
    public static class CollConv
    {
        #region ToEnumerable

        /// <summary>
        /// 转换为 <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="enumerator">集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> ToEnumerable<T>(IEnumerator<T> enumerator)
        {
            if (enumerator is null)
                throw new ArgumentNullException(nameof(enumerator));

            IEnumerable<T> Implementation()
            {
                while (enumerator.MoveNext())
                    yield return enumerator.Current;
            }

            return Implementation();
        }

        /// <summary>
        /// 转换为 <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="enumerator">集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> ToEnumerableAfter<T>(IEnumerator<T> enumerator)
        {
            if (enumerator is null)
                throw new ArgumentNullException(nameof(enumerator));

            IEnumerable<T> Implementation()
            {
                do
                {
                    yield return enumerator.Current;
                } while (enumerator.MoveNext());
            }

            return Implementation();
        }

        #endregion

        #region ToIndexedSequence

        /// <summary>
        /// 转换为具有索引的序列
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<KeyValuePair<int, T>> ToIndexedSequence<T>(IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return source.Select((t, i) => new KeyValuePair<int, T>(i, t));
        }

        #endregion

        #region ToSortedArray

        /// <summary>
        /// 转换为有序数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="comparer">比较器</param>
        public static T[] ToSortedArray<T>(IEnumerable<T> source, Comparison<T> comparer)
        {
            var res = source.ToArray();
            Array.Sort(res, comparer);
            return res;
        }

        /// <summary>
        /// 转换为有序数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        public static T[] ToSortedArray<T>(IEnumerable<T> source) where T : IComparable<T>
        {
            var res = source.ToArray();
            Array.Sort(res);
            return res;
        }

        #endregion

        #region AsOptionals

        /// <summary>
        /// 转换为可空成员集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T?> AsOptionals<T>(IEnumerable<T> source) where T : struct
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return source.Cast<T?>();
        }

        #endregion

        #region AsProxy

        /// <summary>
        /// 转换为 <see cref="EnumerableProxy{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="enumerable">集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static EnumerableProxy<T> AsEnumerableProxy<T>(IEnumerable<T> enumerable)
        {
            if (enumerable is null)
                throw new ArgumentNullException(nameof(enumerable));
            return new EnumerableProxy<T>(enumerable);
        }

        #endregion

        #region AsNullWhenEmpty

        /// <summary>
        /// 如果为空，则返回 null
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">集合</param>
        public static IEnumerable<T> AsNullWhenEmpty<T>(this IEnumerable<T> source)
        {
            using var disposable = new InternalReleasableDisposable<IEnumerator<T>>(source.GetEnumerator());
            var enumerator = disposable.Disposable;
            if (!enumerator.MoveNext())
                return null;
            disposable.Release();
            var wrapper = new InternalNullIfEmptySkipFirstMoveNextEnumeratorWrapper<T>(enumerator);
            return new InternalSingleUseEnumerable<T>(wrapper);
        }

        #endregion
    }

    /// <summary>
    /// 集合转换器扩展
    /// </summary>
    public static class CollConvExtensions
    {
        /// <summary>
        /// 转换为 <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="enumerator">集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator) => CollConv.ToEnumerable(enumerator);

        /// <summary>
        /// 转换为 <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="enumerator">集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> ToEnumerableAfter<T>(this IEnumerator<T> enumerator) => CollConv.ToEnumerableAfter(enumerator);

        /// <summary>
        /// 转换为具有索引的序列
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<KeyValuePair<int, T>> ToIndexedSequence<T>(this IEnumerable<T> source) => CollConv.ToIndexedSequence(source);

        /// <summary>
        /// 转换为有序数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="comparer">比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToSortedArray<T>(this IEnumerable<T> source, Comparison<T> comparer) => CollConv.ToSortedArray(source, comparer);

        /// <summary>
        /// 转换为有序数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToSortedArray<T>(this IEnumerable<T> source) where T : IComparable<T> =>
            CollConv.ToSortedArray(source);

#if NETFRAMEWORK || NETSTANDARD2_0
#if !NET472 && !NET478

        /// <summary>
        /// 转换为 <see cref="HashSet{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) where T : IComparable<T>
        {
            return source.ToHashSet(EqualityComparer<T>.Default);
        }

#endif

        /// <summary>
        /// 转换为 <see cref="HashSet{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="comparer">相等比较器</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
            where T : IComparable<T>
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer));
            return new HashSet<T>(source, comparer);
        }

#endif

        /// <summary>
        /// 转换为 <see cref="HashSet{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="ignoreDup">是否忽略重复内容</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, bool ignoreDup) where T : IComparable<T>
        {
            return ignoreDup
                ? source.Distinct().ToHashSet(EqualityComparer<T>.Default)
                : source.ToHashSet(EqualityComparer<T>.Default);
        }

        /// <summary>
        /// 转换为 <see cref="HashSet{T}"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="keyFunc">键函数</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<TKey> ToHashSet<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keyFunc)
            where TKey : IComparable<TKey>
        {
            if (keyFunc is null)
                throw new ArgumentNullException(nameof(keyFunc));
            return source.Select(keyFunc).ToHashSet(EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// 转换为 <see cref="HashSet{T}"/>，并忽略重复的值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> ToHashSetIgnoringDuplicates<T>(this IEnumerable<T> source) where T : IComparable<T> => source.ToHashSet(true);
    }

    /// <summary>
    /// 集合转换器捷径扩展
    /// </summary>
    public static class CollConvShortcutExtensions
    {
        /// <summary>
        /// 转换为字符串列表
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="stringConverter">字符串转换函数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<string> ToList<T>(this IEnumerable<T> source, Func<T, string> stringConverter) => source.Select(stringConverter).ToList();

        /// <summary>
        /// 转换为列表
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="func">条件函数</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<T> ToList<T>(this IEnumerable<T> source, Func<T, bool> func)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return func is null ? source.ToList() : source.Where(func).ToList();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bing.Collections
{
    /// <summary>
    /// 集合工具扩展
    /// </summary>
    public static partial class CollsExtensions
    {
        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BeContainedIn<T>(this T item, params T[] items) =>
            Colls.BeContainedIn(item, items, EqualityComparer<T>.Default);

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="equalityComparer">相等比较器</param>
        /// <param name="items">集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BeContainedIn<T>(this T item, IEqualityComparer<T> equalityComparer, params T[] items) =>
            Colls.BeContainedIn(item, items, equalityComparer);

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BeContainedIn<T>(this T item, IEnumerable<T> items) =>
            Colls.BeContainedIn(item, items, EqualityComparer<T>.Default);

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        /// <param name="equalityComparer">相等比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BeContainedIn<T>(this T item, IEnumerable<T> items, IEqualityComparer<T> equalityComparer) =>
            Colls.BeContainedIn(item, items, equalityComparer);

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        /// <param name="condition">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BeContainedIn<T>(this T item, IEnumerable<T> items, Expression<Func<T, bool>> condition) =>
            Colls.BeContainedIn(item, items, condition);

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        /// <param name="condition">条件</param>
        /// <param name="equalityComparer">相等比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BeContainedIn<T>(this T item, IEnumerable<T> items, Expression<Func<T, bool>> condition, IEqualityComparer<T> equalityComparer) =>
            Colls.BeContainedIn(item, items, condition, equalityComparer);


        /// <summary>
        /// 检查集合中是否包含给定条件的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="condition">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this IEnumerable<T> source, Expression<Func<T, bool>> condition) =>
            Colls.Contains(source, condition);

        /// <summary>
        /// 检查一个集合是否拥有指定数量的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAtLeast<T>(this ICollection<T> source, int count) =>
            Colls.ContainsAtLeast(source, count);

        /// <summary>
        /// 检查一个集合是否拥有指定数量的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="condition">条件</param>
        /// <param name="count">数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAtLeast<T>(this IEnumerable<T> source, Expression<Func<T, bool>> condition, int count) =>
            Colls.ContainsAtLeast(source, condition, count);

        /// <summary>
        /// 检查一个集合是否拥有指定数量的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAtLeast<T>(this IQueryable<T> source, int count) =>
            Colls.ContainsAtLeast(source, count);

        /// <summary>
        /// 获取给定元素在集合中的索引值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="item">项</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this IEnumerable<T> source, T item) =>
            Colls.IndexOf(source, item);

        /// <summary>
        /// 获取给定元素在集合中的索引值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="item">项</param>
        /// <param name="equalityComparer">相等比较器</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this IEnumerable<T> source, T item, IEqualityComparer<T> equalityComparer) =>
            Colls.IndexOf(source, item, equalityComparer);

        /// <summary>
        /// 打乱一个集合的顺序
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> OrderByRandom<T>(this IEnumerable<T> source)
            => Colls.OrderByRandom(source);

        /// <summary>
        /// 原地打乱
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrderByShuffle<T>(this IList<T> items) =>
            Colls.OrderByShuffle(items);

        /// <summary>
        /// 原地打乱
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        /// <param name="times">次数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrderByShuffle<T>(this IList<T> items, int times) =>
            Colls.OrderByShuffle(items, times);

        /// <summary>
        /// 原地打乱并返回一个新列表
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> OrderByShuffleAndNewInstance<T>(this IList<T> items) =>
            Colls.OrderByShuffleAndNewInstance(items);

        /// <summary>
        /// 原地打乱并返回一个新列表
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        /// <param name="times">次数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> OrderByShuffleAndNewInstance<T>(this IList<T> items, int times) =>
            Colls.OrderByShuffleAndNewInstance(items, times);

        /// <summary>
        /// 不重复元素的数量
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UniqueCount<T>(this IEnumerable<T> source) =>
            Colls.UniqueCount(source);

        /// <summary>
        /// 不重复元素的数量
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="valCalculator">值计算函数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UniqueCount<T, TResult>(this IEnumerable<T> source, Func<T, TResult> valCalculator) =>
            Colls.UniqueCount(source, valCalculator);
    }
}
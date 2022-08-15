using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bing.Collections
{
    /// <summary>
    /// 集合工具扩展
    /// </summary>
    public static partial class CollsExtensions
    {
        /// <summary>
        /// 将一组值添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="collection">其它集合</param>
        /// <param name="limit">限制数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> AddRange<T>(this IEnumerable<T> source, IEnumerable<T> collection, int limit) =>
            Colls.AddRange(source, collection, limit);

        /// <summary>
        /// 如果满足条件则添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        /// <param name="flag">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> AddIf<T>(this IEnumerable<T> source, T value, bool flag) =>
            Colls.AddIf(source, value, flag);

        /// <summary>
        /// 如果满足条件则添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        /// <param name="condition">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> AddIf<T>(this IEnumerable<T> source, T value, Func<bool> condition) =>
            Colls.AddIf(source, value, condition);

        /// <summary>
        /// 如果满足条件则添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        /// <param name="condition">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> AddIf<T>(this IEnumerable<T> source, T value, Func<T, bool> condition) =>
            Colls.AddIf(source, value, condition);

        /// <summary>
        /// 如果元素不存在，则添加。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        /// <param name="existFunc">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> AddIfNotExist<T>(this IEnumerable<T> source, T value, Func<T, bool> existFunc = null)
        {
            Func<T, bool> condition = t => !source.Contains(t);
            return Colls.AddIf(source, value, v => existFunc?.Invoke(v) ?? condition(v));
        }

        /// <summary>
        /// 如果值不为空，则添加。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> AddIfNotNull<T>(this IEnumerable<T> source, T value) =>
            Colls.AddIf(source, value, v => v is not null);

        /// <summary>
        /// 获取，如果不存在则先添加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="selector">选择器</param>
        /// <param name="factory">值工厂</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrAdd<T>(ICollection<T> source, Func<T, bool> selector, Func<T> factory) =>
            Colls.GetOrAdd(source, selector, factory);

        /// <summary>
        /// 移除重复项
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> RemoveDuplicates<T>(this IList<T> source) =>
            Colls.RemoveDuplicates(source);

        /// <summary>
        /// 移除重复项
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TCheck">检查类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="duplicatePredicate">重复条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> RemoveDuplicates<T, TCheck>(this IList<T> source, Func<T, TCheck> duplicatePredicate) =>
            Colls.RemoveDuplicates(source, duplicatePredicate);

        /// <summary>
        /// 移除重复项，不区分大小写
        /// </summary>
        /// <param name="source">源集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<string> RemoveDuplicatesIgnoreCase(this IList<string> source) =>
            Colls.RemoveDuplicatesIgnoreCase(source);

        /// <summary>
        /// 移除满足条件的成员，并最终返回筛选后的结果
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="predicate">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> RemoveIf<T>(this IList<T> source, Func<T, bool> predicate) =>
            Colls.RemoveIf(source, predicate);

        /// <summary>
        /// 合并集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="right">其它集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Merge<T>(this IEnumerable<T> source, IEnumerable<T> right) =>
            Colls.Merge(source, right);

        /// <summary>
        /// 合并集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="right">其它集合</param>
        /// <param name="limit">限制数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Merge<T>(this IEnumerable<T> source, IEnumerable<T> right, int limit) =>
            Colls.Merge(source, right, limit);
    }
}

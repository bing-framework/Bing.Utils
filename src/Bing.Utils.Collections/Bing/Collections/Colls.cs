using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bing.Collections
{
    /// <summary>
    /// Linq集合帮助类
    /// </summary>
    internal static class LinqCollsHelper
    {
        /// <summary>
        /// 获取最后N个元素
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">数量</param>
        public static IEnumerable<T> EnumerableTakeLast<T>(IEnumerable<T> source, int count)
        {
            var window = new Queue<T>();
            foreach (var item in source)
            {
                window.Enqueue(item);
                if (window.Count > count)
                    window.Dequeue();
            }
            return window;
        }

        /// <summary>
        /// 获取最后N个元素
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">数量</param>
        public static IEnumerable<T> CollectionTakeLast<T>(ICollection<T> source, int count)
        {
            count = Math.Min(source.Count, count);
            if (count == 0)
                return Enumerable.Empty<T>();
            if (count == source.Count)
                return source;
            return source.Skip(source.Count - count);
        }

        /// <summary>
        /// 获取最后N个元素
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">数量</param>
        public static IEnumerable<T> ReadOnlyCollectionTakeLat<T>(IReadOnlyCollection<T> source, int count)
        {
            count = Math.Min(source.Count, count);
            if (count == 0)
                return Enumerable.Empty<T>();
            if (count == source.Count)
                return source;
            return source.Skip(source.Count - count);
        }
    }

    /// <summary>
    /// 集合工具
    /// </summary>
    public static partial class Colls
    {
        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool BeContainedIn<T>(T item, IEnumerable<T> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            return items.Contains(item, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        /// <param name="equalityComparer">相等比较器</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool BeContainedIn<T>(T item, IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));
            return items.Contains(item, equalityComparer);
        }

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        /// <param name="condition">条件</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool BeContainedIn<T>(T item, IEnumerable<T> items, Expression<Func<T, bool>> condition)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            return items.Where(condition.Compile()).Contains(item, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// 判断元素是否包含在给定集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="item">项</param>
        /// <param name="items">集合</param>
        /// <param name="condition">条件</param>
        /// <param name="equalityComparer">相等比较器</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool BeContainedIn<T>(T item, IEnumerable<T> items, Expression<Func<T, bool>> condition, IEqualityComparer<T> equalityComparer)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));
            return items.Where(condition.Compile()).Contains(item, equalityComparer);
        }

        /// <summary>
        /// 检查集合中是否包含给定条件的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="condition">条件</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Contains<T>(IEnumerable<T> source, Expression<Func<T, bool>> condition)
        {
            if (condition is null)
                throw new ArgumentNullException(nameof(condition));
            var func = condition.Compile();
            return source.Any(item => func.Invoke(item));
        }

        /// <summary>
        /// 检查一个集合是否拥有指定数量的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAtLeast<T>(ICollection<T> source, int count) => source?.Count >= count;

        /// <summary>
        /// 检查一个集合是否拥有指定数量的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="condition">条件</param>
        /// <param name="count">数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAtLeast<T>(IEnumerable<T> source, Expression<Func<T, bool>> condition, int count) => source?.Where(condition.Compile()).Count() >= count;

        /// <summary>
        /// 检查一个集合是否拥有指定数量的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="count">数量</param>
        public static bool ContainsAtLeast<T>(IQueryable<T> source, int count)
        {
            if (source is null)
                return false;
            return source.Take(count).Count() >= count;
        }

        /// <summary>
        /// 创建一个指定类型 T 的空列表实例
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Empty<T>() => Arrays.Empty<T>().ToList();

        /// <summary>
        /// 获取给定元素在集合中的索引值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="item">项</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(IEnumerable<T> source, T item) => IndexOf(source, item, EqualityComparer<T>.Default);

        /// <summary>
        /// 获取给定元素在集合中的索引值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="item">项</param>
        /// <param name="equalityComparer">相等比较器</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static int IndexOf<T>(IEnumerable<T> source, T item, IEqualityComparer<T> equalityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));
            return source.Select((i, index) => new { Item = i, Index = index })
                .FirstOrDefault(p => equalityComparer.Equals(p.Item, item))
                ?.Index ?? -1;
        }

        /// <summary>
        /// 移动到首位
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="element">元素</param>
        public static List<T> MoveToFirst<T>(List<T> source, T element)
        {
            if (!source.Contains(element))
                return source;
            source.Remove(element);
            source.Insert(0, element);
            return source;
        }

        /// <summary>
        /// 打乱一个集合的顺序
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> OrderByRandom<T>(IEnumerable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return source.OrderBy(_ => Guid.NewGuid());
        }

        /// <summary>
        /// 原地打乱
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrderByShuffle<T>(IList<T> items) => OrderByShuffle(items, 4);

        /// <summary>
        /// 原地打乱
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        /// <param name="times">次数</param>
        public static void OrderByShuffle<T>(IList<T> items, int times)
        {
            for (var j = 0; j < times; j++)
            {
                var rnd = new Random((int)(DateTime.Now.Ticks % int.MaxValue) - j);
                for (var i = 0; i < items.Count; i++)
                {
                    var index = rnd.Next(items.Count - 1);
                    (items[index], items[i]) = (items[i], items[index]);
                }
            }
        }

        /// <summary>
        /// 原地打乱并返回一个新列表
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> OrderByShuffleAndNewInstance<T>(IList<T> items) => OrderByShuffleAndNewInstance(items, 4);

        /// <summary>
        /// 原地打乱并返回一个新列表
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="items">列表</param>
        /// <param name="times">次数</param>
        public static List<T> OrderByShuffleAndNewInstance<T>(IList<T> items, int times)
        {
            var res = new List<T>(items);
            OrderByShuffle(res, times);
            return res;
        }

        /// <summary>
        /// 不重复元素的数量
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UniqueCount<T>(IEnumerable<T> source) => UniqueCount(source, val => val);

        /// <summary>
        /// 不重复元素的数量
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="valCalculator">值计算函数</param>
        public static int UniqueCount<T, TResult>(IEnumerable<T> source, Func<T, TResult> valCalculator)
        {
            var check = new HashSet<TResult>();
            foreach (var item in source)
            {
                var result = valCalculator(item);
                if (!check.Contains(result))
                    check.Add(result);
            }
            return check.Count;
        }
    }
}
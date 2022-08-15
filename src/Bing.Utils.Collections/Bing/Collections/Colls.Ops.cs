using System;
using System.Collections.Generic;
using System.Linq;

namespace Bing.Collections
{
    /// <summary>
    /// 集合工具
    /// </summary>
    public static partial class Colls
    {
        /// <summary>
        /// 将一组值添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="collection">其它集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> AddRange<T>(IEnumerable<T> source, IEnumerable<T> collection)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));
            foreach (var item in source)
                yield return item;
            foreach (var item in collection)
                yield return item;
        }

        /// <summary>
        /// 将一组值添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="collection">其它集合</param>
        /// <param name="limit">限制数量</param>
        public static IEnumerable<T> AddRange<T>(IEnumerable<T> source, IEnumerable<T> collection, int limit)
        {
            var counter = 0;
            return limit <= 0
                ? AddRange(source, collection).ToList()
                : AddRange(source, collection.TakeWhile(_ => counter++ < limit)).ToList();
        }

        /// <summary>
        /// 如果满足条件则添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        /// <param name="flag">条件</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> AddIf<T>(IEnumerable<T> source, T value, bool flag)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            foreach (var item in source)
                yield return item;
            if (flag)
                yield return value;
        }

        /// <summary>
        /// 如果满足条件则添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        /// <param name="condition">条件</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> AddIf<T>(IEnumerable<T> source, T value, Func<bool> condition)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            foreach (var item in source)
                yield return item;
            if (condition())
                yield return value;
        }

        /// <summary>
        /// 如果满足条件则添加到集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="value">值</param>
        /// <param name="condition">条件</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> AddIf<T>(IEnumerable<T> source, T value, Func<T, bool> condition)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            foreach (var item in source)
                yield return item;
            if (condition?.Invoke(value) ?? false)
                yield return value;
        }

        /// <summary>
        /// 获取，如果不存在则先添加
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="selector">选择器</param>
        /// <param name="factory">值工厂</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static T GetOrAdd<T>(ICollection<T> source, Func<T, bool> selector, Func<T> factory)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));
            if (factory is null)
                throw new ArgumentNullException(nameof(factory));
            var item = source.FirstOrDefault(selector);
            if (item is null)
                source.Add(item = factory());
            return item;
        }

        /// <summary>
        /// 移除重复项
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        public static IEnumerable<T> RemoveDuplicates<T>(IList<T> source)
        {
            var duplicateCheck = new HashSet<T>();
            return RemoveIf(source, item =>
            {
                if (duplicateCheck.Contains(item))
                    return true;
                duplicateCheck.Add(item);
                return false;
            });
        }

        /// <summary>
        /// 移除重复项
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TCheck">检查类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="duplicatePredicate">重复条件</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> RemoveDuplicates<T, TCheck>(IList<T> source, Func<T, TCheck> duplicatePredicate)
        {
            if (duplicatePredicate is null)
                throw new ArgumentNullException(nameof(duplicatePredicate));
            var duplicateCheck = new HashSet<TCheck>();
            return RemoveIf(source, item =>
            {
                var val = duplicatePredicate(item);
                if (duplicateCheck.Contains(val))
                    return true;
                duplicateCheck.Add(val);
                return false;
            });
        }

        /// <summary>
        /// 移除重复项，不区分大小写
        /// </summary>
        /// <param name="source">源集合</param>
        public static IEnumerable<string> RemoveDuplicatesIgnoreCase(IList<string> source)
        {
            var duplicateCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return RemoveIf(source, item =>
            {
                if (duplicateCheck.Contains(item))
                    return true;
                duplicateCheck.Add(item);
                return false;
            });
        }

        /// <summary>
        /// 移除满足条件的成员，并最终返回筛选后的结果
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="predicate">条件</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> RemoveIf<T>(IList<T> source, Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            for (var i = source.Count - 1; i >= 0; --i)
            {
                var item = source[i];
                if (!predicate.Invoke(item))
                    continue;
                source.RemoveAt(i);
            }
            return source;
        }

        /// <summary>
        /// 安全地移除指定范围内的成员
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="index">索引</param>
        /// <param name="count">数量</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> RemoveRangeSafety<T>(List<T> source, int index, int count)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (index < 0 || count <= 0)
                return source;
            if (index >= source.Count)
                return source;
            count = Math.Min(count, source.Count) - index;
            source.RemoveRange(index, count);
            return source;
        }

        /// <summary>
        /// 合并
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="first">元素</param>
        /// <param name="right">迭代器</param>
        public static IEnumerable<T> Merge<T>(T first, IEnumerator<T> right)
        {
            yield return first;
            while (right.MoveNext())
                yield return right.Current;
        }

        /// <summary>
        /// 将两个具有相同种类的元素的集合合并
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="left">迭代器</param>
        /// <param name="right">迭代器</param>
        public static IEnumerable<T> Merge<T>(IEnumerator<T> left, IEnumerator<T> right)
        {
            while (left.MoveNext())
                yield return left.Current;
            while (right.MoveNext())
                yield return right.Current;
        }

        /// <summary>
        /// 将一个元素添加到一个具有相同种类的元素的集合内
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="left">迭代器</param>
        /// <param name="last">元素</param>
        public static IEnumerable<T> Merge<T>(IEnumerator<T> left, T last)
        {
            while (left.MoveNext())
                yield return left.Current;
            yield return last;
        }

        /// <summary>
        /// 合并集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="right">其它集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Merge<T>(IEnumerable<T> source, IEnumerable<T> right)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            foreach (var item in source)
                yield return item;
            if (right is null)
                yield break;
            foreach (var item in right)
                yield return item;
        }

        /// <summary>
        /// 合并集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="right">其它集合</param>
        /// <param name="limit">限制数量</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<T> Merge<T>(IEnumerable<T> source, IEnumerable<T> right, int limit)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            foreach (var item in source)
                yield return item;
            if (right is null)
                yield break;
            if (limit <= 0)
            {
                foreach (var item in right)
                    yield return item;
            }
            else
            {
                var counter = 0;
                foreach (var item in right)
                {
                    if (counter++ >= limit)
                        yield break;
                    yield return item;
                }
            }
        }
    }
}

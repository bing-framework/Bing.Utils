using Bing.Extensions;

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 列表(<see cref="IList{T}"/>) 扩展
/// </summary>
public static partial class BingListExtensions
{
    /// <summary>
    /// 插入集合项，添加多个元素到集合中
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="index">索引</param>
    /// <param name="items">元素集合</param>
    public static void InsertRange<T>(this IList<T> source, int index, IEnumerable<T> items)
    {
        foreach (var item in items)
            source.Insert(index++, item);
    }

    /// <summary>
    /// 查找指定条件的索引值
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <returns>如果找到返回该条件所在索引，否则返回-1。</returns>
    public static int FindIndex<T>(this IList<T> source, Predicate<T> selector)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (selector(source[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 将元素添加到首位
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="item">元素</param>
    public static void AddFirst<T>(this IList<T> source, T item) => source.Insert(0, item);

    /// <summary>
    /// 将元素添加到尾部
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="item">元素</param>
    public static void AddLast<T>(this IList<T> source, T item) => source.Insert(source.Count, item);

    /// <summary>
    /// 将元素插入到已存在的元素之后
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="existingItem">已存在的元素</param>
    /// <param name="item">元素</param>
    public static void InsertAfter<T>(this IList<T> source, T existingItem, T item)
    {
        var index = source.IndexOf(existingItem);
        if (index < 0)
        {
            source.AddFirst(item);
            return;
        }
        source.Insert(index + 1, item);
    }

    /// <summary>
    /// 将元素插入到已存在的条件之后
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <param name="item">元素</param>
    public static void InsertAfter<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindIndex(selector);
        if (index < 0)
        {
            source.AddFirst(item);
            return;
        }
        source.Insert(index + 1, item);
    }

    /// <summary>
    /// 将元素插入到已存在的元素之前
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="existingItem">已存在的元素</param>
    /// <param name="item">元素</param>
    public static void InsertBefore<T>(this IList<T> source, T existingItem, T item)
    {
        var index = source.IndexOf(existingItem);
        if (index < 0)
        {
            source.AddLast(item);
            return;
        }
        source.Insert(index, item);
    }

    /// <summary>
    /// 将元素插入到已存在的条件之前
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <param name="item">元素</param>
    public static void InsertBefore<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindIndex(selector);
        if (index < 0)
        {
            source.AddLast(item);
            return;
        }
        source.Insert(index, item);
    }

    /// <summary>
    /// 循环替换符合条件的元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <param name="item">元素</param>
    public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (selector(source[i]))
                source[i] = item;
        }
    }

    /// <summary>
    /// 循环替换符合条件的元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <param name="itemFactory">元素工厂</param>
    public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (selector(item)) 
                source[i] = itemFactory(item);
        }
    }

    /// <summary>
    /// 仅替换一次符合条件的元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <param name="item">元素</param>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (selector(source[i]))
            {
                source[i] = item;
                return;
            }
        }
    }

    /// <summary>
    /// 仅替换一次符合条件的元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <param name="itemFactory">元素工厂</param>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (selector(item))
            {
                source[i] = itemFactory(item);
                return;
            }
        }
    }

    /// <summary>
    /// 仅替换一次符合条件的元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="item">待替换的元素</param>
    /// <param name="replaceWith">目标替换元素</param>
    public static void ReplaceOne<T>(this IList<T> source, T item, T replaceWith)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (Comparer<T>.Default.Compare(source[i], item) == 0)
            {
                source[i] = replaceWith;
                return;
            }
        }
    }

    /// <summary>
    /// 移动项到指定位置。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="selector">条件</param>
    /// <param name="targetIndex">目标索引</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public static void MoveItem<T>(this List<T> source, Predicate<T> selector, int targetIndex)
    {
        if (!targetIndex.IsBetween(0, source.Count - 1))
            throw new IndexOutOfRangeException($"targetIndex should be between 0 and {source.Count - 1}.");
        var currentIndex = source.FindIndex(0, selector);
        if (currentIndex == targetIndex)
            return;
        var item = source[currentIndex];
        source.RemoveAt(currentIndex);
        source.Insert(targetIndex, item);
    }

    /// <summary>
    /// 将 <see cref="List{T}"/> 按指定大小进行分块（通过 GetRange 切片方式实现）
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="chunkSize">每个分组的最大元素数</param>
    /// <returns>分组后的集合序列，每组为一个 <see cref="List{T}"/></returns>
    /// <exception cref="ArgumentNullException">当 source 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 chunkSize 小于等于 0 时抛出</exception>
    public static IEnumerable<List<T>> ChunkByView<T>(this List<T> source, int chunkSize)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        if (chunkSize <= 0) 
            throw new ArgumentOutOfRangeException(nameof(chunkSize));

        for (var i = 0; i < source.Count; i += chunkSize)
        {
            var count = Math.Min(chunkSize, source.Count - i);
            yield return source.GetRange(i, count);
        }
    }
}
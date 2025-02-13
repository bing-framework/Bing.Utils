using Bing.Collections.Internals;

namespace Bing.Collections;

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
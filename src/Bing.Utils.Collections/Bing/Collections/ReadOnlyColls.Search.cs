namespace Bing.Collections;

/// <summary>
/// 只读集合工具
/// </summary>
public static partial class ReadOnlyColls
{
    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(IReadOnlyList<T> source, T value) =>
        BinarySearch(source, t => t, value);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="map">映射函数</param>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<TSource, TValue>(IReadOnlyList<TSource> source, Func<TSource, TValue> map, TValue value) =>
        BinarySearch(source, map, value, Comparer<TValue>.Default);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="index">索引</param>
    /// <param name="length">长度</param>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(IReadOnlyList<T> source, int index, int length, T value) =>
        BinarySearch(source, index, length, t => t, value);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="index">索引</param>
    /// <param name="length">长度</param>
    /// <param name="map">映射函数</param>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<TSource, TValue>(IReadOnlyList<TSource> source, int index, int length, Func<TSource, TValue> map, TValue value) =>
        BinarySearch(source, index, length, map, value, Comparer<TValue>.Default);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="value">值</param>
    /// <param name="comparer">比较器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(IReadOnlyList<T> source, T value, IComparer<T> comparer) => 
        BinarySearch(source, 0, source.Count, t => t, value, comparer);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="map">映射函数</param>
    /// <param name="value">值</param>
    /// <param name="comparer">比较器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<TSource, TValue>(IReadOnlyList<TSource> source, Func<TSource, TValue> map, TValue value, IComparer<TValue> comparer) =>
        BinarySearch(source, 0, source.Count, map, value, comparer);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="index">索引</param>
    /// <param name="length">长度</param>
    /// <param name="value">值</param>
    /// <param name="comparer">比较器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(IReadOnlyList<T> source, int index, int length, T value, IComparer<T> comparer) =>
        BinarySearch(source, index, length, t => t, value, comparer);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="index">索引</param>
    /// <param name="length">长度</param>
    /// <param name="map">映射函数</param>
    /// <param name="value">值</param>
    /// <param name="comparer">比较器</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static int BinarySearch<TSource, TValue>(IReadOnlyList<TSource> source, int index, int length, Func<TSource, TValue> map, TValue value, IComparer<TValue> comparer)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (map is null)
            throw new ArgumentNullException(nameof(map));
        if (comparer is null)
            throw new ArgumentNullException(nameof(comparer));
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"The {nameof(index)} parameter must be a non-negative value.");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"The {nameof(length)} parameter must be a non-negative value.");
        if (index + length > source.Count)
            throw new InvalidOperationException($"The value of {nameof(index)} plus {nameof(length)} must be less than or equal to the value of the number of items in the {nameof(source)}.");

        int low = index;
        int high = index + length - 1;

        while (low <= high)
        {
            int midpoint = low + ((high - low) >> 1);
            int order = comparer.Compare(map(source[midpoint]), value);
            if (order == 0)
                return midpoint;
            if (order < 0)
                low = midpoint + 1;
            else
                high = midpoint - 1;
        }
        return ~low;
    }
}
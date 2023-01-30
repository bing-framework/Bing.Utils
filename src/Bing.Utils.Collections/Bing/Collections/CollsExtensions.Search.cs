namespace Bing.Collections;

/// <summary>
/// 集合工具扩展
/// </summary>
public static partial class CollsExtensions
{
    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(this IList<T> source, T value) =>
        Colls.BinarySearch(source, t => t, value);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="map">映射函数</param>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<TSource, TValue>(this IList<TSource> source, Func<TSource, TValue> map, TValue value) =>
        Colls.BinarySearch(source, map, value, Comparer<TValue>.Default);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="index">索引</param>
    /// <param name="length">长度</param>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(this IList<T> source, int index, int length, T value) =>
        Colls.BinarySearch(source, index, length, t => t, value);

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
    public static int BinarySearch<TSource, TValue>(this IList<TSource> source, int index, int length, Func<TSource, TValue> map, TValue value) =>
        Colls.BinarySearch(source, index, length, map, value, Comparer<TValue>.Default);

    /// <summary>
    /// 二进制查询
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <param name="value">值</param>
    /// <param name="comparer">比较器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(this IList<T> source, T value, IComparer<T> comparer) =>
        Colls.BinarySearch(source, 0, source.Count, t => t, value, comparer);

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
    public static int BinarySearch<TSource, TValue>(this IList<TSource> source, Func<TSource, TValue> map, TValue value, IComparer<TValue> comparer) =>
        Colls.BinarySearch(source, 0, source.Count, map, value, comparer);

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
    public static int BinarySearch<T>(this IList<T> source, int index, int length, T value, IComparer<T> comparer) =>
        Colls.BinarySearch(source, index, length, t => t, value, comparer);

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<TSource, TValue>(this IList<TSource> source, int index, int length, Func<TSource, TValue> map, TValue value, IComparer<TValue> comparer) =>
        Colls.BinarySearch(source, index, length, map, value, comparer);
}
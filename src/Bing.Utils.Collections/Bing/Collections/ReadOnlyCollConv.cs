using System.Collections.ObjectModel;
using Bing.Collections.Internals;

namespace Bing.Collections;

/// <summary>
/// 只读集合转换器
/// </summary>
public static class ReadOnlyCollConv
{
    /// <summary>
    /// 转换为列表
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="list">列表</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IList<T> AsList<T>(IReadOnlyList<T> list)
    {
        if (list is null)
            throw new ArgumentNullException(nameof(list));
        return new ReadOnlyListWrapper<T>(list);
    }
}

/// <summary>
/// 只读集合转换器扩展
/// </summary>
public static class ReadOnlyCollConvExtensions
{
    /// <summary>
    /// 转换为只读集合
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源集合</param>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyCollection<T> AsReadOnly<T>(this IEnumerable<T> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        return ReadOnlyCollsHelper.WrapInReadOnlyCollection(source.ToList());
    }

    /// <summary>
    /// 转换为列表
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="list">列表</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IList<T> AsList<T>(this IReadOnlyList<T> list) => ReadOnlyCollConv.AsList(list);
}
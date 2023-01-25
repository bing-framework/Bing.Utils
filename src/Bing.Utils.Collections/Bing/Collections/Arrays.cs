namespace Bing.Collections;

/// <summary>
/// 数组 操作
/// </summary>
public static partial class Arrays
{
    /// <summary>
    /// 空数组
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Empty<T>() => InternalArray.ForEmpty<T>();

    /// <summary>
    /// 安全地转换为数组
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <param name="src">数据源</param>
    /// <param name="count">元素总数</param>
    public static TElement[] ToArraySafety<TElement>(IEnumerable<TElement> src, int count)
    {
        if (count <= 0)
            return Empty<TElement>();
        var elements = new TElement[count];
        if (src is null)
            return elements;
        var index = 0;
        foreach (var item in src)
        {
            if (index == count)
                break;
            elements[index++] = item;
        }
        return elements;
    }

    /// <summary>
    /// 安全地转换为数组
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <param name="src">数据源</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TElement[] ToArraySafety<TElement>(IEnumerable<TElement> src)
    {
        if (src is null)
            return Empty<TElement>();
        return src as TElement[] ?? src.ToArray();
    }

    /// <summary>
    /// 安全地转换为数组
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <param name="src">数据源</param>
    /// <param name="count">元素总数</param>
    public static TElement[] ToArraySafety<TElement>(Array src, int count)
    {
        if (count <= 0)
            return Empty<TElement>();
        var elements = new TElement[count];
        if (src is null)
            return elements;
        var index = 0;
        foreach (var item in src)
        {
            if (index == count)
                break;
            elements[index++] = (TElement)item;
        }
        return elements;
    }

    /// <summary>
    /// 安全地转换为数组
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <param name="src">数据源</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TElement[] ToArraySafety<TElement>(Array src)
    {
        if (src is null)
            return Empty<TElement>();
        var elements = new TElement[src.Length];
        var index = 0;
        foreach (var item in src) 
            elements[index++] = (TElement)item;
        return elements;
    }
}
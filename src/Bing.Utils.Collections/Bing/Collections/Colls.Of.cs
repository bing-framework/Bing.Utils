using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bing.Collections;

/// <summary>
/// 集合工具
/// </summary>
public static partial class Colls
{
    /// <summary>
    /// 创建一个指定类型 T 的列表实例
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> OfList<T>() => new();

    /// <summary>
    /// 创建一个指定类型 T 的列表实例
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="params">参数数组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> OfList<T>(params T[] @params) => new(@params);

    /// <summary>
    /// 创建一个指定类型 T 的列表实例
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="listParams">集合参数数组</param>
    public static List<T> OfList<T>(IEnumerable<T>[] listParams)
    {
        var ret = new List<T>();
        if (listParams is not null)
            foreach (var list in listParams)
                ret.AddRange(list);
        return ret;
    }

    /// <summary>
    /// 创建一个指定类型 T 的列表实例
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="listParams">集合参数数组</param>
    public static List<T> OfList<T>(IEnumerable<T> list, params IEnumerable<T>[] listParams)
    {
        var ret = new List<T>(list);
        if (listParams is not null)
            foreach (var @params in listParams)
                ret.AddRange(@params);
        return ret;
    }
}
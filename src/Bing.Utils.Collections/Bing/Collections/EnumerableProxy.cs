using System;
using System.Collections;
using System.Collections.Generic;

namespace Bing.Collections;

/// <summary>
/// 枚举代理
/// </summary>
/// <typeparam name="T">类型</typeparam>
public class EnumerableProxy<T> : IEnumerable<T>
{
    /// <summary>
    /// 集合
    /// </summary>
    private readonly IEnumerable<T> _enumerable;

    /// <summary>
    /// 初始化一个<see cref="EnumerableProxy{T}"/>类型的实例
    /// </summary>
    /// <param name="enumerable">集合</param>
    public EnumerableProxy(IEnumerable<T> enumerable) => _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
﻿using System.Collections.ObjectModel;

namespace Bing.Collections;

/// <summary>
/// 只读字典转换器
/// </summary>
public static class ReadOnlyDictConv
{
    /// <summary>
    /// 转换为只读字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源字典</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        return new ReadOnlyDictionary<TKey, TValue>(source.ToDictionary(p => p.Key, p => p.Value, EqualityComparer<TKey>.Default));
    }

    /// <summary>
    /// 转换为只读字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源字典</param>
    /// <param name="comparer">相等比较器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (comparer is null)
            throw new ArgumentNullException(nameof(comparer));
        return new ReadOnlyDictionary<TKey, TValue>(source.ToDictionary(p => p.Key, p => p.Value, comparer));
    }

    /// <summary>
    /// 转换为只读字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源字典</param>
    /// <param name="keySelector">键选择器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<TValue> source, Func<TValue, TKey> keySelector)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector is null)
            throw new ArgumentNullException(nameof(keySelector));
        return ReadOnlyDictsHelper.WrapInReadOnlyDictionary(source.ToDictionary(keySelector));
    }

    /// <summary>
    /// 转换为只读字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源字典</param>
    /// <param name="keySelector">键选择器</param>
    /// <param name="comparer">相等比较器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<TValue> source, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector is null)
            throw new ArgumentNullException(nameof(keySelector));
        if (comparer is null)
            throw new ArgumentNullException(nameof(comparer));
        return ReadOnlyDictsHelper.WrapInReadOnlyDictionary(source.ToDictionary(keySelector, comparer));
    }

    /// <summary>
    /// 转换为只读字典
    /// </summary>
    /// <typeparam name="TSource">元素类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源字典</param>
    /// <param name="keySelector">键选择器</param>
    /// <param name="elementSelector">元素选择器</param>
    /// <param name="comparer">相等比较器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TSource, TKey, TValue>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey> comparer)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector is null)
            throw new ArgumentNullException(nameof(keySelector));
        if (elementSelector is null)
            throw new ArgumentNullException(nameof(elementSelector));
        if (comparer is null)
            throw new ArgumentNullException(nameof(comparer));
        return ReadOnlyDictsHelper.WrapInReadOnlyDictionary(source.ToDictionary(keySelector, elementSelector, comparer));
    }

    /// <summary>
    /// 转换为只读字典
    /// </summary>
    /// <typeparam name="TSource">元素类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源字典</param>
    /// <param name="keySelector">键选择器</param>
    /// <param name="elementSelector">元素选择器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TSource, TKey, TValue>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector is null)
            throw new ArgumentNullException(nameof(keySelector));
        if (elementSelector is null)
            throw new ArgumentNullException(nameof(elementSelector));
        return ReadOnlyDictsHelper.WrapInReadOnlyDictionary(source.ToDictionary(keySelector, elementSelector));
    }
}
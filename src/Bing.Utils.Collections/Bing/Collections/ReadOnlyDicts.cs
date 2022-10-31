using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Bing.Collections;

/// <summary>
/// 只读字典帮助类
/// </summary>
internal static class ReadOnlyDictsHelper
{
    /// <summary>
    /// 包装成只读字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static ReadOnlyDictionary<TKey, TValue> WrapInReadOnlyDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is null)
            throw new ArgumentNullException(nameof(dictionary));
        return new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }
}

/// <summary>
/// 只读字典工具
/// </summary>
public static class ReadOnlyDicts
{
    #region Empty

    /// <summary>
    /// 获取一个空的只读字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>() => EmptyReadOnlyDictionarySingleton<TKey, TValue>.Instance;

    /// <summary>
    /// 空的只读字典单例
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    private static class EmptyReadOnlyDictionarySingleton<TKey, TValue>
    {
        /// <summary>
        /// 实例
        /// </summary>
        internal static readonly ReadOnlyDictionary<TKey, TValue> Instance = new(new Dictionary<TKey, TValue>());
    }

    #endregion

    #region GetValueOrDefault

    /// <summary>
    /// 获取值或默认值
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <param name="key">键</param>
    /// <param name="valueCalculator">值计算</param>
    public static TValue GetValueOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key,
        Func<TKey, TValue> valueCalculator)
    {
        if (dictionary is not null && dictionary.TryGetValue(key, out var value))
            return value;
        if (valueCalculator is null)
            return default;
        return valueCalculator.Invoke(key);
    }


    /// <summary>
    /// 获取值或默认值
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue GetValueOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        return dictionary is not null && dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// 获取值或默认值
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <param name="key">键</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue GetValueOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary is not null && dictionary.TryGetValue(key, out var value) ? value : default;
    }

    #endregion

    #region GetValueOrDefaultCascading

    /// <summary>
    /// 级联地获取一个值或默认值
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionaryColl">字典集合</param>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue GetValueOrDefaultCascading<TKey, TValue>(IEnumerable<IReadOnlyDictionary<TKey, TValue>> dictionaryColl, TKey key, TValue defaultValue)
    {
        if (dictionaryColl is null)
            throw new ArgumentNullException(nameof(dictionaryColl));
        return TryGetValueCascading(dictionaryColl, key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// 级联地获取一个值或默认值
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionaryColl">字典集合</param>
    /// <param name="key">键</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue GetValueOrDefaultCascading<TKey, TValue>(IEnumerable<IReadOnlyDictionary<TKey, TValue>> dictionaryColl, TKey key)
    {
        return GetValueOrDefaultCascading(dictionaryColl, key, default);
    }

    /// <summary>
    /// 级联地尝试获取一个可选值
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionaryColl">字典集合</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool TryGetValueCascading<TKey, TValue>(IEnumerable<IReadOnlyDictionary<TKey, TValue>> dictionaryColl, TKey key, out TValue value)
    {
        if (dictionaryColl is null)
            throw new ArgumentNullException(nameof(dictionaryColl));
        value = default;
        foreach (var dictionary in dictionaryColl)
            if (dictionary.TryGetValue(key, out value))
                return true;
        return false;
    }

    #endregion
}
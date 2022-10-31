using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Bing.Collections.Internals;

/// <summary>
/// 转换只读字典包装器
/// </summary>
/// <typeparam name="TFromKey">源键类型</typeparam>
/// <typeparam name="TFromValue">源值类型</typeparam>
/// <typeparam name="TToKey">目标键类型</typeparam>
/// <typeparam name="TToValue">目标值类型</typeparam>
internal class CastingReadOnlyDictionaryWrapper<TFromKey, TFromValue, TToKey, TToValue> : IReadOnlyDictionary<TToKey, TToValue>
    where TFromKey : TToKey
    where TFromValue : TToValue
{
    /// <summary>
    /// 源数据
    /// </summary>
    private readonly IReadOnlyDictionary<TFromKey, TFromValue> _source;

    /// <summary>
    /// 初始化一个<see cref="CastingReadOnlyDictionaryWrapper{TFromKey,TFromValue,TToKey,TToValue}"/>类型的实例
    /// </summary>
    /// <param name="source">源数据</param>
    internal CastingReadOnlyDictionaryWrapper(IReadOnlyDictionary<TFromKey, TFromValue> source)
    {
        _source = source ?? throw new ArgumentException(nameof(source));
    }

    /// <summary>
    /// 获取迭代器
    /// </summary>
    public IEnumerator<KeyValuePair<TToKey, TToValue>> GetEnumerator()
    {
        return _source.Select(p => new KeyValuePair<TToKey, TToValue>(p.Key, p.Value)).GetEnumerator();
    }


    /// <summary>
    /// 获取迭代器
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// 计数
    /// </summary>
    public int Count => _source.Count;

    /// <summary>
    /// 是否包含指定键名
    /// </summary>
    /// <param name="key">键名</param>
    public bool ContainsKey(TToKey key) => _source.ContainsKey((TFromKey)key);

    /// <summary>
    /// 尝试获取值
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="value">值</param>
    public bool TryGetValue(TToKey key, out TToValue value)
    {
        var result = _source.TryGetValue((TFromKey)key, out var typedValue);
        value = typedValue;
        return result;
    }

    /// <summary>
    /// 根据键名获取值
    /// </summary>
    /// <param name="key">键名</param>
    public TToValue this[TToKey key] => _source[(TFromKey)key];

    /// <summary>
    /// 获取键名集合
    /// </summary>
    public IEnumerable<TToKey> Keys => _source.Keys.Cast<TToKey>();

    /// <summary>
    /// 获取值集合
    /// </summary>
    public IEnumerable<TToValue> Values => _source.Values.Cast<TToValue>();
}
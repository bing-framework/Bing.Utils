using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bing.Collections
{
    /// <summary>
    /// 字典工具
    /// </summary>
    public static class Dicts
    {
        /// <summary>
        /// 添加或覆盖
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void AddValueOrOverride<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value) => dictionary[key] = value;

        /// <summary>
        /// 添加或更新
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="insertFunc">新增函数</param>
        /// <param name="updateFunc">更新函数</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddValueOrUpdate<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> insertFunc, Func<TKey, TValue, TValue> updateFunc)
        {
            if (insertFunc is null)
                throw new ArgumentNullException(nameof(insertFunc));
            if (updateFunc is null)
                throw new ArgumentNullException(nameof(updateFunc));
            var newValue = dictionary.ContainsKey(key) ? updateFunc(key, dictionary[key]) : insertFunc(key);
            AddValueOrOverride(dictionary, key, newValue);
        }

        /// <summary>
        /// 添加或更新
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="insertFunc">新增函数</param>
        /// <param name="doAct">更新操作</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddValueOrDo<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> insertFunc, Action<TKey, TValue> doAct)
        {
            if (insertFunc is null)
                throw new ArgumentNullException(nameof(insertFunc));
            if (doAct is null)
                throw new ArgumentNullException(nameof(doAct));
            if (dictionary.ContainsKey(key))
                doAct(key, dictionary[key]);
            else
                AddValueOrOverride(dictionary, key, insertFunc(key));
        }

        /// <summary>
        /// 添加。如果不存在则添加
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void AddValueIfNotExist<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                return;
            dictionary[key] = value;
        }

        /// <summary>
        /// 添加。将其它字典添加到当前字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="source">源字典</param>
        /// <param name="other">其它字典</param>
        public static void AddRange<TKey, TValue>(Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> other)
        {
            foreach (var pair in other)
                source.Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// 添加。将键值对集合添加到当前字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="source">源字典</param>
        /// <param name="keyValues">键值对集合</param>
        /// <param name="replaceExisted">是否替换已存在的键值对</param>
        public static void AddRange<TKey, TValue>(Dictionary<TKey, TValue> source, IEnumerable<KeyValuePair<TKey, TValue>> keyValues, bool replaceExisted)
        {
            foreach (var item in keyValues)
            {
                if (source.ContainsKey(item.Key) && replaceExisted)
                {
                    source[item.Key] = item.Value;
                    continue;
                }

                if (!source.ContainsKey(item.Key))
                    source.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// 获取值或默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="valueCalculator">值计算</param>
        public static TValue GetValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueCalculator)
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
        public static TValue GetValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
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
        public static TValue GetValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary is not null && dictionary.TryGetValue(key, out var value) ? value : default;
        }

        /// <summary>
        /// 级联地获取一个值或默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionaryColl">字典集合</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static TValue GetValueOrDefaultCascading<TKey, TValue>(IEnumerable<IDictionary<TKey, TValue>> dictionaryColl, TKey key, TValue defaultValue)
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
        public static TValue GetValueOrDefaultCascading<TKey, TValue>(IEnumerable<IDictionary<TKey, TValue>> dictionaryColl, TKey key)
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
        public static bool TryGetValueCascading<TKey, TValue>(IEnumerable<IDictionary<TKey, TValue>> dictionaryColl, TKey key, out TValue value)
        {
            if (dictionaryColl is null)
                throw new ArgumentNullException(nameof(dictionaryColl));
            value = default;
            foreach (var dictionary in dictionaryColl)
                if (dictionary.TryGetValue(key, out value))
                    return true;
            return false;
        }

        /// <summary>
        /// 获取或添加。如果指定的值不存在，则添加值并返回
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static TValue GetValueOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out var res))
                return res;
            return dictionary[key] = value;
        }

        /// <summary>
        /// 获取或添加。如果指定的值不存在，则添加值并返回
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="newValueCreator">值函数。如果在字典中找不到值，则用于创建值的工厂方法</param>
        public static TValue GetValueOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValueCreator)
        {
            return GetValueOrAdd(dictionary, key, _ => newValueCreator());
        }

        /// <summary>
        /// 获取或添加。如果指定的值不存在，则添加值并返回
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="newValueCreator">值函数。如果在字典中找不到值，则用于创建值的工厂方法</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static TValue GetValueOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> newValueCreator)
        {
            if (newValueCreator is null)
                throw new ArgumentNullException(nameof(newValueCreator));
            if (dictionary.TryGetValue(key, out var res))
                return res;
            return dictionary[key] = newValueCreator(key);
        }

        /// <summary>
        /// 获取或添加一个默认新实例。如果指定的值不存在，则添加一个默认新实例并返回
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        public static TValue GetValueOrAddNewInstance<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            return GetValueOrAdd(dictionary, key, _ => new TValue());
        }

        /// <summary>
        /// 分组为字典
        /// </summary>
        /// <typeparam name="TItem">项类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <param name="list">集合</param>
        /// <param name="keyFunc">键函数</param>
        public static Dictionary<TKey, List<TItem>> GroupByAsDictionary<TItem, TKey>(IEnumerable<TItem> list, Func<TItem, TKey> keyFunc)
        {
            return GroupByAsDictionary(list, keyFunc, x => x);
        }

        /// <summary>
        /// 分组为字典
        /// </summary>
        /// <typeparam name="TItem">项类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="list">集合</param>
        /// <param name="keyFunc">键函数</param>
        /// <param name="valueFunc">值函数</param>
        public static Dictionary<TKey, List<TValue>> GroupByAsDictionary<TItem, TKey, TValue>(IEnumerable<TItem> list, Func<TItem, TKey> keyFunc, Func<TItem, TValue> valueFunc)
        {
            var res = new Dictionary<TKey, List<TValue>>();
            foreach (var item in list)
            {
                var key = keyFunc(item);
                var value = valueFunc(item);
                if (!res.TryGetValue(key, out var values))
                {
                    values = new List<TValue>();
                    res.Add(key, values);
                }
                values.Add(value);
            }
            return res;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void SetValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
    }

    /// <summary>
    /// 字典工具扩展
    /// </summary>
    public static class DictsExtensions
    {
        /// <summary>
        /// 添加。将其它字典添加到当前字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="source">源字典</param>
        /// <param name="other">其它字典</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> other) => Dicts.AddRange(source, other);

        /// <summary>
        /// 添加一组键值对到给定字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="source">源字典</param>
        /// <param name="pair">键值对</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> source, KeyValuePair<TKey, TValue> pair) => source.Add(pair.Key, pair.Value);

        /// <summary>
        /// 添加或覆盖
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddValueOrOverride<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) => Dicts.AddValueOrOverride(dictionary, key, value);

        /// <summary>
        /// 添加或更新
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="insertFunc">新增函数</param>
        /// <param name="updateFunc">更新函数</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddValueOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> insertFunc, Func<TKey, TValue, TValue> updateFunc) =>
            Dicts.AddValueOrUpdate(dictionary, key, insertFunc, updateFunc);

        /// <summary>
        /// 添加或更新
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="insertFunc">新增函数</param>
        /// <param name="doAct">更新操作</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddValueOrDo<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> insertFunc, Action<TKey, TValue> doAct) =>
            Dicts.AddValueOrDo(dictionary, key, insertFunc, doAct);

        /// <summary>
        /// 添加。如果不存在则添加
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddValueIfNotExist<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) => Dicts.AddValueIfNotExist(dictionary, key, value);

        /// <summary>
        /// 获取值或默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="valueCalculator">值计算</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueCalculator) => Dicts.GetValueOrDefault(dictionary, key, valueCalculator);

#if NETFRAMEWORK || NETSTANDARD2_0
        /// <summary>
        /// 获取值或默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) => Dicts.GetValueOrDefault(dictionary, key, defaultValue);

        /// <summary>
        /// 获取值或默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) => Dicts.GetValueOrDefault(dictionary, key);
#endif

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
        public static TValue GetValueOrDefaultCascading<TKey, TValue>(this IEnumerable<IDictionary<TKey, TValue>> dictionaryColl, TKey key, TValue defaultValue) =>
            Dicts.GetValueOrDefaultCascading(dictionaryColl, key, defaultValue);

        /// <summary>
        /// 级联地获取一个值或默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionaryColl">字典集合</param>
        /// <param name="key">键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueOrDefaultCascading<TKey, TValue>(this IEnumerable<IDictionary<TKey, TValue>> dictionaryColl, TKey key) =>
            Dicts.GetValueOrDefaultCascading(dictionaryColl, key);

        /// <summary>
        /// 级联地尝试获取一个可选值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionaryColl">字典集合</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValueCascading<TKey, TValue>(this IEnumerable<IDictionary<TKey, TValue>> dictionaryColl, TKey key, out TValue value) => 
            Dicts.TryGetValueCascading(dictionaryColl, key, out value);

        /// <summary>
        /// 获取或添加。如果指定的值不存在，则添加值并返回
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) =>
            Dicts.GetValueOrAdd(dictionary, key, value);

        /// <summary>
        /// 获取或添加。如果指定的值不存在，则添加值并返回
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="newValueCreator">值函数。如果在字典中找不到值，则用于创建值的工厂方法</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> newValueCreator) => 
            Dicts.GetValueOrAdd(dictionary, key, newValueCreator);

        /// <summary>
        /// 获取或添加一个默认新实例。如果指定的值不存在，则添加一个默认新实例并返回
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueOrAddNewInstance<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new() => 
            Dicts.GetValueOrAddNewInstance(dictionary, key);

        /// <summary>
        /// 设置值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) =>
            Dicts.SetValue(dictionary, key, value);
    }
}
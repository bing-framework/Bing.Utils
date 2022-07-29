using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections.Internals;

namespace Bing.Collections
{
    /// <summary>
    /// 字典转换器
    /// </summary>
    public static class DictConv
    {
        /// <summary>
        /// 转换
        /// </summary>
        /// <typeparam name="TFromKey">源键类型</typeparam>
        /// <typeparam name="TFromValue">源值类型</typeparam>
        /// <typeparam name="TToKey">目标键类型</typeparam>
        /// <typeparam name="TToValue">目标值类型</typeparam>
        /// <param name="source">源数据</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IReadOnlyDictionary<TToKey, TToValue> Cast<TFromKey, TFromValue, TToKey, TToValue>(IReadOnlyDictionary<TFromKey, TFromValue> source)
            where TFromKey : TToKey
            where TFromValue : TToValue
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            return new CastingReadOnlyDictionaryWrapper<TFromKey, TFromValue, TToKey, TToValue>(source);
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="hash">哈希表</param>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Hashtable hash)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            foreach (var item in hash.Keys) 
                dictionary.Add((TKey)item, (TValue)hash[item]);
            return dictionary;
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="source">数据源</param>
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            return ToDictionary(source, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="source">源数据</param>
        /// <param name="equalityComparer">相等比较器</param>
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> equalityComparer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));
            return source.ToDictionary(p => p.Key, p => p.Value, equalityComparer);
        }
    }
}
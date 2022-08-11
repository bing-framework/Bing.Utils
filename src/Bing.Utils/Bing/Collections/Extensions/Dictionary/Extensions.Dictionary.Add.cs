using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Bing.Collections
{
    /// <summary>
    /// 字典(<see cref="IDictionary{TKey,TValue}"/>) 扩展
    /// </summary>
    public static partial class DictionaryExtensions
    {
        /// <summary>
        /// 添加。如果不存在指定键，则添加到当前字典中
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="this">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool AddIfNotContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue value)
        {
            if (!@this.ContainsKey(key))
            {
                @this.Add(key, value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加。如果不存在指定键，则添加到当前字典中
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="this">字典</param>
        /// <param name="key">键</param>
        /// <param name="valueFactory">值函数</param>
        public static bool AddIfNotContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TValue> valueFactory)
        {
            if (!@this.ContainsKey(key))
            {
                @this.Add(key, valueFactory());
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加。如果不存在指定键，则添加到当前字典中
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="this">字典</param>
        /// <param name="key">键</param>
        /// <param name="valueFactory">值函数</param>
        public static bool AddIfNotContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TKey, TValue> valueFactory)
        {
            if (!@this.ContainsKey(key))
            {
                @this.Add(key, valueFactory(key));
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加或更新
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="this">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue value)
        {
            if (!@this.ContainsKey(key))
                @this.Add(new KeyValuePair<TKey, TValue>(key, value));
            else
                @this[key] = value;
            return @this[key];
        }

#if NETSTANDARD2_0
        /// <summary>
        /// 尝试将键值对添加到字典中。如果不存在，则添加；存在，不添加也不抛异常
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="this">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue value)
        {
            if (@this.ContainsKey(key))
                return false;
            @this.Add(key, value);
            return true;
        }
#endif
    }
}

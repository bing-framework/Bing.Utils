﻿using System.Collections;
using Bing.Text;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 字典(<see cref="IDictionary{TKey,TValue}"/>) 扩展
/// </summary>
public static class DictionaryExtensions
{
    #region Sort(字段排序)

    /// <summary>
    /// 对指定的字典进行排序
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    public static IDictionary<TKey, TValue> Sort<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));
        return new SortedDictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// 对指定的字典进行排序
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <param name="comparer">比较器，用于排序字典</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDictionary<TKey, TValue> Sort<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
        IComparer<TKey> comparer)
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));
        if (comparer == null)
            throw new ArgumentNullException(nameof(comparer));
        return new SortedDictionary<TKey, TValue>(dictionary, comparer);
    }

    /// <summary>
    /// 对指定的字典进行排序，根据值元素进行排序
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    public static IDictionary<TKey, TValue> SortByValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) =>
        new SortedDictionary<TKey, TValue>(dictionary).OrderBy(x => x.Value)
            .ToDictionary(x => x.Key, x => x.Value);

    #endregion

    #region ToQueryString(将字典转换成查询字符串)

    /// <summary>
    /// 将字典转换成查询字符串
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    public static string ToQueryString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        if (dictionary == null || !dictionary.Any())
            return string.Empty;
        var sb = new StringBuilder();
        foreach (var item in dictionary)
            sb.Append($"{item.Key.ToString()}={item.Value.ToString()}&");
        sb.TrimEnd("&");
        return sb.ToString();
    }

    #endregion

    #region ToHashTable(将字典转换成哈希表)

    /// <summary>
    /// 将字典转换成哈希表
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    public static Hashtable ToHashTable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        var table = new Hashtable();
        foreach (var item in dictionary)
            table.Add(item.Key, item.Value);
        return table;
    }

    #endregion

    #region Invert(字典颠倒)

    /// <summary>
    /// 对指定字典进行颠倒键值对，创建新字典（值为键，键为值）
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    public static IDictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));
        return dictionary.ToDictionary(x => x.Value, x => x.Key);
    }

    #endregion

    #region EqualsTo(判断两个字典中的元素是否相等)

    /// <summary>
    /// 判断两个字典中的元素是否相等
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="sourceDict">源字典</param>
    /// <param name="targetDict">目标字典</param>
    /// <exception cref="ArgumentNullException">源字典对象为空、目标字典对象为空</exception>
    public static bool EqualsTo<TKey, TValue>(this IDictionary<TKey, TValue> sourceDict,
        IDictionary<TKey, TValue> targetDict)
    {
        if (sourceDict == null)
            throw new ArgumentNullException(nameof(sourceDict), $@"源字典对象不可为空！");
        if (targetDict == null)
            throw new ArgumentNullException(nameof(sourceDict), $@"目标字典对象不可为空！");
        // 长度对比
        if (sourceDict.Count != targetDict.Count)
            return false;
        if (!sourceDict.Any() && !targetDict.Any())
            return true;
        // 深度对比
        var sourceKeyValues = sourceDict.OrderBy(x => x.Key).ToArray();
        var targetKeyValues = targetDict.OrderBy(x => x.Key).ToArray();
        var sourceKeys = sourceKeyValues.Select(x => x.Key);
        var targetKeys = targetKeyValues.Select(x => x.Key);
        var sourceValues = sourceKeyValues.Select(x => x.Value);
        var targetValues = targetKeyValues.Select(x => x.Value);
        if (sourceKeys.EqualsTo(targetKeys) && sourceValues.EqualsTo(targetValues))
            return true;
        return false;
    }

    #endregion

    #region FillFormDataStream(填充表单信息的Stream)

    /// <summary>
    /// 填充表单信息的Stream
    /// </summary>
    /// <param name="formData">表单数据</param>
    /// <param name="stream">流</param>
    public static void FillFormDataStream(this IDictionary<string, string> formData, Stream stream)
    {
        var dataStr = ToQueryString(formData);
        var formDataBytes = formData == null ? new byte[0] : Encoding.UTF8.GetBytes(dataStr);
        stream.Write(formDataBytes, 0, formDataBytes.Length);
        stream.Seek(0, SeekOrigin.Begin);// 设置指针读取位置
    }

    /// <summary>
    /// 填充表单信息的Stream
    /// </summary>
    /// <param name="formData">表单数据</param>
    /// <param name="stream">流</param>
    public static async Task FillFormDataStreamAsync(this IDictionary<string, string> formData, Stream stream)
    {
        var dataStr = ToQueryString(formData);
        var formDataBytes = formData == null ? new byte[0] : Encoding.UTF8.GetBytes(dataStr);
        await stream.WriteAsync(formDataBytes, 0, formDataBytes.Length);
        stream.Seek(0, SeekOrigin.Begin);// 设置指针读取位置
    }

    #endregion

}
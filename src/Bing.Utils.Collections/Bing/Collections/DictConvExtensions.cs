using System.Collections;

namespace Bing.Collections;

/// <summary>
/// 字典转换器扩展
/// </summary>
public static class DictConvExtensions
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyDictionary<TToKey, TToValue> Cast<TFromKey, TFromValue, TToKey, TToValue>(this IReadOnlyDictionary<TFromKey, TFromValue> source)
        where TFromKey : TToKey
        where TFromValue : TToValue =>
        DictConv.Cast<TFromKey, TFromValue, TToKey, TToValue>(source);

    /// <summary>
    /// 转换为字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="hash">哈希表</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this Hashtable hash) => DictConv.ToDictionary<TKey, TValue>(hash);

    /// <summary>
    /// 转换为字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">数据源</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) => DictConv.ToDictionary(source);

    /// <summary>
    /// 转换为字典
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源数据</param>
    /// <param name="equalityComparer">相等比较器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> equalityComparer) =>
        DictConv.ToDictionary(source, equalityComparer);

    /// <summary>
    /// 转换为元祖
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Tuple<TKey, TValue>> ToTuple<TKey, TValue>(IDictionary<TKey, TValue> source) => DictConv.ToTuple(source);

    /// <summary>
    /// 转换为有序集合
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <param name="source">源数据</param>
    /// <param name="asc">是否升序</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<KeyValuePair<TKey, int>> ToSortedArrayByValue<TKey>(Dictionary<TKey, int> source, bool asc = true) =>
        DictConv.ToSortedArrayByValue(source, asc);

    /// <summary>
    /// 转换为有序集合
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="source">源数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<KeyValuePair<TKey, TValue>> ToSortedArrayByKey<TKey, TValue>(this Dictionary<TKey, TValue> source)
        where TKey : IComparable<TKey> =>
        DictConv.ToSortedArrayByKey(source);
}
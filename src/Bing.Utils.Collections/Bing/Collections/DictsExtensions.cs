namespace Bing.Collections;

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
    /// 添加。如果不存在则添加
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <param name="key">键</param>
    /// <param name="valueCalculator">值计算</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddValueIfNotExist<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueCalculator) => Dicts.AddValueIfNotExist(dictionary, key, valueCalculator);

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

#if NETSTANDARD2_0
    /// <summary>
    /// 尝试将键值对添加到字典中。如果不存在，则添加；存在，不添加也不抛异常
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="this">字典</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> @this, TKey key, TValue value)
    {
        if (@this.ContainsKey(key))
            return false;
        @this.Add(key, value);
        return true;
    }
#endif
}
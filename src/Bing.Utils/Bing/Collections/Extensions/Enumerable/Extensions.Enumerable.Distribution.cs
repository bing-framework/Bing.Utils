// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 可枚举类型(<see cref="IEnumerable{T}"/>) 轮询分配扩展
/// </summary>
public static partial class BingEnumerableExtensions
{
    /// <summary>
    /// 将源集合元素按容器顺序轮询分配到多个容器。
    /// </summary>
    /// <typeparam name="T">源集合元素类型。</typeparam>
    /// <typeparam name="TKey">容器键类型。</typeparam>
    /// <param name="source">要分配的源集合。</param>
    /// <param name="keys">容器键集合。</param>
    /// <returns>包含所有容器键及其分配元素列表的字典。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="keys"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">当容器键集合为空、包含 <c>null</c> 键或包含重复键时抛出。</exception>
    /// <remarks>
    /// 返回字典按容器键的输入顺序创建；每个列表保持源集合中的相对顺序。空源集合仍返回所有容器的空列表。
    /// 此方法会立即枚举源集合并创建新的列表，不维护共享状态，也不承诺并发安全或按业务成本均衡。
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = new[] { 1, 2, 3, 4, 5 }.DistributeRoundRobin(new[] { "A", "B" });
    /// // A: [1, 3, 5], B: [2, 4]
    /// </code>
    /// </example>
    public static Dictionary<TKey, List<T>> DistributeRoundRobin<T, TKey>(this IEnumerable<T> source, IEnumerable<TKey> keys)
        where TKey : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        var result = new Dictionary<TKey, List<T>>();
        var orderedKeys = new List<TKey>();
        foreach (var key in keys)
        {
            if (key is null)
                throw new ArgumentException("容器键不能为 null。", nameof(keys));
            if (result.ContainsKey(key))
                throw new ArgumentException("容器键不能重复。", nameof(keys));
            result.Add(key, new List<T>());
            orderedKeys.Add(key);
        }
        if (orderedKeys.Count == 0)
            throw new ArgumentException("容器键集合不能为空。", nameof(keys));

        var index = 0;
        foreach (var item in source)
        {
            result[orderedKeys[index]].Add(item);
            index = (index + 1) % orderedKeys.Count;
        }
        return result;
    }
}
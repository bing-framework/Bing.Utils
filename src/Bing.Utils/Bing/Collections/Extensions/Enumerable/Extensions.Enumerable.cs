
// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 可枚举类型(<see cref="IEnumerable{T}"/>) 扩展
/// </summary>
public static partial class BingEnumerableExtensions
{
    /// <summary>
    /// 将 <see cref="IEnumerable{T}"/> 按指定大小分组（分块）
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">原始集合</param>
    /// <param name="chunkSize">每个分组的最大元素数</param>
    /// <returns>分组后的集合序列，每组为一个 <see cref="List{T}"/></returns>
    /// <exception cref="ArgumentNullException">当 source 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 chunkSize 小于等于 0 时抛出</exception>
    public static IEnumerable<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source), "源集合对象不可为空！");
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "分块大小必须大于0！");
        if (source is IList<T> list)
        {
            for (var i = 0; i < list.Count; i += chunkSize)
            {
                var count = Math.Min(chunkSize, list.Count - i);
                var chunk = new List<T>(count);
                for (var j = 0; j < count; j++)
                    chunk.Add(list[i + j]);
                yield return chunk;
            }
            yield break;
        }
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var chunk = new List<T>(chunkSize) { enumerator.Current };
            for (var i = 1; i < chunkSize && enumerator.MoveNext(); i++)
                chunk.Add(enumerator.Current);
            yield return chunk;
        }
    }
    
}
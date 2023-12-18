// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 集合(<see cref="ICollection{T}"/>) 扩展
/// </summary>
public static partial class BingCollectionExtensions
{
    /// <summary>
    /// 添加批量项。添加多个元素到集合中
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="collection">集合</param>
    /// <param name="items">元素集合</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns>添加成功的元素数量</returns>
    public static int AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));
        if (items == null)
            throw new ArgumentNullException(nameof(items));
        var addedCount = 0;
        foreach (var item in items)
        {
            collection.Add(item);
            addedCount++;
        }

        return addedCount;
    }
}
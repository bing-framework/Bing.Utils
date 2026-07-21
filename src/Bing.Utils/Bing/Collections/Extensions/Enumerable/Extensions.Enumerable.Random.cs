using Bing.Helpers;

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 可枚举类型(<see cref="IEnumerable{T}"/>) 随机元素扩展
/// </summary>
public static partial class BingEnumerableExtensions
{
    /// <summary>
    /// 从集合中随机获取一个元素。
    /// </summary>
    /// <typeparam name="T">集合元素类型。</typeparam>
    /// <param name="source">源集合。</param>
    /// <param name="random">随机数生成器。为 <c>null</c> 时使用线程安全的默认生成器。</param>
    /// <returns>随机选中的元素。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当 <paramref name="source"/> 为空集合时抛出。</exception>
    /// <remarks>
    /// <see cref="IList{T}"/> 和 <see cref="IReadOnlyList{T}"/> 使用索引随机访问；其他集合使用蓄水池抽样，
    /// 仅枚举一次且不缓存整个集合。未传入 <paramref name="random"/> 时，默认随机数生成器可安全用于并发调用。
    /// 调用方传入同一个 <see cref="Random"/> 实例时，应自行保证其并发访问安全。
    /// </remarks>
    /// <example>
    /// <code>
    /// var value = new[] { 1, 2, 3 }.RandomElement(new Random(1));
    /// </code>
    /// </example>
    public static T RandomElement<T>(this IEnumerable<T> source, Random random = null)
    {
        if (!source.TryRandomElement(out var result, random))
            throw new InvalidOperationException("源集合不包含元素。");
        return result;
    }

    /// <summary>
    /// 尝试从集合中随机获取一个元素。
    /// </summary>
    /// <typeparam name="T">集合元素类型。</typeparam>
    /// <param name="source">源集合。</param>
    /// <param name="result">随机选中的元素；当集合为空时为默认值。</param>
    /// <param name="random">随机数生成器。为 <c>null</c> 时使用线程安全的默认生成器。</param>
    /// <returns>集合包含元素时返回 <c>true</c>；集合为空时返回 <c>false</c>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <c>null</c> 时抛出。</exception>
    /// <remarks>
    /// <see cref="IList{T}"/> 和 <see cref="IReadOnlyList{T}"/> 使用索引随机访问；其他集合使用蓄水池抽样，
    /// 仅枚举一次且不缓存整个集合。未传入 <paramref name="random"/> 时，默认随机数生成器可安全用于并发调用。
    /// 调用方传入同一个 <see cref="Random"/> 实例时，应自行保证其并发访问安全。
    /// </remarks>
    public static bool TryRandomElement<T>(this IEnumerable<T> source, out T result, Random random = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        random ??= Encrypt.Random;
        if (source is IList<T> list)
        {
            if (list.Count == 0)
            {
                result = default;
                return false;
            }
            result = list[random.Next(list.Count)];
            return true;
        }
        if (source is IReadOnlyList<T> readOnlyList)
        {
            if (readOnlyList.Count == 0)
            {
                result = default;
                return false;
            }
            result = readOnlyList[random.Next(readOnlyList.Count)];
            return true;
        }

        var count = 0;
        result = default;
        foreach (var item in source)
        {
            count++;
            if (random.Next(count) == 0)
                result = item;
        }
        return count > 0;
    }
}
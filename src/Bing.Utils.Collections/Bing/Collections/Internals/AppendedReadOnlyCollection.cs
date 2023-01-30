using System.Collections;

namespace Bing.Collections.Internals;

/// <summary>
/// 可追加的只读集合
/// </summary>
/// <typeparam name="T">类型</typeparam>
internal class AppendedReadOnlyCollection<T> : IReadOnlyCollection<T>
{
    /// <summary>
    /// 集合
    /// </summary>
    private readonly IEnumerable<T> _enumerable;

    /// <summary>
    /// 初始化一个<see cref="AppendedReadOnlyCollection{T}"/>类型的实例
    /// </summary>
    /// <param name="root">只读集合</param>
    /// <param name="item">追加项</param>
    internal AppendedReadOnlyCollection(IReadOnlyCollection<T> root, T item)
    {
        _enumerable = ReadOnlyCollsHelper.Append(root, item);
        Count = root.Count + 1;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int Count { get; }
}
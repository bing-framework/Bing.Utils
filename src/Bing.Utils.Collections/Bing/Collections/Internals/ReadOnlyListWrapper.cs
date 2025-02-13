using System.Collections;

namespace Bing.Collections.Internals;

/// <summary>
/// 只读列表包装器
/// </summary>
/// <typeparam name="T">类型</typeparam>
internal sealed class ReadOnlyListWrapper<T> : IList<T>
{
    /// <summary>
    /// 只读列表
    /// </summary>
    private readonly IReadOnlyList<T> _list;

    /// <summary>
    /// 初始化一个<see cref="ReadOnlyListWrapper{T}"/>类型的实例
    /// </summary>
    /// <param name="list">只读列表</param>
    internal ReadOnlyListWrapper(IReadOnlyList<T> list) => _list = list ?? throw new ArgumentNullException(nameof(list));

    /// <summary>
    /// 抛出 <see cref="NotSupportedException"/>
    /// </summary>
    /// <param name="member">成员名称</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    private object ThrowNotSupportedException([CallerMemberName] string member = "")
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        throw new NotSupportedException($"{GetType().FullName} does not support the {member} member.");
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public void Add(T item) => ThrowNotSupportedException();

    /// <inheritdoc />
    public void Clear() => ThrowNotSupportedException();

    /// <inheritdoc />
    public bool Contains(T item) => _list.Contains(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex) => Buffer.BlockCopy(this.ToArray(), 0, array, arrayIndex, Count);

    /// <inheritdoc />
    public bool Remove(T item) => (bool)ThrowNotSupportedException();

    /// <inheritdoc />
    public int Count => _list.Count;

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        return _list.Select((t, i) => new { Item = t, Index = i }).FirstOrDefault(p => comparer.Equals(p.Item, item))?.Index ?? -1;
    }

    /// <inheritdoc />
    public void Insert(int index, T item) => ThrowNotSupportedException();

    /// <inheritdoc />
    public void RemoveAt(int index) => ThrowNotSupportedException();

    /// <inheritdoc />
    public T this[int index]
    {
        get => _list[index];
        set => ThrowNotSupportedException();
    }
}
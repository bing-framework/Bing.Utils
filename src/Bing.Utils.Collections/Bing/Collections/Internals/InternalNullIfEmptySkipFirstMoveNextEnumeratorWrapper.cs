using System.Collections;

namespace Bing.Collections.Internals;

/// <summary>
/// 迭代器包装。如果存在可空元素则可跳过
/// </summary>
/// <typeparam name="T">类型</typeparam>
internal class InternalNullIfEmptySkipFirstMoveNextEnumeratorWrapper<T> : IEnumerator<T>
{
    /// <summary>
    /// 是否已跳过
    /// </summary>
    private bool _skipped;

    /// <summary>
    /// 内部迭代器
    /// </summary>
    private readonly IEnumerator<T> _inner;

    /// <summary>
    /// 初始化一个<see cref="InternalNullIfEmptySkipFirstMoveNextEnumeratorWrapper{T}"/>类型的实例
    /// </summary>
    /// <param name="inner">迭代器</param>
    public InternalNullIfEmptySkipFirstMoveNextEnumeratorWrapper(IEnumerator<T> inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public T Current => _inner.Current;

    /// <inheritdoc />
    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_skipped)
        {
            _skipped = true;
            return true;
        }
        return _inner.MoveNext();
    }

    /// <inheritdoc />
    public void Reset() => _inner.Reset();

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
using System;
using System.Collections;
using System.Collections.Generic;

namespace Bing.Collections.Internals
{
    /// <summary>
    /// 一次性使用集合
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    internal class InternalSingleUseEnumerable<T> : IEnumerable<T>
    {
        /// <summary>
        /// 迭代器对象锁
        /// </summary>
        private readonly object _enumeratorLock = new object();

        /// <summary>
        /// 迭代器
        /// </summary>
        private IEnumerator<T> _enumerator;

        /// <summary>
        /// 初始化一个<see cref="InternalSingleUseEnumerable{T}"/>类型的实例
        /// </summary>
        /// <param name="enumerator">迭代器</param>
        public InternalSingleUseEnumerable(IEnumerator<T> enumerator) => _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            lock (_enumeratorLock)
            {
                if (_enumerator is null)
                    throw new InvalidOperationException($"{nameof(GetEnumerator)} may only be called once on this instance of {nameof(InternalSingleUseEnumerable<T>)}.");
                var enumeratorCopy = _enumerator;
                _enumerator = null;
                return enumeratorCopy;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
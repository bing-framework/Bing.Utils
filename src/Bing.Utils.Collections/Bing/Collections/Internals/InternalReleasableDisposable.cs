using System;

namespace Bing.Collections.Internals
{
    /// <summary>
    /// 内部释放
    /// </summary>
    /// <typeparam name="T">可释放对象类型</typeparam>
    internal class InternalReleasableDisposable<T> : IDisposable where T : class, IDisposable
    {
        /// <summary>
        /// 初始化一个<see cref="InternalReleasableDisposable{T}"/>类型的实例
        /// </summary>
        /// <param name="disposable">可释放对象</param>
        public InternalReleasableDisposable(T disposable) => Disposable = disposable;

        /// <summary>
        /// 可释放对象
        /// </summary>
        public T Disposable { get; private set; }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release() => Disposable = null;

        /// <inheritdoc />
        public void Dispose()
        {
            using (Disposable) { }
        }
    }
}
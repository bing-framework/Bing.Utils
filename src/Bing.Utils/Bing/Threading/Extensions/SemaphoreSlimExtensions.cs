// ReSharper disable once CheckNamespace
namespace Bing.Threading;

/// <summary>
/// 信号量(<see cref="SemaphoreSlim"/>) 扩展
/// </summary>
public static class SemaphoreSlimExtensions
{
    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim)
    {
        await semaphoreSlim.WaitAsync();
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="millisecondsTimeout">超时时间。单位：毫秒数</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, int millisecondsTimeout)
    {
        if(await semaphoreSlim.WaitAsync(millisecondsTimeout))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="millisecondsTimeout">超时时间。单位：毫秒数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if(await semaphoreSlim.WaitAsync(millisecondsTimeout, cancellationToken))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, TimeSpan timeout)
    {
        if(await semaphoreSlim.WaitAsync(timeout))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if(await semaphoreSlim.WaitAsync(timeout, cancellationToken))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim)
    {
        semaphoreSlim.Wait();
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken)
    {
        semaphoreSlim.Wait(cancellationToken);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="millisecondsTimeout">超时时间。单位：毫秒数</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, int millisecondsTimeout)
    {
        if (semaphoreSlim.Wait(millisecondsTimeout))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="millisecondsTimeout">超时时间。单位：毫秒数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (semaphoreSlim.Wait(millisecondsTimeout, cancellationToken))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, TimeSpan timeout)
    {
        if (semaphoreSlim.Wait(timeout))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (semaphoreSlim.Wait(timeout, cancellationToken))
            return GetDispose(semaphoreSlim);
        throw new TimeoutException();
    }

    /// <summary>
    /// 获取可释放对象
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    private static IDisposable GetDispose(this SemaphoreSlim semaphoreSlim)
    {
        return new DisposeAction<SemaphoreSlim>(static (semaphoreSlim) =>
        {
            semaphoreSlim.Release();
        }, semaphoreSlim);
    }
}
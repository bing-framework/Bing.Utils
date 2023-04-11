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
        await semaphoreSlim.WaitAsync(millisecondsTimeout);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="millisecondsTimeout">超时时间。单位：毫秒数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        await semaphoreSlim.WaitAsync(millisecondsTimeout, cancellationToken);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, TimeSpan timeout)
    {
        await semaphoreSlim.WaitAsync(timeout);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphoreSlim, TimeSpan timeout, CancellationToken cancellationToken)
    {
        await semaphoreSlim.WaitAsync(timeout, cancellationToken);
        return GetDispose(semaphoreSlim);
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
        semaphoreSlim.Wait(millisecondsTimeout);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="millisecondsTimeout">超时时间。单位：毫秒数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        semaphoreSlim.Wait(millisecondsTimeout, cancellationToken);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, TimeSpan timeout)
    {
        semaphoreSlim.Wait(timeout);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static IDisposable Lock(this SemaphoreSlim semaphoreSlim, TimeSpan timeout, CancellationToken cancellationToken)
    {
        semaphoreSlim.Wait(timeout, cancellationToken);
        return GetDispose(semaphoreSlim);
    }

    /// <summary>
    /// 获取可释放对象
    /// </summary>
    /// <param name="semaphoreSlim">信号量</param>
    private static IDisposable GetDispose(this SemaphoreSlim semaphoreSlim)
    {
        return new DisposeAction(semaphoreSlim.Dispose);
    }
}
namespace Bing.Reflection;

/// <summary>
/// 锁扩展
/// </summary>
public static class LockExtensions
{
    /// <summary>
    /// 对指定的资源进行加锁，然后执行委托
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="action">操作</param>
    public static void LockAndRun(this object source, Action action)
    {
        lock (source)
            action();
    }

    /// <summary>
    /// 对指定的资源进行加锁，然后执行委托
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="action">操作</param>
    public static void LockAndRun<T>(this T source, Action<T> action)
    {
        lock (source)
            action(source);
    }

    /// <summary>
    /// 对指定的资源进行加锁，执行委托并返回结果
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="func">操作</param>
    public static TResult LockAndReturn<TResult>(this object source, Func<TResult> func)
    {
        lock (source)
            return func();
    }

    /// <summary>
    /// 对指定的资源进行加锁，执行委托并返回结果
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="func">操作</param>
    public static TResult LockAndReturn<T, TResult>(this T source, Func<T, TResult> func)
    {
        lock (source)
            return func(source);
    }
}
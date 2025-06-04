namespace Bing;

/// <summary>
/// 提供一个异步释放操作的封装，实现 <see cref="IAsyncDisposable"/> 接口，
/// 可在对象异步释放时执行自定义的异步操作。
/// </summary>
/// <remarks>
/// <para>此类是 <see cref="DisposeAction"/> 的异步版本，专门用于需要异步清理资源的场景。</para>
/// <para>主要用途：</para>
/// <list type="bullet">
///     <li>在 await using 语句中执行临时的异步操作并确保操作被清理</li>
///     <li>创建临时的异步资源管理封装</li>
///     <li>实现自定义的异步资源释放策略</li>
///     <li>为实现 IAsyncDisposable 接口的类提供灵活的异步释放行为</li>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // 基本用法示例
/// await using (var asyncDisposeFunc = new AsyncDisposeFunc(async () => 
/// {
///     // 执行一些异步清理操作
///     await CleanupResourcesAsync();
///     Console.WriteLine("异步资源已清理");
/// }))
/// {
///     // 执行一些需要资源的操作
///     Console.WriteLine("正在使用资源");
/// } // 当 await using 块结束时，将执行异步清理操作
/// 
/// // 创建临时异步锁定机制
/// public async ValueTask&lt;IAsyncDisposable> LockResourceAsync()
/// {
///     await _asyncLock.WaitAsync();
///     return new AsyncDisposeFunc(async () => 
///     {
///         await Task.Delay(100); // 模拟一些异步清理工作
///         _asyncLock.Release();
///     });
/// }
/// </code>
/// </example>
public class AsyncDisposeFunc : IAsyncDisposable
{
    /// <summary>
    /// 在对象异步释放时要执行的异步操作
    /// </summary>
    private readonly Func<Task> _func;

    /// <summary>
    /// 初始化一个<see cref="AsyncDisposeFunc"/>类型的实例
    /// </summary>
    /// <param name="func">在对象异步释放时要执行的异步操作</param>
    /// <exception cref="ArgumentNullException">当 func 参数为 null 时抛出</exception>
    public AsyncDisposeFunc(Func<Task> func)
    {
        _func = func ?? throw new ArgumentNullException(nameof(func));
    }

    /// <summary>
    /// 异步释放资源并执行注册的异步操作
    /// </summary>
    /// <returns>表示异步释放操作的 <see cref="ValueTask"/></returns>
    /// <remarks>
    /// 当对象被 await using 语句或手动调用 DisposeAsync 方法时，
    /// 将执行构造函数中提供的异步 func 委托。
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await _func();
    }
}
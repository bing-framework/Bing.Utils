namespace Bing;

/// <summary>
/// 提供一个轻量级的释放操作封装，实现 <see cref="IDisposable"/> 接口，
/// 可在对象释放时执行自定义操作。
/// </summary>
/// /// <remarks>
/// <para>此类主要用于以下场景：</para>
/// <list type="bullet">
///     <li>在 using 语句中执行临时操作并确保操作被清理</li>
///     <li>创建临时的资源管理封装</li>
///     <li>实现自定义的资源释放策略</li>
///     <li>为不直接支持 IDisposable 的对象提供释放行为</li>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // 基本用法示例
/// using (var disposeAction = new DisposeAction(() => Console.WriteLine("资源已清理")))
/// {
///     // 执行一些需要资源的操作
///     Console.WriteLine("正在使用资源");
/// } // 当 using 块结束时，将输出 "资源已清理"
/// 
/// // 创建临时锁定机制
/// public IDisposable LockResource()
/// {
///     _lock.Enter();
///     return new DisposeAction(() => _lock.Exit());
/// }
/// </code>
/// </example>
public class DisposeAction : IDisposable
{
    /// <summary>
    /// 在对象释放时要执行的操作
    /// </summary>
    private readonly Action _action;

    /// <summary>
    /// 初始化一个<see cref="DisposeAction"/>类型的实例
    /// </summary>
    /// <param name="action">操作</param>
    public DisposeAction(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));

    /// <summary>
    /// 释放资源并执行注册的操作
    /// </summary>
    /// <remarks>
    /// 当对象被 using 语句或手动调用 Dispose 方法时，将执行构造函数中提供的 action 委托。
    /// </remarks>
    public void Dispose() => _action();
}

/// <summary>
/// 提供一个带参数的轻量级释放操作封装，实现 <see cref="IDisposable"/> 接口，
/// 可在对象释放时执行带参数的自定义操作。
/// </summary>
/// <typeparam name="T">操作参数的类型</typeparam>
/// <remarks>
/// <para>此类是 <see cref="DisposeAction"/> 的泛型版本，允许在操作中传递额外的参数。</para>
/// <para>主要用于以下场景：</para>
/// <list type="bullet">
///     <li>需要释放特定对象的资源</li>
///     <li>创建支持上下文的资源管理器</li>
///     <li>为包含状态的对象提供清理操作</li>
///     <li>实现特定于类型的资源释放策略</li>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // 使用示例：释放数据库连接
/// var connection = new SqlConnection(connectionString);
/// connection.Open();
/// 
/// // 创建一个在 Dispose 时会关闭连接的对象
/// using (var connectionCloser = new DisposeAction&lt;SqlConnection>(conn => conn.Close(), connection))
/// {
///     // 使用数据库连接进行操作
///     // ...
/// } // 当 using 块结束时，连接将被关闭
/// 
/// // SemaphoreSlim 锁定示例
/// public static IDisposable Lock(this SemaphoreSlim semaphore)
/// {
///     semaphore.Wait();
///     return new DisposeAction&lt;SemaphoreSlim>(s => s.Release(), semaphore);
/// }
/// </code>
/// </example>
public class DisposeAction<T> : IDisposable
{
    /// <summary>
    /// 在对象释放时要执行的操作
    /// </summary>
    private readonly Action<T> _action;

    /// <summary>
    /// 要传递给操作的参数
    /// </summary>
    private readonly T _parameter;

    /// <summary>
    /// 初始化一个<see cref="DisposeAction{T}"/>类型的实例
    /// </summary>
    /// <param name="action">在对象释放时要执行的操作</param>
    /// <param name="parameter">要传递给操作的参数</param>
    /// <exception cref="ArgumentNullException">当 action 参数为 null 时抛出</exception>
    public DisposeAction(Action<T> action, T parameter)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _parameter = parameter;
    }

    /// <summary>
    /// 释放资源并使用指定参数执行注册的操作
    /// </summary>
    /// <remarks>
    /// 当对象被 using 语句或手动调用 Dispose 方法时，将执行构造函数中提供的 action 委托，
    /// 并将 parameter 参数传递给它。如果参数为 null，则不执行操作。
    /// </remarks>
    public void Dispose()
    {
        if (_parameter != null)
            _action(_parameter);
    }
}
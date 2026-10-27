namespace Bing.Threading;

/// <summary>
/// 让同一键的重叠调用共享一次异步执行。
/// </summary>
/// <typeparam name="TKey">请求键类型，不允许 null 键。</typeparam>
/// <typeparam name="TValue">共享结果类型。</typeparam>
/// <remarks>
/// 完成结果不缓存。调用方取消仅退出自己的等待；工厂收到组件生命周期令牌。
/// 所有等待者退出也不会中断工厂。引用类型结果由等待者共享，建议返回不可变对象。
/// 工厂必须避免等待同实例同键的 RunAsync，否则将产生循环等待。
/// </remarks>
public sealed class SingleFlight<TKey, TValue>
{
    /// <summary>
    /// 保护共享任务的登记、发布和回收。
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// 尚未发布完成结果的共享任务。
    /// </summary>
    private readonly Dictionary<TKey, TaskCompletionSource<TValue>> _inFlight;

    /// <summary>
    /// 构造时固定的执行函数。
    /// </summary>
    private readonly Func<TKey, CancellationToken, Task<TValue>> _factory;

    /// <summary>
    /// 由组件所有者控制并传递给工厂的生命周期令牌。
    /// </summary>
    private readonly CancellationToken _lifetimeCancellationToken;

    /// <summary>
    /// 初始化共享执行组件。
    /// </summary>
    /// <param name="factory">执行函数；不得返回 null 任务，超时由函数自身配置。</param>
    /// <param name="comparer">键比较器，null 使用类型默认比较器。</param>
    /// <param name="lifetimeCancellationToken">控制新执行和工厂生命周期的令牌；工厂需要协作响应取消。</param>
    /// <exception cref="ArgumentNullException">执行函数为 null。</exception>
    public SingleFlight(Func<TKey, CancellationToken, Task<TValue>> factory,
        IEqualityComparer<TKey> comparer = null, CancellationToken lifetimeCancellationToken = default)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _inFlight = new Dictionary<TKey, TaskCompletionSource<TValue>>(comparer);
        _lifetimeCancellationToken = lifetimeCancellationToken;
    }

    /// <summary>
    /// 获取尚未发布结果的键数量。
    /// </summary>
    internal int InFlightCount { get { lock (_gate) return _inFlight.Count; } }

    /// <summary>
    /// 创建或加入指定键的共享执行。
    /// </summary>
    /// <param name="key">请求键，不可为 null。</param>
    /// <param name="cancellationToken">仅控制本调用的等待；预取消不会创建执行。</param>
    /// <returns>共享结果；工厂异常保持原异常，取消保持任务取消状态。</returns>
    /// <exception cref="ArgumentNullException">请求键为 null。</exception>
    /// <exception cref="OperationCanceledException">本次等待、生命周期或工厂被取消。</exception>
    /// <exception cref="InvalidOperationException">工厂返回 null 任务。</exception>
    public Task<TValue> RunAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<TValue>(cancellationToken);
        if (_lifetimeCancellationToken.IsCancellationRequested) return Task.FromCanceled<TValue>(_lifetimeCancellationToken);

        TaskCompletionSource<TValue> completion;
        bool start;
        lock (_gate)
        {
            start = !_inFlight.TryGetValue(key, out completion);
            if (start)
            {
                completion = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
                _inFlight.Add(key, completion);
            }
        }
        // 用户代码永远在登记表锁外启动，包括异步函数首个 await 之前的部分。
        if (start) _ = ExecuteAsync(key, completion);
        return cancellationToken.CanBeCanceled
            ? SelectWaiterAsync(completion.Task, cancellationToken).Unwrap()
            : completion.Task;
    }

    /// <summary>
    /// 执行工厂并原子发布终态及回收登记。
    /// </summary>
    /// <param name="key">共享执行键。</param>
    /// <param name="completion">本次执行的完成源。</param>
    private async Task ExecuteAsync(TKey key, TaskCompletionSource<TValue> completion)
    {
        TValue result = default;
        Exception error = null;
        Task<TValue> factoryTask = null;
        try
        {
            _lifetimeCancellationToken.ThrowIfCancellationRequested();
            factoryTask = _factory(key, _lifetimeCancellationToken);
            if (factoryTask == null) throw new InvalidOperationException("SingleFlight 执行函数不能返回 null 任务。");
            result = await factoryTask.ConfigureAwait(false);
        }
        catch (Exception exception) { error = exception; }

        lock (_gate)
        {
            if (_inFlight.TryGetValue(key, out var active) && ReferenceEquals(active, completion)) _inFlight.Remove(key);
            if (error is OperationCanceledException canceled && (factoryTask == null || factoryTask.IsCanceled))
                completion.TrySetCanceled(canceled.CancellationToken);
            else if (error != null)
            {
                if (factoryTask?.IsFaulted == true) completion.TrySetException(factoryTask.Exception.InnerExceptions);
                else completion.TrySetException(error);
                // 即使全部调用方已取消等待，也观察共享任务异常；后续 await 仍能收到原异常。
                _ = completion.Task.Exception;
            }
            else completion.TrySetResult(result);
        }
    }

    /// <summary>
    /// 提供不取消共享任务的独立可取消等待。
    /// </summary>
    /// <param name="shared">共享任务。</param>
    /// <param name="token">本次等待的令牌。</param>
    /// <returns>获胜的任务，由 Unwrap 保留故障或取消终态。</returns>
    private static async Task<Task<TValue>> SelectWaiterAsync(Task<TValue> shared, CancellationToken token)
    {
        var canceled = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (token.Register(() => canceled.TrySetCanceled(token)))
        {
            return await Task.WhenAny(shared, canceled.Task).ConfigureAwait(false);
        }
    }
}

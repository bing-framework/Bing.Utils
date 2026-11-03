using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Bing.Exceptions;

/// <summary>
/// Bing.Utils 异常 API 的独立消费者。
/// </summary>
/// <remarks>
/// 编译公开调用并验证受控上下文中的异步行为。
/// </remarks>
internal static class Program
{
    /// <summary>
    /// 执行消费者 API 和受控上下文检查。
    /// </summary>
    /// <param name="args">启动参数；首项为 context 时运行上下文检查。</param>
    /// <returns>全部检查通过时返回 0，出现未处理错误时返回 1。</returns>
    private static async Task<int> Main(string[] args)
    {
        Console.InputEncoding = new System.Text.UTF8Encoding(false);
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);

        try
        {
            if (args.Length > 0 && string.Equals(args[0], "context", StringComparison.OrdinalIgnoreCase))
            {
                await RunContextChecksAsync();
                Console.WriteLine("Exceptions context checks passed.");
                return 0;
            }

            var assembly = typeof(Try).Assembly;
            var framework = assembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
                .Cast<System.Runtime.Versioning.TargetFrameworkAttribute>().Single().FrameworkName;
            Console.WriteLine($"Loaded asset: {assembly.Location}; target={framework}");
            await RunConsumerChecksAsync();
            Console.WriteLine("Exceptions consumer checks passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    /// <summary>
    /// 编译并执行异常 API 的公开调用检查。
    /// </summary>
    private static async Task RunConsumerChecksAsync()
    {
        var original = new InvalidOperationException("consumer-inner");
        var outer = new Exception("consumer-outer", original);

        Require(ReferenceEquals(outer.Unwrap<InvalidOperationException>(), original), "Generic extension Unwrap returned a different exception.");
        Require(ReferenceEquals(ExceptionHelper.Unwrap<InvalidOperationException>(outer), original), "Static Unwrap returned a different exception.");
        Require(ReferenceEquals(ExceptionHelperExtensions.Unwrap<InvalidOperationException>(outer), original), "Extension-class static Unwrap returned a different exception.");

        var value = Try.LiftValue(7);
        var success = (Success<int>)value;
        var failure = (Failure<int>)Try.LiftException<int>(original, "consumer-cause");
        ExerciseTapSurface(value, success, failure);

        var aggregate = new AggregateException(
            new ArgumentException("first-branch"),
            new AggregateException(original));
        Require(ReferenceEquals(
            ExceptionHelper.Unwrap(aggregate, typeof(InvalidOperationException), true, true), original),
            "Explicit aggregate Unwrap did not find the nested target.");
        Require(ReferenceEquals(
            aggregate.Unwrap<InvalidOperationException>(includeAggregateExceptions: true), original),
            "Generic aggregate Unwrap did not find the nested target.");

        var action = Try.Invoke((Action)(() => throw new InvalidOperationException("action-inner")));
        Require(action.ExceptionAs<InvalidOperationException>() is not null, "TryAction.ExceptionAs did not find the original exception.");
        Require(action.ExceptionAs<InvalidOperationException>(includeAggregateExceptions: true) is not null,
            "TryAction.ExceptionAs aggregate overload did not compile or return a match.");
        Require(failure.ExceptionAs<InvalidOperationException>() is not null, "Try<T>.ExceptionAs did not find the original exception.");
        Require(failure.ExceptionAs<InvalidOperationException>(includeAggregateExceptions: true) is not null,
            "Try<T>.ExceptionAs aggregate overload did not compile or return a match.");

        var created = await Try.CreateAsync(() => Task.FromResult(11));
        var createdWithToken = await Try.CreateAsync(
            (CancellationToken _) => Task.FromResult(12), CancellationToken.None);
        var invoked = await Try.InvokeTaskAsync(() => Task.CompletedTask);
        var invokedWithToken = await Try.InvokeTaskAsync(
            (CancellationToken _) => Task.CompletedTask, CancellationToken.None);
        var legacyActionCalls = 0;
        var legacyInvoked = await Try.InvokeAsync(Task.FromResult<Action>(() => legacyActionCalls++));

        Require(created.IsSuccess && created.Value == 11, "CreateAsync consumer call failed.");
        Require(createdWithToken.IsSuccess && createdWithToken.Value == 12, "CreateAsync token overload failed.");
        Require(invoked.IsSuccess && invokedWithToken.IsSuccess, "InvokeTaskAsync consumer call failed.");
        Require(legacyInvoked.IsSuccess && legacyActionCalls == 1,
            "The legacy Task<Action> overload did not execute the action exactly once.");

        var formatOptions = new ExceptionFormatOptions
        {
            MaxDepth = 64,
            MaxExceptionCount = 256,
            MaxOutputLength = 32768,
            MaxIndentLevel = 8,
            IncludeAggregateExceptions = false,
            HideStackTrace = false
        };

        _ = outer.FormatMessage(formatOptions);
        _ = ExceptionHelper.GetExceptionDetail(outer, formatOptions);

        var minimumFormat = outer.FormatMessage(new ExceptionFormatOptions
        {
            MaxDepth = 64,
            MaxExceptionCount = 256,
            MaxOutputLength = 64,
            MaxIndentLevel = 8,
            IncludeAggregateExceptions = false,
            HideStackTrace = false
        });
        Require(minimumFormat.Length <= 64, "The minimum format budget exceeded its output limit.");
    }

    /// <summary>
    /// 分别以基类和两个具体结果类型检查 Tap 调用形式。
    /// </summary>
    /// <param name="value">以 Try&lt;int&gt; 静态类型访问的结果。</param>
    /// <param name="success">以 Success&lt;int&gt; 静态类型访问的成功结果。</param>
    /// <param name="failure">以 Failure&lt;int&gt; 静态类型访问的失败结果。</param>
    private static void ExerciseTapSurface(Try<int> value, Success<int> success, Failure<int> failure)
    {
        ExerciseTap(value);
        ExerciseTap(success);
        ExerciseTap(failure);
    }

    /// <summary>
    /// 编译 Try&lt;int&gt; 静态类型可用的 Tap 重载。
    /// </summary>
    /// <param name="result">待检查调用面的 Try 结果。</param>
    private static void ExerciseTap(Try<int> result)
    {
        _ = result.Tap();
        _ = result.Tap((Action<int>)null!);
        _ = result.Tap(failureAction: (Action<TryCreatingValueException>)null!);
        _ = result.Tap(failureAction: (Action<Exception, string>)null!);
        _ = result.Tap(_ => { });
        _ = result.Tap(successAction: _ => { }, failureAction: (TryCreatingValueException _) => { });
        _ = result.Tap(successAction: _ => { }, failureAction: (Exception _, string _) => { });
    }

    /// <summary>
    /// 编译 Success&lt;int&gt; 静态类型可用的 Tap 重载。
    /// </summary>
    /// <param name="result">待检查调用面的成功结果。</param>
    private static void ExerciseTap(Success<int> result)
    {
        _ = result.Tap();
        _ = result.Tap((Action<int>)null!);
        _ = result.Tap(failureAction: (Action<TryCreatingValueException>)null!);
        _ = result.Tap(failureAction: (Action<Exception, string>)null!);
        _ = result.Tap(_ => { });
        _ = result.Tap(successAction: _ => { }, failureAction: (TryCreatingValueException _) => { });
        _ = result.Tap(successAction: _ => { }, failureAction: (Exception _, string _) => { });
    }

    /// <summary>
    /// 编译 Failure&lt;int&gt; 静态类型可用的 Tap 重载。
    /// </summary>
    /// <param name="result">待检查调用面的失败结果。</param>
    private static void ExerciseTap(Failure<int> result)
    {
        _ = result.Tap();
        _ = result.Tap((Action<int>)null!);
        _ = result.Tap(failureAction: (Action<TryCreatingValueException>)null!);
        _ = result.Tap(failureAction: (Action<Exception, string>)null!);
        _ = result.Tap(_ => { });
        _ = result.Tap(successAction: _ => { }, failureAction: (TryCreatingValueException _) => { });
        _ = result.Tap(successAction: _ => { }, failureAction: (Exception _, string _) => { });
    }

    /// <summary>
    /// 在受控同步上下文和任务调度器中验证异步 API 的行为。
    /// </summary>
    private static async Task RunContextChecksAsync()
    {
        var context = new PumpSynchronizationContext();
        context.Run(async () =>
        {
            var oldDelegateCalled = false;
            var oldResult = Try.CreateFromTask<int>(() =>
            {
                oldDelegateCalled = true;
                return Task.FromResult(1);
            });

            Require(oldResult.IsFailure, "CreateFromTask should reject the controlled synchronization context.");
            Require(!oldDelegateCalled, "CreateFromTask invoked its delegate before rejecting the context.");
            Require(oldResult.Exception.InnerException is InvalidOperationException,
                "CreateFromTask should report an InvalidOperationException in the failure chain.");

            var created = await Try.CreateAsync(async () =>
            {
                await Task.Yield();
                return 42;
            });
            Require(created.IsSuccess && created.Value == 42, "CreateAsync did not finish under the controlled context.");

            var invoked = await Try.InvokeTaskAsync(async () => await Task.Yield());
            Require(invoked.IsSuccess, "InvokeTaskAsync did not finish under the controlled context.");
        });

        var scheduler = new InlineTaskScheduler();
        Task.Factory.StartNew(async () =>
        {
            var oldDelegateCalled = false;
            var oldResult = Try.CreateFromTask<int>(() =>
            {
                oldDelegateCalled = true;
                return Task.FromResult(2);
            });

            Require(oldResult.IsFailure, "CreateFromTask should reject a non-default TaskScheduler.");
            Require(!oldDelegateCalled, "CreateFromTask invoked its delegate before rejecting the scheduler.");
            Require(oldResult.Exception.InnerException is InvalidOperationException,
                "CreateFromTask should report an InvalidOperationException for a non-default scheduler.");
            var created = await Try.CreateAsync(async () =>
            {
                await Task.Delay(10);
                return 43;
            });
            Require(created.IsSuccess && created.Value == 43, "CreateAsync did not finish under the controlled scheduler.");
            var invoked = await Try.InvokeTaskAsync(async () => await Task.Delay(10));
            Require(invoked.IsSuccess, "InvokeTaskAsync did not finish under the controlled scheduler.");
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler).Unwrap().GetAwaiter().GetResult();

        await Task.CompletedTask;
    }

    /// <summary>
    /// 在消费者检查失败时抛出包含诊断信息的异常。
    /// </summary>
    /// <param name="condition">检查是否通过。</param>
    /// <param name="message">检查失败时使用的诊断信息。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    /// <summary>
    /// 在单独线程上泵送回调，模拟不能被同步阻塞的 UI 上下文。
    /// </summary>
    private sealed class PumpSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// 保存需要在受控线程上执行的同步上下文回调及其状态。
        /// </summary>
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();

        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state) => _queue.Add((callback, state));

        /// <summary>
        /// 在专用线程上执行回调并泵送该上下文的待处理工作。
        /// </summary>
        /// <param name="callback">需要在此上下文中运行的异步操作。</param>
        public void Run(Func<Task> callback)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                SetSynchronizationContext(this);
                try
                {
                    var task = callback();
                    while (!task.IsCompleted)
                    {
                        var work = _queue.Take();
                        work.Callback(work.State);
                    }
                    task.GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    SetSynchronizationContext(null);
                    _queue.CompleteAdding();
                }
            })
            {
                IsBackground = true,
                Name = "ExceptionsConsumerSynchronizationContext"
            };

            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("The controlled synchronization context did not complete within 15 seconds.");
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <summary>
    /// 在当前线程执行任务以暴露非默认调度器边界。
    /// </summary>
    private sealed class InlineTaskScheduler : TaskScheduler
    {
        /// <inheritdoc />
        protected override IEnumerable<Task> GetScheduledTasks() => Array.Empty<Task>();

        /// <inheritdoc />
        protected override void QueueTask(Task task) => TryExecuteTask(task);

        /// <inheritdoc />
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => TryExecuteTask(task);
    }
}

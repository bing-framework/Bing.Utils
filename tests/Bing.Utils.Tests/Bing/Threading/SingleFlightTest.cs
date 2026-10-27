using System.Diagnostics;
using System.Threading;
using Bing.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// 验证共享执行的并发、终态及独立取消语义。
/// </summary>
public class SingleFlightTest
{
    /// <summary>
    /// 重叠请求共享同一个对象，完成后不会缓存。
    /// </summary>
    [Fact]
    public async Task OverlappingCalls_ShareOneExecution_WithoutCaching()
    {
        var release = Signal<object>();
        var calls = 0;
        var flight = new SingleFlight<string, object>((_, _) => { Interlocked.Increment(ref calls); return release.Task; });
        var requests = Enumerable.Range(0, 24).Select(_ => flight.RunAsync("a")).ToArray();
        calls.ShouldBe(1);
        flight.InFlightCount.ShouldBe(1);
        var value = new object();
        release.SetResult(value);
        var results = await Task.WhenAll(requests);
        results.All(x => ReferenceEquals(x, value)).ShouldBeTrue();
        flight.InFlightCount.ShouldBe(0);
        (await flight.RunAsync("a")).ShouldBeSameAs(value);
        calls.ShouldBe(2);
    }

    /// <summary>
    /// 不同键可同时执行，同一比较器认定相等的键共享任务。
    /// </summary>
    [Fact]
    public async Task DifferentKeys_ExecuteIndependently()
    {
        var release = Signal<int>();
        var calls = 0;
        var flight = new SingleFlight<string, int>((_, _) => { Interlocked.Increment(ref calls); return release.Task; }, StringComparer.OrdinalIgnoreCase);
        var a = flight.RunAsync("A");
        var alias = flight.RunAsync("a");
        var b = flight.RunAsync("b");
        calls.ShouldBe(2);
        flight.InFlightCount.ShouldBe(2);
        release.SetResult(7);
        (await Task.WhenAll(a, alias, b)).ShouldBe(new[] { 7, 7, 7 });
        flight.InFlightCount.ShouldBe(0);
    }

    /// <summary>
    /// 调用方取消只退出自己的等待，不传递给共享工厂。
    /// </summary>
    [Fact]
    public async Task WaiterCancellation_DoesNotCancelFactoryOrOtherWaiters()
    {
        using var owner = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        var release = Signal<int>();
        var observed = default(CancellationToken);
        var flight = new SingleFlight<string, int>((_, token) => { observed = token; return release.Task; }, lifetimeCancellationToken: owner.Token);
        var first = flight.RunAsync("x", caller.Token);
        var second = flight.RunAsync("x");
        caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        observed.ShouldBe(owner.Token);
        observed.IsCancellationRequested.ShouldBeFalse();
        second.IsCompleted.ShouldBeFalse();
        release.SetResult(42);
        (await second).ShouldBe(42);
        flight.InFlightCount.ShouldBe(0);
    }

    /// <summary>
    /// 预取消不会启动工厂，生命周期令牌只由所有者控制。
    /// </summary>
    [Fact]
    public async Task PreCancellation_DoesNotStartFactory()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var calls = 0;
        var flight = new SingleFlight<int, int>((_, _) => { calls++; return Task.FromResult(1); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flight.RunAsync(1, canceled.Token));
        var stopped = new SingleFlight<int, int>((_, _) => { calls++; return Task.FromResult(1); }, lifetimeCancellationToken: canceled.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopped.RunAsync(1));
        calls.ShouldBe(0);
        flight.InFlightCount.ShouldBe(0);
    }

    /// <summary>
    /// 同步异常、空任务和任务取消都会清理条目，后续调用可重新执行。
    /// </summary>
    [Fact]
    public async Task FactoryTerminalStates_ArePreserved_AndCanRetry()
    {
        var calls = 0;
        var error = new InvalidOperationException("原始异常");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var flight = new SingleFlight<int, int>((_, _) => ++calls switch
        {
            1 => throw error,
            2 => null,
            3 => Task.FromCanceled<int>(canceled.Token),
            _ => Task.FromResult(9)
        });
        (await Assert.ThrowsAsync<InvalidOperationException>(() => flight.RunAsync(1))).ShouldBeSameAs(error);
        await Assert.ThrowsAsync<InvalidOperationException>(() => flight.RunAsync(1));
        var task = flight.RunAsync(1);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        task.IsCanceled.ShouldBeTrue();
        (await flight.RunAsync(1)).ShouldBe(9);
        flight.InFlightCount.ShouldBe(0);
    }

    /// <summary>
    /// 所有等待者退出后，工厂失败仍会被观察并回收。
    /// </summary>
    [Fact]
    public async Task AbandonedFactoryFault_IsCleanedUp()
    {
        var release = Signal<int>();
        using var caller = new CancellationTokenSource();
        var flight = new SingleFlight<int, int>((_, _) => release.Task);
        var request = flight.RunAsync(1, caller.Token);
        caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        flight.InFlightCount.ShouldBe(1);
        release.SetException(new InvalidOperationException("无人等待的失败"));
        var deadline = Stopwatch.StartNew();
        while (flight.InFlightCount != 0 && deadline.Elapsed < TimeSpan.FromSeconds(10)) await Task.Yield();
        flight.InFlightCount.ShouldBe(0);
    }

    /// <summary>
    /// 创建异步续延的测试信号。
    /// </summary>
    /// <typeparam name="T">信号值类型。</typeparam>
    /// <returns>用于控制工厂完成时机的信号。</returns>
    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 工厂返回故障任务时，即使异常类型为取消异常也保留故障终态。
    /// </summary>
    [Fact]
    public async Task FaultedCancellationException_RemainsFaulted()
    {
        var error = new OperationCanceledException("作为普通故障返回");
        var flight = new SingleFlight<int, int>((_, _) => Task.FromException<int>(error));
        var task = flight.RunAsync(1);
        (await Assert.ThrowsAsync<OperationCanceledException>(() => task)).ShouldBeSameAs(error);
        task.IsFaulted.ShouldBeTrue();
        using var caller = new CancellationTokenSource();
        var cancelableWait = flight.RunAsync(1, caller.Token);
        (await Assert.ThrowsAsync<OperationCanceledException>(() => cancelableWait)).ShouldBeSameAs(error);
        cancelableWait.IsFaulted.ShouldBeTrue();
        flight.InFlightCount.ShouldBe(0);
    }

    /// <summary>
    /// 多线程同时登记仍只启动一次工厂，生命周期取消传递给所有等待者。
    /// </summary>
    [Fact]
    public async Task ConcurrentRegistration_AndLifetimeCancellation_AreShared()
    {
        using var owner = new CancellationTokenSource();
        var release = Signal<int>();
        var calls = 0;
        var joined = 0;
        var allJoined = Signal<bool>();
        var start = Signal<bool>();
        var flight = new SingleFlight<int, int>((_, token) =>
        {
            Interlocked.Increment(ref calls);
            token.Register(() => release.TrySetCanceled(token));
            return release.Task;
        }, lifetimeCancellationToken: owner.Token);
        var requests = Enumerable.Range(0, 24).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            var shared = flight.RunAsync(1);
            if (Interlocked.Increment(ref joined) == 24) allJoined.TrySetResult(true);
            return await shared;
        })).ToArray();
        start.SetResult(true);
        (await Task.WhenAny(allJoined.Task, Task.Delay(TimeSpan.FromSeconds(10)))).ShouldBeSameAs(allJoined.Task);
        calls.ShouldBe(1);
        owner.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(requests));
        requests.All(x => x.IsCanceled).ShouldBeTrue();
        flight.InFlightCount.ShouldBe(0);
    }

    /// <summary>
    /// 工厂同步部分受阻时，另一个键仍可登记并启动工厂。
    /// </summary>
    [Fact]
    public async Task BlockingFactoryPrefix_DoesNotHoldRegistryLock()
    {
        var enteredFirst = Signal<bool>();
        var enteredSecond = Signal<bool>();
        using var releaseFirst = new ManualResetEventSlim();
        var flight = new SingleFlight<int, int>((key, _) =>
        {
            if (key == 1)
            {
                enteredFirst.TrySetResult(true);
                releaseFirst.Wait();
            }
            else enteredSecond.TrySetResult(true);
            return Task.FromResult(key);
        });
        var first = Task.Run(() => flight.RunAsync(1));
        Task<int> second = null;
        try
        {
            (await Task.WhenAny(enteredFirst.Task, Task.Delay(TimeSpan.FromSeconds(10)))).ShouldBeSameAs(enteredFirst.Task);
            second = Task.Run(() => flight.RunAsync(2));
            (await Task.WhenAny(enteredSecond.Task, Task.Delay(TimeSpan.FromSeconds(10)))).ShouldBeSameAs(enteredSecond.Task);
        }
        finally { releaseFirst.Set(); }
        (await first).ShouldBe(1);
        (await second).ShouldBe(2);
    }
}

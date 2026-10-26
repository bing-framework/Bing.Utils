using System.Collections;
using System.Threading;

namespace Bing.Collections;

[Trait("Category", "PerformanceApiReview")]
public class EnumerableSchedulingRegressionTest
{
    private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task AwaitSignal(Task task)
    {
        Assert.Same(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))));
        await task;
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(4)]
    public async Task PreCanceled_DoesNotEnumerate_AndPropagatesToken(int? parallelism)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ThrowingSource().ForEachAsync(
            _ => Task.CompletedTask, parallelism, cancellationToken: cts.Token));
        Assert.Equal(cts.Token, error.CancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task RejectingHandler_StopsBeforeNextItem(int? parallelism)
    {
        var calls = 0;
        var task = new[] { 1, 2, 3 }.ForEachAsync(_ => { calls++; throw new FormatException("action"); },
            parallelism, (_, _) => false);
        if (parallelism.HasValue) await Assert.ThrowsAsync<AggregateException>(() => task);
        else await Assert.ThrowsAsync<FormatException>(() => task);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task ThrowingHandler_StopsAndPropagatesHandlerFailure(int? parallelism)
    {
        var calls = 0;
        var task = new[] { 1, 2, 3 }.ForEachAsync(_ => { calls++; throw new FormatException(); },
            parallelism, (_, _) => throw new ApplicationException("handler"));
        if (parallelism.HasValue)
        {
            var error = await Assert.ThrowsAsync<AggregateException>(() => task);
            Assert.IsType<ApplicationException>(error.InnerExceptions.Single().InnerException);
        }
        else await Assert.ThrowsAsync<ApplicationException>(() => task);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task BoundedWorkers_EnumerateIncrementally_AndDrainOnCancellation(int degree)
    {
        using var cts = new CancellationTokenSource();
        var started = Signal(); var release = Signal();
        var read = 0; var active = 0; var completed = 0; var disposed = false;
        IEnumerable<int> Source()
        {
            try { for (var i = 0; i < 1000; i++) { Interlocked.Increment(ref read); yield return i; } }
            finally { disposed = true; }
        }
        var task = Source().ForEachWithCancellationAsync(async (_, token) =>
        {
            Assert.Equal(cts.Token, token);
            if (Interlocked.Increment(ref active) == degree) started.TrySetResult(true);
            await release.Task;
            Interlocked.Increment(ref completed);
        }, degree, cancellationToken: cts.Token);
        try
        {
            await AwaitSignal(started.Task);
            Assert.Equal(degree, Volatile.Read(ref read));
            cts.Cancel();
            Assert.False(task.IsCompleted);
        }
        finally { release.TrySetResult(true); }
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(cts.Token, error.CancellationToken);
        Assert.Equal(degree, completed);
        Assert.True(disposed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task EnumerationFailure_WaitsForStartedOperation(int? degree)
    {
        var release = Signal(); var started = Signal(); var finished = false;
        IEnumerable<int> Source() { yield return 1; throw new FormatException("enumeration"); }
        var task = Source().ForEachAsync(async _ => { started.SetResult(true); await release.Task; finished = true; }, degree);
        try { await AwaitSignal(started.Task); Assert.False(task.IsCompleted); }
        finally { release.TrySetResult(true); }
        var error = await Assert.ThrowsAsync<FormatException>(() => task);
        Assert.Equal("enumeration", error.Message);
        Assert.True(finished);
    }

    [Fact]
    public async Task UnlimitedCancellation_DrainsStartedTasks()
    {
        using var cts = new CancellationTokenSource();
        var release = Signal(); var calls = 0;
        var task = Enumerable.Range(1, 3).ForEachAsync(async _ => { calls++; await release.Task; }, cancellationToken: cts.Token);
        Assert.Equal(3, calls);
        cts.Cancel();
        Assert.False(task.IsCompleted);
        release.SetResult(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidParallelism_IsRejected(int degree)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = Array.Empty<int>().ForEachAsync(_ => Task.CompletedTask, degree ); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = Array.Empty<int>().ForEachWithCancellationAsync((_, _) => Task.CompletedTask, degree ); });
    }

    [Fact]
    public async Task NewEntry_EmptyAndNullContracts()
    {
        await Array.Empty<int>().ForEachWithCancellationAsync((_, _) => throw new Exception());
        Assert.Throws<ArgumentNullException>(() => { _ = BingEnumerableExtensions.ForEachWithCancellationAsync<int>(null, (_, _) => Task.CompletedTask ); });
        Assert.Throws<ArgumentNullException>(() => { _ = Array.Empty<int>().ForEachWithCancellationAsync(null ); });
        // 旧入口的 null/default 调用仍可编译。
        Assert.Throws<ArgumentNullException>(() => { _ = Array.Empty<int>().ForEachAsync(null, default ); });
    }

    [Fact]
    public async Task VeryLargeBound_DoesNotAllocateWorkersForMissingItems()
    {
        await Array.Empty<int>().ForEachAsync(_ => Task.CompletedTask, int.MaxValue);
        var calls = 0;
        await new[] { 1, 2 }.ForEachAsync(_ => { Interlocked.Increment(ref calls); return Task.CompletedTask; }, int.MaxValue);
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task CleanupFailure_DoesNotOverrideCancellationOrOperationFailure(int? degree)
    {
        using var cts = new CancellationTokenSource();
        var source = new FailingDisposeSource();
        var canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.ForEachAsync(
            _ => { cts.Cancel(); return Task.CompletedTask; }, degree, cancellationToken: cts.Token));
        Assert.Equal(cts.Token, canceled.CancellationToken);
        Assert.True(source.Disposed);
        var failed = source.ForEachAsync(_ => throw new FormatException("action"), degree);
        if (degree.HasValue) Assert.IsType<FormatException>((await Assert.ThrowsAsync<AggregateException>(() => failed)).InnerException.InnerException);
        else await Assert.ThrowsAsync<FormatException>(() => failed);
        await Assert.ThrowsAsync<ApplicationException>(() => source.ForEachAsync(_ => Task.CompletedTask, degree));
    }

    private sealed class FailingDisposeSource : IEnumerable<int>
    {
        public bool Disposed;
        public IEnumerator<int> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private sealed class Enumerator : IEnumerator<int>
        {
            private readonly FailingDisposeSource _owner;
            private bool _read;
            public Enumerator(FailingDisposeSource owner) => _owner = owner;
            public int Current => 1;
            object IEnumerator.Current => Current;
            public bool MoveNext() { if (_read) return false; _read = true; return true; }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { _owner.Disposed = true; throw new ApplicationException("dispose"); }
        }
    }

    private static IEnumerable<int> ThrowingSource()
    {
        throw new Exception("must not enumerate");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}

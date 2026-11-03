using System.Collections.Concurrent;

namespace Bing.IdUtils;

/// <summary>
/// 覆盖 TimestampId 的初始化、序列耗尽、回拨和并发边界。
/// </summary>
[Collection("Id static state")]
[Trait("IdUtilsUT", "Timestamp.Boundary")]
public class TimestampIdBoundaryTest : IDisposable
{
    private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public TimestampIdBoundaryTest() => TimestampId.ResetInstanceForTests();

    public void Dispose() => TimestampId.ResetInstanceForTests();

    [Fact]
    public void GetInstance_FirstNonNullInitialDate_IsUsedAndLaterDateIsIgnored()
    {
        var first = TimestampId.GetInstance(Epoch);
        var second = TimestampId.GetInstance(Epoch.AddDays(1));

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void GetInstance_NullInitialDate_StillInitializesSingleton()
    {
        var first = TimestampId.GetInstance();
        var second = TimestampId.GetInstance(Epoch);

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void GetId_SameMillisecond_UsesSequenceOneThrough9999()
    {
        var now = Epoch.AddMilliseconds(1);
        var instance = CreateControlled(() => now, TimeSpan.FromSeconds(1));

        string last = null;
        for (var i = 0; i < 9999; i++)
            last = instance.GetId();

        last.ShouldNotBeNull();
        last.Substring(last.Length - 4).ShouldBe("9999");
    }

    [Fact]
    public void GetId_SequenceExhaustionTimeout_PreservesStateUntilTimeAdvances()
    {
        var now = Epoch.AddMilliseconds(1);
        var monotonic = 0L;
        var instance = CreateControlled(() => now, TimeSpan.Zero, () => monotonic++);

        for (var i = 0; i < 9999; i++)
            instance.GetId();

        Should.Throw<TimeoutException>(() => instance.GetId());
        now = now.AddMilliseconds(1);
        var next = instance.GetId();

        next.EndsWith("0001").ShouldBeTrue();
        next.StartsWith("2", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void GetId_WhenClockMovesBack_Throws()
    {
        var now = Epoch.AddMilliseconds(2);
        var instance = CreateControlled(() => now);
        instance.GetId();
        now = now.AddMilliseconds(-1);

        Should.Throw<InvalidOperationException>(() => instance.GetId());
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    public void GetId_SequenceExhaustion_ChecksBudgetBeforeCommittingAdvance(int elapsed, bool timeout)
    {
        var now = Epoch.AddMilliseconds(1);
        var monotonic = 0L;
        var advanceOnDelay = true;
        var instance = new TimestampId(Epoch, () => now, () => monotonic,
            TimeSpan.FromMilliseconds(10), _ =>
            {
                monotonic += elapsed;
                if (advanceOnDelay)
                    now = Epoch.AddMilliseconds(2);
            });
        for (var index = 0; index < 9999; index++)
            instance.GetId();

        if (!timeout)
        {
            instance.GetId().ShouldBe("20001");
            return;
        }

        Should.Throw<TimeoutException>(() => instance.GetId());
        // 失败不能提交时间或清空序列：回到最后成功的毫秒仍应处于耗尽状态。
        now = Epoch.AddMilliseconds(1);
        advanceOnDelay = false;
        Should.Throw<TimeoutException>(() => instance.GetId());
        now = Epoch.AddMilliseconds(2);
        instance.GetId().ShouldBe("20001");
    }

    [Fact]
    public async Task GetId_CallerHoldingInstanceMonitor_DoesNotBlockGeneration()
    {
        var instance = CreateControlled(() => Epoch.AddMilliseconds(1));
        Task<string> task = null;
        try
        {
            lock (instance)
            {
                task = Task.Run(() => instance.GetId());
                task.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            }
        }
        finally
        {
            if (task != null)
                await task;
        }
        (await task).ShouldBe("10001");
    }

    [Fact]
    public async Task GetId_ConcurrentCalls_DoNotDuplicate()
    {
        var now = Epoch.AddMilliseconds(3);
        var instance = CreateControlled(() => now);
        var results = new ConcurrentBag<string>();
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 200; i++)
                    results.Add(instance.GetId());
            }))
            .ToArray();

        var completed = Task.WhenAll(tasks);
        try
        {
            (await Task.WhenAny(completed, Task.Delay(TimeSpan.FromSeconds(5)))).ShouldBeSameAs(completed);
        }
        finally
        {
            await completed;
        }
        results.Count.ShouldBe(1600);
        results.Distinct().Count().ShouldBe(results.Count);
    }

    private static TimestampId CreateControlled(
        Func<DateTime> utcNow,
        TimeSpan? timeout = null,
        Func<long> monotonic = null)
    {
        return new TimestampId(
            Epoch,
            utcNow,
            monotonic ?? (() => 0),
            timeout ?? TimeSpan.FromSeconds(1),
            _ => { });
    }
}

using System.Collections.Concurrent;
using System.Threading;
using Bing.IdUtils;

namespace Bing.Helpers;

/// <summary>
/// 覆盖雪花 ID 配置生命周期、上下文覆盖和并发生成契约。
/// </summary>
[Collection("Id static state")]
[Trait("Bing.Helpers", "Id.Snowflake")]
public class IdSnowflakeTest : IDisposable
{
    public IdSnowflakeTest()
    {
        Id.ResetSnowflakeIdForTests();
        Id.Reset();
    }

    public void Dispose()
    {
        Id.Reset();
        Id.ResetSnowflakeIdForTests();
    }

    [Fact]
    public void CreateSnowflakeId_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeId());
    }

    [Fact]
    public void CreateSnowflakeIds_WithoutConfiguration_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeIds(1));
    }

    [Fact]
    public void ConfigureSnowflakeId_NullFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Id.ConfigureSnowflakeId(null));
    }

    [Fact]
    public void ConfigureSnowflakeId_NullResult_ThrowsAndKeepsUnconfiguredState()
    {
        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => null));
        Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeId());
    }

    [Fact]
    public void ConfigureSnowflakeId_FactoryFailure_LeavesStateUnchanged()
    {
        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => throw new InvalidOperationException("factory failure")));
        Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeId());
    }

    [Fact]
    public void CreateSnowflakeId_ProviderFailure_AllowsReconfigurationBeforeAnySuccess()
    {
        Id.ConfigureSnowflakeId(() => new FailingSnowflakeId());

        Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeId());

        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.CreateSnowflakeId().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CreateSnowflakeIds_ProviderFailure_AllowsReconfigurationBeforeAnySuccess()
    {
        Id.ConfigureSnowflakeId(() => new FailingSnowflakeId());

        Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeIds(2));

        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.CreateSnowflakeIds(2).Length.ShouldBe(2);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ConfigureSnowflakeId_WhileGenerationInProgress_IsRejectedUntilCompletion(bool batch, bool fail)
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Id.ConfigureSnowflakeId(() => new BlockingSnowflakeId(entered, release, fail));

        var task = Task.Run(() => Record.Exception(() => Generate(batch)));
        try
        {
            entered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            var factoryCalled = false;
            Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() =>
            {
                factoryCalled = true;
                return SnowflakeGenerator.Create(2, 1);
            }));
            factoryCalled.ShouldBeFalse();
            Should.Throw<InvalidOperationException>(() => Id.ResetSnowflakeId());
        }
        finally
        {
            release.Set();
            // 必须等待后台调用退出后，才由 Dispose 清理静态状态。
            await task;
        }

        if (fail)
        {
            (await task).ShouldBeOfType<InvalidOperationException>();
            Id.ResetSnowflakeId();
            Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1));
        }
        else
        {
            (await task).ShouldBeNull();
            Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1)));
            Should.Throw<InvalidOperationException>(() => Id.ResetSnowflakeId());
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ConcurrentGeneration_CompletionsPreserveOtherCallsAndSuccessfulFreeze(bool firstSucceeds, bool secondSucceeds)
    {
        using var firstEntered = new ManualResetEventSlim();
        using var secondEntered = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var releaseSecond = new ManualResetEventSlim();
        var calls = 0;
        var firstProvider = new BlockingSnowflakeId(firstEntered, releaseFirst, !firstSucceeds);
        var secondProvider = new BlockingSnowflakeId(secondEntered, releaseSecond, !secondSucceeds);
        Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() =>
            Interlocked.Increment(ref calls) == 1 ? firstProvider.NextId() : secondProvider.NextId()));
        var first = Task.Run(() => Record.Exception(() => Id.CreateSnowflakeId()));
        Task<Exception> second = null;
        try
        {
            firstEntered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            second = Task.Run(() => Record.Exception(() => Id.CreateSnowflakeIds(1)));
            secondEntered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            releaseFirst.Set();
            var firstError = await first;
            (firstError == null).ShouldBe(firstSucceeds);

            // 首个调用结束后，另一个调用仍在途，不能提前替换实例。
            Should.Throw<InvalidOperationException>(() => Id.ResetSnowflakeId());
            Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1)));
        }
        finally
        {
            releaseFirst.Set();
            releaseSecond.Set();
            await first;
            if (second != null)
                await second;
        }

        ((await second) == null).ShouldBe(secondSucceeds);
        if (firstSucceeds || secondSucceeds)
        {
            Should.Throw<InvalidOperationException>(() => Id.ResetSnowflakeId());
            Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1)));
        }
        else
        {
            Id.ResetSnowflakeId();
            Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1));
        }
    }

    [Fact]
    public void CreateSnowflakeIds_PartialProviderFailure_PreservesSequenceAndAllowsReplacement()
    {
        var calls = 0;
        Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() =>
        {
            if (++calls >= 2)
                throw new InvalidOperationException("batch failure");
            return calls;
        }));

        long[] result = null;
        Should.Throw<InvalidOperationException>(() => result = Id.CreateSnowflakeIds(2));
        result.ShouldBeNull();
        calls.ShouldBe(2);
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1));
        Id.CreateSnowflakeId().ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateSnowflakeId_FailureAfterSuccess_DoesNotUnfreezeConfiguration(bool batch)
    {
        var fail = false;
        Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() => fail
            ? throw new InvalidOperationException("provider failure") : 42));
        Generate(batch);
        fail = true;
        Should.Throw<InvalidOperationException>(() => Generate(batch));
        Should.Throw<InvalidOperationException>(() => Id.ResetSnowflakeId());
        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1)));
    }

    [Fact]
    public void ConfigureSnowflakeId_FactoryGeneratesBeforePublishing_CannotReplaceUsedProvider()
    {
        var original = new DelegateSnowflakeId(() => 42);
        Id.ConfigureSnowflakeId(() => original);
        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() =>
        {
            Id.CreateSnowflakeId().ShouldBe(42);
            return new DelegateSnowflakeId(() => 99);
        }));
        Id.CreateSnowflakeId().ShouldBe(42);
    }

    [Fact]
    public void ConfigureSnowflakeId_InvalidReplacement_PreservesExistingProvider()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));

        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => null));
        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => throw new InvalidOperationException("factory failure")));

        Id.CreateSnowflakeId().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ConfigureSnowflakeId_FactoryWaitsForWorkerGeneration_DoesNotDeadlockOrReplaceUsedProvider()
    {
        Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() => 42));
        Task<long> generation = null;
        try
        {
            Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() =>
            {
                generation = Task.Run(() => Id.CreateSnowflakeId());
                // 有界等待使旧实现能暴露锁互等问题，同时确保测试可以收尾。
                generation.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
                generation.Result.ShouldBe(42);
                return new DelegateSnowflakeId(() => 99);
            }));
        }
        finally
        {
            if (generation != null)
                await generation;
        }
        Id.CreateSnowflakeId().ShouldBe(42);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfigureSnowflakeId_FactoryChangesConfiguration_CannotPublishStaleCandidate(bool reset)
    {
        Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() => 42));
        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() =>
        {
            if (reset)
                Id.ResetSnowflakeId();
            else
                Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() => 77));
            return new DelegateSnowflakeId(() => 99);
        }));

        if (reset)
            Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeId());
        else
            Id.CreateSnowflakeId().ShouldBe(77);
    }

    [Fact]
    public async Task ConfigureSnowflakeId_ConcurrentReplacement_PreservesNewerConfiguration()
    {
        Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() => 42));
        using var factoryEntered = new ManualResetEventSlim();
        using var releaseFactory = new ManualResetEventSlim();
        var pending = Task.Run(() => Record.Exception(() => Id.ConfigureSnowflakeId(() =>
        {
            factoryEntered.Set();
            releaseFactory.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            return new DelegateSnowflakeId(() => 99);
        })));
        try
        {
            factoryEntered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            Id.ConfigureSnowflakeId(() => new DelegateSnowflakeId(() => 77));
        }
        finally
        {
            releaseFactory.Set();
            await pending;
        }

        (await pending).ShouldBeOfType<InvalidOperationException>();
        Id.CreateSnowflakeId().ShouldBe(77);
    }

    [Fact]
    public void ConfigureSnowflakeId_BeforeFirstGeneration_CanReplaceConfiguration()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1));

        var id = Id.CreateSnowflakeId();

        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ConfigureSnowflakeId_AfterFirstGeneration_ThrowsAndKeepsOriginalProvider()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        var first = Id.CreateSnowflakeId();

        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1)));
        var second = Id.CreateSnowflakeId();

        second.ShouldBeGreaterThan(first);
    }

    [Fact]
    public void ResetSnowflakeId_BeforeFirstGeneration_ClearsConfiguration()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.ResetSnowflakeId();

        Should.Throw<InvalidOperationException>(() => Id.CreateSnowflakeId());
    }

    [Fact]
    public void ResetSnowflakeId_AfterFirstGeneration_Throws()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.CreateSnowflakeId();

        Should.Throw<InvalidOperationException>(() => Id.ResetSnowflakeId());
    }

    [Fact]
    public void CreateSnowflakeIds_FirstBatch_FreezesConfiguration()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.CreateSnowflakeIds(2).Length.ShouldBe(2);

        Should.Throw<InvalidOperationException>(() => Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2, 1)));
        Should.Throw<InvalidOperationException>(() => Id.ResetSnowflakeId());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("922337203685477580")]
    public void CreateSnowflakeId_WithContextId_ReturnsContextValue(string contextId)
    {
        Id.SetId(contextId);

        Id.CreateSnowflakeId().ShouldBe(long.Parse(contextId));
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.ResetSnowflakeId();
    }

    [Fact]
    public void CreateSnowflakeIds_WithContextId_IgnoresContextAndGeneratesIds()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        Id.SetId("123");

        var ids = Id.CreateSnowflakeIds(3);

        ids.Length.ShouldBe(3);
        ids.Distinct().Count().ShouldBe(3);
        ids.ShouldNotContain(123);
        Id.CreateSnowflakeId().ShouldBe(123);
    }

    [Fact]
    public void CreateSnowflakeIds_InvalidCount_ThrowsBeforeConfigurationLookup()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Id.CreateSnowflakeIds(0));
        Should.Throw<ArgumentOutOfRangeException>(() => Id.CreateSnowflakeIds(100001));
    }

    [Fact]
    public void CreateSnowflakeId_ConcurrentGeneration_IsUnique()
    {
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1, 1));
        var results = new ConcurrentBag<long>();
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 200; i++)
                    results.Add(Id.CreateSnowflakeId());
            }))
            .ToArray();

        try
        {
            Task.WaitAll(tasks, TimeSpan.FromSeconds(5)).ShouldBeTrue();
        }
        finally
        {
            Task.WaitAll(tasks);
        }
        results.Count.ShouldBe(1600);
        results.Distinct().Count().ShouldBe(results.Count);
    }

    [Fact]
    public void Configure_ObsoleteAlias_UsesSameLifecycle()
    {
#pragma warning disable CS0618
        Id.Configure(() => SnowflakeGenerator.Create(1, 1));
#pragma warning restore CS0618

        Id.CreateSnowflakeId().ShouldBeGreaterThan(0);
    }

    private sealed class FailingSnowflakeId : ISnowflakeId
    {
        public long NextId() => throw new InvalidOperationException("provider failure");

        public long[] NextIds(uint size) => throw new InvalidOperationException("provider failure");
    }

    private sealed class BlockingSnowflakeId : ISnowflakeId
    {
        private readonly ManualResetEventSlim _entered;
        private readonly ManualResetEventSlim _release;
        private readonly bool _fail;

        public BlockingSnowflakeId(ManualResetEventSlim entered, ManualResetEventSlim release, bool fail)
        {
            _entered = entered;
            _release = release;
            _fail = fail;
        }

        public long NextId()
        {
            _entered.Set();
            if (!_release.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("test release timed out");
            if (_fail)
                throw new InvalidOperationException("provider failure");
            return 1;
        }

        public long[] NextIds(uint size) => new[] { NextId() };
    }

    private static void Generate(bool batch)
    {
        if (batch)
            Id.CreateSnowflakeIds(1);
        else
            Id.CreateSnowflakeId();
    }

    private sealed class DelegateSnowflakeId : ISnowflakeId
    {
        private readonly Func<long> _nextId;

        public DelegateSnowflakeId(Func<long> nextId) => _nextId = nextId;

        public long NextId() => _nextId();

        public long[] NextIds(uint size) => Enumerable.Range(0, checked((int)size)).Select(_ => NextId()).ToArray();
    }
}

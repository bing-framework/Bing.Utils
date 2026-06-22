using Bing.Threading;
using System.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// <see cref="AsyncOneTimeRunner"/> 单元测试
/// </summary>
public class AsyncOneTimeRunnerTests
{
    // ─────────────────────────────────────────────────────────────────
    // 基本功能
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_FirstCall_ExecutesAction()
    {
        var runner = new AsyncOneTimeRunner();
        var called = 0;
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        called.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_SecondCall_DoesNotExecuteAction()
    {
        var runner = new AsyncOneTimeRunner();
        var called = 0;
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        called.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_ThirdCall_DoesNotExecuteAction()
    {
        var runner = new AsyncOneTimeRunner();
        var called = 0;
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        called.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_AfterFirstRun_SkipsImmediately()
    {
        var runner = new AsyncOneTimeRunner();
        await runner.RunAsync(() => Task.CompletedTask);

        var started = false;
        await runner.RunAsync(async () =>
        {
            started = true;
            await Task.Yield();
        });
        started.ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // 并发场景
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ConcurrentCalls_ExecutesOnlyOnce()
    {
        var runner = new AsyncOneTimeRunner();
        var called = 0;
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            runner.RunAsync(async () =>
            {
                Interlocked.Increment(ref called);
                await Task.Yield();
            }));
        await Task.WhenAll(tasks);
        called.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────
    // 独立实例互不干扰
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TwoRunners_AreIndependent()
    {
        var runner1 = new AsyncOneTimeRunner();
        var runner2 = new AsyncOneTimeRunner();
        var count = 0;

        await runner1.RunAsync(() => { count++; return Task.CompletedTask; });
        await runner2.RunAsync(() => { count++; return Task.CompletedTask; });

        count.ShouldBe(2);
    }
}

/// <summary>
/// <see cref="AsyncLocalExtensions"/> 单元测试
/// </summary>
public class AsyncLocalExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // SetScoped
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SetScoped_SetsNewValue_WithinScope()
    {
        var local = new AsyncLocal<int>();
        using (local.SetScoped(42))
        {
            local.Value.ShouldBe(42);
        }
    }

    [Fact]
    public void SetScoped_RestoresPreviousValue_AfterDispose()
    {
        var local = new AsyncLocal<int>();
        local.Value = 10;
        using (local.SetScoped(99))
        {
            local.Value.ShouldBe(99);
        }
        local.Value.ShouldBe(10);
    }

    [Fact]
    public void SetScoped_WithDefaultPreviousValue_RestoresDefault()
    {
        var local = new AsyncLocal<string>();
        using (local.SetScoped("hello"))
        {
            local.Value.ShouldBe("hello");
        }
        local.Value.ShouldBeNull();
    }

    [Fact]
    public void SetScoped_ReturnsIDisposable()
    {
        var local = new AsyncLocal<int>();
        var disposable = local.SetScoped(1);
        disposable.ShouldBeAssignableTo<IDisposable>();
        disposable.Dispose();
    }

    [Fact]
    public void SetScoped_NestedScopes_RestoreInCorrectOrder()
    {
        var local = new AsyncLocal<int>();
        local.Value = 1;

        using (local.SetScoped(2))
        {
            local.Value.ShouldBe(2);
            using (local.SetScoped(3))
            {
                local.Value.ShouldBe(3);
            }
            local.Value.ShouldBe(2);
        }
        local.Value.ShouldBe(1);
    }

    [Fact]
    public void SetScoped_StringType_Works()
    {
        var local = new AsyncLocal<string>();
        local.Value = "original";
        using (local.SetScoped("modified"))
        {
            local.Value.ShouldBe("modified");
        }
        local.Value.ShouldBe("original");
    }
}

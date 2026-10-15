using System.Threading;
using Bing.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// <see cref="AsyncOneTimeRunner"/> 单元测试
/// </summary>
public class AsyncOneTimeRunnerTests
{
    // ─────────────────────────────────────────────────────────────────
    // 基本功能
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证首次调用执行异步操作。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task RunAsync_FirstCall_ExecutesAction()
    {
        var runner = new AsyncOneTimeRunner();
        var called = 0;
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        called.ShouldBe(1);
    }

    /// <summary>
    /// 验证第二次调用不再执行异步操作。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task RunAsync_SecondCall_DoesNotExecuteAction()
    {
        var runner = new AsyncOneTimeRunner();
        var called = 0;
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        await runner.RunAsync(() => { called++; return Task.CompletedTask; });
        called.ShouldBe(1);
    }

    /// <summary>
    /// 验证连续三次调用仅执行一次异步操作。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证首次执行后不再进入新的异步委托。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证并发调用仅执行一次异步操作。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证不同异步运行器各自执行一次操作。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证作用域内使用新设置的异步局部值。
    /// </summary>
    [Fact]
    public void SetScoped_SetsNewValue_WithinScope()
    {
        var local = new AsyncLocal<int>();
        using (local.SetScoped(42))
        {
            local.Value.ShouldBe(42);
        }
    }

    /// <summary>
    /// 验证释放作用域后恢复原有异步局部值。
    /// </summary>
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

    /// <summary>
    /// 验证释放作用域后恢复空异步局部值。
    /// </summary>
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

    /// <summary>
    /// 验证设置作用域返回可释放对象。
    /// </summary>
    [Fact]
    public void SetScoped_ReturnsIDisposable()
    {
        var local = new AsyncLocal<int>();
        var disposable = local.SetScoped(1);
        disposable.ShouldBeAssignableTo<IDisposable>();
        disposable.Dispose();
    }

    /// <summary>
    /// 验证嵌套作用域按退出顺序恢复异步局部值。
    /// </summary>
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

    /// <summary>
    /// 验证字符串异步局部值在作用域退出后恢复。
    /// </summary>
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

using System.Security.Principal;
using System.Threading;
using ThreadHelper = Bing.Helpers.Thread;
namespace Bing.Helpers;
/// <summary>
/// 测试类：覆盖 `Thread` 相关行为。
/// </summary>
[Trait("Bing.Helpers", "Thread")]
public class ThreadTest
{
    /// <summary>
    /// 测试用例：验证 `ThreadId` 在 `CurrentThread` 场景下，结果为 `ReturnsManagedThreadId`。
    /// </summary>
    [Fact]
    public void ThreadId_CurrentThread_ReturnsManagedThreadId()
    {
        ThreadHelper.ThreadId.ShouldBe(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
    }
    /// <summary>
    /// 测试用例：验证 `MaxThreadNumberInThreadPool` 在 `Always` 场景下，结果为 `ReturnsPositiveValue`。
    /// </summary>
    [Fact]
    public void MaxThreadNumberInThreadPool_Always_ReturnsPositiveValue()
    {
        ThreadHelper.MaxThreadNumberInThreadPool.ShouldBeGreaterThan(0);
    }
    /// <summary>
    /// 测试用例：验证 `CurrentPrincipal` 在 `SetAndGet` 场景下，结果为 `ReturnsConfiguredPrincipal`。
    /// </summary>
    [Fact]
    public void CurrentPrincipal_SetAndGet_ReturnsConfiguredPrincipal()
    {
        var original = ThreadHelper.CurrentPrincipal;
        var principal = new GenericPrincipal(new GenericIdentity("bing-utils-test"), new[] { "dev" });
        try
        {
            ThreadHelper.CurrentPrincipal = principal;
            ThreadHelper.CurrentPrincipal.ShouldBeSameAs(principal);
        }
        finally
        {
            ThreadHelper.CurrentPrincipal = original;
        }
    }
    /// <summary>
    /// 测试用例：验证 `WaitAll` 在 `NullActions` 场景下，结果为 `DoesNotThrow`。
    /// </summary>
    [Fact]
    public void WaitAll_NullActions_DoesNotThrow()
    {
        Should.NotThrow(() => ThreadHelper.WaitAll(null));
    }
    /// <summary>
    /// 测试用例：验证 `WaitAll` 在 `WithTwoActions` 场景下，结果为 `ExecutesAllActions`。
    /// </summary>
    [Fact]
    public void WaitAll_WithTwoActions_ExecutesAllActions()
    {
        var count = 0;
        ThreadHelper.WaitAll(
            () => Interlocked.Increment(ref count),
            () => Interlocked.Increment(ref count));
        count.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `ParallelExecute` 在 `ActionAndCount` 场景下，结果为 `ExecutesExpectedTimes`。
    /// </summary>
    [Fact]
    public void ParallelExecute_ActionAndCount_ExecutesExpectedTimes()
    {
        var count = 0;
        ThreadHelper.ParallelExecute(() => Interlocked.Increment(ref count), 20);
        count.ShouldBe(20);
    }
    /// <summary>
    /// 测试用例：验证 `StartTask` 在 `Action` 场景下，结果为 `ExecutesHandler`。
    /// </summary>
    [Fact]
    public void StartTask_Action_ExecutesHandler()
    {
        var signal = new ManualResetEventSlim(false);
        ThreadHelper.StartTask(() => signal.Set());
        signal.Wait(TimeSpan.FromSeconds(2)).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `StartTask` 在 `WithState` 场景下，结果为 `PassesStateToHandler`。
    /// </summary>
    [Fact]
    public void StartTask_WithState_PassesStateToHandler()
    {
        var signal = new ManualResetEventSlim(false);
        object received = null;
        ThreadHelper.StartTask(state =>
        {
            received = state;
            signal.Set();
        }, 123);
        signal.Wait(TimeSpan.FromSeconds(2)).ShouldBeTrue();
        received.ShouldBe(123);
    }
}


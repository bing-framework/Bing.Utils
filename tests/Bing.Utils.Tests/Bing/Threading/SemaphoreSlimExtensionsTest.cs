using System.Threading;
namespace Bing.Threading;
/// <summary>
/// 信号量(<see cref="SemaphoreSlim"/>) 扩展 测试
/// </summary>
public class SemaphoreSlimExtensionsTest
{
    /// <summary>
    /// 验证信号量锁定的超时、持有与释放行为。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task Test_LockAsync()
    {
        var semaphore = new SemaphoreSlim(0, 1);
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await semaphore.LockAsync(10);
        });
        semaphore = new SemaphoreSlim(1, 1);
        using (await semaphore.LockAsync())
            semaphore.CurrentCount.ShouldBe(0);
        semaphore.CurrentCount.ShouldBe(1);
    }
}

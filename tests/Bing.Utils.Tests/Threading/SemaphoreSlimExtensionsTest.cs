using System.Threading;
using Bing.Threading;

namespace Bing.Utils.Tests.Threading;

/// <summary>
/// 信号量(<see cref="SemaphoreSlim"/>) 扩展 测试
/// </summary>
public class SemaphoreSlimExtensionsTest
{
    /// <summary>
    /// 测试 - 锁定
    /// </summary>
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
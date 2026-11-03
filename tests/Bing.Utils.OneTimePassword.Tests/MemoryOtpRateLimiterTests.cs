using Bing.OneTimePassword;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests;

/// <summary>
/// 验证有界计费、固定过期和取消边界。
/// </summary>
public sealed class MemoryOtpRateLimiterTests
{
    [Fact]
    public async Task ConcurrentRequests_HaveExactBudget_AndExpiryDoesNotSlide()
    {
        var clock=new TestClock();
        using var limiter=new MemoryOtpRateLimiter(10,clock);
        var results=await Task.WhenAll(Enumerable.Range(0,30).Select(_=>Task.Run(()=>limiter.TryAcquireAsync("p",5,TimeSpan.FromMinutes(1)))));
        Assert.Equal(5,results.Count(x=>x));
        clock.UtcNow=clock.UtcNow.AddSeconds(59);
        Assert.False(await limiter.TryAcquireAsync("p",5,TimeSpan.FromMinutes(1)));
        clock.UtcNow=clock.UtcNow.AddSeconds(1);
        Assert.True(await limiter.TryAcquireAsync("p",5,TimeSpan.FromMinutes(1)));
    }
    [Fact]
    public async Task CapacityDoesNotEvictActiveBudget_AndCancellationDoesNotCharge()
    {
        var clock=new TestClock();
        using var limiter=new MemoryOtpRateLimiter(1,clock);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>limiter.TryAcquireAsync("p",1,TimeSpan.FromSeconds(1),new CancellationToken(true)));
        Assert.True(await limiter.TryAcquireAsync("p",1,TimeSpan.FromSeconds(1)));
        Assert.False(await limiter.TryAcquireAsync("q",1,TimeSpan.FromSeconds(1)));
        Assert.False(await limiter.TryAcquireAsync("p",1,TimeSpan.FromSeconds(1)));
        clock.UtcNow=clock.UtcNow.AddSeconds(1);
        Assert.True(await limiter.TryAcquireAsync("q",1,TimeSpan.FromSeconds(1)));
    }
}

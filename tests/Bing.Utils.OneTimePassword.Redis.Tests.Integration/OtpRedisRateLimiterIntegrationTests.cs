using System.Security.Cryptography;
using System.Text;
using Bing.OneTimePassword.Redis;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace Bing.Utils.OneTimePassword.Redis.Tests.Integration;

/// <summary>
/// 仅在配置真实 Redis 连接时执行的集成测试标记。
/// </summary>
public sealed class RedisFactAttribute : FactAttribute
{
    /// <summary>
    /// 未设置 Redis 地址时将测试标记为外部环境阻塞。
    /// </summary>
    public RedisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_OTP_REDIS")))
            Skip = "BLOCKED_EXTERNAL: Set BING_OTP_REDIS to run real Redis integration tests.";
    }
}

/// <summary>
/// 验证 OTP Redis 限流器的真实原子计数、分区隔离和固定过期窗口。
/// </summary>
public sealed class OtpRedisRateLimiterIntegrationTests
{
    /// <summary>
    /// 验证并发请求中成功数恰好等于配置限额。
    /// </summary>
    [RedisFact]
    public async Task TryAcquireAsync_ConcurrentRequests_ShouldAllowExactlyTheLimit()
    {
        var (multiplexer, limiter, prefix) = await CreateLimiterAsync();
        using (multiplexer)
        {
            const int limit = 24;
            var partition = "parallel:" + Guid.NewGuid().ToString("N");
            var key = Key(prefix, partition);
            try
            {
                var results = await Task.WhenAll(Enumerable.Range(0, limit * 3)
                    .Select(_ => limiter.TryAcquireAsync(partition, limit, TimeSpan.FromSeconds(30))));

                results.Count(result => result).ShouldBe(limit);
                results.Count(result => !result).ShouldBe(limit * 2);
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    /// <summary>
    /// 验证分区之间使用独立的计数键。
    /// </summary>
    [RedisFact]
    public async Task TryAcquireAsync_WhenPartitionsDiffer_ShouldUseIndependentLimits()
    {
        var (multiplexer, limiter, prefix) = await CreateLimiterAsync();
        using (multiplexer)
        {
            var partitionA = "partition-a:" + Guid.NewGuid().ToString("N");
            var partitionB = "partition-b:" + Guid.NewGuid().ToString("N");
            try
            {
                (await limiter.TryAcquireAsync(partitionA, 1, TimeSpan.FromSeconds(30))).ShouldBeTrue();
                (await limiter.TryAcquireAsync(partitionA, 1, TimeSpan.FromSeconds(30))).ShouldBeFalse();
                (await limiter.TryAcquireAsync(partitionB, 1, TimeSpan.FromSeconds(30))).ShouldBeTrue();
            }
            finally
            {
                var database = multiplexer.GetDatabase();
                await database.KeyDeleteAsync(Key(prefix, partitionA));
                await database.KeyDeleteAsync(Key(prefix, partitionB));
            }
        }
    }

    /// <summary>
    /// 验证后续超额请求不会续期首次请求设置的过期时间。
    /// </summary>
    [RedisFact]
    public async Task TryAcquireAsync_WhenWindowExpires_ShouldNotBeExtendedByLaterRequests()
    {
        var (multiplexer, limiter, prefix) = await CreateLimiterAsync();
        using (multiplexer)
        {
            const string partition = "fixed-window-expiry";
            var key = Key(prefix, partition);
            try
            {
                (await limiter.TryAcquireAsync(partition, 1, TimeSpan.FromSeconds(4))).ShouldBeTrue();
                await Task.Delay(TimeSpan.FromSeconds(2));
                (await limiter.TryAcquireAsync(partition, 1, TimeSpan.FromSeconds(4))).ShouldBeFalse();

                var ttl = await multiplexer.GetDatabase().KeyTimeToLiveAsync(key);
                ttl.ShouldNotBeNull();
                ttl!.Value.ShouldBeLessThan(TimeSpan.FromSeconds(3));

                var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
                var database = multiplexer.GetDatabase();
                while (DateTimeOffset.UtcNow < deadline && await database.KeyExistsAsync(key))
                    await Task.Delay(100);
                (await database.KeyExistsAsync(key)).ShouldBeFalse();
                (await limiter.TryAcquireAsync(partition, 1, TimeSpan.FromSeconds(4))).ShouldBeTrue();
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    private static async Task<(ConnectionMultiplexer Multiplexer, OtpRedisRateLimiter Limiter, string Prefix)> CreateLimiterAsync()
    {
        var configuration = Environment.GetEnvironmentVariable("BING_OTP_REDIS");
        if (string.IsNullOrWhiteSpace(configuration))
            throw new InvalidOperationException("Redis integration configuration disappeared after test discovery.");

        var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration);
        var prefix = "bing:otp:rate:integration:" + Guid.NewGuid().ToString("N") + ":";
        return (multiplexer, new OtpRedisRateLimiter(multiplexer, prefix), prefix);
    }

    private static RedisKey Key(string prefix, string partition) =>
        prefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(partition)));
}

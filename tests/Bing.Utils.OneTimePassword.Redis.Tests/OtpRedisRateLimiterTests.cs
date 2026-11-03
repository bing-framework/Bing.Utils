using System.Security.Cryptography;
using System.Text;
using Bing.OneTimePassword;
using Bing.OneTimePassword.Redis;
using Moq;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace Bing.Utils.OneTimePassword.Redis.Tests;

/// <summary>
/// 验证 Redis OTP 限流器的输入、原子脚本和取消行为。
/// </summary>
public sealed class OtpRedisRateLimiterTests
{
    private const string KeyPrefix = "unit:otp:rate:";

    /// <summary>
    /// 验证分区计数达到限额后拒绝请求且仍然只派发一次脚本。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_WhenLimitIsReached_ShouldCountEveryRequest()
    {
        var database = new Mock<IDatabase>();
        var results = new Queue<RedisResult>(new[] { Integer(1), Integer(2) });
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Returns(() => Task.FromResult(results.Dequeue()));
        var limiter = CreateLimiter(database);

        (await limiter.TryAcquireAsync("account-1", 1, TimeSpan.FromSeconds(2))).ShouldBeTrue();
        (await limiter.TryAcquireAsync("account-1", 1, TimeSpan.FromSeconds(2))).ShouldBeFalse();

        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), CommandFlags.DemandMaster), Times.Exactly(2));
    }

    /// <summary>
    /// 验证 Redis 键只包含前缀和分区 UTF-8 SHA-256 摘要。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldUseSha256KeyAndFixedWindowArguments()
    {
        var database = new Mock<IDatabase>();
        string? script = null;
        RedisKey[]? keys = null;
        RedisValue[]? arguments = null;
        CommandFlags? flags = null;
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((value, redisKeys, values, commandFlags) =>
            {
                script = value;
                keys = redisKeys;
                arguments = values;
                flags = commandFlags;
            })
            .ReturnsAsync(Integer(1));
        var limiter = CreateLimiter(database);

        (await limiter.TryAcquireAsync("用户-42", 10, TimeSpan.FromMilliseconds(2500))).ShouldBeTrue();

        script.ShouldNotBeNull();
        script!.ShouldContain("redis.call('INCR', KEYS[1])");
        script.ShouldContain("if count == 1 then");
        script.ShouldContain("redis.call('PEXPIRE', KEYS[1], ARGV[1])");
        keys.ShouldNotBeNull();
        keys![0].ToString().ShouldBe(KeyPrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("用户-42"))));
        arguments.ShouldNotBeNull();
        arguments![0].ToString().ShouldBe("2500");
        flags.ShouldBe(CommandFlags.DemandMaster);
    }

    /// <summary>
    /// 验证空分区、越界限额和窗口在派发 Redis 前被拒绝。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_WhenArgumentsAreInvalid_ShouldThrowWithoutDispatch()
    {
        var database = new Mock<IDatabase>();
        var limiter = CreateLimiter(database);

        await Should.ThrowAsync<ArgumentNullException>(() => limiter.TryAcquireAsync(null!, 1, TimeSpan.FromSeconds(1)));
        await Should.ThrowAsync<ArgumentException>(() => limiter.TryAcquireAsync("", 1, TimeSpan.FromSeconds(1)));
        await Should.ThrowAsync<ArgumentException>(() => limiter.TryAcquireAsync(new string('x', 513), 1, TimeSpan.FromSeconds(1)));
        await Should.ThrowAsync<ArgumentException>(() => limiter.TryAcquireAsync("\ud800", 1, TimeSpan.FromSeconds(1)));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => limiter.TryAcquireAsync("p", 0, TimeSpan.FromSeconds(1)));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => limiter.TryAcquireAsync("p", 10001, TimeSpan.FromSeconds(1)));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => limiter.TryAcquireAsync("p", 1, TimeSpan.FromMilliseconds(999)));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => limiter.TryAcquireAsync("p", 1, TimeSpan.FromDays(1).Add(TimeSpan.FromTicks(1))));

        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    /// <summary>
    /// 验证构造函数拒绝无效前缀和数据库编号。
    /// </summary>
    [Fact]
    public void Constructor_WhenOptionsAreInvalid_ShouldThrow()
    {
        var multiplexer = new Mock<IConnectionMultiplexer>();

        Should.Throw<ArgumentNullException>(() => new OtpRedisRateLimiter(null!));
        Should.Throw<ArgumentNullException>(() => new OtpRedisRateLimiter(multiplexer.Object, null!));
        Should.Throw<ArgumentException>(() => new OtpRedisRateLimiter(multiplexer.Object, ""));
        Should.Throw<ArgumentException>(() => new OtpRedisRateLimiter(multiplexer.Object, new string('x', 129)));
        Should.Throw<ArgumentException>(() => new OtpRedisRateLimiter(multiplexer.Object, "bad\u0001prefix"));
        Should.Throw<ArgumentException>(() => new OtpRedisRateLimiter(multiplexer.Object, "bad\udfffprefix"));
        Should.Throw<ArgumentException>(() => new OtpRedisRateLimiter(multiplexer.Object, "prefix-\ud83d\ude00"));
        Should.Throw<ArgumentOutOfRangeException>(() => new OtpRedisRateLimiter(multiplexer.Object, database: -2));
    }

    /// <summary>
    /// 验证取消发生在派发前时不访问 Redis。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_WhenCanceledBeforeDispatch_ShouldNotCallRedis()
    {
        var database = new Mock<IDatabase>();
        var limiter = CreateLimiter(database);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            limiter.TryAcquireAsync("cancel-before-send", 1, TimeSpan.FromSeconds(1), cancellation.Token));

        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    /// <summary>
    /// 验证派发后取消只停止等待，并观察 Redis 操作的迟到故障。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_WhenCanceledAfterDispatch_ShouldObserveLateFailureWithoutRetry()
    {
        var database = new Mock<IDatabase>();
        var dispatched = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = new TaskCompletionSource<RedisResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback(() => dispatched.TrySetResult(true))
            .Returns(() => operation.Task);
        var limiter = CreateLimiter(database);
        using var cancellation = new CancellationTokenSource();

        var call = limiter.TryAcquireAsync("cancel-after-send", 1, TimeSpan.FromSeconds(1), cancellation.Token);
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => call);
        operation.SetException(new RedisServerException("late Redis failure"));

        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    /// <summary>
    /// 验证 Redis 异常包装为存储异常且失败后不重试。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_WhenRedisFails_ShouldWrapFailureWithoutRetry()
    {
        var database = new Mock<IDatabase>();
        var failure = new RedisServerException("script failed");
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromException<RedisResult>(failure));
        var limiter = CreateLimiter(database);

        var exception = await Should.ThrowAsync<OtpStoreException>(() =>
            limiter.TryAcquireAsync("redis-failure", 1, TimeSpan.FromSeconds(1)));

        exception.InnerException.ShouldBeSameAs(failure);
        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    /// <summary>
    /// 验证未由调用方取消导致的 Redis 操作取消会包装为存储故障。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_WhenRedisOperationCancelsUnexpectedly_ShouldWrapFailure()
    {
        var database = new Mock<IDatabase>();
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromException<RedisResult>(new OperationCanceledException("Redis operation canceled.")));
        var limiter = CreateLimiter(database);

        var exception = await Should.ThrowAsync<OtpStoreException>(() =>
            limiter.TryAcquireAsync("redis-operation-cancel", 1, TimeSpan.FromSeconds(1)));

        exception.InnerException.ShouldBeOfType<OperationCanceledException>();
    }

    /// <summary>
    /// 验证无效 Redis 返回类型或计数被作为存储故障拒绝。
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidResults))]
    public async Task TryAcquireAsync_WhenRedisProtocolResultIsInvalid_ShouldThrow(RedisResult result)
    {
        var database = new Mock<IDatabase>();
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(result);
        var limiter = CreateLimiter(database);

        await Should.ThrowAsync<OtpStoreException>(() => limiter.TryAcquireAsync("invalid-result", 1, TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// 验证限流器不会释放宿主提供的 Redis 连接。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldLeaveMultiplexerOwnedByHost()
    {
        var multiplexer = new Mock<IConnectionMultiplexer>();
        var database = new Mock<IDatabase>();
        multiplexer.Setup(multiplexer => multiplexer.GetDatabase(-1, null)).Returns(database.Object);
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(Integer(1));
        var limiter = new OtpRedisRateLimiter(multiplexer.Object);

        await limiter.TryAcquireAsync("owned-by-host", 1, TimeSpan.FromSeconds(1));

        multiplexer.Verify(multiplexer => multiplexer.Dispose(), Times.Never);
    }

    /// <summary>
    /// 返回不符合限流器协议的 Redis 结果样本。
    /// </summary>
    /// <returns>Redis 非整数结果以及非正整数计数。</returns>
    public static IEnumerable<object[]> InvalidResults() => new[]
    {
        new object[] { RedisResult.Create((RedisValue)"one", ResultType.BulkString) },
        new object[] { Integer(0) },
        new object[] { Integer(-1) }
    };

    private static OtpRedisRateLimiter CreateLimiter(Mock<IDatabase> database)
    {
        var multiplexer = new Mock<IConnectionMultiplexer>();
        multiplexer.Setup(multiplexer => multiplexer.GetDatabase(-1, null)).Returns(database.Object);
        return new OtpRedisRateLimiter(multiplexer.Object, KeyPrefix);
    }

    private static RedisResult Integer(long value) => RedisResult.Create((RedisValue)value, ResultType.Integer);
}

using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using Bing.Captcha;
using Bing.Captcha.Redis;
using Moq;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace Bing.Utils.Captcha.Redis.Tests;

public sealed class CaptchaRedisStoreTests
{
    [Fact]
    public async Task Operations_WhenCanceledBeforeDispatch_DoNotCallRedis()
    {
        var database = new Mock<IDatabase>();
        var store = CreateStore(database);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => store.ReplaceAsync(
            CreateContext(), CreateEntry("cancel-before-send"), cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => store.TryTakeAsync(
            CreateContext(), "cancel-before-send", cancellation.Token));

        database.Verify(database => database.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()), Times.Never);
        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public void ReplaceAsync_WhenCanceledAfterDispatch_ObservesLateFailureAndDoesNotRetry()
    {
        var lateFailure = new RedisServerException("late Redis failure");
        var unobservedFailure = 0;
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception.Flatten().InnerExceptions.Any(exception => ReferenceEquals(exception, lateFailure)))
            {
                Interlocked.Exchange(ref unobservedFailure, 1);
                args.SetObserved();
            }
        };
        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            var operationReference = DispatchCanceledWriteAndFail(lateFailure);
            CollectUntilReleased(operationReference);

            IsAlive(operationReference).ShouldBeFalse();
            Volatile.Read(ref unobservedFailure).ShouldBe(0);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }
    }

    [Fact]
    public async Task TryTakeAsync_WhenCanceledAfterDispatch_ThrowsAndLeavesConsumptionOutcomeUnknown()
    {
        var database = new Mock<IDatabase>();
        byte[]? storedPayload = null;
        database.Setup(database => database.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, value, _, _, _) =>
            {
                var bytes = (byte[]?)value;
                storedPayload = bytes?.ToArray() ?? throw new InvalidOperationException("Redis payload was null.");
            })
            .ReturnsAsync(true);
        var dispatched = NewSignal();
        var operation = new TaskCompletionSource<RedisResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback(() => dispatched.TrySetResult(true))
            .Returns(() => operation.Task);
        var store = CreateStore(database);
        var context = CreateContext();
        using var cancellation = new CancellationTokenSource();
        var entry = CreateEntry("take-after-send");
        await store.ReplaceAsync(context, entry);

        var call = store.TryTakeAsync(context, entry.CaptchaId, cancellation.Token);
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => call);

        operation.SetResult(RedisResult.Create((RedisValue)storedPayload!, ResultType.BulkString));
        operation.Task.IsCompletedSuccessfully.ShouldBeTrue();
        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceAsync_WhenRedisWriteFails_ThrowsStoreExceptionWithoutRetry()
    {
        var database = new Mock<IDatabase>();
        var failure = new RedisServerException("write failed");
        database.Setup(database => database.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromException<bool>(failure));
        var store = CreateStore(database);

        var exception = await Should.ThrowAsync<CaptchaStoreException>(() =>
            store.ReplaceAsync(CreateContext(), CreateEntry("write-failure")));

        exception.InnerException.ShouldBeSameAs(failure);
        database.Verify(database => database.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task TryTakeAsync_WhenRedisFails_ThrowsStoreExceptionWithOriginalFailure()
    {
        var database = new Mock<IDatabase>();
        var failure = new RedisServerException("script failed");
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromException<RedisResult>(failure));
        var store = CreateStore(database);

        var exception = await Should.ThrowAsync<CaptchaStoreException>(() =>
            store.TryTakeAsync(CreateContext(), "backend-failure"));

        exception.InnerException.ShouldBeSameAs(failure);
        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceAndTake_RoundTripAllEntryFields()
    {
        var database = new Mock<IDatabase>();
        byte[]? storedPayload = null;
        database.Setup(database => database.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, value, _, _, _) =>
            {
                var bytes = (byte[]?)value;
                storedPayload = bytes?.ToArray() ?? throw new InvalidOperationException("Redis payload was null.");
            })
            .ReturnsAsync(true);
        database.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Returns(() => Task.FromResult(RedisResult.Create((RedisValue)storedPayload!, ResultType.BulkString)));
        var store = CreateStore(database);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(2).AddTicks(1234);
        var expectedHash = RandomNumberGenerator.GetBytes(32);
        var entry = new CaptchaEntry("round-trip", expectedHash, "key-version", expiresAt, true, 1);

        await store.ReplaceAsync(CreateContext(), entry);
        var actual = await store.TryTakeAsync(CreateContext(), entry.CaptchaId);

        actual.ShouldNotBeNull();
        actual!.CaptchaId.ShouldBe(entry.CaptchaId);
        actual.KeyVersion.ShouldBe(entry.KeyVersion);
        actual.ExpiresAt.ToUnixTimeMilliseconds().ShouldBe(entry.ExpiresAt.ToUnixTimeMilliseconds());
        actual.IgnoreAsciiCase.ShouldBeTrue();
        actual.NormalizationVersion.ShouldBe(1);
        actual.AnswerHash.SequenceEqual(expectedHash).ShouldBeTrue();
        database.Verify(database => database.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(), CommandFlags.DemandMaster), Times.Once);
        database.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
            CommandFlags.DemandMaster), Times.Once);
    }

    private static CaptchaRedisStore CreateStore(Mock<IDatabase> database)
    {
        var multiplexer = new Mock<IConnectionMultiplexer>();
        multiplexer.Setup(multiplexer => multiplexer.GetDatabase(-1, null)).Returns(database.Object);
        return new CaptchaRedisStore(multiplexer.Object, "unit:captcha:");
    }

    private static CaptchaContext CreateContext() => new("login", "unit-session", "form");

    private static CaptchaEntry CreateEntry(string id) => new(
        id,
        new byte[32],
        "unit-key",
        DateTimeOffset.UtcNow.AddMinutes(2),
        true);

    private static TaskCompletionSource<bool> NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Task<bool>> DispatchCanceledWriteAndFail(Exception failure)
    {
        var database = new Mock<IDatabase>();
        var dispatched = NewSignal();
        var operation = new TaskCompletionSource<bool>();
        database.Setup(database => database.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .Callback(() => dispatched.TrySetResult(true))
            .Returns(() => operation.Task);
        var store = CreateStore(database);
        using var cancellation = new CancellationTokenSource();

        var call = store.ReplaceAsync(CreateContext(), CreateEntry("cancel-after-send"), cancellation.Token);
        dispatched.Task.GetAwaiter().GetResult();
        cancellation.Cancel();
        Should.Throw<OperationCanceledException>(() => call.GetAwaiter().GetResult());
        operation.SetException(failure);
        database.Verify(database => database.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()), Times.Once);

        return new WeakReference<Task<bool>>(operation.Task);
    }

    private static void CollectUntilReleased<T>(WeakReference<T> reference) where T : class
    {
        for (var attempt = 0; attempt < 5 && IsAlive(reference); attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
    }

    private static bool IsAlive<T>(WeakReference<T> reference) where T : class => reference.TryGetTarget(out _);
}

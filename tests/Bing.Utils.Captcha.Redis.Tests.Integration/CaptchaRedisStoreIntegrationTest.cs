using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bing.Captcha;
using Bing.Captcha.Redis;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace Bing.Utils.Captcha.Redis.Tests.Integration;

public sealed class RedisFactAttribute : FactAttribute
{
    public RedisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_CAPTCHA_REDIS")))
            Skip = "BLOCKED_EXTERNAL: Set BING_CAPTCHA_REDIS to run real Redis integration tests.";
    }
}

public sealed class CaptchaRedisStoreIntegrationTest
{
    [Fact]
    public async Task TryTakeAsync_WhenRedisConnectionIsUnavailable_ThrowsStoreException()
    {
        var configuration = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            BacklogPolicy = BacklogPolicy.FailFast,
            ConnectRetry = 0,
            ConnectTimeout = 500,
            SyncTimeout = 500,
            AsyncTimeout = 500
        };
        configuration.EndPoints.Add(IPAddress.Loopback, GetUnusedLoopbackPort());
        using var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration);
        var store = new CaptchaRedisStore(multiplexer, "bing:captcha:unavailable:");

        var exception = await Should.ThrowAsync<CaptchaStoreException>(() =>
            store.TryTakeAsync(new CaptchaContext("login", "unavailable-session", "form"), "connection-failure"));

        exception.InnerException.ShouldNotBeNull();
    }

    [RedisFact]
    public async Task ReplaceAsync_SetsExpiryAndReplacesCurrentEntry()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                var database = multiplexer.GetDatabase();
                var first = CreateEntry("old-id", DateTimeOffset.UtcNow.AddSeconds(30));
                var current = CreateEntry("new-id", DateTimeOffset.UtcNow.AddSeconds(30));
                await store.ReplaceAsync(context, first);
                await store.ReplaceAsync(context, current);

                var ttl = await database.KeyTimeToLiveAsync(key);
                ttl.ShouldNotBeNull();
                ttl!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
                ttl.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
                (await store.TryTakeAsync(context, first.CaptchaId)).ShouldBeNull();
                var consumed = await store.TryTakeAsync(context, current.CaptchaId);
                consumed!.CaptchaId.ShouldBe(current.CaptchaId);
                consumed.ExpiresAt.ToUnixTimeMilliseconds().ShouldBe(current.ExpiresAt.ToUnixTimeMilliseconds());
                (await store.TryTakeAsync(context, current.CaptchaId)).ShouldBeNull();
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task TryTakeAsync_ConcurrentCalls_ReturnsEntryExactlyOnce()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                var entry = CreateEntry("single-use", DateTimeOffset.UtcNow.AddSeconds(30));
                await store.ReplaceAsync(context, entry);
                var results = await Task.WhenAll(Enumerable.Range(0, 16)
                    .Select(_ => store.TryTakeAsync(context, entry.CaptchaId)));

                results.Count(result => result != null).ShouldBe(1);
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task TryTakeAsync_AcrossIndependentMultiplexers_ReturnsEntryExactlyOnce()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        using (var secondMultiplexer = await ConnectAsync())
        {
            try
            {
                var secondStore = new CaptchaRedisStore(secondMultiplexer, store.KeyPrefix);
                var entry = CreateEntry("independent-single-use", DateTimeOffset.UtcNow.AddSeconds(30));
                await store.ReplaceAsync(context, entry);

                var results = await Task.WhenAll(
                    store.TryTakeAsync(context, entry.CaptchaId),
                    secondStore.TryTakeAsync(context, entry.CaptchaId));

                results.Count(result => result != null).ShouldBe(1);
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task ReplaceAsync_ExpiredEntryIsRemovedByRedis()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                await store.ReplaceAsync(context, CreateEntry("expires", DateTimeOffset.UtcNow.AddSeconds(3)));
                var deadline = DateTimeOffset.UtcNow.AddSeconds(8);
                var database = multiplexer.GetDatabase();
                while (DateTimeOffset.UtcNow < deadline && await database.KeyExistsAsync(key))
                    await Task.Delay(100);

                (await database.KeyExistsAsync(key)).ShouldBeFalse();
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task ReplaceAsync_ConcurrentRefreshAndOldIdTake_PreservesReplacement()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        using (var secondMultiplexer = await ConnectAsync())
        {
            try
            {
                var secondStore = new CaptchaRedisStore(secondMultiplexer, store.KeyPrefix);
                var oldEntry = CreateEntry("race-old-id", DateTimeOffset.UtcNow.AddSeconds(30));
                var replacement = CreateEntry("race-new-id", DateTimeOffset.UtcNow.AddSeconds(30));
                await store.ReplaceAsync(context, oldEntry);

                var refresh = store.ReplaceAsync(context, replacement);
                var oldTake = secondStore.TryTakeAsync(context, oldEntry.CaptchaId);
                await Task.WhenAll(refresh, oldTake);

                if (oldTake.Result != null)
                    oldTake.Result.CaptchaId.ShouldBe(oldEntry.CaptchaId);
                (await store.TryTakeAsync(context, replacement.CaptchaId))!.CaptchaId.ShouldBe(replacement.CaptchaId);
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task TryTakeAsync_DoesNotConsumeEntryForDifferentContext()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                var entry = CreateEntry("bound-id", DateTimeOffset.UtcNow.AddSeconds(30));
                await store.ReplaceAsync(context, entry);

                var otherContext = new CaptchaContext(context.Purpose, context.BindingId + "-other", context.FlowId);
                (await store.TryTakeAsync(otherContext, entry.CaptchaId)).ShouldBeNull();
                (await store.TryTakeAsync(context, entry.CaptchaId))!.CaptchaId.ShouldBe(entry.CaptchaId);
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task TryTakeAsync_RoundTripsSingleCharacterIdAcrossLuaHeaderOffset()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                var entry = CreateEntry("A", DateTimeOffset.UtcNow.AddSeconds(30));
                await store.ReplaceAsync(context, entry);

                var consumed = await store.TryTakeAsync(context, entry.CaptchaId);
                consumed.ShouldNotBeNull();
                consumed!.CaptchaId.ShouldBe("A");
                consumed.AnswerHash.SequenceEqual(entry.AnswerHash).ShouldBeTrue();
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task TryTakeAsync_UnknownProtocolIsPreservedAndReturnsStorageError()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                await multiplexer.GetDatabase().StringSetAsync(key, new byte[] { 0, 1, 2, 3, 4, 5 });

                await Should.ThrowAsync<CaptchaStoreException>(() => store.TryTakeAsync(context, "A"));
                (await multiplexer.GetDatabase().KeyExistsAsync(key)).ShouldBeTrue();
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task TryTakeAsync_MalformedIdLengthIsPreservedAndReturnsStorageError()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                await multiplexer.GetDatabase().StringSetAsync(key, new byte[] { 0x42, 0x43, 0x52, 0x01, 0, 0 });

                await Should.ThrowAsync<CaptchaStoreException>(() => store.TryTakeAsync(context, "A"));
                (await multiplexer.GetDatabase().KeyExistsAsync(key)).ShouldBeTrue();
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    [RedisFact]
    public async Task TryTakeAsync_RoundTripsMaximumBmpUtf8Fields()
    {
        var (multiplexer, store, context, key) = await CreateStoreAsync();
        using (multiplexer)
        {
            try
            {
                var id = new string('验', 64);
                var keyVersion = new string('密', 64);
                var entry = CreateEntry(id, DateTimeOffset.UtcNow.AddSeconds(30), keyVersion);
                await store.ReplaceAsync(context, entry);

                var consumed = await store.TryTakeAsync(context, id);
                consumed.ShouldNotBeNull();
                consumed!.CaptchaId.ShouldBe(id);
                consumed.KeyVersion.ShouldBe(keyVersion);
            }
            finally
            {
                await multiplexer.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }

    private static async Task<(ConnectionMultiplexer Multiplexer, CaptchaRedisStore Store, CaptchaContext Context, RedisKey Key)> CreateStoreAsync()
    {
        var multiplexer = await ConnectAsync();
        var prefix = "bing:captcha:integration:" + Guid.NewGuid().ToString("N") + ":";
        var store = new CaptchaRedisStore(multiplexer, prefix);
        var context = new CaptchaContext("login", Guid.NewGuid().ToString("N"), "form");
        return (multiplexer, store, context, prefix + context.GetStorageKey());
    }

    private static async Task<ConnectionMultiplexer> ConnectAsync()
    {
        var configuration = Environment.GetEnvironmentVariable("BING_CAPTCHA_REDIS");
        if (string.IsNullOrWhiteSpace(configuration))
            throw new InvalidOperationException("Redis integration test configuration disappeared after test discovery.");
        return await ConnectionMultiplexer.ConnectAsync(configuration);
    }

    private static int GetUnusedLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static CaptchaEntry CreateEntry(string id, DateTimeOffset expiresAt, string keyVersion = "test-key-v1")
    {
        var hash = RandomNumberGenerator.GetBytes(32);
        try
        {
            return new CaptchaEntry(id, hash, keyVersion, expiresAt, true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
        }
    }
}

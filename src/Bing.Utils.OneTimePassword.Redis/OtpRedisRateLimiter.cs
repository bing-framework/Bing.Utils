using System.Security.Cryptography;
using System.Text;
using Bing.OneTimePassword;
using StackExchange.Redis;

namespace Bing.OneTimePassword.Redis;

/// <summary>
/// 使用 Redis 为单个分区执行原子固定窗口限流。
/// </summary>
public sealed class OtpRedisRateLimiter : IOtpRateLimiter
{
    private const string RateLimitScript = @"
local count = redis.call('INCR', KEYS[1])
if count == 1 then
    local expirySet = redis.call('PEXPIRE', KEYS[1], ARGV[1])
    if expirySet ~= 1 then
        return redis.error_reply('Invalid OTP rate limiter expiry protocol')
    end
end
return count";

    private const string HexAlphabet = "0123456789ABCDEF";

    private readonly IDatabase _database;
    private readonly string _keyPrefix;

    /// <summary>
    /// 创建限流器并复用由宿主持有的 Redis 连接。
    /// </summary>
    /// <param name="multiplexer">由调用方负责释放的 Redis 多路复用器。</param>
    /// <param name="keyPrefix">应用隔离前缀，长度为 1 到 128 个 UTF-16 码元。</param>
    /// <param name="database">Redis 数据库编号；-1 表示默认数据库。</param>
    public OtpRedisRateLimiter(IConnectionMultiplexer multiplexer, string keyPrefix = "bing:otp:rate:v1:", int database = -1)
    {
        if (multiplexer == null)
            throw new ArgumentNullException(nameof(multiplexer));
        if (keyPrefix == null)
            throw new ArgumentNullException(nameof(keyPrefix));
        if (keyPrefix.Length == 0 || keyPrefix.Length > 128 || ContainsUnsupportedPrefixCharacter(keyPrefix))
            throw new ArgumentException("Redis 限流键前缀长度无效或包含不支持的字符。", nameof(keyPrefix));
        if (database < -1)
            throw new ArgumentOutOfRangeException(nameof(database));

        _database = multiplexer.GetDatabase(database);
        _keyPrefix = keyPrefix;
    }

    /// <summary>
    /// 原子增加分区计数，并仅在首次请求时设置固定窗口过期时间。
    /// </summary>
    /// <param name="partition">用于隔离额度的非空分区标识，最多 512 个 UTF-16 码元。</param>
    /// <param name="limit">允许的最大请求数，范围为 1 到 10000。</param>
    /// <param name="window">固定窗口长度，范围为 1 秒到 1 天。</param>
    /// <param name="cancellationToken">用于取消派发前检查或停止等待在途 Redis 操作。</param>
    /// <returns>当前计数未超过限额时返回 <see langword="true" />。</returns>
    /// <exception cref="ArgumentNullException">分区标识为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentException">分区标识为空、过长或含无效 Unicode 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">限额或窗口超出支持范围时抛出。</exception>
    /// <exception cref="OperationCanceledException">取消请求先于操作完成时抛出。</exception>
    /// <exception cref="OtpStoreException">Redis 操作失败或返回协议不符合预期时抛出。</exception>
    public async Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        Validate(partition, limit, window);
        cancellationToken.ThrowIfCancellationRequested();

        var key = GetKey(partition);
        var ttlMilliseconds = (long)Math.Ceiling(window.TotalMilliseconds);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var operation = _database.ScriptEvaluateAsync(
                RateLimitScript,
                new[] { (RedisKey)key },
                new RedisValue[] { ttlMilliseconds },
                CommandFlags.DemandMaster);
            var result = await AwaitWithCancellation(operation, cancellationToken).ConfigureAwait(false);
            if (result.Resp2Type != ResultType.Integer)
                throw new OtpStoreException("Redis 限流器返回了无效协议结果。");

            var count = (long)result;
            if (count < 1)
                throw new OtpStoreException("Redis 限流器返回了无效计数。");
            return count <= limit;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OtpStoreException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new OtpStoreException("Redis 限流操作失败。", exception);
        }
    }

    private static void Validate(string partition, int limit, TimeSpan window)
    {
        if (partition == null)
            throw new ArgumentNullException(nameof(partition));
        if (partition.Length == 0 || partition.Length > 512 || HasMalformedSurrogate(partition))
            throw new ArgumentException("限流分区标识为空、过长或包含无效 Unicode。", nameof(partition));
        if (limit < 1 || limit > 10000)
            throw new ArgumentOutOfRangeException(nameof(limit));
        if (window < TimeSpan.FromSeconds(1) || window > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(window));
    }

    private string GetKey(string partition)
    {
        var partitionBytes = Encoding.UTF8.GetBytes(partition);
        byte[] digest;
        try
        {
            using var sha256 = SHA256.Create();
            digest = sha256.ComputeHash(partitionBytes);
        }
        finally
        {
            Array.Clear(partitionBytes, 0, partitionBytes.Length);
        }

        try
        {
            var suffix = new char[digest.Length * 2];
            for (var index = 0; index < digest.Length; index++)
            {
                suffix[index * 2] = HexAlphabet[digest[index] >> 4];
                suffix[index * 2 + 1] = HexAlphabet[digest[index] & 0x0f];
            }
            try
            {
                return _keyPrefix + new string(suffix);
            }
            finally
            {
                Array.Clear(suffix, 0, suffix.Length);
            }
        }
        finally
        {
            Array.Clear(digest, 0, digest.Length);
        }
    }

    private static bool ContainsUnsupportedPrefixCharacter(string value)
    {
        foreach (var character in value)
        {
            if (char.IsControl(character) || char.IsSurrogate(character))
                return true;
        }
        return false;
    }

    private static bool HasMalformedSurrogate(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    return true;
                index++;
            }
            else if (char.IsLowSurrogate(value[index]))
            {
                return true;
            }
        }
        return false;
    }

    private static async Task<T> AwaitWithCancellation<T>(Task<T> operation, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
            return await operation.ConfigureAwait(false);

        var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), canceled))
        {
            var completed = await Task.WhenAny(operation, canceled.Task).ConfigureAwait(false);
            if (!ReferenceEquals(completed, operation))
            {
                ObserveLateFailure(operation);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        return await operation.ConfigureAwait(false);
    }

    private static void ObserveLateFailure(Task operation)
    {
        _ = operation.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}

using System.Security.Cryptography;

namespace Bing.IdUtils;

/// <summary>
/// 随机字符串生成器
/// </summary>
public static class RandomNonceStrGenerator
{
    /// <summary>
    /// 每次从密码学安全随机数生成器读取的最大字节数。
    /// </summary>
    private const int RandomByteChunkSize = 256;

    /// <summary>
    /// 随机字符串
    /// </summary>
    private const string NonceStr = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// 使用密码学安全随机数生成器生成随机字符串；结果不保证全局唯一。
    /// </summary>
    /// <param name="forceToAvoidRepetition">为兼容旧调用保留，不影响随机算法；结果不承诺全局唯一</param>
    public static string Create(bool forceToAvoidRepetition = false) => Create(16, forceToAvoidRepetition);

    /// <summary>
    /// 使用密码学安全随机数生成器生成随机字符串；结果不保证全局唯一。
    /// </summary>
    /// <param name="length">长度</param>
    /// <param name="forceToAvoidRepetition">为兼容旧调用保留，不影响随机算法；结果不承诺全局唯一</param>
    public static string Create(int length, bool forceToAvoidRepetition = false)
    {
        if (length <= 16)
            length = 16;

        using var rng = RandomNumberGenerator.Create();
        return CreateCore(length, rng);
    }

    /// <summary>
    /// 根据提供的随机源生成字符串，供测试确定性验证字符映射与拒绝采样。
    /// </summary>
    /// <param name="length">目标长度；小于等于 16 时使用 16。</param>
    /// <param name="rng">随机源，由调用方管理其生命周期。</param>
    /// <returns>生成的随机字符串。</returns>
    internal static string CreateCore(int length, RandomNumberGenerator rng)
    {
        if (rng is null)
            throw new ArgumentNullException(nameof(rng));
        if (length <= 16)
            length = 16;

        var chars = new char[length];
        var randomBytes = new byte[Math.Min(RandomByteChunkSize, chars.Length)];
        var usableRange = byte.MaxValue + 1 - (byte.MaxValue + 1) % NonceStr.Length;
        var charIndex = 0;
        while (charIndex < chars.Length)
        {
            var requestedByteCount = Math.Min(randomBytes.Length, chars.Length - charIndex);
            rng.GetBytes(randomBytes, 0, requestedByteCount);
            for (var byteIndex = 0; byteIndex < requestedByteCount && charIndex < chars.Length; byteIndex++)
            {
                var value = randomBytes[byteIndex];
                if (value >= usableRange)
                    continue;

                chars[charIndex++] = NonceStr[value % NonceStr.Length];
            }
        }

        return new string(chars);
    }
}

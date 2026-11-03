using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bing.Security;

namespace Bing.OneTimePassword;

/// <summary>
/// 计算基于 HMAC 的事件计数器口令。
/// </summary>
public static class Hotp
{
    /// <summary>
    /// 根据共享密钥和计数器生成固定宽度的十进制口令。
    /// </summary>
    /// <param name="secret">至少 16 字节且不超过 128 字节的共享密钥。</param>
    /// <param name="counter">以无符号 64 位整数表示的移动计数器。</param>
    /// <param name="algorithm">用于计算 HMAC 的算法。</param>
    /// <param name="digits">生成 6 位或 8 位口令。</param>
    /// <returns>保留前导零的 ASCII 数字口令。</returns>
    public static string Generate(ReadOnlySpan<byte> secret, ulong counter, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1, int digits = 6)
    {
        Validate(secret, algorithm, digits);
        var digest = ComputeMac(secret, counter, algorithm);
        try
        {
            var offset = digest[digest.Length - 1] & 0x0f;
            var binary = ((uint)(digest[offset] & 0x7f) << 24)
                | ((uint)digest[offset + 1] << 16)
                | ((uint)digest[offset + 2] << 8)
                | digest[offset + 3];
            var divisor = digits == 6 ? 1_000_000u : 100_000_000u;
            return (binary % divisor).ToString(digits == 6 ? "D6" : "D8", CultureInfo.InvariantCulture);
        }
        finally
        {
            Array.Clear(digest, 0, digest.Length);
        }
    }

    /// <summary>
    /// 在有界的向前计数器窗口内匹配口令。
    /// </summary>
    /// <param name="secret">至少 16 字节且不超过 128 字节的共享密钥。</param>
    /// <param name="code">用户提交的 ASCII 数字口令。</param>
    /// <param name="counter">本次验证的起始计数器。</param>
    /// <param name="lookAhead">从起始计数器开始包含的最大偏移，范围为 0 到 100。</param>
    /// <param name="algorithm">用于计算 HMAC 的算法。</param>
    /// <param name="digits">当前口令应为 6 位或 8 位。</param>
    /// <returns>最高的匹配计数器；格式错误或窗口内无匹配时返回 <see langword="null" />。</returns>
    public static ulong? FindMatch(ReadOnlySpan<byte> secret, string? code, ulong counter, int lookAhead = 5, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1, int digits = 6)
    {
        Validate(secret, algorithm, digits);
        if (lookAhead < 0 || lookAhead > 100)
            throw new ArgumentOutOfRangeException(nameof(lookAhead));
        if (!IsCode(code, digits))
            return null;

        var upper = ulong.MaxValue - counter < (ulong)lookAhead
            ? ulong.MaxValue
            : counter + (ulong)lookAhead;
        ulong? match = null;
        var candidate = counter;
        while (true)
        {
            var expected = Generate(secret, candidate, algorithm, digits);
            if (FixedTimeCodeEquals(code!, expected))
                match = candidate;

            if (candidate == upper)
                break;
            candidate++;
        }
        return match;
    }

    /// <summary>
    /// 验证共享密钥和算法参数的边界。
    /// </summary>
    /// <param name="secret">待验证的共享密钥。</param>
    /// <param name="algorithm">待验证的 HMAC 算法。</param>
    /// <param name="digits">待验证的口令位数。</param>
    /// <exception cref="ArgumentOutOfRangeException">密钥长度、算法或位数无效时抛出。</exception>
    internal static void Validate(ReadOnlySpan<byte> secret, OtpHashAlgorithm algorithm, int digits)
    {
        if (secret.Length < 16 || secret.Length > 128)
            throw new ArgumentOutOfRangeException(nameof(secret), "OTP 密钥必须为 16 到 128 字节。");
        if (!Enum.IsDefined(typeof(OtpHashAlgorithm), algorithm))
            throw new ArgumentOutOfRangeException(nameof(algorithm));
        if (digits != 6 && digits != 8)
            throw new ArgumentOutOfRangeException(nameof(digits));
    }

    /// <summary>
    /// 计算指定算法的 HMAC 摘要并清理临时密钥与计数器缓冲区。
    /// </summary>
    /// <param name="secret">共享密钥。</param>
    /// <param name="counter">计数器。</param>
    /// <param name="algorithm">HMAC 算法。</param>
    /// <returns>新分配的 HMAC 摘要；调用方负责清理。</returns>
    private static byte[] ComputeMac(ReadOnlySpan<byte> secret, ulong counter, OtpHashAlgorithm algorithm)
    {
        var key = secret.ToArray();
        var message = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(message, counter);
        try
        {
            using HMAC hmac = algorithm switch
            {
                OtpHashAlgorithm.Sha1 => new HMACSHA1(key),
                OtpHashAlgorithm.Sha256 => new HMACSHA256(key),
                OtpHashAlgorithm.Sha512 => new HMACSHA512(key),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
            };
            return hmac.ComputeHash(message);
        }
        finally
        {
            Array.Clear(key, 0, key.Length);
            Array.Clear(message, 0, message.Length);
        }
    }

    /// <summary>
    /// 检查用户输入是否恰为所需宽度的 ASCII 数字。
    /// </summary>
    /// <param name="code">待检查的用户输入。</param>
    /// <param name="digits">所需位数。</param>
    /// <returns>输入格式正确时为 <see langword="true" />。</returns>
    internal static bool IsCode(string? code, int digits)
    {
        if (code == null || code.Length != digits)
            return false;
        for (var index = 0; index < code.Length; index++)
        {
            if (code[index] < '0' || code[index] > '9')
                return false;
        }
        return true;
    }

    /// <summary>
    /// 使用固定时间比较两条 ASCII 数字口令。
    /// </summary>
    /// <param name="code">用户输入。</param>
    /// <param name="expected">算法生成的预期值。</param>
    /// <returns>两条口令完全相同时为 <see langword="true" />。</returns>
    internal static bool FixedTimeCodeEquals(string code, string expected)
    {
        var actualBytes = Encoding.ASCII.GetBytes(code);
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        try
        {
            return SecureComparison.FixedTimeEquals(actualBytes, expectedBytes);
        }
        finally
        {
            Array.Clear(actualBytes, 0, actualBytes.Length);
            Array.Clear(expectedBytes, 0, expectedBytes.Length);
        }
    }
}

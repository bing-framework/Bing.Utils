namespace Bing.OneTimePassword;

/// <summary>
/// 计算基于 Unix 时间步的一次性口令。
/// </summary>
public static class Totp
{
    /// <summary>
    /// 根据 UTC 时刻生成当前时间步口令。
    /// </summary>
    /// <param name="secret">至少 16 字节且不超过 128 字节的共享密钥。</param>
    /// <param name="timestamp">当前 UTC 时间，不得早于 Unix Epoch。</param>
    /// <param name="algorithm">用于计算 HMAC 的算法。</param>
    /// <param name="digits">生成 6 位或 8 位口令。</param>
    /// <param name="periodSeconds">时间步长度，必须为 30 或 60 秒。</param>
    /// <returns>保留前导零的 ASCII 数字口令。</returns>
    public static string Generate(ReadOnlySpan<byte> secret, DateTimeOffset timestamp, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1, int digits = 6, int periodSeconds = 30)
    {
        Validate(timestamp, periodSeconds);
        return Hotp.Generate(secret, (ulong)GetStep(timestamp, periodSeconds), algorithm, digits);
    }

    /// <summary>
    /// 在当前时间步及有界前后窗口中匹配口令。
    /// </summary>
    /// <param name="secret">至少 16 字节且不超过 128 字节的共享密钥。</param>
    /// <param name="code">用户提交的 ASCII 数字口令。</param>
    /// <param name="timestamp">当前 UTC 时间，不得早于 Unix Epoch。</param>
    /// <param name="window">包含当前步的前后窗口，范围为 0 或 1 步。</param>
    /// <param name="algorithm">用于计算 HMAC 的算法。</param>
    /// <param name="digits">当前口令应为 6 位或 8 位。</param>
    /// <param name="periodSeconds">时间步长度，必须为 30 或 60 秒。</param>
    /// <returns>最高的匹配时间步；格式错误或窗口内无匹配时返回 <see langword="null" />。</returns>
    public static long? FindMatch(ReadOnlySpan<byte> secret, string? code, DateTimeOffset timestamp, int window = 1, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1, int digits = 6, int periodSeconds = 30)
    {
        Validate(timestamp, periodSeconds);
        Hotp.Validate(secret, algorithm, digits);
        if (window < 0 || window > 1)
            throw new ArgumentOutOfRangeException(nameof(window));
        if (!Hotp.IsCode(code, digits))
            return null;

        var current = GetStep(timestamp, periodSeconds);
        long? match = null;
        for (var offset = -window; offset <= window; offset++)
        {
            var step = current + offset;
            if (step < 0)
                continue;
            var expected = Hotp.Generate(secret, (ulong)step, algorithm, digits);
            if (Hotp.FixedTimeCodeEquals(code!, expected))
                match = step;
        }
        return match;
    }

    /// <summary>
    /// 获取时间戳对应的时间步计数器。
    /// </summary>
    /// <param name="timestamp">Unix Epoch 或之后的时间戳。</param>
    /// <param name="periodSeconds">已验证的时间步长度。</param>
    /// <returns>以有符号 64 位整数表示的移动因子。</returns>
    internal static long GetStep(DateTimeOffset timestamp, int periodSeconds) => timestamp.ToUnixTimeSeconds() / periodSeconds;

    /// <summary>
    /// 验证时间戳和 TOTP 时间步边界。
    /// </summary>
    /// <param name="timestamp">待验证的时间戳。</param>
    /// <param name="periodSeconds">待验证的时间步秒数。</param>
    private static void Validate(DateTimeOffset timestamp, int periodSeconds)
    {
        if (timestamp.ToUnixTimeSeconds() < 0)
            throw new ArgumentOutOfRangeException(nameof(timestamp));
        if (periodSeconds != 30 && periodSeconds != 60)
            throw new ArgumentOutOfRangeException(nameof(periodSeconds));
    }
}

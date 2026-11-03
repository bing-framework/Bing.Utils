namespace Bing.OneTimePassword;

/// <summary>
/// 一次性口令类型。
/// </summary>
public enum OtpType
{
    /// <summary>
    /// 基于时间步的口令。
    /// </summary>
    Totp = 1,
    /// <summary>
    /// 基于计数器的口令。
    /// </summary>
    Hotp = 2
}

/// <summary>
/// 一次性口令使用的 HMAC 算法。
/// </summary>
public enum OtpHashAlgorithm
{
    /// <summary>
    /// RFC 4226 的 HMAC-SHA1。
    /// </summary>
    Sha1 = 1,
    /// <summary>
    /// TOTP 标准或 HOTP 扩展的 HMAC-SHA256。
    /// </summary>
    Sha256 = 2,
    /// <summary>
    /// TOTP 标准或 HOTP 扩展的 HMAC-SHA512。
    /// </summary>
    Sha512 = 3
}

/// <summary>
/// 绑定时固定的一次性口令参数。
/// </summary>
public sealed class OtpParameters
{
    /// <summary>
    /// 创建不可变算法参数。
    /// </summary>
    /// <param name="type">TOTP 或 HOTP。</param>
    /// <param name="algorithm">HMAC 算法。</param>
    /// <param name="digits">6 或 8 位数字。</param>
    /// <param name="periodSeconds">TOTP 的 30 或 60 秒时间步；HOTP 固定为 30。</param>
    public OtpParameters(OtpType type = OtpType.Totp, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1, int digits = 6, int periodSeconds = 30)
    {
        if (type != OtpType.Totp && type != OtpType.Hotp) throw new ArgumentOutOfRangeException(nameof(type));
        if (!Enum.IsDefined(typeof(OtpHashAlgorithm), algorithm)) throw new ArgumentOutOfRangeException(nameof(algorithm));
        if (digits != 6 && digits != 8) throw new ArgumentOutOfRangeException(nameof(digits));
        if (periodSeconds != 30 && periodSeconds != 60) throw new ArgumentOutOfRangeException(nameof(periodSeconds));
        if (type == OtpType.Hotp && periodSeconds != 30) throw new ArgumentException("HOTP 不配置时间步。", nameof(periodSeconds));
        Type = type; Algorithm = algorithm; Digits = digits; PeriodSeconds = periodSeconds;
    }

    /// <summary>
    /// 获取口令类型。
    /// </summary>
    public OtpType Type { get; }
    /// <summary>
    /// 获取 HMAC 算法。
    /// </summary>
    public OtpHashAlgorithm Algorithm { get; }
    /// <summary>
    /// 获取输出位数。
    /// </summary>
    public int Digits { get; }
    /// <summary>
    /// 获取时间步秒数。
    /// </summary>
    public int PeriodSeconds { get; }
}

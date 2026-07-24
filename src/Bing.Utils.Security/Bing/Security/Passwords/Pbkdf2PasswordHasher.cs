#if !NETSTANDARD2_0
using Bing.Security.Encoding;
using Bing.Security.Randomness;

namespace Bing.Security.Passwords;

/// <summary>
/// 提供版本化 PBKDF2-HMAC-SHA256 密码哈希和验证操作。
/// </summary>
public sealed class Pbkdf2PasswordHasher
{
    /// <summary>
    /// 密码哈希格式版本标识。
    /// </summary>
    private const string FormatVersion = "BSP1";

    /// <summary>
    /// 密码哈希算法标识。
    /// </summary>
    private const string Algorithm = "PBKDF2-SHA256";

    /// <summary>
    /// 当前密码哈希选项。
    /// </summary>
    private readonly Pbkdf2PasswordHasherOptions _options;

    /// <summary>
    /// 使用指定或默认安全参数初始化密码哈希器。
    /// </summary>
    /// <param name="options">密码哈希选项，未指定时使用默认值。</param>
    public Pbkdf2PasswordHasher(Pbkdf2PasswordHasherOptions options = null)
    {
        _options = options ?? new Pbkdf2PasswordHasherOptions();
        ValidateOptions(_options);
    }

    /// <summary>
    /// 为密码创建版本化 PBKDF2-HMAC-SHA256 哈希。
    /// </summary>
    /// <param name="password">要哈希的密码，可为空字符串。</param>
    /// <returns><c>BSP1$PBKDF2-SHA256$iterations$salt$hash</c> 格式的密码记录。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="password"/> 为 <c>null</c> 时抛出。</exception>
    public string Hash(string password)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));

        var salt = SecurityRandom.GetBytes(_options.SaltSize);
        var hash = Pbkdf2KeyDerivation.DeriveKey(password, salt, _options.HashSize, _options.IterationCount);
        try
        {
            return string.Join("$", FormatVersion, Algorithm, _options.IterationCount.ToString(System.Globalization.CultureInfo.InvariantCulture), Base64UrlEncoding.Encode(salt), Base64UrlEncoding.Encode(hash));
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(salt);
            CryptographicOperationsCompat.ZeroMemory(hash);
        }
    }

    /// <summary>
    /// 验证密码是否匹配版本化 PBKDF2-HMAC-SHA256 哈希。
    /// </summary>
    /// <param name="password">要验证的密码。</param>
    /// <param name="encodedHash">已编码的密码哈希记录。</param>
    /// <returns>验证失败、成功或需要重新哈希的结果。</returns>
    public PasswordVerificationResult Verify(string password, string encodedHash)
    {
        if (password == null || string.IsNullOrWhiteSpace(encodedHash))
            return PasswordVerificationResult.Failed;
        var parts = encodedHash.Split('$');
        if (parts.Length != 5 || parts[0] != FormatVersion || parts[1] != Algorithm || !int.TryParse(parts[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var iterations) || iterations < 100000 || !Base64UrlEncoding.TryDecode(parts[3], out var salt) || !Base64UrlEncoding.TryDecode(parts[4], out var expected) || salt.Length < 16 || expected.Length < 32)
            return PasswordVerificationResult.Failed;

        try
        {
            var actual = Pbkdf2KeyDerivation.DeriveKey(password, salt, expected.Length, iterations);
            try
            {
                if (!SecureComparison.FixedTimeEquals(actual, expected))
                    return PasswordVerificationResult.Failed;
                return iterations < _options.IterationCount || salt.Length < _options.SaltSize || expected.Length < _options.HashSize
                    ? PasswordVerificationResult.SuccessRehashNeeded
                    : PasswordVerificationResult.Success;
            }
            finally
            {
                CryptographicOperationsCompat.ZeroMemory(actual);
            }
        }
        catch (ArgumentException)
        {
            return PasswordVerificationResult.Failed;
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(salt);
            CryptographicOperationsCompat.ZeroMemory(expected);
        }
    }

    /// <summary>
    /// 验证密码哈希选项的安全下限。
    /// </summary>
    /// <param name="options">要验证的选项。</param>
    /// <exception cref="ArgumentOutOfRangeException">参数低于安全下限时抛出。</exception>
    private static void ValidateOptions(Pbkdf2PasswordHasherOptions options)
    {
        if (options.IterationCount < 100000)
            throw new ArgumentOutOfRangeException(nameof(options), "PBKDF2 迭代次数必须不小于 100000。");
        if (options.SaltSize < 16)
            throw new ArgumentOutOfRangeException(nameof(options), "密码盐长度必须不小于 16 字节。");
        if (options.HashSize < 32)
            throw new ArgumentOutOfRangeException(nameof(options), "密码哈希长度必须不小于 32 字节。");
    }
}
#endif
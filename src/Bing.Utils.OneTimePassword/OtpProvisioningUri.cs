using System.Globalization;
using System.Text;

namespace Bing.OneTimePassword;

/// <summary>
/// 创建并解析供认证器扫描或手工录入的 otpauth URI。
/// </summary>
public static class OtpProvisioningUri
{
    /// <summary>
    /// Provisioning URI 输入与输出的最大字符数。
    /// </summary>
    private const int MaximumUriLength = 8192;

    /// <summary>
    /// 创建包含算法、位数和类型参数的 RFC 4648 无填充 URI。
    /// </summary>
    /// <param name="parameters">绑定期间固定的 OTP 参数。</param>
    /// <param name="issuer">服务或租户显示名称，不得包含冒号或控制字符。</param>
    /// <param name="account">账号显示名称，不得包含冒号或控制字符。</param>
    /// <param name="secret">16 到 128 字节的共享密钥。</param>
    /// <param name="initialCounter">HOTP 初始计数器；TOTP 必须为零。</param>
    /// <returns>认证器可导入的 otpauth URI；它包含密钥，须作为敏感信息处理。</returns>
    /// <exception cref="ArgumentNullException">参数、服务名称或账号名称为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentException">名称不合规或 TOTP 计数器非零时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">共享密钥长度无效时抛出。</exception>
    public static string Create(OtpParameters parameters, string issuer, string account, ReadOnlySpan<byte> secret, ulong initialCounter = 0)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
        ValidateLabel(issuer, nameof(issuer));
        ValidateLabel(account, nameof(account));
        ValidateSecret(secret);
        if (parameters.Type == OtpType.Totp && initialCounter != 0)
            throw new ArgumentException("TOTP 不使用初始计数器。", nameof(initialCounter));

        var type = parameters.Type == OtpType.Totp ? "totp" : "hotp";
        var label = Uri.EscapeDataString(issuer) + ":" + Uri.EscapeDataString(account);
        var query = "secret=" + OtpSecretEncoding.Encode(secret)
            + "&issuer=" + Uri.EscapeDataString(issuer)
            + "&algorithm=" + parameters.Algorithm.ToString().ToUpperInvariant()
            + "&digits=" + parameters.Digits.ToString(CultureInfo.InvariantCulture);
        query += parameters.Type == OtpType.Totp
            ? "&period=" + parameters.PeriodSeconds.ToString(CultureInfo.InvariantCulture)
            : "&counter=" + initialCounter.ToString(CultureInfo.InvariantCulture);
        var result = "otpauth://" + type + "/" + label + "?" + query;
        if (result.Length > MaximumUriLength)
            throw new ArgumentOutOfRangeException(nameof(issuer), "otpauth URI 超过长度上限。");
        return result;
    }

    /// <summary>
    /// 严格解析受限长度且不含未知或重复参数的 otpauth URI。
    /// </summary>
    /// <param name="value">待解析的完整 TOTP 或 HOTP URI。</param>
    /// <returns>不自动打印密钥的解析结果；其 Base32 密钥属性仍属敏感数据。</returns>
    /// <exception cref="ArgumentNullException">输入为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="FormatException">URI 结构、参数、标签或共享密钥无效时抛出。</exception>
    public static OtpProvisioningInfo Parse(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (value.Length == 0 || value.Length > MaximumUriLength || value.IndexOf('#') >= 0 || !IsSafeText(value))
            throw Invalid();
        // Uri 会规范化部分畸形转义，必须先校验原始文本。
        ValidateEscapes(value);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !uri.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.Port != -1)
            throw Invalid();

        var type = uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase)
            ? OtpType.Totp
            : uri.Host.Equals("hotp", StringComparison.OrdinalIgnoreCase)
                ? OtpType.Hotp
                : throw Invalid();
        var expectedAuthority = type == OtpType.Totp ? "totp" : "hotp";
        if (!uri.Authority.Equals(expectedAuthority, StringComparison.OrdinalIgnoreCase))
            throw Invalid();

        var path = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        if (path.Length == 0 || path.IndexOf('/') >= 0)
            throw Invalid();
        var label = DecodeComponent(path);
        var separator = label.IndexOf(':');
        if (separator <= 0 || separator != label.LastIndexOf(':') || separator == label.Length - 1)
            throw Invalid();
        var labelIssuer = label.Substring(0, separator);
        var account = label.Substring(separator + 1);
        ValidateLabelForParse(labelIssuer);
        ValidateLabelForParse(account);

        var query = ParseQuery(uri.Query);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "secret", "issuer", "algorithm", "digits", "period", "counter"
        };
        if (query.Keys.Any(key => !allowed.Contains(key)) || !query.TryGetValue("secret", out var encodedSecret))
            throw Invalid();

        var issuer = query.TryGetValue("issuer", out var queryIssuer) ? queryIssuer : labelIssuer;
        ValidateLabelForParse(issuer);
        if (!issuer.Equals(labelIssuer, StringComparison.Ordinal))
            throw Invalid();

        var algorithm = OtpHashAlgorithm.Sha1;
        if (query.TryGetValue("algorithm", out var algorithmValue))
        {
            switch (algorithmValue.ToUpperInvariant())
            {
                case "SHA1":
                    algorithm = OtpHashAlgorithm.Sha1;
                    break;
                case "SHA256":
                    algorithm = OtpHashAlgorithm.Sha256;
                    break;
                case "SHA512":
                    algorithm = OtpHashAlgorithm.Sha512;
                    break;
                default:
                    throw Invalid();
            }
        }

        var digits = ReadInteger(query, "digits", 6);
        var period = ReadInteger(query, "period", 30);
        var hasCounter = query.TryGetValue("counter", out var counterValue);
        if ((type == OtpType.Totp && hasCounter) || (type == OtpType.Hotp && query.ContainsKey("period")))
            throw Invalid();
        ulong counter = 0;
        if (type == OtpType.Hotp && (!hasCounter || !ulong.TryParse(counterValue, NumberStyles.None, CultureInfo.InvariantCulture, out counter)))
            throw Invalid();

        OtpParameters parameters;
        try
        {
            parameters = new OtpParameters(type, algorithm, digits, period);
        }
        catch (ArgumentException)
        {
            throw Invalid();
        }

        byte[] secret;
        try
        {
            secret = OtpSecretEncoding.Decode(encodedSecret);
        }
        catch (FormatException)
        {
            throw Invalid();
        }
        try
        {
            ValidateSecret(secret);
            return new OtpProvisioningInfo(parameters, issuer, account, OtpSecretEncoding.Encode(secret), counter);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw Invalid();
        }
        finally
        {
            Array.Clear(secret, 0, secret.Length);
        }
    }

    /// <summary>
    /// 解析查询串并拒绝空项、重复键和畸形百分号转义。
    /// </summary>
    /// <param name="query">URI 的原始查询部分。</param>
    /// <returns>用不区分大小写的键保存的解码参数。</returns>
    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            throw Invalid();
        var value = query[0] == '?' ? query.Substring(1) : query;
        foreach (var pair in value.Split('&'))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0 || separator == pair.Length - 1 || pair.IndexOf('=', separator + 1) >= 0)
                throw Invalid();
            var key = DecodeComponent(pair.Substring(0, separator));
            var item = DecodeComponent(pair.Substring(separator + 1));
            if (key.Length == 0 || item.Length == 0 || result.ContainsKey(key))
                throw Invalid();
            result.Add(key, item);
        }
        return result;
    }

    /// <summary>
    /// 解码一个 URI 组件，同时拒绝不完整或非十六进制百分号转义。
    /// </summary>
    /// <param name="value">已从 URI 中截取的转义组件。</param>
    /// <returns>解码后的 Unicode 文本。</returns>
    private static string DecodeComponent(string value)
    {
        ValidateEscapes(value);
        return Uri.UnescapeDataString(value);
    }

    /// <summary>
    /// 拒绝畸形百分号和非规范 UTF-8 字节序列，防止 URI 规范化掩盖输入错误。
    /// </summary>
    /// <param name="value">尚未百分号解码的 URI 或组件。</param>
    private static void ValidateEscapes(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
                continue;
            var start = index;
            var count = 0;
            while (index < value.Length && value[index] == '%')
            {
                if (index + 2 >= value.Length || !Uri.IsHexDigit(value[index + 1]) || !Uri.IsHexDigit(value[index + 2]))
                    throw Invalid();
                count++;
                index += 3;
            }
            var bytes = new byte[count];
            try
            {
                for (var i = 0; i < count; i++)
                    bytes[i] = byte.Parse(value.Substring(start + i * 3 + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                _ = new UTF8Encoding(false, true).GetCharCount(bytes);
            }
            catch (DecoderFallbackException) { throw Invalid(); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
            index--;
        }
    }

    /// <summary>
    /// 读取查询参数中的无符号十进制整数，或返回兼容默认值。
    /// </summary>
    /// <param name="query">已解析的查询参数。</param>
    /// <param name="name">要读取的参数名称。</param>
    /// <param name="defaultValue">参数缺失时使用的默认值。</param>
    /// <returns>参数的十进制整数值。</returns>
    private static int ReadInteger(Dictionary<string, string> query, string name, int defaultValue)
    {
        if (!query.TryGetValue(name, out var text))
            return defaultValue;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            throw Invalid();
        return value;
    }

    /// <summary>
    /// 验证创建 URI 时提供的 issuer 或账号标签。
    /// </summary>
    /// <param name="value">需要编码进 URI 的标签。</param>
    /// <param name="name">参数名称。</param>
    private static void ValidateLabel(string value, string name)
    {
        if (value == null)
            throw new ArgumentNullException(name);
        if (value.Length == 0 || value.Length > 256 || value.IndexOf(':') >= 0 || !IsSafeText(value))
            throw new ArgumentException("标签为空、过长、含冒号或含非法 Unicode 字符。", name);
    }

    /// <summary>
    /// 验证解析后的 issuer 或账号标签。
    /// </summary>
    /// <param name="value">URI 中解出的标签。</param>
    private static void ValidateLabelForParse(string value)
    {
        if (value.Length == 0 || value.Length > 256 || value.IndexOf(':') >= 0 || !IsSafeText(value))
            throw Invalid();
    }

    /// <summary>
    /// 检查文本无控制字符且 UTF-16 代理项配对完整。
    /// </summary>
    /// <param name="value">待检查文本。</param>
    /// <returns>文本可安全编码时为 <see langword="true" />。</returns>
    private static bool IsSafeText(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsControl(character))
                return false;
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    return false;
                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 验证适合注册给认证器的共享密钥长度。
    /// </summary>
    /// <param name="secret">共享密钥。</param>
    /// <exception cref="ArgumentOutOfRangeException">密钥少于 16 字节或多于 128 字节时抛出。</exception>
    private static void ValidateSecret(ReadOnlySpan<byte> secret)
    {
        if (secret.Length < 16 || secret.Length > 128)
            throw new ArgumentOutOfRangeException(nameof(secret), "OTP 密钥必须为 16 到 128 字节。");
    }

    /// <summary>
    /// 创建不包含 URI 内容的稳定格式异常。
    /// </summary>
    /// <returns>不泄露查询参数的格式异常实例。</returns>
    private static FormatException Invalid() => new("otpauth URI 无效或包含不支持的参数。");
}

/// <summary>
/// 保存认证器 Provisioning URI 的安全解析结果。
/// </summary>
public sealed class OtpProvisioningInfo
{
    /// <summary>
    /// 创建解析结果；构造函数仅供 URI 解析器使用。
    /// </summary>
    /// <param name="parameters">绑定算法参数。</param>
    /// <param name="issuer">认证器显示的服务名称。</param>
    /// <param name="account">认证器显示的账号名称。</param>
    /// <param name="secretBase32">大写无填充的敏感 Base32 密钥。</param>
    /// <param name="initialCounter">HOTP 初始计数器；TOTP 为零。</param>
    internal OtpProvisioningInfo(OtpParameters parameters, string issuer, string account, string secretBase32, ulong initialCounter)
    {
        Parameters = parameters;
        Issuer = issuer;
        Account = account;
        SecretBase32 = secretBase32;
        InitialCounter = initialCounter;
    }

    /// <summary>
    /// 获取不可变算法参数。
    /// </summary>
    public OtpParameters Parameters { get; }
    /// <summary>
    /// 获取认证器显示的服务名称。
    /// </summary>
    public string Issuer { get; }
    /// <summary>
    /// 获取认证器显示的账号名称。
    /// </summary>
    public string Account { get; }
    /// <summary>
    /// 获取大写无填充 Base32 共享密钥；此属性包含敏感数据。
    /// </summary>
    public string SecretBase32 { get; }
    /// <summary>
    /// 获取 HOTP 初始计数器；TOTP 的值恒为零。
    /// </summary>
    public ulong InitialCounter { get; }

    /// <summary>
    /// 返回不包含账号、URI 或共享密钥的类型名称。
    /// </summary>
    /// <returns>固定的非敏感类型描述。</returns>
    public override string ToString() => nameof(OtpProvisioningInfo);
}

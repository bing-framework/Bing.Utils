using System.Text;
using Bing.OneTimePassword;
using Shouldly;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests.Algorithms;

/// <summary>
/// 验证 OTP 算法和认证器 Provisioning URI。
/// </summary>
public sealed class OtpAlgorithmTests
{
    /// <summary>
    /// 验证 RFC 4226 给出的全部 HOTP-SHA1 计算向量。
    /// </summary>
    [Fact]
    public void HotpGenerate_WhenUsingRfc4226Vectors_ShouldMatchEveryCode()
    {
        var key = Encoding.ASCII.GetBytes("12345678901234567890");
        var expected = new[] { "755224", "287082", "359152", "969429", "338314", "254676", "287922", "162583", "399871", "520489" };

        for (ulong counter = 0; counter < (ulong)expected.Length; counter++)
            Hotp.Generate(key, counter).ShouldBe(expected[(int)counter]);
    }

    /// <summary>
    /// 验证 RFC 6238 在 SHA1、SHA256 和 SHA512 下的官方八位 TOTP 向量。
    /// </summary>
    /// <param name="algorithm">向量使用的 HMAC 算法。</param>
    /// <param name="key">算法对应的 RFC 测试密钥。</param>
    /// <param name="expected">六个测试时间对应的官方结果。</param>
    [Theory]
    [InlineData(OtpHashAlgorithm.Sha1, "12345678901234567890", "94287082,07081804,14050471,89005924,69279037,65353130")]
    [InlineData(OtpHashAlgorithm.Sha256, "12345678901234567890123456789012", "46119246,68084774,67062674,91819424,90698825,77737706")]
    [InlineData(OtpHashAlgorithm.Sha512, "1234567890123456789012345678901234567890123456789012345678901234", "90693936,25091201,99943326,93441116,38618901,47863826")]
    public void TotpGenerate_WhenUsingRfc6238Vectors_ShouldMatchAllTimes(OtpHashAlgorithm algorithm, string key, string expected)
    {
        var timestamps = new long[] { 59, 1_111_111_109, 1_111_111_111, 1_234_567_890, 2_000_000_000, 20_000_000_000 };
        var expectedCodes = expected.Split(',');
        var secret = Encoding.ASCII.GetBytes(key);

        for (var index = 0; index < timestamps.Length; index++)
        {
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamps[index]);
            Totp.Generate(secret, timestamp, algorithm, digits: 8).ShouldBe(expectedCodes[index]);
        }
    }

    /// <summary>
    /// 验证口令保持固定宽度并保留前导零。
    /// </summary>
    [Fact]
    public void Generate_WhenCodeContainsLeadingZero_ShouldPreserveIt()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var code = Hotp.Generate(secret, 1);

        code.ShouldBe("287082");
        Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(1_111_111_109), digits: 8).ShouldBe("07081804");
    }

    /// <summary>
    /// 验证 TOTP 在 30 秒边界切换到下一个时间步。
    /// </summary>
    [Fact]
    public void TotpGenerate_WhenCrossingPeriodBoundary_ShouldAdvanceCounter()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");

        Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(29)).ShouldBe("755224");
        Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(30)).ShouldBe("287082");
        Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(59)).ShouldBe("287082");
        Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(60)).ShouldBe("359152");
    }

    /// <summary>
    /// 验证 HOTP 在指定窗口检查全部候选，并优先报告最高匹配计数器。
    /// </summary>
    [Fact]
    public void HotpFindMatch_WhenCodeFallsInWindow_ShouldReturnMatchedCounter()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var code = Hotp.Generate(secret, 3);

        Hotp.FindMatch(secret, code, 0, lookAhead: 5).ShouldBe(3UL);
        Hotp.FindMatch(secret, code, 0, lookAhead: 2).ShouldBeNull();
        Hotp.FindMatch(secret, "not-six", 0).ShouldBeNull();
        Hotp.FindMatch(secret, "１２３４５６", 0).ShouldBeNull();
        Hotp.FindMatch(secret, " 755224", 0).ShouldBeNull();
    }

    /// <summary>
    /// 验证 HOTP 窗口计算不会在 ulong 上限回绕到零。
    /// </summary>
    [Fact]
    public void HotpFindMatch_WhenWindowWouldOverflow_ShouldStopAtMaximumCounter()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var code = Hotp.Generate(secret, ulong.MaxValue);

        Hotp.FindMatch(secret, code, ulong.MaxValue - 1, lookAhead: 5).ShouldBe(ulong.MaxValue);
        Hotp.FindMatch(secret, code, ulong.MaxValue, lookAhead: 0).ShouldBe(ulong.MaxValue);
    }

    /// <summary>
    /// 验证真实 HMAC 碰撞时完整扫描窗口并选择较大计数器。
    /// </summary>
    [Fact]
    public void HotpFindMatch_WhenTwoFactorsCollide_ShouldSelectHighest()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        Hotp.Generate(secret, 2386).ShouldBe("709847");
        Hotp.Generate(secret, 2394).ShouldBe("709847");
        Hotp.FindMatch(secret, "709847", 2386, lookAhead: 8).ShouldBe(2394UL);
    }

    /// <summary>
    /// 验证算法拒绝无效密钥、参数和超出限制的匹配窗口。
    /// </summary>
    [Fact]
    public void GenerateAndFindMatch_WhenConfigurationIsInvalid_ShouldThrow()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(60);

        Should.Throw<ArgumentOutOfRangeException>(() => Hotp.Generate(new byte[15], 0));
        Should.Throw<ArgumentOutOfRangeException>(() => Hotp.Generate(secret, 0, (OtpHashAlgorithm)0));
        Should.Throw<ArgumentOutOfRangeException>(() => Hotp.Generate(secret, 0, digits: 7));
        Should.Throw<ArgumentOutOfRangeException>(() => Hotp.FindMatch(secret, "bad", 0, lookAhead: 101));
        Should.Throw<ArgumentOutOfRangeException>(() => Totp.Generate(secret, timestamp, periodSeconds: 45));
        Should.Throw<ArgumentOutOfRangeException>(() => Totp.FindMatch(secret, "bad", timestamp, window: 2));
        Should.Throw<ArgumentOutOfRangeException>(() => Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(-1)));
        Should.Throw<ArgumentOutOfRangeException>(() => Totp.FindMatch(secret, "bad", timestamp, digits: 7));
    }

    /// <summary>
    /// 验证 TOTP 时间步边界以及允许的前后窗口。
    /// </summary>
    [Fact]
    public void TotpFindMatch_WhenCodeIsAtAdjacentStep_ShouldReturnItsStep()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var currentTime = DateTimeOffset.FromUnixTimeSeconds(60);
        var previousStep = Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(30));
        var nextStep = Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(90));

        Totp.FindMatch(secret, previousStep, currentTime, window: 1).ShouldBe(1L);
        Totp.FindMatch(secret, nextStep, currentTime, window: 1).ShouldBe(3L);
        Totp.FindMatch(secret, previousStep, currentTime, window: 0).ShouldBeNull();
        Totp.FindMatch(secret, "１２３４５６", currentTime).ShouldBeNull();
    }

    /// <summary>
    /// 验证窗口内发生真实六位口令碰撞时返回最大的时间步。
    /// </summary>
    [Fact]
    public void TotpFindMatch_WhenTwoFactorsCollide_ShouldSelectHighest()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var earlierStep = 153567L;
        var laterStep = 153569L;
        var code = "468457";

        Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(earlierStep * 30)).ShouldBe(code);
        Totp.Generate(secret, DateTimeOffset.FromUnixTimeSeconds(laterStep * 30)).ShouldBe(code);
        Totp.FindMatch(secret, code, DateTimeOffset.FromUnixTimeSeconds(153568 * 30), window: 1).ShouldBe(laterStep);
    }

    /// <summary>
    /// 验证 DateTimeOffset 最大值对应的 Unix 时间、口令和匹配步数。
    /// </summary>
    /// <param name="algorithm">测试使用的 HMAC 算法。</param>
    /// <param name="periodSeconds">时间步长度。</param>
    /// <param name="digits">口令位数。</param>
    /// <param name="key">固定的 RFC 测试密钥。</param>
    /// <param name="expectedCode">由独立 HMAC 计算得到的预期口令。</param>
    /// <param name="expectedStep">最大时间对应的预期 Unix 时间步。</param>
    [Theory]
    [InlineData(OtpHashAlgorithm.Sha1, 30, 6, "12345678901234567890", "099568", 8446743359L)]
    [InlineData(OtpHashAlgorithm.Sha1, 60, 8, "12345678901234567890", "12344134", 4223371679L)]
    [InlineData(OtpHashAlgorithm.Sha256, 30, 6, "12345678901234567890123456789012", "300069", 8446743359L)]
    [InlineData(OtpHashAlgorithm.Sha256, 60, 8, "12345678901234567890123456789012", "00571743", 4223371679L)]
    [InlineData(OtpHashAlgorithm.Sha512, 30, 6, "1234567890123456789012345678901234567890123456789012345678901234", "458292", 8446743359L)]
    [InlineData(OtpHashAlgorithm.Sha512, 60, 8, "1234567890123456789012345678901234567890123456789012345678901234", "91573915", 4223371679L)]
    public void TotpGenerateAndFindMatch_WhenTimestampIsMaximum_ShouldUseExpectedCounter(
        OtpHashAlgorithm algorithm, int periodSeconds, int digits, string key, string expectedCode, long expectedStep)
    {
        var secret = Encoding.ASCII.GetBytes(key);
        var timestamp = DateTimeOffset.MaxValue;

        timestamp.ToUnixTimeSeconds().ShouldBe(253402300799L);
        Totp.Generate(secret, timestamp, algorithm, digits, periodSeconds).ShouldBe(expectedCode);
        Totp.FindMatch(secret, expectedCode, timestamp, window: 1, algorithm: algorithm, digits: digits, periodSeconds: periodSeconds).ShouldBe(expectedStep);
    }

    /// <summary>
    /// 验证严格的无填充 Base32 RFC 4648 编解码向量。
    /// </summary>
    /// <param name="text">RFC 测试文本。</param>
    /// <param name="encoded">对应的大写无填充 Base32 值。</param>
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY")]
    [InlineData("fo", "MZXQ")]
    [InlineData("foo", "MZXW6")]
    [InlineData("foob", "MZXW6YQ")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI")]
    public void Base32EncodeAndDecode_WhenUsingRfc4648Vectors_ShouldRoundTrip(string text, string encoded)
    {
        var bytes = Encoding.ASCII.GetBytes(text);

        OtpSecretEncoding.Encode(bytes).ShouldBe(encoded);
        OtpSecretEncoding.Decode(encoded).ShouldBe(bytes);
        OtpSecretEncoding.Decode(encoded.ToLowerInvariant()).ShouldBe(bytes);
    }

    /// <summary>
    /// 验证 Base32 解码拒绝填充、空白、非法尾位和不支持的字符。
    /// </summary>
    /// <param name="encoded">不符合无填充规范的输入。</param>
    [Theory]
    [InlineData("M")]
    [InlineData("MZ")]
    [InlineData("MY=")]
    [InlineData("MY======")]
    [InlineData("M Y")]
    [InlineData("M0")]
    [InlineData("ＭＹ")]
    public void Base32Decode_WhenInputIsNonCanonical_ShouldThrowFormatException(string encoded)
    {
        Should.Throw<FormatException>(() => OtpSecretEncoding.Decode(encoded));
    }

    /// <summary>
    /// 验证每类 Base32 长度的所有非零未使用尾位都被拒绝。
    /// </summary>
    [Fact]
    public void Base32Decode_WhenEveryUnusedTailBitIsNonZero_ShouldThrowForEachLengthClass()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var cases = new[]
        {
            (ByteCount: 1, UnusedBits: 2, Canonical: "AA"),
            (ByteCount: 2, UnusedBits: 4, Canonical: "AAAA"),
            (ByteCount: 3, UnusedBits: 1, Canonical: "AAAAA"),
            (ByteCount: 4, UnusedBits: 3, Canonical: "AAAAAAA")
        };

        foreach (var testCase in cases)
        {
            var bytes = new byte[testCase.ByteCount];
            var canonical = testCase.Canonical;
            (canonical.Length % 8).ShouldBe(testCase.ByteCount switch { 1 => 2, 2 => 4, 3 => 5, _ => 7 });
            OtpSecretEncoding.Encode(bytes).ShouldBe(canonical);
            OtpSecretEncoding.Decode(canonical).ShouldBe(bytes);

            var prefix = canonical[..^1];
            var unusedMask = (1 << testCase.UnusedBits) - 1;
            for (var nonZeroTailBits = 1; nonZeroTailBits <= unusedMask; nonZeroTailBits++)
            {
                var invalid = prefix + alphabet[nonZeroTailBits];
                Should.Throw<FormatException>(() => OtpSecretEncoding.Decode(invalid));
                Should.Throw<FormatException>(() => OtpSecretEncoding.Decode(invalid.ToLowerInvariant()));
            }
        }
    }

    /// <summary>
    /// 验证 TOTP URI 完整往返 issuer、账号、算法、密钥和时间步参数。
    /// </summary>
    [Fact]
    public void ProvisioningUri_WhenUsingTotp_ShouldRoundTripAndKeepSecretOutOfToString()
    {
        var secret = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var parameters = new OtpParameters(OtpType.Totp, OtpHashAlgorithm.Sha256, digits: 8, periodSeconds: 60);
        var uri = OtpProvisioningUri.Create(parameters, "Example Service", "alice+device@example.com", secret);

        uri.ShouldContain("otpauth://totp/Example%20Service:");
        var parsed = OtpProvisioningUri.Parse(uri);
        parsed.Parameters.Type.ShouldBe(OtpType.Totp);
        parsed.Parameters.Algorithm.ShouldBe(OtpHashAlgorithm.Sha256);
        parsed.Parameters.Digits.ShouldBe(8);
        parsed.Parameters.PeriodSeconds.ShouldBe(60);
        parsed.Issuer.ShouldBe("Example Service");
        parsed.Account.ShouldBe("alice+device@example.com");
        parsed.SecretBase32.ShouldBe(OtpSecretEncoding.Encode(secret));
        parsed.InitialCounter.ShouldBe(0UL);
        parsed.ToString().ShouldNotContain(parsed.SecretBase32);
        parsed.ToString().ShouldNotContain(parsed.Account);
    }

    /// <summary>
    /// 验证 HOTP URI 要求并往返保存无符号初始计数器。
    /// </summary>
    [Fact]
    public void ProvisioningUri_WhenUsingHotp_ShouldRoundTripMaximumCounter()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var parameters = new OtpParameters(OtpType.Hotp, OtpHashAlgorithm.Sha1, digits: 6);
        var uri = OtpProvisioningUri.Create(parameters, "Example", "terminal-1", secret, ulong.MaxValue);

        var parsed = OtpProvisioningUri.Parse(uri);
        parsed.Parameters.Type.ShouldBe(OtpType.Hotp);
        parsed.InitialCounter.ShouldBe(ulong.MaxValue);
        parsed.SecretBase32.ShouldBe(OtpSecretEncoding.Encode(secret));
        uri.ShouldContain("&counter=18446744073709551615");
    }

    /// <summary>
    /// 验证 Unicode、百分号及路径分隔符只在组件边界转义一次。
    /// </summary>
    [Fact]
    public void ProvisioningUri_WhenLabelsNeedEscaping_ShouldRoundTrip()
    {
        var uri = OtpProvisioningUri.Create(new OtpParameters(), "测试 / 100%", "账号+手机@example.test", new byte[32]);
        var parsed = OtpProvisioningUri.Parse(uri);
        parsed.Issuer.ShouldBe("测试 / 100%");
        parsed.Account.ShouldBe("账号+手机@example.test");
    }

    /// <summary>
    /// 验证解析器拒绝未知或重复参数、跨来源 issuer、端口和用户信息。
    /// </summary>
    /// <param name="uri">需要拒绝的 Provisioning URI。</param>
    [Theory]
    [InlineData("otpauth://totp/Issuer:bad%GG?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://totp/Issuer:bad%C0%AF?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://totp/Issuer:bad\naccount?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Issuer&extra=x")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Other")]
    [InlineData("otpauth://user@totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://totp:443/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ#fragment")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&counter=1")]
    [InlineData("otpauth://hotp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("otpauth://hotp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&period=30&counter=0")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Issuer&digits=7")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Issuer&algorithm=MD5")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Issuer&algorithm=1")]
    [InlineData("otpauth://totp/Issuer:account?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=Issuer&bad=%Q0")]
    public void ProvisioningUriParse_WhenStructureOrParametersAreInvalid_ShouldThrow(string uri)
    {
        Should.Throw<FormatException>(() => OtpProvisioningUri.Parse(uri));
    }

    /// <summary>
    /// 验证创建 URI 时拒绝不合规的标签、密钥长度和无效 TOTP 计数器。
    /// </summary>
    [Fact]
    public void ProvisioningUriCreate_WhenInputViolatesContract_ShouldThrow()
    {
        var secret = new byte[20];
        var totp = new OtpParameters();

        Should.Throw<ArgumentException>(() => OtpProvisioningUri.Create(totp, "Issuer:Extra", "account", secret));
        Should.Throw<ArgumentException>(() => OtpProvisioningUri.Create(totp, "Issuer", "account", secret, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => OtpProvisioningUri.Create(totp, "Issuer", "account", new byte[15]));
        Should.Throw<ArgumentOutOfRangeException>(() => OtpProvisioningUri.Create(totp, "Issuer", "account", new byte[129]));
    }
}

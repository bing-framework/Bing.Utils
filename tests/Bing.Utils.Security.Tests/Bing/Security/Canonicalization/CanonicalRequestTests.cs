using System;
using System.Globalization;
using Bing.Security.Canonicalization;
using Bing.Security.Keys;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests.Bing.Security.Canonicalization;

/// <summary>
/// 验证确定性参数规范化和请求签名行为。
/// </summary>
public class CanonicalRequestTests
{
    /// <summary>
    /// 测试目的：默认 RFC 3986 编码必须避免参数值中的分隔符与独立参数产生相同待签名文本。
    /// </summary>
    [Fact]
    public void Serialize_WhenValueContainsSeparators_ShouldNotCollideWithSeparateParameters()
    {
        // Arrange
        var embedded = new[] { new CanonicalParameter("a", "1&b=2") };
        var separate = new[] { new CanonicalParameter("a", "1"), new CanonicalParameter("b", "2") };

        // Act
        var embeddedResult = CanonicalParameterSerializer.Serialize(embedded);
        var separateResult = CanonicalParameterSerializer.Serialize(separate);

        // Assert
        embeddedResult.ShouldBe("a=1%26b%3D2");
        separateResult.ShouldBe("a=1&b=2");
        embeddedResult.ShouldNotBe(separateResult);
    }

    /// <summary>
    /// 测试目的：关闭 URL 编码时，含分隔符的值、空分隔符或相同分隔符必须被拒绝。
    /// </summary>
    [Fact]
    public void Serialize_WhenRawSeparatorsAreAmbiguous_ShouldThrowArgumentException()
    {
        // Arrange
        var parameter = new[] { new CanonicalParameter("a", "1&b=2") };
        var rawOptions = new CanonicalParameterOptions { UrlEncodeKeys = false, UrlEncodeValues = false };
        var emptySeparatorOptions = new CanonicalParameterOptions { PairSeparator = string.Empty };
        var sameSeparatorOptions = new CanonicalParameterOptions { PairSeparator = "&", KeyValueSeparator = "&" };

        // Act
        var rawAction = new Action(() => CanonicalParameterSerializer.Serialize(parameter, rawOptions));
        var emptyAction = new Action(() => CanonicalParameterSerializer.Serialize(parameter, emptySeparatorOptions));
        var sameAction = new Action(() => CanonicalParameterSerializer.Serialize(parameter, sameSeparatorOptions));

        // Assert
        rawAction.ShouldThrow<ArgumentException>();
        emptyAction.ShouldThrow<ArgumentException>();
        sameAction.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：未指定 Kind 的 DateTime 不得依赖服务器本地时区参与签名。
    /// </summary>
    [Fact]
    public void Serialize_WhenDateTimeKindIsUnspecified_ShouldThrowArgumentException()
    {
        // Arrange
        var parameters = new[] { new CanonicalParameter("time", new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Unspecified)) };

        // Act
        var action = new Action(() => CanonicalParameterSerializer.Serialize(parameters));

        // Assert
        action.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：规范化应采用 Ordinal 排序、固定文化格式、显式数组规则并区分 null 与空字符串。
    /// </summary>
    [Fact]
    public void Serialize_WhenParametersContainMixedValues_ShouldUseDeterministicRules()
    {
        // Arrange
        var parameters = new[]
        {
            new CanonicalParameter("z", 2, 1),
            new CanonicalParameter("empty", string.Empty),
            new CanonicalParameter("null", (object)null),
            new CanonicalParameter("decimal", 1.5m),
            new CanonicalParameter("time", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(8))),
            new CanonicalParameter("flag", true),
            new CanonicalParameter("items", new[] { "中文", "second" })
        };
        var options = new CanonicalParameterOptions { IgnoreNullValues = false, UrlEncodeValues = true };
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        // Act
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        var result = CanonicalParameterSerializer.Serialize(parameters, options);
        CultureInfo.CurrentCulture = originalCulture;
        CultureInfo.CurrentUICulture = originalUiCulture;

        // Assert
        result.ShouldBe("decimal=1.5&empty=&flag=true&items=%E4%B8%AD%E6%96%87&items=second&null&time=2024-01-01T19%3A04%3A05.0000000%2B00%3A00&z=2&z=1");
    }

    /// <summary>
    /// 测试目的：HMAC、RSA-PSS 和 ECDSA 应签名相同规范化文本并拒绝被修改的参数。
    /// </summary>
    [Fact]
    public void Signers_WhenCanonicalParametersChange_ShouldRejectSignatures()
    {
        // Arrange
        var parameters = new[] { new CanonicalParameter("method", "POST"), new CanonicalParameter("amount", 10.5m) };
        var changedParameters = new[] { new CanonicalParameter("method", "POST"), new CanonicalParameter("amount", 10.6m) };
        var hmacKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var rsaPair = RsaKeyGenerator.Generate(2048);
        var ecdsaPair = EcdsaKeyGenerator.Generate(EcdsaCurve.P256);

        // Act
        var hmacSignature = CanonicalRequestSigner.SignHmacSha256(parameters, hmacKey);
        var rsaSignature = CanonicalRequestSigner.SignRsaPssSha256(parameters, rsaPair.PrivateKeyPem);
        var ecdsaSignature = CanonicalRequestSigner.SignEcdsa(parameters, ecdsaPair.PrivateKeyPem);
        var hmacValid = CanonicalRequestSigner.VerifyHmacSha256(parameters, hmacSignature, hmacKey);
        var rsaValid = CanonicalRequestSigner.VerifyRsaPssSha256(parameters, rsaSignature, rsaPair.PublicKeyPem);
        var ecdsaValid = CanonicalRequestSigner.VerifyEcdsa(parameters, ecdsaSignature, ecdsaPair.PublicKeyPem);
        var hmacChanged = CanonicalRequestSigner.VerifyHmacSha256(changedParameters, hmacSignature, hmacKey);
        var rsaChanged = CanonicalRequestSigner.VerifyRsaPssSha256(changedParameters, rsaSignature, rsaPair.PublicKeyPem);
        var ecdsaChanged = CanonicalRequestSigner.VerifyEcdsa(changedParameters, ecdsaSignature, ecdsaPair.PublicKeyPem);

        // Assert
        hmacValid.ShouldBeTrue();
        rsaValid.ShouldBeTrue();
        ecdsaValid.ShouldBeTrue();
        hmacChanged.ShouldBeFalse();
        rsaChanged.ShouldBeFalse();
        ecdsaChanged.ShouldBeFalse();
    }
}
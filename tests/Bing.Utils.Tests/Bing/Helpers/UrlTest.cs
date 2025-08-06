using Bing.Utils.Tests;

namespace Bing.Helpers;

/// <summary>
/// Url操作测试
/// </summary>
[Trait("Bing.Helpers", "Url")]
public class UrlTest : TestBase
{
    /// <summary>
    /// 测试初始化
    /// </summary>
    public UrlTest(ITestOutputHelper output) : base(output)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    #region Combine 测试

    /// <summary>
    /// 测试 - Combine - 正常路径合并场景
    /// </summary>
    [Theory]
    [InlineData(new string[] { "http://a.com", "b" }, "http://a.com/b")]
    [InlineData(new string[] { "http://a.com/", "b" }, "http://a.com/b")]
    [InlineData(new string[] { "http://a.com", "/b" }, "http://a.com/b")]
    [InlineData(new string[] { "http://a.com/", "/b" }, "http://a.com/b")]
    [InlineData(new string[] { "http://a.com", "b=1" }, "http://a.com/b=1")]
    [InlineData(new string[] { null, null }, "")]
    [InlineData(new string[] { "", "" }, "")]
    [InlineData(new string[] { "a", "b" }, "a/b")]
    [InlineData(new string[] { "a/", "b" }, "a/b")]
    [InlineData(new string[] { "a", "/b" }, "a/b")]
    [InlineData(new string[] { "/a", "b" }, "/a/b")]
    [InlineData(new string[] { "a", "b/" }, "a/b/")]
    [InlineData(new string[] { "/a/", "/b/" }, "/a/b/")]
    public void Combine_NormalScenarios_ReturnsExpectedUrl(string[] urls, string expected)
    {
        // Act
        var result = Url.Combine(urls);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Combine - 处理反斜杠路径分隔符
    /// </summary>
    [Theory]
    [InlineData(new string[] { "http://a.com\\", "b" }, "http://a.com/b")]
    [InlineData(new string[] { "a", "\\b" }, "a/b")]
    [InlineData(new string[] { "a\\", "\\b\\" }, "a/b/")]
    public void Combine_BackslashSeparators_ConvertsToForwardSlash(string[] urls, string expected)
    {
        // Act
        var result = Url.Combine(urls);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Combine - 空值和null处理
    /// </summary>
    [Fact]
    public void Combine_NullArray_ReturnsEmptyString()
    {
        // Act
        var result = Url.Combine(null);

        // Assert
        result.ShouldBe("");
    }

    /// <summary>
    /// 测试 - Combine - 空字符串和空白字符串过滤
    /// </summary>
    [Theory]
    [InlineData(new string[] { "", "" }, "")]
    [InlineData(new string[] { "a", "", "b" }, "a/b")]
    [InlineData(new string[] { "", "a", "" }, "a")]
    [InlineData(new string[] { "  ", "\t", "\n" }, "")]
    public void Combine_EmptyAndWhitespaceStrings_FiltersCorrectly(string[] urls, string expected)
    {
        // Act
        var result = Url.Combine(urls);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Combine - 多个路径片段合并
    /// </summary>
    [Fact]
    public void Combine_MultipleSegments_CombinesCorrectly()
    {
        // Arrange
        var urls = new[] { "http://example.com", "api", "v1", "users", "123" };

        // Act
        var result = Url.Combine(urls);

        // Assert
        result.ShouldBe("http://example.com/api/v1/users/123");
    }

    #endregion

    #region Join 测试

    /// <summary>
    /// 测试 - Join - 单个参数连接
    /// </summary>
    [Theory]
    [InlineData("http://test.com", "a=1", "http://test.com?a=1")]
    [InlineData("http://test.com?", "a=1", "http://test.com?a=1")]
    [InlineData("http://test.com?c=3", "a=1", "http://test.com?c=3&a=1")]
    [InlineData("http://test.com?c=3&", "a=1", "http://test.com?c=3&a=1")]
    public void Join_SingleParameter_ReturnsCorrectUrl(string url, string param, string expected)
    {
        // Act
        var result = Url.Join(url, param);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Join - 多个参数连接
    /// </summary>
    [Theory]
    [InlineData("http://demo.com", new string[] { "a=1", "b=2" }, "http://demo.com?a=1&b=2")]
    [InlineData("http://demo.com?", new string[] { "a=1", "b=2" }, "http://demo.com?a=1&b=2")]
    [InlineData("http://demo.com?c=3", new string[] { "a=1", "b=2" }, "http://demo.com?c=3&a=1&b=2")]
    [InlineData("http://demo.com?c=3&", new string[] { "a=1", "b=2" }, "http://demo.com?c=3&a=1&b=2")]
    public void Join_MultipleParameters_ReturnsCorrectUrl(string url, string[] parameters, string expected)
    {
        // Act
        var result = Url.Join(url, parameters);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Join - 空URL参数异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Join_NullOrEmptyUrl_ThrowsArgumentNullException(string url)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Url.Join(url, "param=value"));
    }

    /// <summary>
    /// 测试 - Join - 空参数处理
    /// </summary>
    [Fact]
    public void Join_EmptyParameter_ReturnsOriginalUrl()
    {
        // Arrange
        const string url = "http://test.com";

        // Act
        var result = Url.Join(url, "");

        // Assert
        result.ShouldBe(url);
    }

    /// <summary>
    /// 测试 - Join - 空参数数组处理
    /// </summary>
    [Fact]
    public void Join_EmptyParametersArray_ReturnsOriginalUrl()
    {
        // Arrange
        const string url = "http://test.com";

        // Act
        var result = Url.Join(url, new string[0]);

        // Assert
        result.ShouldBe(url);
    }

    /// <summary>
    /// 测试 - Join - Uri对象重载
    /// </summary>
    [Fact]
    public void Join_UriOverload_ReturnsCorrectUri()
    {
        // Arrange
        var baseUri = new Uri("http://test.com");
        const string param = "a=1";

        // Act
        var result = Url.Join(baseUri, param);

        // Assert
        result.AbsoluteUri.ShouldBe("http://test.com/?a=1");
    }

    /// <summary>
    /// 测试 - Join - Uri对象空值异常
    /// </summary>
    [Fact]
    public void Join_NullUri_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Url.Join((Uri)null, "param=value"));
    }

    #endregion

    #region GetMainDomain 测试

    /// <summary>
    /// 测试 - GetMainDomain - 标准域名提取
    /// </summary>
    [Theory]
    [InlineData("http://www.baidu.com", "baidu.com")]
    [InlineData("https://www.google.com", "google.com")]
    [InlineData("http://baidu.com", "baidu.com")]
    [InlineData("www.example.com", "example.com")]
    [InlineData("subdomain.example.com", "example.com")]
    [InlineData("deep.subdomain.example.com", "example.com")]
    public void GetMainDomain_StandardDomains_ExtractsCorrectly(string url, string expected)
    {
        // Act
        var result = Url.GetMainDomain(url);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetMainDomain - 带查询参数的URL
    /// </summary>
    [Theory]
    [InlineData("http://www.baidu.com?a=xxx.aaa.cc", "baidu.com")]
    [InlineData("https://sub.example.com/path?param=value", "example.com")]
    [InlineData("http://api.github.com/users/test?tab=repositories", "github.com")]
    public void GetMainDomain_UrlsWithQueryParams_ExtractsCorrectly(string url, string expected)
    {
        // Act
        var result = Url.GetMainDomain(url);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetMainDomain - IP地址处理
    /// </summary>
    [Theory]
    [InlineData("http://192.168.1.1", "192.168.1.1")]
    [InlineData("https://127.0.0.1:8080", "127.0.0.1")]
    [InlineData("192.168.0.100", "192.168.0.100")]
    public void GetMainDomain_IpAddresses_ReturnsIpAddress(string url, string expected)
    {
        // Act
        var result = Url.GetMainDomain(url);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetMainDomain - 空值和无效输入
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("invalid", "invalid")]
    [InlineData("single", "single")]
    public void GetMainDomain_InvalidInputs_ReturnsOriginal(string url, string expected)
    {
        // Act
        var result = Url.GetMainDomain(url);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetMainDomain - 本地域名
    /// </summary>
    [Theory]
    [InlineData("localhost", "localhost")]
    [InlineData("http://localhost:3000", "localhost")]
    [InlineData("api.localhost", "localhost")]
    [InlineData("sub.api.localhost", "localhost")]
    [InlineData("https://app.localhost/path", "localhost")]
    public void GetMainDomain_LocalhostDomains_HandlesCorrectly(string url, string expected)
    {
        // Act
        var result = Url.GetMainDomain(url);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetMainDomain - 特殊本地域名格式
    /// </summary>
    [Theory]
    [InlineData("LOCALHOST", "localhost")] // 大写测试
    [InlineData("Api.LOCALHOST", "localhost")] // 混合大小写
    [InlineData("test.dev.localhost", "localhost")] // 多级子域名
    [InlineData("http://service.localhost:8080", "localhost")] // 带端口
    public void GetMainDomain_LocalhostVariations_HandlesCaseInsensitively(string url, string expected)
    {
        // Act
        var result = Url.GetMainDomain(url);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - GetMainDomain - 边界情况
    /// </summary>
    [Theory]
    [InlineData("com", "com")] // 顶级域名
    [InlineData("a.b", "a.b")] // 最小二级域名
    [InlineData("very.deep.subdomain.example.org", "example.org")] // 深层子域名
    public void GetMainDomain_EdgeCases_HandlesCorrectly(string url, string expected)
    {
        // Act
        var result = Url.GetMainDomain(url);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region UrlEncode 测试

    /// <summary>
    /// 测试 - UrlEncode - 基本编码功能
    /// </summary>
    [Theory]
    [InlineData("hello world", "hello+world")]
    [InlineData("test@example.com", "test%40example.com")]
    [InlineData("http://example.com", "http%3a%2f%2fexample.com")]
    [InlineData("中文测试", "%e4%b8%ad%e6%96%87%e6%b5%8b%e8%af%95")]
    public void UrlEncode_BasicEncoding_ReturnsCorrectResult(string input, string expected)
    {
        // Act
        var result = Url.UrlEncode(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - UrlEncode - 大写十六进制编码
    /// </summary>
    [Theory]
    [InlineData("test@example.com", false, "test%40example.com")]
    [InlineData("test@example.com", true, "test%40example.com")]
    [InlineData("http://", false, "http%3a%2f%2f")]
    [InlineData("http://", true, "http%3A%2F%2F")]
    public void UrlEncode_UppercaseOption_ReturnsCorrectFormat(string input, bool isUpper, string expected)
    {
        // Act
        var result = Url.UrlEncode(input, isUpper);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - UrlEncode - 指定编码名称
    /// </summary>
    [Fact]
    public void UrlEncode_WithEncodingName_ReturnsCorrectResult()
    {
        // Arrange
        const string input = "测试";

        // Act
        var utf8Result = Url.UrlEncode(input, "UTF-8");
        var gbkResult = Url.UrlEncode(input, "GBK");

        // Assert
        utf8Result.ShouldNotBeNull();
        gbkResult.ShouldNotBeNull();
        utf8Result.ShouldNotBe(gbkResult); // 不同编码应产生不同结果
    }

    /// <summary>
    /// 测试 - UrlEncode - 无效编码名称异常
    /// </summary>
    [Fact]
    public void UrlEncode_InvalidEncodingName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Url.UrlEncode("test", "INVALID-ENCODING"));
    }

    /// <summary>
    /// 测试 - UrlEncode - 空值处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UrlEncode_NullOrEmpty_ReturnsOriginal(string input)
    {
        // Act
        var result = Url.UrlEncode(input);

        // Assert
        result.ShouldBe(input);
    }

    /// <summary>
    /// 测试 - UrlEncode - Encoding对象为null异常
    /// </summary>
    [Fact]
    public void UrlEncode_NullEncoding_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Url.UrlEncode("test", (Encoding)null));
    }

    #endregion

    #region UrlDecode 测试

    /// <summary>
    /// 测试 - UrlDecode - 基本解码功能
    /// </summary>
    [Theory]
    [InlineData("hello+world", "hello world")]
    [InlineData("test%40example.com", "test@example.com")]
    [InlineData("http%3a%2f%2fexample.com", "http://example.com")]
    [InlineData("%e4%b8%ad%e6%96%87%e6%b5%8b%e8%af%95", "中文测试")]
    public void UrlDecode_BasicDecoding_ReturnsCorrectResult(string input, string expected)
    {
        // Act
        var result = Url.UrlDecode(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - UrlDecode - 大写十六进制解码
    /// </summary>
    [Theory]
    [InlineData("test%40example.com", "test@example.com")]
    [InlineData("http%3A%2F%2F", "http://")]
    [InlineData("test%3a%2f", "test:/")]
    public void UrlDecode_UppercaseHex_ReturnsCorrectResult(string input, string expected)
    {
        // Act
        var result = Url.UrlDecode(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - UrlDecode - 指定编码解码
    /// </summary>
    [Fact]
    public void UrlDecode_WithSpecificEncoding_ReturnsCorrectResult()
    {
        // Arrange
        const string encoded = "%e4%b8%ad%e6%96%87"; // "中文" 的 UTF-8 编码

        // Act
        var result = Url.UrlDecode(encoded, Encoding.UTF8);

        // Assert
        result.ShouldBe("中文");
    }

    /// <summary>
    /// 测试 - UrlDecode - 空值处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UrlDecode_NullOrEmpty_ReturnsOriginal(string input)
    {
        // Act
        var result = Url.UrlDecode(input);

        // Assert
        result.ShouldBe(input);
    }

    /// <summary>
    /// 测试 - UrlDecode - Encoding对象为null异常
    /// </summary>
    [Fact]
    public void UrlDecode_NullEncoding_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Url.UrlDecode("test", (Encoding)null));
    }

    #endregion

    #region 编码解码往返测试

    /// <summary>
    /// 测试 - 编码解码往返 - 数据一致性
    /// </summary>
    [Theory]
    [InlineData("hello world")]
    [InlineData("test@example.com")]
    [InlineData("http://example.com/path?param=value")]
    [InlineData("中文测试内容")]
    [InlineData("特殊字符: !@#$%^&*()")]
    public void UrlEncodeDecodeRoundtrip_VariousInputs_MaintainsDataIntegrity(string original)
    {
        // Act
        var encoded = Url.UrlEncode(original);
        var decoded = Url.UrlDecode(encoded);

        // Assert
        decoded.ShouldBe(original);
    }

    /// <summary>
    /// 测试 - 编码解码往返 - 不同编码格式
    /// </summary>
    [Theory]
    [InlineData("UTF-8")]
    [InlineData("GBK")]
    public void UrlEncodeDecodeRoundtrip_DifferentEncodings_MaintainsDataIntegrity(string encodingName)
    {
        // Arrange
        const string original = "测试中文内容";
        var encoding = Encoding.GetEncoding(encodingName);

        // Act
        var encoded = Url.UrlEncode(original, encoding);
        var decoded = Url.UrlDecode(encoded, encoding);

        // Assert
        decoded.ShouldBe(original);
    }

    #endregion

    #region 性能和边界测试

    /// <summary>
    /// 测试 - 大字符串处理 - 性能合理性
    /// </summary>
    [Fact]
    public void UrlMethods_LargeStrings_PerformReasonably()
    {
        // Arrange
        var largeString = new string('a', 10000);
        var largeUrl = "http://example.com/" + largeString;

        // Act & Assert - 主要测试不会抛异常
        Should.NotThrow(() =>
        {
            var combined = Url.Combine("http://test.com", largeString);
            var joined = Url.Join("http://test.com", $"param={largeString}");
            var encoded = Url.UrlEncode(largeString);
            var decoded = Url.UrlDecode(encoded);

            combined.ShouldNotBeNull();
            joined.ShouldNotBeNull();
            encoded.ShouldNotBeNull();
            decoded.ShouldBe(largeString);
        });
    }

    /// <summary>
    /// 测试 - 特殊Unicode字符 - 正确处理
    /// </summary>
    [Theory]
    [InlineData("🚀🌟💻")] // Emoji
    [InlineData("Ñoñó Mañana")] // 重音字符
    [InlineData("Русский текст")] // 西里尔字符
    [InlineData("العربية")] // 阿拉伯字符
    public void UrlMethods_UnicodeCharacters_HandleCorrectly(string input)
    {
        // Act & Assert
        Should.NotThrow(() =>
        {
            var encoded = Url.UrlEncode(input);
            var decoded = Url.UrlDecode(encoded);
            decoded.ShouldBe(input);
        });
    }

    #endregion
}
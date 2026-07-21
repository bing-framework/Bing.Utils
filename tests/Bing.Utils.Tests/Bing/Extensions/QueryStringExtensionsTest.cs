using Bing.Net;
using Bing.Extensions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="QueryStringExtensions"/> 单元测试
/// </summary>
public class QueryStringExtensionsTest
{
    /// <summary>
    /// 测试目的：空键值对集合应输出空查询字符串。
    /// </summary>
    [Fact]
    public void ToQueryString_EmptySource_ReturnsEmptyString()
    {
        // Arrange
        var source = Array.Empty<KeyValuePair<string, object>>();

        // Act
        var result = source.ToQueryString();

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：空格、中文和保留字符应按 RFC 3986 编码。
    /// </summary>
    [Fact]
    public void ToQueryString_SpecialCharacters_UsesPercentEncoding()
    {
        // Arrange
        var source = new[]
        {
            new KeyValuePair<string, object>("姓名", "张 三&="),
            new KeyValuePair<string, object>("q q", "a+b/c?")
        };

        // Act
        var result = source.ToQueryString();

        // Assert
        result.ShouldBe("%E5%A7%93%E5%90%8D=%E5%BC%A0%20%E4%B8%89%26%3D&q%20q=a%2Bb%2Fc%3F");
    }

    /// <summary>
    /// 测试目的：默认应忽略空值，并保持输入的枚举顺序。
    /// </summary>
    [Fact]
    public void ToQueryString_NullValueAndMultipleValues_IgnoresNullAndPreservesOrder()
    {
        // Arrange
        var source = new[]
        {
            new KeyValuePair<string, object>("first", 1),
            new KeyValuePair<string, object>("ignored", null),
            new KeyValuePair<string, object>("last", true)
        };

        // Act
        var result = source.ToQueryString();

        // Assert
        result.ShouldBe("first=1&last=true");
    }

    /// <summary>
    /// 测试目的：指定保留空值时应输出空等号值且不产生尾部分隔符。
    /// </summary>
    [Fact]
    public void ToQueryString_NullValueNotIgnored_WritesEmptyValueWithoutTrailingSeparator()
    {
        // Arrange
        var source = new[]
        {
            new KeyValuePair<string, object>("a", null),
            new KeyValuePair<string, object>("b", string.Empty)
        };

        // Act
        var result = source.ToQueryString(ignoreNullValues: false);

        // Assert
        result.ShouldBe("a=&b=");
        result.ShouldNotEndWith("&");
    }

    /// <summary>
    /// 测试目的：数值、布尔值和日期应使用与区域性无关的固定格式。
    /// </summary>
    [Fact]
    public void ToQueryString_FormattableValues_UsesInvariantAndRoundTripFormats()
    {
        // Arrange
        var date = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var source = new[]
        {
            new KeyValuePair<string, object>("number", 1.5m),
            new KeyValuePair<string, object>("flag", true),
            new KeyValuePair<string, object>("date", date)
        };

        // Act
        var result = source.ToQueryString();

        // Assert
        result.ShouldBe("number=1.5&flag=true&date=2024-01-02T03%3A04%3A05.0000000Z");
    }

    /// <summary>
    /// 测试目的：空键应编码为等号前的空字符串。
    /// </summary>
    [Fact]
    public void ToQueryString_EmptyKey_WritesEmptyKey()
    {
        // Arrange
        var source = new[] { new KeyValuePair<string, object>(string.Empty, "value") };

        // Act
        var result = source.ToQueryString();

        // Assert
        result.ShouldBe("=value");
    }

    /// <summary>
    /// 测试目的：对象构建器应复用对象属性并忽略空属性。
    /// </summary>
    [Fact]
    public void FromObject_ObjectWithNullProperty_BuildsEncodedQueryString()
    {
        // Arrange
        var source = new QueryModel { Page = 2, Keyword = "A B", Optional = null };

        // Act
        var result = QueryStringBuilder.FromObject(source);

        // Assert
        result.ShouldBe("Page=2&Keyword=A%20B");
    }

    /// <summary>
    /// 测试目的：空对象应返回空查询字符串。
    /// </summary>
    [Fact]
    public void FromObject_NullObject_ReturnsEmptyString()
    {
        // Act
        var result = QueryStringBuilder.FromObject(null);

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 查询字符串对象模型。
    /// </summary>
    private sealed class QueryModel
    {
        /// <summary>
        /// 页码。
        /// </summary>
        public int Page { get; init; }

        /// <summary>
        /// 关键字。
        /// </summary>
        public string Keyword { get; init; }

        /// <summary>
        /// 可选值。
        /// </summary>
        public string Optional { get; init; }
    }
}
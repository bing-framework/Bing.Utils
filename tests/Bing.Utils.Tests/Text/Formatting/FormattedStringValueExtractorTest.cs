using Bing.Text.Formatting;

namespace Bing.Utils.Tests.Text.Formatting;

/// <summary>
/// 格式化字符串提取器测试
/// </summary>
public class FormattedStringValueExtractorTest
{
    /// <summary>
    /// 测试 - 匹配
    /// </summary>
    [Fact]
    public void Test_Matched()
    {
        Test_Matched_Internal(
            "My name is Andy.",
            "My name is {0}.",
            new NameValue("0", "Andy")
        );

        Test_Matched_Internal(
            "User jxb does not exist.",
            "User {0} does not exist.",
            new NameValue("0", "jxb")
        );

        Test_Matched_Internal(
            "bing.com",
            "{domain}",
            new NameValue("domain", "bing.com")
        );

        Test_Matched_Internal(
            "http://acme.bing.com/gaming/Index.html",
            "http://{TENANCY_NAME}.bing.com/{AREA}/Index.html",
            new NameValue("TENANCY_NAME", "acme"),
            new NameValue("AREA", "gaming")
        );
    }

    /// <summary>
    /// 测试 - 不匹配
    /// </summary>
    [Fact]
    public void Test_Not_Matched()
    {
        Test_Not_Matched_Internal(
            "My name is Andy.",
            "My name is Jam."
        );

        Test_Not_Matched_Internal(
            "Role {0} does not exist.",
            "User name {0} is invalid, can only contain letters or digits."
        );

        Test_Not_Matched_Internal(
            "{0} cannot be null or empty.",
            "Incorrect password."
        );

        Test_Not_Matched_Internal(
            "Incorrect password.",
            "{0} cannot be null or empty."
        );
    }

    /// <summary>
    /// 测试 - 是否完全匹配
    /// </summary>
    [Fact]
    public void Test_IsMatch()
    {
        FormattedStringValueExtractor.IsMatch("User jxb does not exist.", "User {0} does not exist.", out var values).ShouldBe(true);
        values[0].ShouldBe("jxb");
    }

    private static void Test_Matched_Internal(string str, string format, params NameValue[] expectedPairs)
    {
        var result = FormattedStringValueExtractor.Extract(str, format);
        result.IsMatch.ShouldBe(true);

        if (expectedPairs == null)
        {
            result.Matches.Count.ShouldBe(0);
            return;
        }

        result.Matches.Count.ShouldBe(expectedPairs.Length);

        for (var i = 0; i < expectedPairs.Length; i++)
        {
            var actualMatch = result.Matches[i];
            var expectedPair = expectedPairs[i];

            actualMatch.Name.ShouldBe(expectedPair.Name);
            actualMatch.Value.ShouldBe(expectedPair.Value);
        }
    }

    private void Test_Not_Matched_Internal(string str, string format)
    {
        var result = FormattedStringValueExtractor.Extract(str, format);
        result.IsMatch.ShouldBe(false);
    }
}
namespace Bing.Text;

/// <summary>
/// 字符串操作 测试
/// </summary>
public class StrTest
{
    /// <summary>
    /// 测试 - 将Unicode转换为字符串
    /// </summary>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("\\u4e2d\\u56fd", "中国")]
    [InlineData("\\u4F60\\u597D\\u4E16\\u754C", "你好世界")]
    [InlineData("Hello\\u4E16\\u754CWorld", "Hello世界World")]
    [InlineData("\\u0041\\u0042\\u0043", "ABC")]
    [InlineData("混合\\u6D4B\\u8BD5字符串", "混合测试字符串")]
    [InlineData("非Unicode字符串", "非Unicode字符串")]
    public void Test_UnicodeToStr(string input, string result)
    {
        Assert.Equal(result, Str.UnicodeToStr(input));
    }
}
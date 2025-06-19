using System.Globalization;

namespace Bing;

/// <summary>
/// 字符(<see cref="char"/>) 扩展 测试
/// </summary>
public class CharExtensionsTest
{
    /// <summary>
    /// 测试 - 将字符转换为字符串
    /// </summary>
    /// <param name="input"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData('a', "a")]
    [InlineData('Z', "Z")]
    [InlineData('0', "0")]
    [InlineData('中', "中")]
    [InlineData('\n', "\n")]
    public void Test_AsString_ReturnsExpectedString(char input, string expected)
    {
        // 验证 AsString 返回的字符串与字符的 ToString(InvariantCulture) 一致
        Assert.Equal(expected, input.AsString());
        Assert.Equal(input.ToString(CultureInfo.InvariantCulture), input.AsString());
    }
}
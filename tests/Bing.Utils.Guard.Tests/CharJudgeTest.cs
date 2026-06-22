extern alias guard;

namespace Bing.Text;

/// <summary>
/// 字符检查器测试
/// </summary>
public class CharJudgeTest
{
    /// <summary>
    /// 测试目的：验证 IsBetween 方法在各种边界和正常情况下的正确性
    /// </summary>
    [Theory]
    [InlineData('b', 'a', 'c', true)]  // 正常：中间值
    [InlineData('a', 'a', 'c', true)]  // 边界：左边界
    [InlineData('c', 'a', 'c', true)]  // 边界：右边界
    [InlineData('d', 'a', 'c', false)] // 异常：超出右边界
    [InlineData('z', 'a', 'c', false)] // 异常：远超右边界
    [InlineData('1', '0', '9', true)]  // 正常：数字范围
    [InlineData('5', '0', '9', true)]  // 正常：数字中间
    [InlineData('a', '0', '9', false)] // 异常：非数字字符
    [InlineData('中', '一', '十', true)]  // 正常：中文字符在范围内（Unicode码点比较）
    public void IsBetween_VariousInputs_ReturnsExpectedResult(char value, char left, char right, bool expected)
    {
        // Act
        var result = guard::Bing.Text.CharJudge.IsBetween(value, left, right);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证 IsBetween 对特殊字符的处理
    /// </summary>
    [Theory]
    [InlineData('\0', '\0', 'a', true)]   // 边界：null字符作为左边界
    [InlineData('\n', '\n', '\r', true)]  // 正常：换行符范围
    [InlineData('\t', ' ', '~', false)]   // 异常：制表符(9)小于空格(32)，不在范围内
    [InlineData(' ', ' ', ' ', true)]     // 边界：相同字符
    public void IsBetween_SpecialCharacters_ReturnsExpectedResult(char value, char left, char right, bool expected)
    {
        // Act
        var result = guard::Bing.Text.CharJudge.IsBetween(value, left, right);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证 IsBetween 对倒序范围的处理
    /// </summary>
    [Theory]
    [InlineData('b', 'c', 'a', false)] // 异常：倒序范围，中间值
    [InlineData('a', 'c', 'a', false)] // 异常：倒序范围，左值
    [InlineData('c', 'c', 'a', false)] // 异常：倒序范围，c>a故c不在[c,a]内
    public void IsBetween_ReversedRange_ReturnsExpectedResult(char value, char left, char right, bool expected)
    {
        // Act
        var result = guard::Bing.Text.CharJudge.IsBetween(value, left, right);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证 IsBetween 对Unicode字符的处理
    /// </summary>
    [Theory]
    [InlineData('α', 'α', 'ω', true)]   // 正常：希腊字母范围内
    [InlineData('β', 'α', 'ω', true)]   // 正常：希腊字母中间值
    [InlineData('中', '中', '文', true)] // 正常：中文字符等值边界
    [InlineData('一', '一', '九', true)] // 正常：中文数字范围
    public void IsBetween_UnicodeCharacters_ReturnsExpectedResult(char value, char left, char right, bool expected)
    {
        // Act
        var result = guard::Bing.Text.CharJudge.IsBetween(value, left, right);

        // Assert
        Assert.Equal(expected, result);
    }
}

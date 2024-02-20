using Bing.Text;

namespace BingUtilsUT.CharUT;

[Trait("CharUT", "Char.Is")]
public class CharIsTest
{
    /// <summary>
    /// 测试 - 是否空白符
    /// </summary>
    [Fact]
    public void Test_IsBlankChar()
    {
        char a = '\u00A0';
        CharJudge.IsBlankChar(a).ShouldBeTrue();

        char a2 = '\u0020';
        CharJudge.IsBlankChar(a2).ShouldBeTrue();

        char a3 = '\u3000';
        CharJudge.IsBlankChar(a3).ShouldBeTrue();

        char a4 = '\u0000';
        CharJudge.IsBlankChar(a4).ShouldBeTrue();

        char a5 = ' ';
        CharJudge.IsBlankChar(a5).ShouldBeTrue();

        char a6 = '\u202a';
        CharJudge.IsBlankChar(a6).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 是否 Emoji 表情符
    /// </summary>
    [Fact]
    public void Test_IsEmoji()
    {
        string a = """莉🌹""";
        CharJudge.IsEmoji(a[0]).ShouldBeFalse();
        CharJudge.IsEmoji(a[1]).ShouldBeTrue();
    }

    [Fact]
    public void Test_Trim()
    {
        var str = "‪C:/Users/maple/Desktop/tone.txt";
        str[0].ShouldBe('\u202a');
        CharJudge.IsBlankChar(str[0]).ShouldBeTrue();
        str = str.Trim('\u202a');
        str[0].ShouldBe('C');
        CharJudge.IsBlankChar(str[0]).ShouldBeFalse();
    }
}
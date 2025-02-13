namespace Bing.Text;

/// <summary>
/// 字符检查器
/// </summary>
public static class CharJudge
{
    /// <summary>
    /// 是否空白符。 <br />
    /// 空白符包括空格、制表符、全角空格和不间断空格
    /// </summary>
    /// <param name="char">字符</param>
    /// <returns>是否空白符</returns>
    public static bool IsBlankChar(char @char)
    {
        return char.IsWhiteSpace(@char)
               || char.IsSeparator(@char)
               || @char == '\ufeff'
               || @char == '\u202a'
               || @char == '\u0000'
               // I5UGSQ，Hangul Filler
               || @char == '\u3164'
               // Braille Pattern Blank
               || @char == '\u2800'
               // MONGOLIAN VOWEL SEPARATOR
               || @char == '\u180e';
    }

    /// <summary>
    /// 是否为 emoji 表情符
    /// </summary>
    /// <param name="char">字符</param>
    /// <returns>是否为emoji</returns>
    public static bool IsEmoji(char @char)
    {
        return false == ((@char == 0x0) ||
                         (@char == 0x9) ||
                         (@char == 0xA) ||
                         (@char == 0xD) ||
                         ((@char >= 0x20) && (@char <= 0xD7FF)) ||
                         ((@char >= 0xE000) && (@char <= 0xFFFD)) ||
                         ((@char >= 0x10000) && (@char <= 0x10FFFF)));
    }
}
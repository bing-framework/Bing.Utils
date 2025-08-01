namespace Bing.Text;

/// <summary>
/// 字符检查器
/// </summary>
public static class CharJudge
{
    /// <summary>
    /// 判断字符是否为空白符。 <br />
    /// </summary>
    /// <param name="char">待检查的字符</param>
    /// <returns>如果是空白符返回true，否则返回false</returns>
    /// <remarks>
    /// 空白符包括：<br />
    /// - 标准空白字符（空格、制表符、换行符等） <br />
    /// - Unicode分隔符字符 <br />
    /// - 字节顺序标记 (BOM) <br />
    /// - 特殊的空白字符（全角空格、韩文填充符等）
    /// </remarks>
    public static bool IsBlankChar(char @char)
    {
        return char.IsWhiteSpace(@char)
               || char.IsSeparator(@char)
               || @char == '\ufeff'        // ZERO WIDTH NO-BREAK SPACE (BOM)
               || @char == '\u202a'        // LEFT-TO-RIGHT EMBEDDING
               || @char == '\u0000'        // NULL
               || @char == '\u3164'        // HANGUL FILLER
               || @char == '\u2800'        // BRAILLE PATTERN BLANK
               || @char == '\u180e';       // MONGOLIAN VOWEL SEPARATOR
    }

    /// <summary>
    /// 判断字符是否为emoji表情符号
    /// </summary>
    /// <param name="char">待检查的字符</param>
    /// <returns>如果是emoji返回true，否则返回false</returns>
    /// <remarks>
    /// 检查字符是否在常见的emoji Unicode范围内，包括：
    /// - 单字节emoji符号范围
    /// - 多字节emoji的高代理字符范围
    /// - 各种符号和象形文字范围
    /// </remarks>
    public static bool IsEmoji(char @char)
    {
        int codePoint = @char;

        return (codePoint >= 0x1F600 && codePoint <= 0x1F64F) ||  // Emoticons
               (codePoint >= 0x1F300 && codePoint <= 0x1F5FF) ||  // Misc Symbols and Pictographs
               (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) ||  // Transport and Map Symbols
               (codePoint >= 0x1F700 && codePoint <= 0x1F77F) ||  // Alchemical Symbols
               (codePoint >= 0x1F780 && codePoint <= 0x1F7FF) ||  // Geometric Shapes Extended
               (codePoint >= 0x1F800 && codePoint <= 0x1F8FF) ||  // Supplemental Arrows-C
               (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||  // Supplemental Symbols and Pictographs
               (codePoint >= 0x2600 && codePoint <= 0x26FF) ||    // Miscellaneous Symbols (包含☀等)
               (codePoint >= 0x2700 && codePoint <= 0x27BF) ||    // Dingbats
               (codePoint >= 0x2B50 && codePoint <= 0x2B50) ||    // Star (⭐)
               (codePoint >= 0x2934 && codePoint <= 0x2935) ||    // Arrows
               (codePoint >= 0x203C && codePoint <= 0x203C) ||    // Double Exclamation Mark
               (codePoint >= 0x2049 && codePoint <= 0x2049) ||    // Exclamation Question Mark
               (codePoint >= 0x25AA && codePoint <= 0x25AB) ||    // Black/White Small Square
               (codePoint >= 0x25B6 && codePoint <= 0x25B6) ||    // Black Right-Pointing Triangle
               (codePoint >= 0x25C0 && codePoint <= 0x25C0) ||    // Black Left-Pointing Triangle
               (codePoint >= 0x25FB && codePoint <= 0x25FE) ||    // White/Black Medium Squares
               (codePoint >= 0x2764 && codePoint <= 0x2764) ||    // Heavy Black Heart (❤)
               (codePoint >= 0x2728 && codePoint <= 0x2728) ||    // Sparkles (✨)
               (codePoint >= 0x2708 && codePoint <= 0x2708) ||    // Airplane (✈)
               // 高代理字符范围 - 用于多字节emoji
               (codePoint >= 0xD83C && codePoint <= 0xD83F);      // High surrogate range for emoji
    }
}
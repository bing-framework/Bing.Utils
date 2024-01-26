namespace Bing.Text;

/// <summary>
/// 字符工具
/// </summary>
public static class Chars
{
    #region Range

    /// <summary>
    /// 根据给定的两个字符，生成一组连续的字符序列
    /// </summary>
    /// <param name="char"></param>
    /// <param name="toCharacter"></param>
    /// <returns></returns>
    public static IEnumerable<char> Range(char @char, char toCharacter)
    {
        var reverseRequired = @char > toCharacter;

        var first = reverseRequired ? toCharacter : @char;
        var last = reverseRequired ? @char : toCharacter;

        var result = Enumerable.Range(first, last - first + 1).Select(charCode => (char)charCode);

        if (reverseRequired) 
            result = result.Reverse();
        return result;
    }

    #endregion

    #region Repeat

    /// <summary>
    /// 重复指定次数的字符
    /// </summary>
    /// <param name="char">要重复的字符。</param>
    /// <param name="repeatTimes">重复的次数，应大于等于 0。</param>
    /// <returns>生成的重复字符的字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Repeat(char @char, int repeatTimes) => repeatTimes <= 0 ? string.Empty : new string(@char, repeatTimes);

    #endregion
    
}
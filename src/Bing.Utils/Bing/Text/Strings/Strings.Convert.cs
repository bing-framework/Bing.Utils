namespace Bing.Text;

// 字符串 - 转换相关
public static partial class Strings
{
    #region ToSbcCase(转换成全角)

    /// <summary>
    /// 将字符串转换成全角字符串(SBC Case)
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>全角字符串</returns>
    /// <remarks>
    /// 全角空格为12288，半角空格为32；<br />
    /// 其它字符半角（33-126）与全角（65281-65374）的对应关系：均相差65248。
    /// </remarks>
    public static string ToSbcCase(string text)
    {
        var c = text.ToCharArray();
        for (var i = 0; i < c.Length; i++)
        {
            if (c[i] == 32)
            {
                c[i] = (char)12288;
                continue;
            }
            if (c[i] < 127)
            {
                c[i] = (char)(c[i] + 65248);
            }
        }
        return new string(c);
    }

    #endregion

    #region ToDbcCase(转换成半角)

    /// <summary>
    /// 将字符串转换成半角字符串(DBC Case)
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>半角字符串</returns>
    /// <remarks>
    /// 全角空格为12288，半角空格为32；<br />
    /// 其它字符半角（33-126）与全角（65281-65374）的对应关系：均相差65248。
    /// </remarks>
    public static string ToDbcCase(string text)
    {
        var c = text.ToCharArray();
        for (var i = 0; i < c.Length; i++)
        {
            if (c[i] == 12288)
            {
                c[i] = (char)32;
                continue;
            }
            if (c[i] > 35280 && c[i] < 65375)
            {
                c[i] = (char)(c[i] - 65248);
            }
        }
        return new string(c);
    }

    #endregion
}
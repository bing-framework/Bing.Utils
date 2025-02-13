using System.Globalization;

namespace Bing.Text;

/// <summary>
/// 字符串操作 - 工具
/// </summary>
public static partial class Str
{
    /// <summary>
    /// 敏感信息字符
    /// </summary>
    public const char SensitiveChar = '*';

    /// <summary>
    /// 空字符串
    /// </summary>
    public const string Empty = "";

    /// <summary>
    /// 回车换行符。等同 NewLine
    /// </summary>
    public const string CarriageReturnLineFeed = "\r\n";

    /// <summary>
    /// 回车符
    /// </summary>
    public const char CarriageReturn = '\r';

    /// <summary>
    /// 换行符
    /// </summary>
    public const char LineFeed = '\n';

    /// <summary>
    /// 制表符
    /// </summary>
    public const char Tab = '\t';

    #region Repeat(重复指定次数的字符串)

    /// <summary>
    /// 重复指定次数的字符串
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="times">次数</param>
    public static string Repeat(string source, int times) => source.Repeat(times);

    /// <summary>
    /// 重复指定次数的字符
    /// </summary>
    /// <param name="source">字符</param>
    /// <param name="times">次数</param>
    public static string Repeat(char source, int times) => source.Repeat(times);

    #endregion

    #region PadStart(向左填充)

    /// <summary>
    /// 填充。向左填充
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="width">宽度</param>
    /// <param name="appendChar">拼接字符</param>
    public static string PadStart(string source, int width, char appendChar) => source.PadLeft(width, appendChar);

    #endregion

    #region PadEnd(向右填充)

    /// <summary>
    /// 填充。向右填充
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="width">宽度</param>
    /// <param name="appendChar">拼接字符</param>
    public static string PadEnd(string source, int width, char appendChar) => source.PadRight(width, appendChar);

    #endregion

    #region ToPascalCase(转换为帕斯卡命名法)

    /// <summary>
    /// 将字符串转换为帕斯卡命名法。例如：userName -> UserName
    /// </summary>
    /// <param name="s">输入字符串</param>
    /// <returns>转换后的帕斯卡命名法字符串</returns>
    public static string ToPascalCase(string s)
    {
        if (string.IsNullOrEmpty(s) || !char.IsLower(s[0]))
            return s;
        var chars = s.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
                break;
            var hasNext = (i + 1 < chars.Length);
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                break;
            chars[i] = char.ToUpperInvariant(chars[i]);
        }
        return new string(chars);
    }

    #endregion

    #region ToCamelCase(转换为骆驼命名法)

    /// <summary>
    /// 将字符串转换为骆驼命名法。例如：UserName -> userName
    /// </summary>
    /// <param name="s">输入字符串</param>
    /// <returns>转换后的骆驼命名法字符串</returns>
    public static string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
            return s;
        var chars = s.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
                break;
            var hasNext = (i + 1 < chars.Length);
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                if (char.IsSeparator(chars[i + 1]))
                    chars[i] = char.ToLowerInvariant(chars[i]);
                break;
            }
            chars[i] = char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }

    #endregion

    #region ToSnakeCase(转换为蛇形命名法)

    /// <summary>
    /// 将字符串转换为蛇形命名法。例如：UserName -> user_name
    /// </summary>
    /// <param name="s">输入字符串</param>
    /// <returns>转换后的蛇形命名法字符串</returns>
    public static string ToSnakeCase(string s) => ToSeparatedCase(s, '_');

    /// <summary>
    /// 将字符串转换为指定分隔符的命名法。
    /// </summary>
    /// <param name="s">输入字符串</param>
    /// <param name="separator">分隔符</param>
    /// <returns>转换后的字符串</returns>
    private static string ToSeparatedCase(string s, char separator)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        var sb = new StringBuilder();
        var state = SeparatedCaseState.Start;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == ' ')
            {
                if (state != SeparatedCaseState.Start)
                    state = SeparatedCaseState.NewWord;
            }
            else if (char.IsUpper(s[i]))
            {
                switch (state)
                {
                    case SeparatedCaseState.Upper:
                        var hasNext = (i + 1 < s.Length);
                        if (i > 0 && hasNext)
                        {
                            var nextChar = s[i + 1];
                            if (!char.IsUpper(nextChar) && nextChar != separator)
                                sb.Append(separator);
                        }
                        break;
                    case SeparatedCaseState.Lower:
                    case SeparatedCaseState.NewWord:
                        sb.Append(separator);
                        break;
                }

                var c = char.ToLower(s[i], CultureInfo.InvariantCulture);
                sb.Append(c);
                state = SeparatedCaseState.Upper;
            }
            else if (s[i] == separator)
            {
                sb.Append(separator);
                state = SeparatedCaseState.Start;
            }
            else
            {
                if (state == SeparatedCaseState.NewWord)
                    sb.Append(separator);
                sb.Append(s[i]);
                state = SeparatedCaseState.Lower;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 分隔策略状态
    /// </summary>
    private enum SeparatedCaseState
    {
        /// <summary>
        /// 开头状态
        /// </summary>
        Start,

        /// <summary>
        /// 小写状态
        /// </summary>
        Lower,

        /// <summary>
        /// 大写状态
        /// </summary>
        Upper,

        /// <summary>
        /// 新单词状态
        /// </summary>
        NewWord
    }

    #endregion

    #region ToKebabCase(转换为烤肉串命名法)

    /// <summary>
    /// 将字符串转换为烤肉串命名法。例如：UserName -> user-name
    /// </summary>
    /// <param name="s">输入字符串</param>
    /// <returns>转换后的烤肉串命名法字符串</returns>
    public static string ToKebabCase(string s) => ToSeparatedCase(s, '-');

    #endregion
}
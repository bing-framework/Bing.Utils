using System.Diagnostics;

namespace Bing.Text.Formatting;

/// <summary>
/// 格式化字符串提取器
/// </summary>
public class FormattedStringValueExtractor
{
    /// <summary>
    /// 提取
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    public static ExtractionResult Extract(string str, string format, bool ignoreCase = false)
    {
        var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (str == format)
            return new ExtractionResult(true);

        var formatTokens = new FormatStringTokenizer().Tokenize(format);
        if (formatTokens.IsNullOrEmpty())
            return new ExtractionResult(str == "");

        var result = new ExtractionResult(true);
        for (var i = 0; i < formatTokens.Count; i++)
        {
            var currentToken = formatTokens[i];
            var previousToken = i > 0 ? formatTokens[i - 1] : null;

            if (currentToken.Type == FormatStringTokenType.ConstantText)
            {
                if (i == 0)
                {
                    if (!str.StartsWith(currentToken.Text, stringComparison))
                    {
                        result.IsMatch = false;
                        return result;
                    }

                    str = str.Substring(currentToken.Text.Length);
                }
                else
                {
                    var matchIndex = str.IndexOf(currentToken.Text, stringComparison);
                    if (matchIndex < 0)
                    {
                        result.IsMatch = false;
                        return result;
                    }

                    Debug.Assert(previousToken != null, "previousToken can not be null since i > 0 here");

                    result.Matches.Add(new NameValue(previousToken!.Text, str.Substring(0, matchIndex)));
                    str = str.Substring(matchIndex + currentToken.Text.Length);
                }
            }
        }

        var lastToken = formatTokens.Last();
        if (lastToken.Type == FormatStringTokenType.DynamicValue)
            result.Matches.Add(new NameValue(lastToken.Text, str));
        return result;
    }

    /// <summary>
    /// 是否完全匹配
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="values">提取值</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    public static bool IsMatch(string str, string format, out string[] values, bool ignoreCase = false)
    {
        var result = Extract(str, format, ignoreCase);
        if (!result.IsMatch)
        {
            values = Array.Empty<string>();
            return false;
        }

        values = result.Matches.Select(m => m.Value).ToArray();
        return true;
    }

    /// <summary>
    /// 提取结果
    /// </summary>
    public class ExtractionResult
    {
        /// <summary>
        /// 是否完全匹配
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// 匹配的动态值列表
        /// </summary>
        public List<NameValue> Matches { get; private set; }

        /// <summary>
        /// 初始化一个<see cref="ExtractionResult"/>类型的实例
        /// </summary>
        /// <param name="isMatch">是否完全匹配</param>
        internal ExtractionResult(bool isMatch)
        {
            IsMatch = isMatch;
            Matches = new List<NameValue>();
        }
    }
}
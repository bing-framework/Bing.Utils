using System.Diagnostics;
using Bing.Extensions;

namespace Bing.Text.Formatting;

/// <summary>
/// 格式化字符串提取器。<br />
/// 从格式化字符串中提取动态值，
/// 可参考 <see cref="string.Format(string,object)"/>
/// </summary>
/// <example>
/// 示例:字符串 "My name is Neo" ，字符模板 "My name is {name}"，
/// Extract 方法执行 "Neo" as "name"。
/// </example>
public class FormattedStringValueExtractor
{
    /// <summary>
    /// 从格式化字符串中动态提取值
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    public static ExtractionResult Extract(string str, string format, bool ignoreCase = false) => ExtractValue(str.SafeString(), format.SafeString(), ignoreCase);

    /// <summary>
    /// 提取值
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    private static ExtractionResult ExtractValue(string str, string format, bool ignoreCase = false)
    {
        var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (str == format)
            return new ExtractionResult(true);

        var formatTokens = new FormatStringTokenizer().Tokenize(format);
        if (formatTokens.IsNullOrEmpty())
            return new ExtractionResult(str == "");

        var result = new ExtractionResult(false);
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
                        return result;
                    }
#if NETSTANDARD2_0
                    str = str.Substring(currentToken.Text.Length);
#else
                    str = str[currentToken.Text.Length..];
#endif

                }
                else
                {
                    var matchIndex = str.IndexOf(currentToken.Text, 1, stringComparison);
                    if (matchIndex < 0)
                    {
                        return result;
                    }

                    Debug.Assert(previousToken != null, "previousToken can not be null since i > 0 here");
                    result.IsMatch = true;
#if NETSTANDARD2_0
                    result.Matches.Add(new NameValue(previousToken!.Text, str.Substring(0, matchIndex)));
                    str = str.Substring(matchIndex + currentToken.Text.Length);
#else
                    result.Matches.Add(new NameValue(previousToken!.Text, str[..matchIndex]));
                    str = str[(matchIndex + currentToken.Text.Length)..];
#endif
                }
            }
        }

        var lastToken = formatTokens.Last();
        if (lastToken.Type == FormatStringTokenType.DynamicValue)
        {
            result.Matches.Add(new NameValue(lastToken.Text, str));
            result.IsMatch = true;
        }
        return result;
    }

    /// <summary>
    /// 是否完全匹配。<br />
    /// 检查字符串 <see cref="str"/> 时候符合给定的格式化字符串 <see cref="format"/>，
    /// 同时获取提取到的值。
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="values">提取值</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    public static bool IsMatch(string str, string format, out string[] values, bool ignoreCase = false)
    {
        var result = ExtractValue(str.SafeString(), format.SafeString(), ignoreCase);
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
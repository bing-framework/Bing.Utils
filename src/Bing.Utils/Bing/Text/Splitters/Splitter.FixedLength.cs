namespace Bing.Text.Splitters;

/// <summary>
/// 字符串分割器
/// </summary>
public partial class Splitter : IFixedLengthSplitter
{
    #region TrimResults(裁剪结果)

    /// <summary>
    /// 裁剪结果
    /// </summary>
    IFixedLengthSplitter IFixedLengthSplitter.TrimResults()
    {
        Options.SetTrimResults();
        return this;
    }

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <param name="trimFunc">裁剪函数</param>
    IFixedLengthSplitter IFixedLengthSplitter.TrimResults(Func<string, string> trimFunc)
    {
        Options.SetTrimResults(trimFunc);
        return this;
    }

    #endregion

    #region Limit(限制)

    /// <summary>
    /// 限制
    /// </summary>
    /// <param name="limit">限制长度</param>
    IFixedLengthSplitter IFixedLengthSplitter.Limit(int limit)
    {
        Options.SetLimitLength(limit);
        return this;
    }

    #endregion

    #region WithKeyValueSeparator(设置键值对分隔符)

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapSplitter IFixedLengthSplitter.WithKeyValueSeparator(char separator)
    {
        Options.SetMapSeparator(separator);
        return this;
    }

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapSplitter IFixedLengthSplitter.WithKeyValueSeparator(string separator)
    {
        Options.SetMapSeparator(separator);
        return this;
    }

    #endregion

    #region Split - List - FixedLength

    /// <summary>
    /// 切割
    /// </summary>
    /// <param name="originalString">字符串</param>
    IEnumerable<string> IFixedLengthSplitter.Split(string originalString) => InternalSplitToEnumerable2(originalString, s => s);

    /// <summary>
    /// 切割为列表
    /// </summary>
    /// <param name="originalString">字符串</param>
    List<string> IFixedLengthSplitter.SplitToList(string originalString) => ((IFixedLengthSplitter)this).Split(originalString).ToList();

    /// <summary>
    /// 将字符串切割成集合【内部】
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="originalString">字符串</param>
    /// <param name="to">转换函数</param>
    private IEnumerable<TValue> InternalSplitToEnumerable2<TValue>(string originalString, Func<string, TValue> to)
    {
        if (string.IsNullOrWhiteSpace(originalString))
            return Enumerable.Empty<TValue>();

        var result = new List<string>();
        var middle = SplitterUtils.SplitByFixedLength(originalString, _fixedLengthPattern);

        if (Options.LimitLength >= 0)
        {
            var counter = 0;
            result.AddRange(SplitterUtils.OptionalRange(Options, middle).TakeWhile(_ => counter++ < Options.LimitLength));
        }
        else
        {
            result.AddRange(SplitterUtils.OptionalRange(Options, middle));
        }

        return result.Select(to);
    }

    #endregion

    #region Split - Array - FiexdLength

    /// <summary>
    /// 切割为数组
    /// </summary>
    /// <param name="originalString">字符串</param>
    string[] IFixedLengthSplitter.SplitToArray(string originalString) => ((IFixedLengthSplitter)this).Split(originalString).ToArray();

    #endregion

    /// <summary>
    /// 文本分割器工具
    /// </summary>
    private static partial class SplitterUtils
    {
        /// <summary>
        /// 按指定长度进行切割字符串
        /// </summary>
        /// <param name="originalString">字符串</param>
        /// <param name="length">长度</param>
        public static string[] SplitByFixedLength(string originalString, int length)
        {
            var mod = originalString.Length % length;
            var group = mod > 0 ? originalString.Length / length + 1 : originalString.Length / length;
            var lastGroupLength = mod == 0 ? length : mod;
            var middle = new char[group][];
            var pointer = 0;

            for (var i = 0; i < group; i++)
            {
                var inlineRangeTimes = i == group - 1 ? lastGroupLength : length;
                middle[i] = new char[inlineRangeTimes];
                for (var j = 0; j < inlineRangeTimes; j++, pointer++)
                    middle[i][j] = originalString[pointer];
            }

            return CharArrayToStringArray(middle);
        }

        /// <summary>
        /// 将二维字符数组转换为字符串数组
        /// </summary>
        /// <param name="charArray">二维字符数组</param>
        private static string[] CharArrayToStringArray(char[][] charArray)
        {
            var result = new string[charArray.Length];
            for (var i = 0; i < charArray.Length; i++)
                result[i] = new string(charArray[i]);
            return result;
        }
    }
}
using System.Text.RegularExpressions;

namespace Bing.Text.Splitters;

/// <summary>
/// 字符串分割器
/// </summary>
public partial class Splitter : ISplitter
{
    /// <summary>
    /// 是否正则表达式模式
    /// </summary>
    private readonly bool _regexMode;

    /// <summary>
    /// 是否固定长度模式
    /// </summary>
    private readonly bool _fixedLengthMode;

    /// <summary>
    /// 分隔符
    /// </summary>
    private readonly string[] _on;

    /// <summary>
    /// 字符串正则表达式
    /// </summary>
    private readonly string _pattern;

    /// <summary>
    /// 正则表达式
    /// </summary>
    private readonly Regex _regexPattern;

    /// <summary>
    /// 固定长度
    /// </summary>
    private readonly int _fixedLengthPattern;

    /// <summary>
    /// 文本分割器选项配置
    /// </summary>
    private SplitterOptions Options { get; set; } = new();

    /// <summary>
    /// 初始化一个<see cref="Splitter"/>类型的实例
    /// </summary>
    /// <param name="on">分隔符</param>
    private Splitter(string[] on)
    {
        _on = on;
        _pattern = string.Empty;
        _regexPattern = null;
        _regexMode = false;
        _fixedLengthPattern = 0;
        _fixedLengthMode = false;
    }

    /// <summary>
    /// 初始化一个<see cref="Splitter"/>类型的实例
    /// </summary>
    /// <param name="pattern">字符串正则表达式</param>
    private Splitter(string pattern)
    {
        _on = Array.Empty<string>();
        _pattern = pattern;
        _regexPattern = null;
        _regexMode = true;
        _fixedLengthPattern = 0;
        _fixedLengthMode = false;
    }

    /// <summary>
    /// 初始化一个<see cref="Splitter"/>类型的实例
    /// </summary>
    /// <param name="regexPattern">正则表达式</param>
    private Splitter(Regex regexPattern)
    {
        _on = Array.Empty<string>();
        _pattern = string.Empty;
        _regexPattern = regexPattern;
        _regexMode = true;
        _fixedLengthPattern = 0;
        _fixedLengthMode = false;
    }

    /// <summary>
    /// 初始化一个<see cref="Splitter"/>类型的实例
    /// </summary>
    /// <param name="fixedLength">固定长度</param>
    private Splitter(int fixedLength)
    {
        _on = Array.Empty<string>();
        _pattern = string.Empty;
        _regexPattern = null;
        _regexMode = false;
        _fixedLengthPattern = fixedLength;
        _fixedLengthMode = true;
    }

    #region OmitEmptyStrings(忽略空字符串)

    /// <summary>
    /// 忽略空字符串
    /// </summary>
    public ISplitter OmitEmptyStrings()
    {
        Options.SetOmitEmptyStrings();
        return this;
    }

    #endregion

    #region TrimResults(裁剪结果)

    /// <summary>
    /// 裁剪结果
    /// </summary>
    ISplitter ISplitter.TrimResults()
    {
        Options.SetTrimResults();
        return this;
    }

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <param name="trimFunc">裁剪函数</param>
    ISplitter ISplitter.TrimResults(Func<string, string> trimFunc)
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
    ISplitter ISplitter.Limit(int limit)
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
    IMapSplitter ISplitter.WithKeyValueSeparator(char separator)
    {
        Options.SetMapSeparator(separator);
        return this;
    }

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapSplitter ISplitter.WithKeyValueSeparator(string separator)
    {
        Options.SetMapSeparator(separator);
        return this;
    }

    #endregion

    #region Split - List

    /// <summary>
    /// 是否使用限制长度模式
    /// </summary>
    private bool _doesUseInLimitedMode() => Options.LimitLength >= 0 && (!_regexMode && _regexPattern == null);

    /// <summary>
    /// 切割
    /// </summary>
    /// <param name="originalString">字符串</param>
    IEnumerable<string> ISplitter.Split(string originalString) => InternalSplitToEnumerable(originalString, s => s);

    /// <summary>
    /// 切割为列表
    /// </summary>
    /// <param name="originalString">字符串</param>
    List<string> ISplitter.SplitToList(string originalString) => ((ISplitter)this).Split(originalString).ToList();

    /// <summary>
    /// 将字符串切割成集合【内部】
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="originalString">字符串</param>
    /// <param name="to">转换函数</param>
    private IEnumerable<TValue> InternalSplitToEnumerable<TValue>(string originalString, Func<string, TValue> to)
    {
        if (string.IsNullOrWhiteSpace(originalString))
            return Enumerable.Empty<TValue>();

        var result = new List<string>();
        var middle = _regexMode
            ? SplitterUtils.SplitPatternList(Options, originalString, _pattern, _regexPattern)
            : SplitterUtils.SplitList(Options, originalString, _on);

        if (_doesUseInLimitedMode())
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

    #region Split - Array

    /// <summary>
    /// 切割为数组
    /// </summary>
    /// <param name="originalString">字符串</param>
    string[] ISplitter.SplitToArray(string originalString) => ((ISplitter)this).Split(originalString).ToArray();

    #endregion

    #region On

    /// <summary>
    /// On 操作，设置一个符号，当命中该符号时执行切割，并返回一个 <see cref="Splitter"/> 实例
    /// </summary>
    /// <param name="on">符号</param>
    /// <param name="on2">符号数组</param>
    public static ISplitter On(char on, params char[] on2)
    {
        var o = new string[(on2?.Length ?? 0) + 1];
        o[0] = $"{on}";
        if (o.Length > 1 && on2 is { Length: > 0 })
            for (var i = 0; i < on2.Length; i++)
                o[i + 1] = $"{on2[i]}";
        return new Splitter(o);
    }

    /// <summary>
    /// On 操作，设置一个符号，当命中该符号时执行切割，并返回一个 <see cref="Splitter"/> 实例
    /// </summary>
    /// <param name="on">符号</param>
    /// <param name="on2">符号数组</param>
    public static ISplitter On(string on, params string[] on2)
    {
        var o = new string[(on2?.Length ?? 0) + 1];
        o[0] = on;
        // if (o.Length > 1 && on2 != null && on2.Length > 0)
        if (o.Length > 1 && on2 is { Length: > 0 })
            Array.Copy(on2, 0, o, 1, on2.Length);
        return new Splitter(o);
    }

    /// <summary>
    /// On 操作，设置一个符号，当命中该符号时执行切割，并返回一个 <see cref="Splitter"/> 实例
    /// </summary>
    /// <param name="pattern">正则表达式</param>
    public static ISplitter On(Regex pattern) => new Splitter(pattern);

    /// <summary>
    /// On 操作，设置一个符号，当命中该符号时执行切割，并返回一个 <see cref="Splitter"/> 实例
    /// </summary>
    /// <param name="separatorPattern">分隔符正则表达式</param>
    public static ISplitter OnPattern(string separatorPattern) => new Splitter(separatorPattern);

    #endregion

    #region FixedLength
    
    /// <summary>
    /// 固定长度
    /// </summary>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IFixedLengthSplitter FixedLength(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, "fixedLength 必须大于0");
        return new Splitter(length);
    }

    #endregion

    /// <summary>
    /// 文本分割器选项配置
    /// </summary>
    private partial class SplitterOptions
    {
        #region OmitEmptyStrings(忽略空字符串)

        /// <summary>
        /// 忽略空字符串
        /// </summary>
        private bool OmitEmptyStrings { get; set; }

        /// <summary>
        /// 设置忽略空字符串
        /// </summary>
        public void SetOmitEmptyStrings() => OmitEmptyStrings = true;

        /// <summary>
        /// 获取字符串分割选项配置
        /// </summary>
        public StringSplitOptions GetStringSplitOptions() => OmitEmptyStrings ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

        #endregion

        #region Limit(限制长度)

        /// <summary>
        /// 限制长度
        /// </summary>
        public int LimitLength { get; private set; } = -1;

        /// <summary>
        /// 设置限制长度
        /// </summary>
        /// <param name="limit">限制长度</param>
        public void SetLimitLength(int limit)
        {
            if (limit <= 0)
                LimitLength = -1;
            else
                LimitLength = limit;
        }

        #endregion

        #region TrimResults(裁剪结果)

        /// <summary>
        /// 裁剪结果标记
        /// </summary>
        public bool TrimResultsFlag { get; private set; }

        /// <summary>
        /// 裁剪函数
        /// </summary>
        public Func<string, string> TrimFunc { get; private set; }

        /// <summary>
        /// 设置裁剪结果
        /// </summary>
        public void SetTrimResults()
        {
            TrimResultsFlag = true;
            TrimFunc = s => s.Trim();
        }

        /// <summary>
        /// 设置裁剪结果
        /// </summary>
        /// <param name="func">裁剪函数</param>
        public void SetTrimResults(Func<string, string> func)
        {
            TrimResultsFlag = true;
            TrimFunc = func ?? (s => s.Trim());
        }

        #endregion
    }

    /// <summary>
    /// 文本分割器工具
    /// </summary>
    private static partial class SplitterUtils
    {
        /// <summary>
        /// 可选的数组
        /// </summary>
        /// <param name="options">文本分割器选项配置</param>
        /// <param name="middleStrings">中间的字符串</param>
        public static IEnumerable<string> OptionalRange(SplitterOptions options, string[] middleStrings) =>
            options.TrimResultsFlag ? middleStrings.Select(options.TrimFunc) : middleStrings;

        /// <summary>
        /// 切割为列表
        /// </summary>
        /// <param name="options">文本分割器选项配置</param>
        /// <param name="originalString">字符串</param>
        /// <param name="on">分隔符</param>
        public static string[] SplitList(SplitterOptions options, string originalString, string[] on) => originalString.Split(on, options.GetStringSplitOptions());

        /// <summary>
        /// 切割为模式列表
        /// </summary>
        /// <param name="options">文本分割器选项配置</param>
        /// <param name="originalString">字符串</param>
        /// <param name="stringPattern">字符串正则表达式</param>
        /// <param name="regexPattern">正则表达式</param>
        public static string[] SplitPatternList(SplitterOptions options, string originalString, string stringPattern, Regex regexPattern)
        {
            return regexPattern == null
                ? Regex.Split(originalString, stringPattern)
                : options.LimitLength > 0
                    ? regexPattern.Split(originalString, options.LimitLength)
                    : regexPattern.Split(originalString);
        }
    }
}
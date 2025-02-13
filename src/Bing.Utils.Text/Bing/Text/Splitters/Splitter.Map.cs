namespace Bing.Text.Splitters;

/// <summary>
/// 字符串分割器
/// </summary>
public partial class Splitter : IMapSplitter
{
    #region TrimResults(裁剪结果)
    
    /// <summary>
    /// 裁剪结果
    /// </summary>
    IMapSplitter IMapSplitter.TrimResults()
    {
        Options.SetTrimResults(k => k.Trim(), v => v.Trim());
        return this;
    }

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <param name="keyTrimFunc">键裁剪函数</param>
    /// <param name="valueTrimFunc">值裁剪函数</param>
    public IMapSplitter TrimResults(Func<string, string> keyTrimFunc, Func<string, string> valueTrimFunc)
    {
        Options.SetTrimResults(keyTrimFunc, valueTrimFunc);
        return this;
    }

    #endregion

    #region Limit(限制)

    /// <summary>
    /// 限制
    /// </summary>
    /// <param name="limit">限制长度</param>
    IMapSplitter IMapSplitter.Limit(int limit)
    {
        Options.SetLimitLength(limit);
        return this;
    }

    #endregion

    #region Split - KeyValuePair

    /// <summary>
    /// 切割
    /// </summary>
    /// <param name="originalString">字符串</param>
    IEnumerable<KeyValuePair<string, string>> IMapSplitter.Split(string originalString) =>
        InternalSplitToKeyValuePair(originalString, s => s);

    /// <summary>
    /// 切割为字典
    /// </summary>
    /// <param name="originalString">字符串</param>
    public Dictionary<string, string> SplitToDictionary(string originalString) =>
        ((IMapSplitter)this).Split(originalString).ToDictionary(k => k.Key, v => v.Value);

    /// <summary>
    /// 将字符串切割成键值对集合【内部】
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="originalString">字符串</param>
    /// <param name="to">转换函数</param>
    private IEnumerable<KeyValuePair<string, TValue>> InternalSplitToKeyValuePair<TValue>(string originalString, Func<string, TValue> to)
    {
        if (string.IsNullOrWhiteSpace(originalString))
            return Enumerable.Empty<KeyValuePair<string, TValue>>();

        var result = new List<KeyValuePair<string, TValue>>();
        var middle = _fixedLengthMode
            ? ((IFixedLengthSplitter)this).Split(originalString)
            : ((ISplitter)this).Split(originalString);

        foreach (var item in middle)
        {
            var tmp = SplitterUtils.SplitMap(Options, item);
            result.Add(SplitterUtils.OptionalMap(Options, tmp.Key, tmp.Value, to));
        }

        return result;
    }

    #endregion

    /// <summary>
    /// 文本分割器选项配置
    /// </summary>
    private partial class SplitterOptions
    {
        #region WithKeyValueSeparator(设置键值分隔符)

        /// <summary>
        /// 映射分隔符
        /// </summary>
        public string MapSeparator { get; private set; }

        /// <summary>
        /// 设置映射分隔符
        /// </summary>
        /// <param name="separator">分隔符</param>
        public void SetMapSeparator(string separator) => MapSeparator = separator;

        /// <summary>
        /// 设置映射分隔符
        /// </summary>
        /// <param name="separator">分隔符</param>
        public void SetMapSeparator(char separator) => MapSeparator = $"{separator}";

        #endregion

        #region TrimResults(裁剪结果)

        /// <summary>
        /// 映射裁剪结果标记
        /// </summary>
        public bool MapTrimResultsFlag { get; private set; }

        /// <summary>
        /// 键裁剪函数
        /// </summary>
        public Func<string, string> KeyTrimFunc { get; private set; }

        /// <summary>
        /// 值裁剪函数
        /// </summary>
        public Func<string, string> ValueTrimFunc { get; private set; }

        /// <summary>
        /// 设置裁剪结果
        /// </summary>
        /// <param name="keyFunc">键裁剪函数</param>
        /// <param name="valueFunc">值裁剪函数</param>
        public void SetTrimResults(Func<string, string> keyFunc, Func<string, string> valueFunc)
        {
            MapTrimResultsFlag = true;
            KeyTrimFunc = keyFunc ?? (k => k.Trim());
            ValueTrimFunc = valueFunc ?? (v => v.Trim());
        }

        #endregion
    }

    /// <summary>
    /// 文本分割器工具
    /// </summary>
    private static partial class SplitterUtils
    {
        /// <summary>
        /// 可选的映射
        /// </summary>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="options">文本分割器选项配置</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="to">转换函数</param>
        public static KeyValuePair<string, TValue> OptionalMap<TValue>(SplitterOptions options, string key, string value, Func<string, TValue> to) =>
            options.MapTrimResultsFlag
                ? new KeyValuePair<string, TValue>(options.KeyTrimFunc(key), to(options.ValueTrimFunc(value)))
                : new KeyValuePair<string, TValue>(key, to(value));

        /// <summary>
        /// 切割为键值对
        /// </summary>
        /// <param name="options">文本分割器选项配置</param>
        /// <param name="middleString">中间字符串</param>
        public static (string Key, string Value) SplitMap(SplitterOptions options, string middleString)
        {
#if NETSTANDARD2_0
            var t = middleString.Split(new[] { options.MapSeparator }, StringSplitOptions.None);
#else
            var t = middleString.Split(options.MapSeparator);
#endif
            var key = t[0];
            var value = t.Length > 1 ? t[1] : string.Empty;
            return (key, value);
        }
    }
}
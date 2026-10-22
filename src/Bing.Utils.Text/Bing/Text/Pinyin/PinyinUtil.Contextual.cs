using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bing.Text.Pinyin;

/// <summary>
/// 提供拼音转换方法。
/// </summary>
public static partial class PinyinUtil
{
#if !BING_TEXT_EXTERNAL_ONLY
    /// <summary>
    /// 首次调用上下文拼音接口时加载的只读词库。
    /// </summary>
    private static readonly Lazy<ContextualData> Contextual = new Lazy<ContextualData>(LoadContextualData);

    /// <summary>
    /// 获取带声调的上下文拼音。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">相邻拼音音节之间的分隔符。</param>
    /// <returns>音节首字母大写的带调拼音；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">separator 为 null。</exception>
    /// <remarks>优先选用最长完整词组；无词组读音时采用单字数据中的首个读音。未收录字符保持原文。</remarks>
    public static string GetPinyinWithTone(string text, string separator = "") =>
        ConvertDefaultContextual(text, separator, true);

    /// <summary>
    /// 获取无声调的上下文拼音。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">相邻拼音音节之间的分隔符。</param>
    /// <returns>音节首字母大写的无调拼音；text 为 null 时返回 null。</returns>
    /// <exception cref="ArgumentNullException">separator 为 null。</exception>
    /// <remarks>与带调入口使用同一词组读音，现有 <see cref="GetPinyin" /> 保留固定 GBK 区码行为。</remarks>
    public static string GetContextualPinyin(string text, string separator = "") =>
        ConvertDefaultContextual(text, separator, false);

    /// <summary>
    /// 使用内置词库转换文本。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">拼音音节分隔符。</param>
    /// <param name="withTone">是否保留声调。</param>
    /// <returns>转换结果。</returns>
    private static string ConvertDefaultContextual(string text, string separator, bool withTone)
    {
        if (separator == null)
            throw new ArgumentNullException(nameof(separator));
        if (string.IsNullOrEmpty(text))
            return text;
        return ConvertContextual(text, separator, withTone, Contextual.Value);
    }
#endif

    /// <summary>
    /// 根据词组和单字读音转换文本。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">拼音音节分隔符。</param>
    /// <param name="withTone">是否保留声调。</param>
    /// <param name="data">转换使用的词库快照。</param>
    /// <returns>转换结果；text 为 null 时返回 null。</returns>
    internal static string ConvertContextual(string text, string separator, bool withTone, ContextualData data)
    {
        if (separator == null)
            throw new ArgumentNullException(nameof(separator));
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        var previousWasPinyin = false;
        for (var index = 0; index < text.Length;)
        {
            if (TryFindPhrase(data.Phrases, text, index, out var length, out var syllables))
            {
                foreach (var syllable in syllables)
                    AppendSyllable(result, syllable, separator, withTone, ref previousWasPinyin);
                index += length;
                continue;
            }

            var value = text[index];
            var codeUnits = char.IsHighSurrogate(value) && index + 1 < text.Length &&
                            char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
            var codePoint = codeUnits == 2 ? char.ConvertToUtf32(value, text[index + 1]) : value;
            if (data.Characters.TryGetValue(codePoint, out var reading))
                AppendSyllable(result, reading, separator, withTone, ref previousWasPinyin);
            else
            {
                result.Append(text, index, codeUnits);
                previousWasPinyin = false;
            }
            index += codeUnits;
        }
        return result.ToString();
    }

    /// <summary>
    /// 追加一个拼音音节。
    /// </summary>
    /// <param name="result">输出文本。</param>
    /// <param name="syllable">原始带调音节。</param>
    /// <param name="separator">音节分隔符。</param>
    /// <param name="withTone">是否保留声调。</param>
    /// <param name="previousWasPinyin">上一个输出是否为拼音音节。</param>
    private static void AppendSyllable(StringBuilder result, string syllable, string separator, bool withTone,
        ref bool previousWasPinyin)
    {
        if (previousWasPinyin)
            result.Append(separator);
        var value = withTone ? syllable : RemoveTone(syllable);
        result.Append(char.ToUpperInvariant(value[0])).Append(value, 1, value.Length - 1);
        previousWasPinyin = true;
    }

    /// <summary>
    /// 移除声调符号并保留 ü 的两点。
    /// </summary>
    /// <param name="syllable">带调音节。</param>
    /// <returns>无调音节。</returns>
    private static string RemoveTone(string syllable)
    {
        var decomposed = syllable.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (var value in decomposed)
            if (value == '\u0308' || CharUnicodeInfo.GetUnicodeCategory(value) != UnicodeCategory.NonSpacingMark)
                result.Append(value);
        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// 查找当前位置最长的完整词组。
    /// </summary>
    /// <param name="root">词组前缀树根节点。</param>
    /// <param name="text">待查询文本。</param>
    /// <param name="index">查询起点。</param>
    /// <param name="length">匹配的 UTF-16 长度。</param>
    /// <param name="syllables">匹配的音节；未匹配时为 null。</param>
    /// <returns>找到完整词组时为 true，否则为 false。</returns>
    private static bool TryFindPhrase(PhraseNode root, string text, int index, out int length,
        out string[] syllables)
    {
        length = 0;
        syllables = null;
        var node = root;
        for (var position = index; position < text.Length; position++)
        {
            if (!node.Children.TryGetValue(text[position], out node))
                break;
            if (node.Syllables == null)
                continue;
            length = position - index + 1;
            syllables = node.Syllables;
        }
        return syllables != null;
    }

#if !BING_TEXT_EXTERNAL_ONLY
    /// <summary>
    /// 从程序集内的固定快照构建拼音索引。
    /// </summary>
    /// <returns>构建完成的只读查询数据。</returns>
    private static ContextualData LoadContextualData()
    {
        using (var reader = OpenData("characters.gz"))
        using (var phrases = OpenData("phrases.gz"))
            return ParseContextualData(reader, phrases);
    }
#endif

    /// <summary>
    /// 从调用方提供的压缩流构建独立词库。
    /// </summary>
    /// <param name="characters">单字词库流。</param>
    /// <param name="phrases">词组词库流。</param>
    /// <returns>构建完成的词库快照。</returns>
    internal static ContextualData LoadContextualData(Stream characters, Stream phrases)
    {
        if (characters == null)
            throw new ArgumentNullException(nameof(characters));
        if (phrases == null)
            throw new ArgumentNullException(nameof(phrases));
        if (!characters.CanRead)
            throw new ArgumentException("单字词库流不可读。", nameof(characters));
        if (!phrases.CanRead)
            throw new ArgumentException("词组词库流不可读。", nameof(phrases));

        using (var characterReader = OpenExternalData(characters))
        using (var phraseReader = OpenExternalData(phrases))
            return ParseContextualData(characterReader, phraseReader);
    }

    /// <summary>
    /// 解析单字与词组词库。
    /// </summary>
    /// <param name="characters">单字记录读取器。</param>
    /// <param name="phrases">词组记录读取器。</param>
    /// <returns>构建完成的词库快照。</returns>
    private static ContextualData ParseContextualData(TextReader characters, TextReader phrases)
    {
        var data = new ContextualData();
        try
        {
            string line;
            while ((line = characters.ReadLine()) != null)
            {
                var separator = line.IndexOf('\t');
                if (separator <= 0 || separator == line.Length - 1 ||
                    !int.TryParse(line.Substring(0, separator), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out var codePoint) ||
                    codePoint > 0x10FFFF || codePoint >= 0xD800 && codePoint <= 0xDFFF ||
                    data.Characters.ContainsKey(codePoint))
                    throw new InvalidDataException("单字拼音词库格式无效。");
                data.Characters.Add(codePoint, line.Substring(separator + 1));
            }
            while ((line = phrases.ReadLine()) != null)
            {
                var separator = line.IndexOf('\t');
                if (separator <= 0 || separator == line.Length - 1)
                    throw new InvalidDataException("词组拼音词库格式无效。");
                var phrase = line.Substring(0, separator);
                var syllables = line.Substring(separator + 1).Split(' ');
                if (syllables.Length != CountCodePoints(phrase) || Array.Exists(syllables, string.IsNullOrEmpty))
                    throw new InvalidDataException("词组拼音词库格式无效。");
                var node = data.Phrases;
                foreach (var value in phrase)
                {
                    if (!node.Children.TryGetValue(value, out var next))
                    {
                        next = new PhraseNode();
                        node.Children.Add(value, next);
                    }
                    node = next;
                }
                if (node.Syllables != null)
                    throw new InvalidDataException("词组拼音词库包含重复词组。");
                node.Syllables = syllables;
            }
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidDataException("拼音词库不是有效的 UTF-8 文本。", error);
        }
        return data;
    }

    /// <summary>
    /// 统计词组的 Unicode 代码点数。
    /// </summary>
    /// <param name="value">待统计词组。</param>
    /// <returns>代码点数。</returns>
    private static int CountCodePoints(string value)
    {
        var count = 0;
        for (var index = 0; index < value.Length; index++, count++)
            if (char.IsHighSurrogate(value[index]) && index + 1 < value.Length &&
                char.IsLowSurrogate(value[index + 1]))
                index++;
        return count;
    }

    /// <summary>
    /// 打开调用方的压缩词库流，同时保留流的所有权。
    /// </summary>
    /// <param name="stream">UTF-8 gzip 词库流。</param>
    /// <returns>用于逐行读取的文本读取器。</returns>
    private static StreamReader OpenExternalData(Stream stream) =>
        new StreamReader(new GZipStream(stream, CompressionMode.Decompress, true),
            new UTF8Encoding(false, true), false);

#if !BING_TEXT_EXTERNAL_ONLY
    /// <summary>
    /// 打开程序集内的 UTF-8 压缩数据。
    /// </summary>
    /// <param name="name">压缩资源文件名。</param>
    /// <returns>用于逐行读取的文本读取器。</returns>
    private static StreamReader OpenData(string name)
    {
        var resource = typeof(PinyinUtil).Assembly.GetManifestResourceStream("Bing.Text.Pinyin.Data." + name);
        if (resource == null)
            throw new InvalidOperationException("拼音数据资源缺失：" + name);
        return new StreamReader(new GZipStream(resource, CompressionMode.Decompress),
            new UTF8Encoding(false, true), false);
    }
#endif

    /// <summary>
    /// 保存构建完成的单字和词组索引。
    /// </summary>
    internal sealed class ContextualData
    {
        /// <summary>将 Unicode 代码点映射到首选单字读音。</summary>
        internal readonly Dictionary<int, string> Characters = new Dictionary<int, string>();

        /// <summary>词组最长匹配的根节点。</summary>
        internal readonly PhraseNode Phrases = new PhraseNode();
    }

    /// <summary>
    /// 保存词组前缀及其完整读音。
    /// </summary>
    internal sealed class PhraseNode
    {
        /// <summary>后续 UTF-16 代码单元的索引。</summary>
        internal readonly Dictionary<char, PhraseNode> Children = new Dictionary<char, PhraseNode>();

        /// <summary>当前前缀为完整词组时的音节。</summary>
        internal string[] Syllables;
    }
}

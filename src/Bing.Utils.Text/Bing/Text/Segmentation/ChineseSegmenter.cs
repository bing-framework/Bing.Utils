using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace Bing.Text.Segmentation;

/// <summary>
/// 使用调用方提供的词频词典对中文进行分词。
/// </summary>
/// <remarks>词典在构造时完整读取，实例随后可并发查询。未登录词识别需要显式加载外置 HMM 模型。</remarks>
public sealed class ChineseSegmenter
{
    /// <summary>
    /// 空片段结果。
    /// </summary>
    private static readonly IReadOnlyList<ChineseSegment> EmptySegments =
        new ReadOnlyCollection<ChineseSegment>(new List<ChineseSegment>());

    /// <summary>
    /// 空词值结果。
    /// </summary>
    private static readonly IReadOnlyList<string> EmptyWords =
        new ReadOnlyCollection<string>(new List<string>());

    /// <summary>
    /// 词频快照。
    /// </summary>
    private readonly Dictionary<string, int> _frequencies;

    /// <summary>
    /// 业务词频覆盖项。
    /// </summary>
    private readonly Dictionary<string, int> _overrides;

    /// <summary>
    /// 当前词频总量。
    /// </summary>
    private readonly long _totalFrequency;

    /// <summary>
    /// 最长词条的 UTF-16 长度。
    /// </summary>
    private readonly int _maxWordLength;

    /// <summary>
    /// 词频总量的对数。
    /// </summary>
    private readonly double _logTotalFrequency;

    /// <summary>
    /// 可选的未登录词识别模型。
    /// </summary>
    private readonly ChineseHmmModel _hmmModel;

    /// <summary>
    /// 初始化一个 <see cref="ChineseSegmenter"/> 类型的实例。
    /// </summary>
    /// <param name="dictionary">当前位置起读取的 UTF-8 jieba 格式词典流。</param>
    /// <exception cref="ArgumentNullException">dictionary 为 null。</exception>
    /// <exception cref="ArgumentException">dictionary 不可读。</exception>
    /// <exception cref="InvalidDataException">词条或词频不符合格式。</exception>
    /// <remarks>每行格式为“词 词频 [词性]”。重复词保留首次出现的记录；读取结束后不关闭输入流。</remarks>
    public ChineseSegmenter(Stream dictionary)
    {
        _frequencies = ReadDictionary(dictionary, false);
        _overrides = new Dictionary<string, int>(StringComparer.Ordinal);
        long totalFrequency = 0;
        var maxWordLength = 0;
        foreach (var entry in _frequencies)
        {
            totalFrequency += entry.Value;
            if (entry.Key.Length > maxWordLength)
                maxWordLength = entry.Key.Length;
        }
        _totalFrequency = totalFrequency;
        _maxWordLength = maxWordLength;
        _logTotalFrequency = Math.Log(Math.Max(totalFrequency, 1));
    }

    /// <summary>
    /// 初始化一个 <see cref="ChineseSegmenter"/> 类型的实例。
    /// </summary>
    /// <param name="frequencies">基础词频。</param>
    /// <param name="overrides">业务词频覆盖项。</param>
    /// <param name="totalFrequency">词频总量。</param>
    /// <param name="maxWordLength">最长词条的 UTF-16 长度。</param>
    /// <param name="hmmModel">可选的未登录词识别模型。</param>
    private ChineseSegmenter(Dictionary<string, int> frequencies, Dictionary<string, int> overrides,
        long totalFrequency, int maxWordLength, ChineseHmmModel hmmModel)
    {
        _frequencies = frequencies;
        _overrides = overrides;
        _totalFrequency = totalFrequency;
        _maxWordLength = maxWordLength;
        _logTotalFrequency = Math.Log(Math.Max(totalFrequency, 1));
        _hmmModel = hmmModel;
    }

    /// <summary>
    /// 叠加业务词频并创建新分词器。
    /// </summary>
    /// <param name="dictionary">当前位置起读取的 UTF-8 jieba 格式词典流。</param>
    /// <returns>包含业务词频的新分词器。</returns>
    /// <exception cref="ArgumentNullException">dictionary 为 null。</exception>
    /// <exception cref="ArgumentException">dictionary 不可读。</exception>
    /// <exception cref="InvalidDataException">词条或词频不符合格式。</exception>
    /// <remarks>同一业务词典中重复词以最后一条为准；业务词频覆盖基础词频。原实例不变，读取结束后不关闭输入流。</remarks>
    public ChineseSegmenter WithDictionary(Stream dictionary)
    {
        var additions = ReadDictionary(dictionary, true);
        var overrides = new Dictionary<string, int>(_overrides, StringComparer.Ordinal);
        var totalFrequency = _totalFrequency;
        var maxWordLength = _maxWordLength;
        foreach (var entry in additions)
        {
            if (TryGetFrequency(entry.Key, out var previous))
                totalFrequency -= previous;
            totalFrequency += entry.Value;
            overrides[entry.Key] = entry.Value;
            if (entry.Key.Length > maxWordLength)
                maxWordLength = entry.Key.Length;
        }
        return new ChineseSegmenter(_frequencies, overrides, totalFrequency, maxWordLength, _hmmModel);
    }

    /// <summary>
    /// 加载未登录词模型并创建新分词器。
    /// </summary>
    /// <param name="model">当前位置起读取的 gzip 概率模型流。</param>
    /// <returns>启用未登录词识别的新分词器。</returns>
    /// <exception cref="ArgumentNullException">model 为 null。</exception>
    /// <exception cref="ArgumentException">model 不可读。</exception>
    /// <exception cref="InvalidDataException">模型格式无效或数据不完整。</exception>
    /// <remarks>模型仅处理词频路径上的连续单字 BMP 汉字片段；读取结束后不关闭输入流，原实例不变。</remarks>
    public ChineseSegmenter WithHmmModel(Stream model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (!model.CanRead)
            throw new ArgumentException("模型流不可读。", nameof(model));
        var hmmModel = ChineseHmmModel.Load(model);
        return new ChineseSegmenter(_frequencies, _overrides, _totalFrequency, _maxWordLength, hmmModel);
    }

    /// <summary>
    /// 将文本分割为有原文位置的片段。
    /// </summary>
    /// <param name="text">待分词文本。</param>
    /// <returns>顺序排列的只读片段列表；null 或空文本返回空列表。</returns>
    /// <remarks>位置和长度以 UTF-16 代码单元计。中文使用词频最佳路径；连续 ASCII 字母数字和连续空白各成一段，其余字符按 Unicode 标量保留。</remarks>
    public IReadOnlyList<ChineseSegment> FindAll(string text)
    {
        if (string.IsNullOrEmpty(text))
            return EmptySegments;

        var segments = new List<ChineseSegment>();
        for (var index = 0; index < text.Length;)
        {
            var codePoint = ReadCodePoint(text, index, out var width);
            if (IsHan(codePoint))
            {
                var end = index + width;
                while (end < text.Length && IsHan(ReadCodePoint(text, end, out width)))
                    end += width;
                SegmentHanRun(text, index, end, segments);
                index = end;
                continue;
            }

            var length = width;
            if (IsAsciiWord(codePoint))
            {
                while (index + length < text.Length && IsAsciiWord(text[index + length]))
                    length++;
            }
            else if (codePoint <= char.MaxValue && char.IsWhiteSpace((char)codePoint))
            {
                while (index + length < text.Length && char.IsWhiteSpace(text[index + length]))
                    length++;
            }
            segments.Add(new ChineseSegment(text.Substring(index, length), index));
            index += length;
        }
        return segments.AsReadOnly();
    }

    /// <summary>
    /// 将文本分割为词值列表。
    /// </summary>
    /// <param name="text">待分词文本。</param>
    /// <returns>顺序排列的只读词值列表；null 或空文本返回空列表。</returns>
    public IReadOnlyList<string> Cut(string text)
    {
        return SelectValues(FindAll(text));
    }

    /// <summary>
    /// 查找适合检索的分词及词典子词。
    /// </summary>
    /// <param name="text">待分词文本。</param>
    /// <returns>按起点和长度排列的只读片段列表；null 或空文本返回空列表。</returns>
    /// <remarks>保留原分词，并从长度不少于三个 Unicode 标量的汉字分词中提取词典收录的二字和三字子词。位置和长度以 UTF-16 代码单元计。</remarks>
    public IReadOnlyList<ChineseSegment> FindForSearch(string text)
    {
        var source = FindAll(text);
        if (source.Count == 0)
            return EmptySegments;

        var results = new List<ChineseSegment>(source);
        foreach (var segment in source)
        {
            if (!IsHan(ReadCodePoint(segment.Value, 0, out _)))
                continue;
            var offsets = new List<int> { 0 };
            for (var position = 0; position < segment.Length;)
            {
                ReadCodePoint(segment.Value, position, out var width);
                position += width;
                offsets.Add(position);
            }
            var count = offsets.Count - 1;
            if (count < 3)
                continue;
            for (var start = 0; start < count; start++)
            {
                for (var length = 2; length <= 3 && start + length <= count; length++)
                {
                    var value = segment.Value.Substring(offsets[start], offsets[start + length] - offsets[start]);
                    if (TryGetFrequency(value, out _))
                        results.Add(new ChineseSegment(value, segment.Index + offsets[start]));
                }
            }
        }
        results.Sort((left, right) =>
        {
            var byIndex = left.Index.CompareTo(right.Index);
            return byIndex != 0 ? byIndex : left.Length.CompareTo(right.Length);
        });
        for (var index = results.Count - 1; index > 0; index--)
        {
            if (results[index].Index == results[index - 1].Index &&
                results[index].Length == results[index - 1].Length)
                results.RemoveAt(index);
        }
        return results.AsReadOnly();
    }

    /// <summary>
    /// 获取适合检索的分词词值。
    /// </summary>
    /// <param name="text">待分词文本。</param>
    /// <returns>按起点和长度排列的只读词值列表；null 或空文本返回空列表。</returns>
    public IReadOnlyList<string> CutForSearch(string text) => SelectValues(FindForSearch(text));

    /// <summary>
    /// 提取分词词值。
    /// </summary>
    /// <param name="segments">分词片段。</param>
    /// <returns>只读词值列表。</returns>
    private static IReadOnlyList<string> SelectValues(IReadOnlyList<ChineseSegment> segments)
    {
        if (segments.Count == 0)
            return EmptyWords;
        var words = new List<string>(segments.Count);
        foreach (var segment in segments)
            words.Add(segment.Value);
        return words.AsReadOnly();
    }

    /// <summary>
    /// 计算汉字区段中词频最高的切分路径。
    /// </summary>
    /// <param name="text">原文。</param>
    /// <param name="start">区段起点。</param>
    /// <param name="end">区段终点。</param>
    /// <param name="segments">输出片段列表。</param>
    private void SegmentHanRun(string text, int start, int end, List<ChineseSegment> segments)
    {
        var offsets = new List<int> { start };
        for (var position = start; position < end;)
        {
            ReadCodePoint(text, position, out var width);
            position += width;
            offsets.Add(position);
        }
        var count = offsets.Count - 1;
        var scores = new double[count + 1];
        var next = new int[count];
        for (var index = count - 1; index >= 0; index--)
        {
            var best = double.NegativeInfinity;
            for (var endIndex = index + 1; endIndex <= count &&
                 offsets[endIndex] - offsets[index] <= Math.Max(_maxWordLength, 2); endIndex++)
            {
                var word = text.Substring(offsets[index], offsets[endIndex] - offsets[index]);
                if (!TryGetFrequency(word, out var frequency))
                {
                    if (endIndex != index + 1)
                        continue;
                    frequency = 1;
                }
                var score = Math.Log(frequency) - _logTotalFrequency + scores[endIndex];
                if (score < best || score == best && endIndex <= next[index])
                    continue;
                best = score;
                next[index] = endIndex;
            }
            scores[index] = best;
        }
        for (var index = 0; index < count;)
        {
            if (_hmmModel != null && next[index] == index + 1 &&
                offsets[index + 1] - offsets[index] == 1)
            {
                var singleStart = index;
                while (index < count && next[index] == index + 1 &&
                       offsets[index + 1] - offsets[index] == 1)
                    index++;
                var word = text.Substring(offsets[singleStart], offsets[index] - offsets[singleStart]);
                if (index - singleStart > 1 && !TryGetFrequency(word, out _))
                    _hmmModel.Segment(word, offsets[singleStart], segments);
                else
                {
                    for (var part = singleStart; part < index; part++)
                        segments.Add(new ChineseSegment(text.Substring(offsets[part], 1), offsets[part]));
                }
                continue;
            }
            segments.Add(new ChineseSegment(text.Substring(offsets[index], offsets[next[index]] - offsets[index]),
                offsets[index]));
            index = next[index];
        }
    }

    /// <summary>
    /// 查询当前实例的有效词频。
    /// </summary>
    /// <param name="word">待查询词条。</param>
    /// <param name="frequency">有效词频。</param>
    /// <returns>词条存在时返回 true。</returns>
    private bool TryGetFrequency(string word, out int frequency) =>
        _overrides.TryGetValue(word, out frequency) || _frequencies.TryGetValue(word, out frequency);

    /// <summary>
    /// 读取 UTF-8 词频词典。
    /// </summary>
    /// <param name="dictionary">词典流。</param>
    /// <param name="replaceDuplicates">是否以最后一条重复记录为准。</param>
    /// <returns>词频字典。</returns>
    private static Dictionary<string, int> ReadDictionary(Stream dictionary, bool replaceDuplicates)
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));
        if (!dictionary.CanRead)
            throw new ArgumentException("词典流不可读。", nameof(dictionary));

        var frequencies = new Dictionary<string, int>(StringComparer.Ordinal);
        var lineNumber = 0;
        try
        {
            using (var reader = new StreamReader(dictionary, new UTF8Encoding(false, true), false, 4096, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (lineNumber == 1 && line.Length > 0 && line[0] == '\uFEFF')
                        line = line.Substring(1);
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2 || parts.Length > 3 ||
                        !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var frequency) ||
                        frequency <= 0 || string.IsNullOrWhiteSpace(parts[0]))
                        throw new InvalidDataException("无效的中文分词词典记录，行号：" + lineNumber);
                    if (replaceDuplicates || !frequencies.ContainsKey(parts[0]))
                        frequencies[parts[0]] = frequency;
                }
            }
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidDataException("中文分词词典不是有效的 UTF-8 文本。", error);
        }
        return frequencies;
    }

    /// <summary>
    /// 读取一个 Unicode 标量或孤立代理项。
    /// </summary>
    /// <param name="text">原文。</param>
    /// <param name="index">UTF-16 起点。</param>
    /// <param name="width">占用的 UTF-16 代码单元数。</param>
    /// <returns>代码点或孤立代理项的值。</returns>
    private static int ReadCodePoint(string text, int index, out int width)
    {
        width = char.IsHighSurrogate(text[index]) && index + 1 < text.Length &&
                char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
        return width == 2 ? char.ConvertToUtf32(text[index], text[index + 1]) : text[index];
    }

    /// <summary>
    /// 判断代码点是否位于常用汉字及扩展汉字区段。
    /// </summary>
    /// <param name="codePoint">Unicode 代码点。</param>
    /// <returns>属于汉字区段时返回 true；否则返回 false。</returns>
    private static bool IsHan(int codePoint) =>
        codePoint >= 0x3400 && codePoint <= 0x9FFF ||
        codePoint >= 0xF900 && codePoint <= 0xFAFF ||
        codePoint >= 0x20000 && codePoint <= 0x323AF;

    /// <summary>
    /// 判断字符是否为 ASCII 字母或数字。
    /// </summary>
    /// <param name="codePoint">Unicode 代码点。</param>
    /// <returns>属于 ASCII 字母或数字时返回 true；否则返回 false。</returns>
    private static bool IsAsciiWord(int codePoint) =>
        codePoint >= '0' && codePoint <= '9' ||
        codePoint >= 'A' && codePoint <= 'Z' ||
        codePoint >= 'a' && codePoint <= 'z';
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bing.Text.Segmentation;

/// <summary>
/// 使用外置 B/M/E/S 概率模型识别未登录汉字词。
/// </summary>
internal sealed class ChineseHmmModel
{
    /// <summary>
    /// 模型状态数量。
    /// </summary>
    private const int StateCount = 4;

    /// <summary>
    /// 未收录发射和非法转移的概率下界。
    /// </summary>
    private const double MinProbability = -3.14e100;

    /// <summary>
    /// 各状态的初始对数概率。
    /// </summary>
    private readonly double[] _starts;

    /// <summary>
    /// 各状态之间的转移对数概率。
    /// </summary>
    private readonly double[,] _transitions;

    /// <summary>
    /// 各状态下汉字的发射对数概率。
    /// </summary>
    private readonly Dictionary<char, double>[] _emissions;

    /// <summary>
    /// 初始化一个 <see cref="ChineseHmmModel"/> 类型的实例。
    /// </summary>
    /// <param name="starts">初始概率。</param>
    /// <param name="transitions">转移概率。</param>
    /// <param name="emissions">发射概率。</param>
    private ChineseHmmModel(double[] starts, double[,] transitions,
        Dictionary<char, double>[] emissions)
    {
        _starts = starts;
        _transitions = transitions;
        _emissions = emissions;
    }

    /// <summary>
    /// 从调用方流加载概率模型。
    /// </summary>
    /// <param name="stream">当前位置起读取的 gzip 模型流。</param>
    /// <returns>可并发复用的模型。</returns>
    internal static ChineseHmmModel Load(Stream stream)
    {
        try
        {
            using (var gzip = new GZipStream(stream, CompressionMode.Decompress, true))
            using (var reader = new BinaryReader(gzip, Encoding.UTF8, true))
            {
                var magic = reader.ReadBytes(4);
                if (magic.Length != 4 || magic[0] != 'B' || magic[1] != 'H' ||
                    magic[2] != 'M' || magic[3] != '1')
                    throw new InvalidDataException("无效的中文分词 HMM 模型版本。");

                var starts = new double[StateCount];
                var transitions = new double[StateCount, StateCount];
                var emissions = new Dictionary<char, double>[StateCount];
                for (var state = 0; state < StateCount; state++)
                    starts[state] = ReadProbability(reader);
                for (var source = 0; source < StateCount; source++)
                for (var target = 0; target < StateCount; target++)
                    transitions[source, target] = ReadProbability(reader);
                for (var state = 0; state < StateCount; state++)
                {
                    var count = reader.ReadInt32();
                    if (count < 0 || count > char.MaxValue + 1)
                        throw new InvalidDataException("无效的中文分词 HMM 发射概率数量。");
                    var values = new Dictionary<char, double>();
                    for (var index = 0; index < count; index++)
                    {
                        var codePoint = reader.ReadInt32();
                        var probability = ReadProbability(reader);
                        if (codePoint < 0 || codePoint > char.MaxValue ||
                            char.IsSurrogate((char)codePoint) ||
                            values.ContainsKey((char)codePoint))
                            throw new InvalidDataException("无效的中文分词 HMM 发射概率记录。");
                        values.Add((char)codePoint, probability);
                    }
                    emissions[state] = values;
                }
                if (reader.ReadBytes(1).Length != 0)
                    throw new InvalidDataException("中文分词 HMM 模型包含多余数据。");
                return new ChineseHmmModel(starts, transitions, emissions);
            }
        }
        catch (EndOfStreamException error)
        {
            throw new InvalidDataException("中文分词 HMM 模型不完整。", error);
        }
    }

    /// <summary>
    /// 使用维特比路径拆分一段 BMP 汉字文本。
    /// </summary>
    /// <param name="text">待识别文本。</param>
    /// <param name="index">片段在原文中的 UTF-16 起点。</param>
    /// <param name="segments">输出片段列表。</param>
    internal void Segment(string text, int index, List<ChineseSegment> segments)
    {
        var previous = new double[StateCount];
        var current = new double[StateCount];
        var back = new byte[text.Length, StateCount];
        for (var state = 0; state < StateCount; state++)
            previous[state] = _starts[state] + GetEmission(state, text[0]);

        for (var position = 1; position < text.Length; position++)
        {
            for (var state = 0; state < StateCount; state++)
            {
                var best = double.NegativeInfinity;
                for (var source = 0; source < StateCount; source++)
                {
                    var score = previous[source] + _transitions[source, state];
                    if (score <= best)
                        continue;
                    best = score;
                    back[position, state] = (byte)source;
                }
                current[state] = best + GetEmission(state, text[position]);
            }
            var swap = previous;
            previous = current;
            current = swap;
        }

        var path = new byte[text.Length];
        var last = previous[2] >= previous[3] ? 2 : 3;
        for (var position = text.Length - 1; position >= 0; position--)
        {
            path[position] = (byte)last;
            last = back[position, last];
        }
        var begin = 0;
        for (var position = 0; position < text.Length; position++)
        {
            if (path[position] != 2 && path[position] != 3)
                continue;
            var length = position - begin + 1;
            segments.Add(new ChineseSegment(text.Substring(begin, length), index + begin));
            begin = position + 1;
        }
        if (begin < text.Length)
            segments.Add(new ChineseSegment(text.Substring(begin), index + begin));
    }

    /// <summary>
    /// 读取并校验一个对数概率。
    /// </summary>
    /// <param name="reader">模型读取器。</param>
    /// <returns>有效的对数概率。</returns>
    private static double ReadProbability(BinaryReader reader)
    {
        var probability = reader.ReadDouble();
        if (double.IsNaN(probability) || double.IsInfinity(probability))
            throw new InvalidDataException("无效的中文分词 HMM 概率。");
        return probability;
    }

    /// <summary>
    /// 查询状态和汉字对应的发射概率。
    /// </summary>
    /// <param name="state">模型状态。</param>
    /// <param name="value">汉字。</param>
    /// <returns>已收录概率或默认下界。</returns>
    private double GetEmission(int state, char value) =>
        _emissions[state].TryGetValue(value, out var probability) ? probability : MinProbability;
}

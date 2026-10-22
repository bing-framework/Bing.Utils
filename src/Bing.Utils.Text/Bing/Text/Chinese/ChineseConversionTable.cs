using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bing.Text.Chinese;

/// <summary>
/// 保存一个不可变的中文转换前缀树并执行最长词转换。
/// </summary>
internal sealed class ChineseConversionTable
{
    /// <summary>
    /// 保存词表前缀树根节点。
    /// </summary>
    private readonly Node _root;

    /// <summary>
    /// 初始化一个 <see cref="ChineseConversionTable"/> 类型的实例。
    /// </summary>
    /// <param name="root">词表前缀树根节点。</param>
    private ChineseConversionTable(Node root)
    {
        _root = root;
    }

    /// <summary>
    /// 从 UTF-8 gzip 词表流加载转换表。
    /// </summary>
    /// <param name="stream">从当前位置读取的 gzip 流。</param>
    /// <param name="name">词表名称，用于异常信息。</param>
    /// <param name="leaveOpen">读取完成后是否保留输入流开放。</param>
    /// <returns>加载完成的转换表。</returns>
    internal static ChineseConversionTable Load(Stream stream, string name, bool leaveOpen)
    {
        var root = new Node();
        try
        {
            using (var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen))
            using (var reader = new StreamReader(gzip, new UTF8Encoding(false, true), false, 4096, leaveOpen))
            {
                string line;
                var lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (lineNumber == 1 && line.Length > 0 && line[0] == '\uFEFF')
                        line = line.Substring(1);
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var separator = line.IndexOf('\t');
                    if (separator <= 0 || separator == line.Length - 1)
                        throw new InvalidDataException("Invalid Chinese conversion resource: " + name);

                    var key = line.Substring(0, separator);
                    var value = line.Substring(separator + 1);
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                        throw new InvalidDataException("Invalid Chinese conversion resource: " + name);

                    var node = root;
                    for (var index = 0; index < key.Length; index++)
                    {
                        var character = key[index];
                        if (!node.Children.TryGetValue(character, out var child))
                        {
                            child = new Node();
                            node.Children.Add(character, child);
                        }
                        node = child;
                    }
                    if (node.Value != null)
                        throw new InvalidDataException("Duplicate Chinese conversion entry: " + name);
                    node.Value = value;
                }
            }
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Invalid UTF-8 Chinese conversion resource: " + name, exception);
        }

        return new ChineseConversionTable(root);
    }

    /// <summary>
    /// 按最长词组转换文本。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <returns>转换后的文本；null 或空文本保持原值。</returns>
    internal string Convert(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length;)
        {
            var node = _root;
            string replacement = null;
            var length = 0;
            for (var position = index; position < text.Length; position++)
            {
                if (!node.Children.TryGetValue(text[position], out node))
                    break;
                if (node.Value == null)
                    continue;
                replacement = node.Value;
                length = position - index + 1;
            }

            if (replacement != null)
            {
                result.Append(replacement);
                index += length;
            }
            else
            {
                result.Append(text[index]);
                index++;
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// 保存一个词组前缀及其转换结果。
    /// </summary>
    private sealed class Node
    {
        /// <summary>
        /// 按 UTF-16 代码单元索引的后续词组节点。
        /// </summary>
        public readonly Dictionary<char, Node> Children = new Dictionary<char, Node>();

        /// <summary>
        /// 完整词组的替换文本；未到达词组结尾时为 null。
        /// </summary>
        public string Value;
    }
}

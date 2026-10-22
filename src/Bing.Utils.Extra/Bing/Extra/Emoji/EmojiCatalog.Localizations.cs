using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Bing.Extra.Emoji;

/// <summary>
/// Emoji 序列前缀树及元数据索引。
/// </summary>
public sealed partial class EmojiCatalog
{
    /// <summary>
    /// 从离线 JSON 创建带本地化数据的目录。
    /// </summary>
    /// <param name="stream">从当前位置读取的 UTF-8 JSON 流。</param>
    /// <returns>完成本地化合并的不可变新目录。</returns>
    /// <exception cref="ArgumentNullException">stream 为 null。</exception>
    /// <exception cref="ArgumentException">stream 不可读。</exception>
    /// <exception cref="InvalidDataException">JSON 格式、版本或本地化记录无效。</exception>
    /// <remarks>
    /// 不关闭输入流，不要求流可定位；底层读取异常原样传播。
    /// 同一表情的同语言名称和关键词整条替换，其他元数据保持不变；原目录和静态目录不受影响。
    /// 新增语言标识统一小写，已有语言保留其标识。导入期间调用方不得并发修改输入流。
    /// </remarks>
    public EmojiCatalog WithLocalizations(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("本地化数据流必须可读。", nameof(stream));

        var document = ReadLocalizationDocument(stream);
        if (document.FormatVersion != 1 || string.IsNullOrWhiteSpace(document.Locale) ||
            document.Locale.Any(char.IsWhiteSpace) || document.Entries == null)
            throw new InvalidDataException("本地化版本、语言或记录列表无效。");

        var locale = GetLocales().FirstOrDefault(value =>
            string.Equals(value, document.Locale, StringComparison.OrdinalIgnoreCase)) ?? document.Locale.ToLowerInvariant();
        var replacements = new Dictionary<string, EmojiLocalization>(StringComparer.Ordinal);
        foreach (var entry in document.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Unicode) ||
                string.IsNullOrWhiteSpace(entry.Name) || entry.Keywords == null ||
                !TryGetByUnicode(entry.Unicode, out var emoji))
                throw new InvalidDataException("本地化记录必须引用目录中的完整表情，并提供名称和关键词数组。");
            if (replacements.ContainsKey(emoji.Unicode))
                throw new InvalidDataException("本地化记录包含重复的规范表情。");

            var keywords = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var keyword in entry.Keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    throw new InvalidDataException("本地化关键词不能为空或仅含空白。");
                if (seen.Add(keyword))
                    keywords.Add(keyword);
            }
            replacements.Add(emoji.Unicode, new EmojiLocalization(locale, entry.Name, keywords.ToArray()));
        }

        var items = new List<EmojiInfo>(Items.Count);
        foreach (var item in Items)
        {
            if (!replacements.TryGetValue(item.Unicode, out var replacement))
            {
                items.Add(item);
                continue;
            }
            var localizations = item.Localizations.ToList();
            var index = localizations.FindIndex(value => string.Equals(value.Locale, locale, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                localizations.Add(replacement);
            else
                localizations[index] = replacement;
            items.Add(new EmojiInfo(item.Unicode, item.Variants.ToArray(), item.Name, item.Group,
                item.Subgroup, item.Version, item.Aliases.ToArray(), item.Tags.ToArray(), localizations.ToArray()));
        }
        return Create(items);
    }

    /// <summary>
    /// 获取目录中的本地化语言标识。
    /// </summary>
    /// <returns>按元数据首次出现顺序排列、忽略大小写去重的只读列表；不包含英文元数据隐含的默认语言。</returns>
    public IReadOnlyList<string> GetLocales()
    {
        var values = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Items)
        foreach (var localization in item.Localizations)
            if (seen.Add(localization.Locale))
                values.Add(localization.Locale);
        return values.AsReadOnly();
    }

    /// <summary>
    /// 查询表情的本地化元数据。
    /// </summary>
    /// <param name="unicode">完整规范序列或受支持的兼容序列。</param>
    /// <param name="locale">不区分大小写、精确匹配的语言标识。</param>
    /// <param name="localization">找到的本地化数据；查询失败时为 null。</param>
    /// <returns>找到记录时为 true；参数为空、表情或语言未知时为 false。</returns>
    /// <remarks>不自动回退到英文、基础语言或其他区域语言。</remarks>
    public bool TryGetLocalization(string unicode, string locale, out EmojiLocalization localization)
    {
        localization = null;
        if (string.IsNullOrEmpty(locale) || !TryGetByUnicode(unicode, out var emoji))
            return false;
        localization = emoji.Localizations.FirstOrDefault(value =>
            string.Equals(value.Locale, locale, StringComparison.OrdinalIgnoreCase));
        return localization != null;
    }

    /// <summary>
    /// 读取并校验离线本地化文档。
    /// </summary>
    /// <param name="stream">调用方持有的可读流。</param>
    /// <returns>完成结构校验的本地化文档。</returns>
    private static LocalizationDocument ReadLocalizationDocument(Stream stream)
    {
        // 先读取以保证底层 I/O 异常不会被解析器包装成格式异常。
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        try
        {
            var bytes = buffer.ToArray();
            var encoding = new UTF8Encoding(false, true);
            var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            ValidateJsonDocumentEnd(encoding.GetString(bytes, offset, bytes.Length - offset));
            using var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, offset, bytes.Length - offset, encoding, XmlDictionaryReaderQuotas.Max, null);
            var xml = XDocument.Load(reader);
            var root = xml.Root;
            RequireJsonType(root, "object");
            RequireJsonMember(root, "formatVersion", "number");
            RequireJsonMember(root, "locale", "string");
            var entries = RequireJsonMember(root, "entries", "array");
            foreach (var entry in entries.Elements())
            {
                RequireJsonType(entry, "object");
                RequireJsonMember(entry, "unicode", "string");
                RequireJsonMember(entry, "name", "string");
                var keywords = RequireJsonMember(entry, "keywords", "array");
                foreach (var keyword in keywords.Elements())
                    RequireJsonType(keyword, "string");
            }
            using var documentReader = xml.CreateReader();
            return (LocalizationDocument)new DataContractJsonSerializer(typeof(LocalizationDocument)).ReadObject(documentReader);
        }
        catch (Exception exception) when (exception is SerializationException || exception is XmlException ||
                                          exception is FormatException || exception is OverflowException ||
                                          exception is DecoderFallbackException)
        {
            throw new InvalidDataException("本地化 JSON 格式无效。", exception);
        }
    }

    /// <summary>
    /// 验证 JSON 根对象结束后不存在其他内容。
    /// </summary>
    /// <param name="json">严格按 UTF-8 解码且不含 BOM 的文档。</param>
    /// <remarks>框架 JSON 读取器可能忽略根对象之后的内容；这里只校验文档边界，内部语法仍交给读取器。</remarks>
    private static void ValidateJsonDocumentEnd(string json)
    {
        var start = 0;
        while (start < json.Length && IsJsonWhitespace(json[start]))
            start++;
        if (start == json.Length || json[start] != '{')
            throw new InvalidDataException("本地化 JSON 必须是一个对象。");

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = start; index < json.Length; index++)
        {
            var value = json[index];
            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (value == '\\')
                    escaped = true;
                else if (value == '"')
                    inString = false;
                continue;
            }
            if (value == '"')
                inString = true;
            else if (value == '{' || value == '[')
                depth++;
            else if (value == '}' || value == ']')
            {
                depth--;
                if (depth != 0)
                    continue;
                for (var trailing = index + 1; trailing < json.Length; trailing++)
                    if (!IsJsonWhitespace(json[trailing]))
                        throw new InvalidDataException("本地化 JSON 根对象后包含多余内容。");
                return;
            }
        }
        throw new InvalidDataException("本地化 JSON 对象未完整结束。");
    }

    /// <summary>
    /// 判断字符是否为 JSON 允许的空白。
    /// </summary>
    /// <param name="value">待判断字符。</param>
    /// <returns>为空格、制表符或换行符时为 true，否则为 false。</returns>
    private static bool IsJsonWhitespace(char value) => value == ' ' || value == '\t' || value == '\r' || value == '\n';

    /// <summary>
    /// 校验必需的 JSON 成员及其类型。
    /// </summary>
    /// <param name="parent">包含成员的对象。</param>
    /// <param name="name">成员名称。</param>
    /// <param name="type">预期 JSON 类型。</param>
    /// <returns>唯一且类型正确的成员节点。</returns>
    private static XElement RequireJsonMember(XElement parent, string name, string type)
    {
        var members = parent.Elements(name).ToArray();
        if (members.Length != 1)
            throw new InvalidDataException("本地化 JSON 成员缺失或重复：" + name);
        RequireJsonType(members[0], type);
        return members[0];
    }

    /// <summary>
    /// 校验 JSON 节点类型。
    /// </summary>
    /// <param name="element">待校验节点。</param>
    /// <param name="type">预期 JSON 类型。</param>
    private static void RequireJsonType(XElement element, string type)
    {
        if (element == null || (string)element.Attribute("type") != type)
            throw new InvalidDataException("本地化 JSON 节点类型无效，预期：" + type);
    }

    /// <summary>
    /// 离线本地化文件的数据契约。
    /// </summary>
    [DataContract]
    private sealed class LocalizationDocument
    {
        /// <summary>
        /// 获取或设置格式版本。
        /// </summary>
        [DataMember(Name = "formatVersion", IsRequired = true)]
        public int FormatVersion { get; set; }

        /// <summary>
        /// 获取或设置语言标识。
        /// </summary>
        [DataMember(Name = "locale", IsRequired = true)]
        public string Locale { get; set; }

        /// <summary>
        /// 获取或设置本地化记录。
        /// </summary>
        [DataMember(Name = "entries", IsRequired = true)]
        public LocalizationEntry[] Entries { get; set; }
    }

    /// <summary>
    /// 单个表情的离线本地化数据契约。
    /// </summary>
    [DataContract]
    private sealed class LocalizationEntry
    {
        /// <summary>
        /// 获取或设置完整表情序列。
        /// </summary>
        [DataMember(Name = "unicode", IsRequired = true)]
        public string Unicode { get; set; }

        /// <summary>
        /// 获取或设置本地化名称。
        /// </summary>
        [DataMember(Name = "name", IsRequired = true)]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置本地化关键词。
        /// </summary>
        [DataMember(Name = "keywords", IsRequired = true)]
        public string[] Keywords { get; set; }
    }
}

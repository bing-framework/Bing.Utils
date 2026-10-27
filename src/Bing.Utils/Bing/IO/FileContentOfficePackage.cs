using System.Xml;
using System.Xml.Linq;

namespace Bing.IO;

/// <summary>用 OPC 清单与包级主部件关系识别六种 Office 格式，不解析正文。</summary>
internal static class FileContentOfficePackage
{
    private const string ContentNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string RelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    private const string StrictOfficeRelationship = "http://purl.oclc.org/ooxml/officeDocument/relationships/officeDocument";

    /// <summary>OPC 名称只折叠 ASCII 大小写，不把不同的 Unicode 名称合并。</summary>
    internal static readonly IEqualityComparer<string> PartNameComparer = new AsciiNameComparer();

    /// <summary>按主部件内容类型索引；只声明容器类型，不推断是否实际含 VBA。</summary>
    private static readonly IReadOnlyDictionary<string, FileContentFormat> MainTypes = new Dictionary<string, FileContentFormat>(PartNameComparer)
    {
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"] =
            FileContentFormats.Docx,
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"] =
            FileContentFormats.Xlsx,
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"] =
            FileContentFormats.Pptx,
        ["application/vnd.ms-word.document.macroEnabled.main+xml"] =
            FileContentFormats.Docm,
        ["application/vnd.ms-excel.sheet.macroEnabled.main+xml"] =
            FileContentFormats.Xlsm,
        ["application/vnd.ms-powerpoint.presentation.macroEnabled.main+xml"] =
            FileContentFormats.Pptm
    };

    internal static bool IsOfficeExtension(string extension) => MainTypes.Values.Any(f => f.Extensions.Any(
        x => string.Equals(x, extension, StringComparison.OrdinalIgnoreCase)));

    /// <summary>验证清单名称空间与主关系，返回支持的子类型；无主关系或未知主类型仍为普通 ZIP。</summary>
    internal static FileContentFormat Identify(byte[] typesData, byte[] relationshipsData, IEnumerable<string> entryNames, int maxCharacters, out string mainPart)
    {
        mainPart = null;
        var types = ReadXml(typesData, maxCharacters);
        var relationships = ReadXml(relationshipsData, maxCharacters);
        XNamespace ct = ContentNamespace;
        XNamespace rel = RelationshipNamespace;
        if (types.Name != ct + "Types" || relationships.Name != rel + "Relationships") throw FileContentZipInspector.Invalid();

        var overrides = new Dictionary<string, string>(PartNameComparer);
        var defaults = new Dictionary<string, string>(PartNameComparer);
        foreach (var element in types.Elements())
        {
            if (element.Name == ct + "Override")
            {
                var part = (string)element.Attribute("PartName");
                var contentType = (string)element.Attribute("ContentType");
                var name = NormalizePartName(part);
                if (part == null || !part.StartsWith("/", StringComparison.Ordinal) || name == null ||
                    string.IsNullOrEmpty(contentType) || overrides.ContainsKey(name)) throw FileContentZipInspector.Invalid();
                overrides.Add(name, contentType);
            }
            else if (element.Name == ct + "Default")
            {
                var extension = (string)element.Attribute("Extension");
                var contentType = (string)element.Attribute("ContentType");
                if (!IsExtension(extension) || string.IsNullOrEmpty(contentType) || defaults.ContainsKey(extension)) throw FileContentZipInspector.Invalid();
                defaults.Add(extension, contentType);
            }
            else throw FileContentZipInspector.Invalid();
        }

        var main = new List<XElement>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var relationship in relationships.Elements())
        {
            var id = (string)relationship.Attribute("Id");
            var type = (string)relationship.Attribute("Type");
            if (relationship.Name != rel + "Relationship" || string.IsNullOrEmpty(id) || !ids.Add(id) ||
                string.IsNullOrEmpty(type) || string.IsNullOrEmpty((string)relationship.Attribute("Target"))) throw FileContentZipInspector.Invalid();
            if (type == OfficeRelationship || type == StrictOfficeRelationship) main.Add(relationship);
        }
        if (main.Count == 0) return null;
        if (main.Count != 1) throw FileContentZipInspector.Invalid();
        var mode = (string)main[0].Attribute("TargetMode");
        if (mode != null && mode != "Internal") throw FileContentZipInspector.Invalid();
        var target = ResolveTarget((string)main[0].Attribute("Target"));
        if (target == null || IsReservedPart(target)) throw FileContentZipInspector.Invalid();
        // OPC 部件名按 ASCII 大小写等价处理，不由 ZIP 枚举顺序决定选哪个重复部件。
        if (entryNames.Count(x => PartNameComparer.Equals(NormalizeEntryName(x), target)) != 1)
            throw FileContentZipInspector.Invalid();
        if (!overrides.TryGetValue(target, out var mainType))
        {
            var dot = target.LastIndexOf('.');
            if (dot <= target.LastIndexOf('/') || !defaults.TryGetValue(target.Substring(dot + 1), out mainType)) throw FileContentZipInspector.Invalid();
        }
        mainPart = target;
        return MainTypes.TryGetValue(mainType, out var format) ? format : null;
    }

    /// <summary>禁用 DTD 与外部实体，仅从已受解压预算约束的内存读取 XML。</summary>
    private static XElement ReadXml(byte[] bytes, int maxCharacters)
    {
        using var input = new MemoryStream(bytes, false);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = maxCharacters,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        });
        var document = XDocument.Load(reader, LoadOptions.None);
        return document.Root ?? throw FileContentZipInspector.Invalid();
    }

    /// <summary>Override 的 PartName 必须是绝对部件路径，不能用点段改写名称。</summary>
    private static string NormalizePartName(string value)
        => value != null && value.StartsWith("/", StringComparison.Ordinal)
            ? NormalizePath(value.Substring(1), false) : null;

    /// <summary>ZIP 中的部件名不含根斜杠；不为异常名称做路径修正。</summary>
    internal static string NormalizeEntryName(string value) => NormalizePath(value, false);

    /// <summary>根关系允许相对点段，拒绝 URI scheme、authority、查询、片段及越过包根。</summary>
    private static string ResolveTarget(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "." || value == ".." ||
            value.EndsWith("/.", StringComparison.Ordinal) || value.EndsWith("/..", StringComparison.Ordinal)) return null;
        if (value.StartsWith("/", StringComparison.Ordinal)) return NormalizePath(value.Substring(1), true);
        var firstSlash = value.IndexOf('/');
        var colon = value.IndexOf(':');
        if (colon >= 0 && (firstSlash < 0 || colon < firstSlash)) return null;
        return NormalizePath(value, true);
    }

    /// <summary>包清单与关系部件不能冒充 Office 正文主部件。</summary>
    private static bool IsReservedPart(string name)
    {
        if (PartNameComparer.Equals(name, "[Content_Types].xml")) return true;
        var segments = name.Split('/');
        return segments.Length >= 2 && PartNameComparer.Equals(segments[segments.Length - 2], "_rels")
            && PartNameComparer.Equals(segments[segments.Length - 1].Substring(Math.Max(0, segments[segments.Length - 1].Length - 5)), ".rels");
    }

    /// <summary>Default 只允许单个扩展名；其 URI 字符和转义规则与部件名相同。</summary>
    private static bool IsExtension(string value)
        => !string.IsNullOrEmpty(value) && value.IndexOfAny(new[] { '.', '/' }) < 0
            && NormalizePath("part." + value, false) != null;

    private static bool IsUnreserved(char value) => value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z'
        || value >= '0' && value <= '9' || value == '-' || value == '.' || value == '_' || value == '~';

    /// <summary>验证部件路径，统一转义大小写；保留已转义保留字符与原字符的区别。</summary>
    private static string NormalizePath(string value, bool resolveDots)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var parts = new List<string>();
        foreach (var segment in value.Split('/'))
        {
            if (segment.Length == 0) return null;
            if (segment == "." || segment == "..")
            {
                if (!resolveDots) return null;
                if (segment == "..")
                {
                    if (parts.Count == 0) return null;
                    parts.RemoveAt(parts.Count - 1);
                }
                continue;
            }
            // OPC 部件段不能以点结束；也不接受控制字符、空白和非 URI 字符。
            if (segment.EndsWith(".", StringComparison.Ordinal)) return null;
            var normalized = new System.Text.StringBuilder(segment.Length);
            for (var i = 0; i < segment.Length; i++)
            {
                var character = segment[i];
                if (character == '%')
                {
                    if (i + 2 >= segment.Length || !Uri.IsHexDigit(segment[i + 1]) || !Uri.IsHexDigit(segment[i + 2])) return null;
                    var escaped = (char)Convert.ToByte(segment.Substring(i + 1, 2), 16);
                    // %20 是合法的编码空格；仅原始空白与编码控制字符被拒绝。
                    if (IsUnreserved(escaped) || escaped == '/' || escaped == '\\' || escaped < 32 || escaped == 127) return null;
                    normalized.Append('%').Append(char.ToUpperInvariant(segment[++i])).Append(char.ToUpperInvariant(segment[++i]));
                }
                else
                {
                    if (char.IsControl(character) || char.IsWhiteSpace(character) ||
                        character < 128 && !IsUnreserved(character) && "!$&'()*+,;=:@".IndexOf(character) < 0) return null;
                    normalized.Append(character);
                }
            }
            parts.Add(normalized.ToString());
        }
        return parts.Count == 0 ? null : string.Join("/", parts);
    }
    /// <summary>为字典、唯一性检查和 ZIP 条目定位共用同一种 ASCII 等价规则。</summary>
    private sealed class AsciiNameComparer : IEqualityComparer<string>
    {
        private static char Fold(char value) => value >= 'a' && value <= 'z' ? (char)(value - ('a' - 'A')) : value;

        public bool Equals(string left, string right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++)
                if (Fold(left[i]) != Fold(right[i])) return false;
            return true;
        }

        public int GetHashCode(string value)
        {
            if (value == null) return 0;
            var folded = value.ToCharArray();
            for (var i = 0; i < folded.Length; i++) folded[i] = Fold(folded[i]);
            // 使用运行时的字符串哈希，避免为不可信部件名引入固定的简单散列。
            return StringComparer.Ordinal.GetHashCode(new string(folded));
        }
    }

}

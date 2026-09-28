"""从固定的 UTF-8 数据快照生成 Emoji C# 数据及标签索引；普通构建不调用此脚本。"""

import argparse
import base64
import gzip
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "asset/emoji"
OUTPUT = ROOT / "src/Bing.Utils.Extra/Bing/Extra/Emoji/EmojiCatalog.Generated.cs"
TAGS_OUTPUT = ROOT / "src/Bing.Utils.Extra/Bing/Extra/Emoji/EmojiTags.Generated.cs"
LOCALIZATIONS_OUTPUT = ROOT / "src/Bing.Utils.Extra/Bing/Extra/Emoji/EmojiLocalizations.Generated.cs"
STATUSES = {"fully-qualified", "minimally-qualified", "unqualified"}
CLDR_VERSION = "48.2"
LOCALIZATION_SOURCES = (
    ("zh", "zh", ("cldr-zh.xml", "cldr-zh-derived.xml"), None),
    ("zh-Hant", "zh", ("cldr-zh-Hant.xml", "cldr-zh-Hant-derived.xml"), "Hant"),
)


def load_localizations(variants):
    """读取固定 CLDR 本地化注释，并关联到 Unicode 清单中的规范序列。"""
    values = {}
    for locale, language_code, filenames, script in LOCALIZATION_SOURCES:
        locale_values = {}
        for filename in filenames:
            path = DATA / filename
            if not path.exists():
                raise ValueError(f"Missing CLDR snapshot: {filename}")
            root = ET.parse(path).getroot()
            language = root.find("./identity/language")
            actual_script = root.find("./identity/script")
            if language is None or language.attrib.get("type") != language_code:
                raise ValueError(f"Expected {language_code} CLDR snapshot: {filename}")
            if script is None:
                if actual_script is not None:
                    raise ValueError(f"Unexpected CLDR script in {filename}")
            elif actual_script is None or actual_script.attrib.get("type") != script:
                raise ValueError(f"Expected zh-{script} CLDR snapshot: {filename}")
            for annotation in root.findall("./annotations/annotation"):
                value = annotation.attrib.get("cp", "")
                key = value.replace("\ufe0f", "")
                if key not in variants:
                    continue
                text = " ".join((annotation.text or "").split())
                if not text:
                    continue
                entry = locale_values.setdefault(key, dict(name="", fallback="", keywords=[]))
                if annotation.attrib.get("type") == "tts":
                    if entry["name"] and entry["name"] != text:
                        raise ValueError(f"Conflicting {locale} CLDR name for {key!r}")
                    entry["name"] = text
                    continue
                parts = [part.strip() for part in text.split("|") if part.strip()]
                if parts and not entry["fallback"]:
                    entry["fallback"] = parts[0]
                for keyword in parts:
                    if keyword not in entry["keywords"]:
                        entry["keywords"].append(keyword)
        for key, entry in locale_values.items():
            if entry["name"] or entry["fallback"]:
                values.setdefault(key, []).append(
                    dict(locale=locale, name=entry["name"] or entry["fallback"], keywords=entry["keywords"])
                )
    return values


def generate():
    """校验序列与别名的唯一性，并按官方顺序生成规范记录。"""
    source = (DATA / "emoji-test.txt").read_text(encoding="utf-8")
    if "# Version: 18.0\n" not in source:
        raise ValueError("Expected Unicode Emoji 18.0")
    records, variants, seen = {}, {}, set()
    group = subgroup = ""
    for line in source.splitlines():
        if line.startswith("# group: "):
            group = line[9:]
        elif line.startswith("# subgroup: "):
            subgroup = line[12:]
        elif line and not line.startswith("#"):
            match = re.fullmatch(r"([0-9A-F ]+)\s*;\s*(\S+)\s*#\s*\S+\s+E([0-9.]+)\s+(.+)", line)
            if not match:
                raise ValueError(f"Unrecognized data line: {line}")
            codes, status, version, name = match.groups()
            if status not in STATUSES:
                if status != "component":
                    raise ValueError(f"Unknown status: {status}")
                continue
            value = "".join(chr(int(code, 16)) for code in codes.split())
            if value in seen:
                raise ValueError(f"Duplicate sequence: {codes}")
            seen.add(value)
            key = value.replace("\ufe0f", "")
            variants.setdefault(key, []).append(value)
            if status == "fully-qualified":
                if key in records:
                    raise ValueError(f"Duplicate canonical key: {codes}")
                records[key] = dict(unicode=value, name=name, group=group,
                                    subgroup=subgroup, version=version, aliases=[], tags=[], localizations=[])
    if set(records) != set(variants):
        raise ValueError("Every accepted variant must have a canonical form")
    localizations = load_localizations(variants)
    for key, record in records.items():
        record["localizations"] = localizations.get(key, [])
    aliases, skipped = {}, 0
    for entry in json.loads((DATA / "gemoji.json").read_text(encoding="utf-8")):
        value = entry.get("emoji", "")
        if value not in seen:
            skipped += 1
            continue
        key = value.replace("\ufe0f", "")
        for alias in entry["aliases"]:
            if not re.fullmatch(r"[A-Za-z0-9_+\-]+", alias):
                raise ValueError(f"Unsupported alias: {alias}")
            if alias in aliases and aliases[alias] != key:
                raise ValueError(f"Conflicting alias: {alias}")
            aliases[alias] = key
            if alias not in records[key]["aliases"]:
                records[key]["aliases"].append(alias)
        for tag in entry.get("tags", []):
            if not isinstance(tag, str) or not tag:
                raise ValueError(f"Invalid tag: {tag!r}")
            if tag not in records[key]["tags"]:
                records[key]["tags"].append(tag)
    quote = lambda value: json.dumps(value, ensure_ascii=True)
    array = lambda values: "new string[] { " + ", ".join(map(quote, values)) + " }"
    lines = ["// <auto-generated />", "// Unicode Emoji 18.0; gemoji v4.1.0. See asset/emoji/README.md.",
             "namespace Bing.Extra.Emoji;", "", "public sealed partial class EmojiCatalog", "{"]
    items = list(records.items())
    batches = (len(items) + 99) // 100
    lines += ["    private void LoadGeneratedData()", "    {"]
    lines += [f"        LoadBatch{i}();" for i in range(batches)]
    lines += ["    }"]
    for i in range(batches):
        lines += ["", f"    private void LoadBatch{i}()", "    {"]
        for key, record in items[i * 100:(i + 1) * 100]:
            fields = [quote(record[field]) for field in ("unicode", "name", "group", "subgroup", "version")]
            fields += [array(record["aliases"]), array(variants[key])]
            lines.append("        Add(" + ", ".join(fields) + ");")
        lines += ["    }"]
    lines += ["}", ""]
    tag_lines = ["// <auto-generated />", "// gemoji v4.1.0 tags. See asset/emoji/README.md.",
                 "using System;", "using System.Collections.Generic;", "", "namespace Bing.Extra.Emoji;", "",
                 "internal static class EmojiTags", "{",
                 "    private static readonly string[] Empty = new string[0];",
                 "    private static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>(StringComparer.Ordinal)",
                 "    {"]
    for _, record in items:
        if record["tags"]:
            tag_lines.append(f"        {{ {quote(record['unicode'])}, {array(record['tags'])} }},")
    tag_lines += ["    };", "", "    internal static string[] Get(string unicode)", "    {",
                  "        return Values.TryGetValue(unicode, out var tags) ? tags : Empty;", "    }", "}", ""]
    localization_items = [(record["unicode"], record["localizations"]) for _, record in items if record["localizations"]]
    localization_strings = []
    localization_string_indices = {}

    def localization_index(value):
        index = localization_string_indices.get(value)
        if index is None:
            index = len(localization_strings)
            localization_string_indices[value] = index
            localization_strings.append(value)
        return index

    localization_records = []
    for unicode, values in localization_items:
        entries = []
        for item in values:
            entries.append((
                localization_index(item["locale"]),
                localization_index(item["name"]),
                [localization_index(keyword) for keyword in item["keywords"]]))
        localization_records.append((localization_index(unicode), entries))

    localization_payload = bytearray()
    localization_payload.extend(struct.pack("<II", len(localization_strings), len(localization_records)))
    for value in localization_strings:
        encoded = value.encode("utf-8")
        localization_payload.extend(struct.pack("<I", len(encoded)))
        localization_payload.extend(encoded)
    for unicode_index, entries in localization_records:
        localization_payload.extend(struct.pack("<II", unicode_index, len(entries)))
        for locale_index, name_index, keyword_indices in entries:
            localization_payload.extend(struct.pack("<III", locale_index, name_index, len(keyword_indices)))
            for keyword_index in keyword_indices:
                localization_payload.extend(struct.pack("<I", keyword_index))
    compressed = gzip.compress(bytes(localization_payload), compresslevel=9, mtime=0)
    encoded_payload = base64.b64encode(compressed).decode("ascii")
    payload_chunks = [encoded_payload[i:i + 40000] for i in range(0, len(encoded_payload), 40000)]
    localization_lines = ["// <auto-generated />", f"// CLDR {CLDR_VERSION} {", ".join(source[0] for source in LOCALIZATION_SOURCES)} annotations. See asset/emoji/README.md.",
                          "using System;", "using System.Collections.Generic;", "using System.IO;", "using System.IO.Compression;", "using System.Text;", "", "namespace Bing.Extra.Emoji;", "",
                          "internal static class EmojiLocalizations", "{",
                          "    private static readonly string[] Data = new string[]", "    {"]
    localization_lines += [f"        {quote(chunk)}," for chunk in payload_chunks]
    localization_lines += ["    };", "",
                          "    private static readonly EmojiLocalization[] Empty = new EmojiLocalization[0];",
                          "    private static readonly Dictionary<string, EmojiLocalization[]> Values = CreateValues();", "",
                          "    private static Dictionary<string, EmojiLocalization[]> CreateValues()", "    {",
                          "        var values = new Dictionary<string, EmojiLocalization[]>(StringComparer.Ordinal);",
                          "        var compressed = Convert.FromBase64String(string.Concat(Data));",
                          "        using (var input = new MemoryStream(compressed))",
                          "        using (var gzip = new GZipStream(input, CompressionMode.Decompress))",
                          "        using (var payload = new MemoryStream())",
                          "        {",
                          "            gzip.CopyTo(payload);",
                          "            var data = payload.ToArray();",
                          "            var offset = 0;",
                          "            var stringCount = ReadInt32(data, ref offset);",
                          "            var recordCount = ReadInt32(data, ref offset);",
                          "            var texts = new string[stringCount];",
                          "            for (var i = 0; i < stringCount; i++)",
                          "                texts[i] = ReadString(data, ref offset);",
                          "            for (var i = 0; i < recordCount; i++)",
                          "            {",
                          "                var unicode = texts[ReadInt32(data, ref offset)];",
                          "                var localizationCount = ReadInt32(data, ref offset);",
                          "                var localizations = new EmojiLocalization[localizationCount];",
                          "                for (var j = 0; j < localizationCount; j++)",
                          "                {",
                          "                    var locale = texts[ReadInt32(data, ref offset)];",
                          "                    var name = texts[ReadInt32(data, ref offset)];",
                          "                    var keywordCount = ReadInt32(data, ref offset);",
                          "                    var keywords = new string[keywordCount];",
                          "                    for (var k = 0; k < keywordCount; k++)",
                          "                        keywords[k] = texts[ReadInt32(data, ref offset)];",
                          "                    localizations[j] = new EmojiLocalization(locale, name, keywords);",
                          "                }",
                          "                values.Add(unicode, localizations);",
                          "            }",
                          "        }",
                          "        return values;",
                          "    }", "",
                          "    private static int ReadInt32(byte[] data, ref int offset)", "    {",
                          "        if (offset < 0 || offset > data.Length - 4)",
                          "            throw new InvalidDataException(\"Emoji 本地化数据长度无效。\");",
                          "        var value = data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;",
                          "        offset += 4;",
                          "        return value;",
                          "    }", "",
                          "    private static string ReadString(byte[] data, ref int offset)", "    {",
                          "        var length = ReadInt32(data, ref offset);",
                          "        if (length < 0 || length > data.Length - offset)",
                          "            throw new InvalidDataException(\"Emoji 本地化文本长度无效。\");",
                          "        var value = Encoding.UTF8.GetString(data, offset, length);",
                          "        offset += length;",
                          "        return value;",
                          "    }", "",
                          "    internal static EmojiLocalization[] Get(string unicode)", "    {",
                          "        return Values.TryGetValue(unicode, out var values) ? values : Empty;", "    }", "}", ""]
    localized = sum(bool(record["localizations"]) for _, record in items)
    localization_records = sum(len(record["localizations"]) for _, record in items)
    print(f"Canonical: {len(items)}; sequences: {len(seen)}; aliases: {len(aliases)}; tagged: {sum(bool(record['tags']) for _, record in items)}; localized: {localized}; localization records: {localization_records}; skipped gemoji records: {skipped}")
    return "\n".join(lines), "\n".join(tag_lines), "\n".join(localization_lines)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="只校验已提交生成文件，不写入")
    args = parser.parse_args()
    result, tags_result, localizations_result = generate()
    if args.check:
        if (OUTPUT.read_text(encoding="utf-8") != result or
                TAGS_OUTPUT.read_text(encoding="utf-8") != tags_result or
                LOCALIZATIONS_OUTPUT.read_text(encoding="utf-8") != localizations_result):
            raise SystemExit("Generated Emoji data is out of date")
        print("Generated data is current")
    else:
        OUTPUT.parent.mkdir(parents=True, exist_ok=True)
        OUTPUT.write_text(result, encoding="utf-8", newline="\n")
        TAGS_OUTPUT.write_text(tags_result, encoding="utf-8", newline="\n")
        LOCALIZATIONS_OUTPUT.write_text(localizations_result, encoding="utf-8", newline="\n")

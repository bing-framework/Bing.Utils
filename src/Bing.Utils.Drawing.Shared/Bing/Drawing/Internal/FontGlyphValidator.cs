namespace Bing.Drawing.Internal;

/// <summary>
/// 验证字体是否包含指定文字的 Unicode 字形。
/// </summary>
/// <remarks>
/// 通过检查字体的 Unicode cmap，避免不同后端静默使用系统回退字体。
/// </remarks>
internal static class FontGlyphValidator
{
    /// <summary>
    /// 验证字体文件包含指定文字的全部字形。
    /// </summary>
    /// <param name="path">字体文件路径。</param>
    /// <param name="text">待验证的文字。</param>
    /// <exception cref="InvalidDataException">字体结构无效或缺少 Unicode 字形映射。</exception>
    /// <exception cref="ArgumentException">字体不包含指定文字的字形。</exception>
    internal static void Validate(string path, string text)
    {
        var data = File.ReadAllBytes(path);
        uint U32(int p) { Check(p, 4); return ((uint)data[p] << 24) | ((uint)data[p + 1] << 16) | ((uint)data[p + 2] << 8) | data[p + 3]; }
        int U16(int p) { Check(p, 2); return (data[p] << 8) | data[p + 1]; }
        void Check(int p, int size) { if (p < 0 || p > data.Length - size) throw new InvalidDataException("字体表越界。"); }
        int Offset(uint value) { if (value > int.MaxValue) throw new InvalidDataException("字体偏移无效。"); return (int)value; }
        var start = U32(0) == 0x74746366 ? Offset(U32(12)) : 0; // TTC 默认使用首个字体。
        var count = U16(checked(start + 4)); var cmap = -1;
        for (var i = 0; i < count; i++)
        {
            var p = checked(start + 12 + i * 16);
            if (U32(p) == 0x636d6170) { cmap = Offset(U32(p + 8)); break; }
        }
        if (cmap < 0) throw new InvalidDataException("字体缺少 Unicode 字形映射。");
        var tables = new List<int>();
        for (var i = 0; i < U16(cmap + 2); i++)
        {
            var p = checked(cmap + 4 + i * 8); var platform = U16(p); var encoding = U16(p + 2);
            if (platform != 0 && !(platform == 3 && (encoding == 1 || encoding == 10))) continue;
            var table = checked(cmap + Offset(U32(p + 4))); var format = U16(table);
            if (format == 4 || format == 12) tables.Add(table);
        }
        if (tables.Count == 0) throw new NotSupportedException("字体需要 Unicode cmap 4 或 12。");
        for (var i = 0; i < text.Length; i++)
        {
            var code = char.ConvertToUtf32(text, i); if (char.IsHighSurrogate(text[i])) i++;
            if (code == 10 || code == 13 || code == 9) continue;
            var found = false;
            foreach (var table in tables)
            {
                if (U16(table) == 12)
                {
                    var groups = Offset(U32(table + 12)); Check(table + 16, checked(groups * 12));
                    for (var j = 0; j < groups; j++)
                    {
                        var p = table + 16 + j * 12; var first = U32(p); var last = U32(p + 4);
                        if (code >= first && code <= last && U32(p + 8) + code - first != 0) { found = true; break; }
                    }
                }
                else if (code <= 0xffff)
                {
                    var segments = U16(table + 6) / 2; var ends = table + 14; var starts = ends + segments * 2 + 2;
                    var deltas = starts + segments * 2; var offsets = deltas + segments * 2;
                    for (var j = 0; j < segments; j++)
                    {
                        if (code < U16(starts + j * 2) || code > U16(ends + j * 2)) continue;
                        var delta = U16(deltas + j * 2); var offset = U16(offsets + j * 2);
                        var glyph = offset == 0 ? (code + delta) & 0xffff : U16(checked(offsets + j * 2 + offset + 2 * (code - U16(starts + j * 2))));
                        if (offset != 0 && glyph != 0) glyph = (glyph + delta) & 0xffff;
                        found = glyph != 0; break;
                    }
                }
                if (found) break;
            }
            if (!found) throw new ArgumentException($"指定字体不包含字形 U+{code:X}。", nameof(text));
        }
    }
}

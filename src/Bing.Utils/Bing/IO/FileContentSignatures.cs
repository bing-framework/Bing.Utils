namespace Bing.IO;

/// <summary>单个固定偏移签名片段；掩码中的零位不参与比较。</summary>
internal sealed class FileSignatureSegment
{
    internal int Offset { get; }
    internal byte[] Pattern { get; }
    internal byte[] Mask { get; }
    internal int End => Offset + Pattern.Length;

    /// <summary>复制规则字节并验证偏移和掩码。</summary>
    internal FileSignatureSegment(int offset, byte[] pattern, byte[] mask = null)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        if (pattern.Length == 0) throw new ArgumentException("签名片段不能为空。", nameof(pattern));
        if (offset < 0 || offset > int.MaxValue - pattern.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (mask != null && mask.Length != pattern.Length) throw new ArgumentException("掩码必须与签名等长。", nameof(mask));
        Offset = offset;
        Pattern = (byte[])pattern.Clone();
        Mask = mask == null ? null : (byte[])mask.Clone();
    }
}

/// <summary>必须同时满足所有片段的一条格式规则。</summary>
internal sealed class FileSignatureRule
{
    internal FileContentFormat Format { get; }
    internal IReadOnlyList<FileSignatureSegment> Segments { get; }
    internal int RequiredBytes { get; }

    /// <summary>建立一条不可变规则。</summary>
    internal FileSignatureRule(FileContentFormat format, params FileSignatureSegment[] segments)
    {
        Format = format ?? throw new ArgumentNullException(nameof(format));
        if (segments == null) throw new ArgumentNullException(nameof(segments));
        if (segments.Length == 0 || segments.Any(x => x == null)) throw new ArgumentException("规则必须含有非空片段。", nameof(segments));
        Segments = Array.AsReadOnly((FileSignatureSegment[])segments.Clone());
        RequiredBytes = segments.Max(x => x.End);
    }
}

/// <summary>内置二进制签名目录及无 IO 匹配器。</summary>
internal static class FileContentSignatures
{
    // 规则来源和边界见 docs/reference/file-content-detection-guide.md。
    internal static readonly IReadOnlyList<FileSignatureRule> Rules = CreateRules();
    internal static readonly int RequiredBytes = Rules.Max(x => x.RequiredBytes);

    /// <summary>创建常见格式及其变体；变体共享同一格式实例。</summary>
    private static IReadOnlyList<FileSignatureRule> CreateRules()
    {
        var rules = new List<FileSignatureRule>();
        void Add(FileContentFormat format, params byte[][] patterns)
        {
            foreach (var pattern in patterns)
                rules.Add(new FileSignatureRule(format, new FileSignatureSegment(0, pattern)));
        }
        Add(FileContentFormats.Jpeg, new byte[] { 0xff, 0xd8, 0xff });
        Add(FileContentFormats.Png, new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });
        Add(FileContentFormats.Gif, Ascii("GIF87a"), Ascii("GIF89a"));
        Add(FileContentFormats.Bmp, Ascii("BM"));
        Add(FileContentFormats.Tiff,
            new byte[] { 0x49, 0x49, 0x2a, 0 }, new byte[] { 0x4d, 0x4d, 0, 0x2a },
            new byte[] { 0x49, 0x49, 0x2b, 0, 8, 0, 0, 0 }, new byte[] { 0x4d, 0x4d, 0, 0x2b, 0, 8, 0, 0 });
        Add(FileContentFormats.Ico, new byte[] { 0, 0, 1, 0 });
        Add(FileContentFormats.Pdf, Ascii("%PDF-"));
        Add(FileContentFormats.Zip, new byte[] { 0x50, 0x4b, 3, 4 }, new byte[] { 0x50, 0x4b, 5, 6 });
        Add(FileContentFormats.Gzip, new byte[] { 0x1f, 0x8b, 8 });
        Add(FileContentFormats.SevenZip, new byte[] { 0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c });
        Add(FileContentFormats.Rar,
            new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 7, 0 }, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 7, 1, 0 });
        Add(FileContentFormats.Flac, Ascii("fLaC"));
        void Riff(FileContentFormat format, string tag)
        {
            rules.Add(new FileSignatureRule(format,
                new FileSignatureSegment(0, Ascii("RIFF")), new FileSignatureSegment(8, Ascii(tag))));
        }
        Riff(FileContentFormats.WebP, "WEBP");
        Riff(FileContentFormats.Wav, "WAVE");
        Riff(FileContentFormats.Avi, "AVI ");
        return rules.AsReadOnly();
    }

    /// <summary>将目录中的 ASCII 常量转换为签名字节。</summary>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);

    /// <summary>只比较已有字节；保留尚未排除的部分规则，不让较短规则抢先成为最终结论。</summary>
    internal static FileDetectionResult Match(ReadOnlySpan<byte> content, IReadOnlyList<FileSignatureRule> rules,
        FileDetectionReason incompleteReason)
    {
        var matches = new List<FileContentFormat>();
        var incomplete = false;
        foreach (var rule in rules)
        {
            var mismatch = false;
            foreach (var segment in rule.Segments)
            {
                var count = Math.Min(segment.Pattern.Length, Math.Max(0, content.Length - segment.Offset));
                for (var i = 0; i < count; i++)
                {
                    var mask = segment.Mask == null ? 0xff : segment.Mask[i];
                    if ((content[segment.Offset + i] & mask) == (segment.Pattern[i] & mask)) continue;
                    mismatch = true;
                    break;
                }
                if (mismatch) break;
            }
            if (mismatch) continue;
            if (content.Length < rule.RequiredBytes) incomplete = true;
            else if (!matches.Any(x => x.Id == rule.Format.Id)) matches.Add(rule.Format);
        }
        if (content.Length == 0)
            return new FileDetectionResult(FileDetectionStatus.Empty, FileDetectionReason.None, matches, 0);
        if (incomplete)
            return new FileDetectionResult(FileDetectionStatus.Incomplete, incompleteReason, matches, content.Length);
        var status = matches.Count == 0 ? FileDetectionStatus.Unknown : matches.Count == 1
            ? FileDetectionStatus.Matched : FileDetectionStatus.Ambiguous;
        return new FileDetectionResult(status, matches.Count == 0 ? FileDetectionReason.NoSignatureMatch : FileDetectionReason.None,
            matches, content.Length);
    }
}

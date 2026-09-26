using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

using Bing.Drawing;

namespace Bing.Drawing.Internal;

/// <summary>
/// 识别静态 JPEG/PNG 编码数据并迁移元数据。
/// </summary>
/// <remarks>
/// 共享解析器只处理静态 JPEG 和 PNG，WebP 仅用于识别后明确拒绝。
/// </remarks>
internal static class EncodedImageInspector
{
    /// <summary>
    /// ICC 配置文件解压后的最大允许大小。
    /// </summary>
    private const int MaxIccProfileBytes = 16 * 1024 * 1024;

    /// <summary>
    /// PNG 文件签名。
    /// </summary>
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// WebP 文件类型签名。
    /// </summary>
    private static readonly byte[] WebpSignature = Encoding.ASCII.GetBytes("WEBP");

    /// <summary>
    /// 识别编码图像的格式、尺寸、帧数和方向。
    /// </summary>
    /// <param name="data">编码图像数据。</param>
    /// <returns>识别到的图像信息。</returns>
    internal static ImageInfo Identify(byte[] data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        if (IsPng(data))
        {
            var document = PngMetadataSanitizer.Parse(data);
            // 在图像引擎读取元数据前兑现 ICC 解压限制，不能等编码完成后再检查。
            var icc = PngMetadataSanitizer.GetIccChunk(data, document);
            if (icc != null) DecodePngIccChunk(icc);
            if (document.HasAnimation)
                throw new NotSupportedException("统一图像流程仅支持静态 PNG，不支持 APNG 动画。");
            return new ImageInfo
            {
                Width = document.Width,
                Height = document.Height,
                FrameCount = document.FrameCount,
                Orientation = document.Orientation,
                Format = ImageOutputFormat.Png
            };
        }

        if (IsJpeg(data))
        {
            var document = JpegMetadataSanitizer.Parse(data);
            if (document.Width <= 0 || document.Height <= 0)
                throw new InvalidDataException("JPEG 缺少有效的 SOF 尺寸。");
            var orientation = 1;
            var mpfFrameCount = 1;
            var hasIcc = false;
            foreach (var segment in document.Segments)
            {
                if (JpegMetadataSanitizer.IsExifSegment(data, segment))
                    orientation = JpegMetadataSanitizer.GetExifOrientation(data, segment);
                if (JpegMetadataSanitizer.IsMpfSegment(data, segment))
                    mpfFrameCount = Math.Max(mpfFrameCount, JpegMetadataSanitizer.GetMpfFrameCount(data, segment));
                if (JpegMetadataSanitizer.IsIccSegment(data, segment))
                    hasIcc = true;
            }
            if (hasIcc)
                JpegMetadataSanitizer.GetIccProfile(data, document);

            return new ImageInfo
            {
                Width = document.Width,
                Height = document.Height,
                FrameCount = Math.Max(document.FrameCount, mpfFrameCount),
                Orientation = orientation,
                Format = ImageOutputFormat.Jpeg
            };
        }

        if (LooksLikeWebp(data))
            throw new NotSupportedException("WebP 编码信息识别尚未由共享静态解析器实现。");
        throw new NotSupportedException("仅支持静态 JPEG 和 PNG 编码数据。");
    }

    /// <summary>
    /// 将源图像元数据迁移到已编码结果。
    /// </summary>
    /// <remarks>
    /// 同时清理编码器遗留的方向字段；ICC 配置文件可跨 JPEG 和 PNG 迁移，描述性元数据只在同格式之间迁移。
    /// </remarks>
    /// <param name="source">源图像编码数据。</param>
    /// <param name="encoded">已编码的目标图像数据。</param>
    /// <param name="removeMetadata">是否移除描述性元数据。</param>
    /// <param name="preserveIcc">是否保留 ICC 配置文件。</param>
    /// <param name="normalizeOrientation">是否将方向字段规范化为默认方向。</param>
    /// <returns>迁移元数据后的目标图像数据。</returns>
    internal static byte[] CopyMetadata(byte[] source, byte[] encoded, bool removeMetadata, bool preserveIcc, bool normalizeOrientation)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (encoded is null)
            throw new ArgumentNullException(nameof(encoded));

        var sourceInfo = Identify(source);
        var encodedInfo = Identify(encoded);
        if (sourceInfo.FrameCount > 1 || encodedInfo.FrameCount > 1)
            throw new NotSupportedException("多帧 JPEG/PNG 的元数据迁移需要逐帧处理，当前不支持。");

        var sourceIcc = preserveIcc ? ExtractIccProfile(source, sourceInfo.Format) : Array.Empty<byte>();
        if (encodedInfo.Format == ImageOutputFormat.Jpeg)
            return CopyToJpeg(source, encoded, sourceInfo.Format, sourceIcc, removeMetadata, preserveIcc, normalizeOrientation);
        if (encodedInfo.Format == ImageOutputFormat.Png)
            return CopyToPng(source, encoded, sourceInfo.Format, sourceIcc, removeMetadata, preserveIcc, normalizeOrientation);

        throw new NotSupportedException("仅支持 JPEG 和 PNG 的元数据迁移。");
    }

    /// <summary>
    /// 将源元数据写入 JPEG 目标数据。
    /// </summary>
    /// <param name="source">源图像编码数据。</param>
    /// <param name="encoded">目标 JPEG 编码数据。</param>
    /// <param name="sourceFormat">源图像格式。</param>
    /// <param name="sourceIcc">源 ICC 配置文件。</param>
    /// <param name="removeMetadata">是否移除描述性元数据。</param>
    /// <param name="preserveIcc">是否保留 ICC 配置文件。</param>
    /// <param name="normalizeOrientation">是否规范化方向字段。</param>
    /// <returns>迁移元数据后的 JPEG 数据。</returns>
    private static byte[] CopyToJpeg(byte[] source, byte[] encoded, ImageOutputFormat sourceFormat, byte[] sourceIcc,
        bool removeMetadata, bool preserveIcc, bool normalizeOrientation)
    {
        var target = JpegMetadataSanitizer.Parse(encoded);
        var sourceIsJpeg = sourceFormat == ImageOutputFormat.Jpeg;
        var sourceDocument = sourceIsJpeg ? JpegMetadataSanitizer.Parse(source) : null;
        var sourceExif = new List<byte[]>();
        var sourceDescription = new List<byte[]>();
        var sourceAdditionalMetadata = new List<byte[]>();
        var sourceAdditionalMarkers = new HashSet<int>();

        if (sourceDocument is not null)
        {
            foreach (var segment in sourceDocument.Segments)
            {
                if (JpegMetadataSanitizer.IsExifSegment(source, segment))
                {
                    if (!removeMetadata)
                        sourceExif.Add(normalizeOrientation
                            ? JpegMetadataSanitizer.NormalizeExifOrientation(source, segment)
                            : segment.GetRaw(source));
                }
                else if (!removeMetadata && (JpegMetadataSanitizer.IsXmpSegment(source, segment) || segment.Marker == 0xFE))
                {
                    sourceDescription.Add(segment.GetRaw(source));
                }
                else if (!removeMetadata && IsPreservableJpegMetadataSegment(source, segment))
                {
                    // APP/COM 之外的 JPEG 结构段不能迁移；这里保留未明确删除的应用元数据，
                    // 同时排除 ICC、MPF 这类由本次编码重新生成或包含失效偏移的结构。
                    sourceAdditionalMetadata.Add(segment.GetRaw(source));
                    sourceAdditionalMarkers.Add(segment.Marker);
                }
            }
        }

        var targetHeader = new List<byte[]>();
        var targetHasIcc = false;
        var targetHasExif = false;
        foreach (var segment in target.Segments)
        {
            var raw = segment.GetRaw(encoded);
            var isIcc = JpegMetadataSanitizer.IsIccSegment(encoded, segment);
            var isExif = JpegMetadataSanitizer.IsExifSegment(encoded, segment);
            var isDescription = JpegMetadataSanitizer.IsXmpSegment(encoded, segment) || segment.Marker == 0xFE;

            if (isIcc)
            {
                targetHasIcc = true;
                if (!preserveIcc || sourceIcc.Length > 0)
                    continue;
            }

            if (isExif)
            {
                targetHasExif = true;
                if (removeMetadata || (sourceIsJpeg && sourceExif.Count > 0))
                    continue;
                if (normalizeOrientation)
                    raw = JpegMetadataSanitizer.NormalizeExifOrientation(encoded, segment);
            }
            else if (isDescription && (removeMetadata || (sourceIsJpeg && sourceDescription.Count > 0)))
            {
                continue;
            }
            else if (!removeMetadata && sourceIsJpeg && IsPreservableJpegMetadataSegment(encoded, segment) &&
                sourceAdditionalMarkers.Contains(segment.Marker))
            {
                continue;
            }
            else if (removeMetadata && segment.Marker >= 0xE1 && segment.Marker <= 0xEF)
            {
                continue;
            }

            targetHeader.Add(raw);
        }

        if (!removeMetadata && sourceIsJpeg)
        {
            sourceAdditionalMetadata.InsertRange(0, sourceExif);
            sourceAdditionalMetadata.InsertRange(sourceExif.Count, sourceDescription);
        }

        if (preserveIcc && sourceIcc.Length > 0)
            sourceAdditionalMetadata.AddRange(JpegMetadataSanitizer.BuildIccSegments(sourceIcc));
        else if (!preserveIcc && targetHasIcc)
        {
            // 已在上面的分段筛选中移除目标 ICC；该分支仅保留语义说明。
        }

        if (removeMetadata && normalizeOrientation && targetHasExif)
        {
            // removeMetadata 已移除全部 EXIF，确保不会因编码器重复写入而保留旧方向。
        }

        InsertJpegMetadataBeforeStructuralHeader(targetHeader, sourceAdditionalMetadata);

        return JpegMetadataSanitizer.RewriteWithHeader(encoded, target, targetHeader);
    }

    /// <summary>
    /// 将源元数据写入 PNG 目标数据。
    /// </summary>
    /// <param name="source">源图像编码数据。</param>
    /// <param name="encoded">目标 PNG 编码数据。</param>
    /// <param name="sourceFormat">源图像格式。</param>
    /// <param name="sourceIcc">源 ICC 配置文件。</param>
    /// <param name="removeMetadata">是否移除描述性元数据。</param>
    /// <param name="preserveIcc">是否保留 ICC 配置文件。</param>
    /// <param name="normalizeOrientation">是否规范化方向字段。</param>
    /// <returns>迁移元数据后的 PNG 数据。</returns>
    private static byte[] CopyToPng(byte[] source, byte[] encoded, ImageOutputFormat sourceFormat, byte[] sourceIcc,
        bool removeMetadata, bool preserveIcc, bool normalizeOrientation)
    {
        var target = PngMetadataSanitizer.Parse(encoded);
        var sourceIsPng = sourceFormat == ImageOutputFormat.Png;
        var sourceDocument = sourceIsPng ? PngMetadataSanitizer.Parse(source) : null;
        var sourceExif = new List<byte[]>();
        var sourceDescription = new List<byte[]>();
        var sourceBeforePlteChunks = new List<byte[]>();
        var sourceBeforeIdatChunks = new List<byte[]>();
        var sourceAdditionalTypes = new HashSet<string>(StringComparer.Ordinal);

        if (sourceDocument is not null)
        {
            foreach (var chunk in sourceDocument.Chunks)
            {
                if (chunk.Type == "eXIf")
                {
                    if (!removeMetadata)
                    {
                        sourceExif.Add(normalizeOrientation
                            ? PngMetadataSanitizer.NormalizeExifOrientation(source, chunk)
                            : chunk.GetRaw(source));
                    }
                }
                else if (!removeMetadata && (chunk.Type == "tEXt" || chunk.Type == "zTXt" || chunk.Type == "iTXt"))
                {
                    sourceDescription.Add(chunk.GetRaw(source));
                }
                else if (!removeMetadata && IsPreservablePngMetadataChunk(chunk))
                {
                    // 复制颜色、分辨率及私有辅助 chunk；动画控制和图像数据不属于静态元数据。
                    var raw = chunk.GetRaw(source);
                    if (RequiresPngChunkBeforePlte(chunk.Type))
                        sourceBeforePlteChunks.Add(raw);
                    else
                        sourceBeforeIdatChunks.Add(raw);
                    sourceAdditionalTypes.Add(chunk.Type);
                }
            }
        }

        var targetChunks = new List<byte[]>();
        var targetHasIcc = false;
        var targetHasExif = false;
        foreach (var chunk in target.Chunks)
        {
            var raw = chunk.GetRaw(encoded);
            var isIcc = chunk.Type == "iCCP";
            var isExif = chunk.Type == "eXIf";
            var isDescription = chunk.Type == "tEXt" || chunk.Type == "zTXt" || chunk.Type == "iTXt";

            if (isIcc)
            {
                targetHasIcc = true;
                if (!preserveIcc || sourceIcc.Length > 0)
                    continue;
            }

            if (isExif)
            {
                targetHasExif = true;
                if (removeMetadata || (sourceIsPng && sourceExif.Count > 0))
                    continue;
                if (normalizeOrientation)
                    raw = PngMetadataSanitizer.NormalizeExifOrientation(encoded, chunk);
            }
            else if (isDescription && (removeMetadata || (sourceIsPng && sourceDescription.Count > 0)))
            {
                continue;
            }
            else if (!removeMetadata && sourceIsPng && IsPreservablePngMetadataChunk(chunk) &&
                sourceAdditionalTypes.Contains(chunk.Type))
            {
                continue;
            }

            targetChunks.Add(raw);
        }

        sourceBeforeIdatChunks.InsertRange(0, sourceExif);
        sourceBeforeIdatChunks.InsertRange(sourceExif.Count, sourceDescription);
        if (preserveIcc && sourceIcc.Length > 0)
            sourceBeforePlteChunks.Add(PngMetadataSanitizer.BuildChunk("iCCP", BuildPngIccPayload(sourceIcc)));
        InsertPngMetadata(targetChunks, sourceBeforePlteChunks, sourceBeforeIdatChunks);

        if (!preserveIcc && targetHasIcc)
        {
            // 已在上面的 chunk 筛选中移除目标 ICC；该分支仅保留语义说明。
        }
        if (removeMetadata && normalizeOrientation && targetHasExif)
        {
            // removeMetadata 已移除全部 eXIf，确保不会保留旧方向。
        }

        return PngMetadataSanitizer.Rewrite(encoded, targetChunks);
    }

    /// <summary>
    /// 判断 JPEG 分段是否属于可迁移的应用元数据。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">待判断分段。</param>
    /// <returns>分段可迁移时返回 true，否则返回 false。</returns>
    private static bool IsPreservableJpegMetadataSegment(byte[] data, JpegMetadataSanitizer.JpegSegment segment)
    {
        if (segment.Marker < 0xE0 || segment.Marker > 0xEF)
            return false;
        return !JpegMetadataSanitizer.IsExifSegment(data, segment) &&
            !JpegMetadataSanitizer.IsXmpSegment(data, segment) &&
            !JpegMetadataSanitizer.IsIccSegment(data, segment) &&
            !JpegMetadataSanitizer.IsMpfSegment(data, segment);
    }

    /// <summary>
    /// 判断 PNG chunk 是否属于可迁移的辅助元数据。
    /// </summary>
    /// <param name="chunk">待判断 chunk。</param>
    /// <returns>chunk 可迁移时返回 true，否则返回 false。</returns>
    private static bool IsPreservablePngMetadataChunk(PngMetadataSanitizer.PngChunk chunk)
    {
        if (string.IsNullOrEmpty(chunk.Type) || char.IsUpper(chunk.Type[0]))
            return false;
        return chunk.Type != "eXIf" && chunk.Type != "iCCP" && chunk.Type != "tEXt" &&
            chunk.Type != "zTXt" && chunk.Type != "iTXt" && chunk.Type != "acTL" &&
            chunk.Type != "fcTL" && chunk.Type != "fdAT";
    }

    /// <summary>
    /// 判断 PNG chunk 是否必须插入 PLTE 之前。
    /// </summary>
    /// <param name="type">chunk 类型。</param>
    /// <returns>需要提前插入时返回 true，否则返回 false。</returns>
    private static bool RequiresPngChunkBeforePlte(string type)
    {
        return type == "cHRM" || type == "gAMA" || type == "sBIT" || type == "sRGB" || type == "iCCP";
    }

    /// <summary>
    /// 将 PNG 元数据插入目标 chunk 列表的合法位置。
    /// </summary>
    /// <param name="targetChunks">目标 chunk 列表。</param>
    /// <param name="beforePlte">应插入 PLTE 之前的 chunk。</param>
    /// <param name="beforeIdat">应插入图像数据之前的 chunk。</param>
    private static void InsertPngMetadata(List<byte[]> targetChunks, List<byte[]> beforePlte, List<byte[]> beforeIdat)
    {
        var dataIndex = FindPngDataIndex(targetChunks);
        if (beforePlte.Count > 0)
        {
            var plteIndex = FindPngChunkIndex(targetChunks, "PLTE");
            targetChunks.InsertRange(plteIndex >= 0 ? plteIndex : dataIndex, beforePlte);
        }

        if (beforeIdat.Count > 0)
            targetChunks.InsertRange(FindPngDataIndex(targetChunks), beforeIdat);
    }

    /// <summary>
    /// 查找 PNG 图像数据开始位置。
    /// </summary>
    /// <param name="chunks">PNG chunk 列表。</param>
    /// <returns>首个 IDAT、fdAT 或 IEND 的索引。</returns>
    private static int FindPngDataIndex(List<byte[]> chunks)
    {
        for (var i = 0; i < chunks.Count; i++)
        {
            var type = GetPngChunkType(chunks[i]);
            if (type == "IDAT" || type == "fdAT" || type == "IEND")
                return i;
        }
        return chunks.Count;
    }

    /// <summary>
    /// 查找指定类型的 PNG chunk。
    /// </summary>
    /// <param name="chunks">PNG chunk 列表。</param>
    /// <param name="type">目标 chunk 类型。</param>
    /// <returns>首个匹配 chunk 的索引；不存在时返回 -1。</returns>
    private static int FindPngChunkIndex(List<byte[]> chunks, string type)
    {
        for (var i = 0; i < chunks.Count; i++)
        {
            if (GetPngChunkType(chunks[i]) == type)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 从原始 PNG chunk 读取类型。
    /// </summary>
    /// <param name="raw">原始 chunk 数据。</param>
    /// <returns>四字符 chunk 类型；数据无效时返回空字符串。</returns>
    private static string GetPngChunkType(byte[] raw)
    {
        return raw is not null && raw.Length >= 8 ? Encoding.ASCII.GetString(raw, 4, 4) : string.Empty;
    }

    /// <summary>
    /// 将 JPEG 元数据插入结构性头部分段之前。
    /// </summary>
    /// <param name="targetHeader">目标头部分段列表。</param>
    /// <param name="metadata">待插入元数据。</param>
    private static void InsertJpegMetadataBeforeStructuralHeader(List<byte[]> targetHeader, List<byte[]> metadata)
    {
        if (metadata.Count == 0)
            return;

        var insertionIndex = 0;
        while (insertionIndex < targetHeader.Count && IsJpegMetadataRaw(targetHeader[insertionIndex]))
            insertionIndex++;
        targetHeader.InsertRange(insertionIndex, metadata);
    }

    /// <summary>
    /// 判断原始 JPEG 分段是否为应用元数据或注释。
    /// </summary>
    /// <param name="raw">原始分段数据。</param>
    /// <returns>属于元数据分段时返回 true，否则返回 false。</returns>
    private static bool IsJpegMetadataRaw(byte[] raw)
    {
        if (raw is null || raw.Length < 2 || raw[0] != 0xFF)
            return false;
        return (raw[1] >= 0xE0 && raw[1] <= 0xEF) || raw[1] == 0xFE;
    }

    /// <summary>
    /// 从编码图像中提取 ICC 配置文件。
    /// </summary>
    /// <param name="data">编码图像数据。</param>
    /// <param name="format">图像格式。</param>
    /// <returns>ICC 配置文件数据；不存在时返回空数组。</returns>
    private static byte[] ExtractIccProfile(byte[] data, ImageOutputFormat format)
    {
        if (format == ImageOutputFormat.Jpeg)
        {
            var document = JpegMetadataSanitizer.Parse(data);
            return JpegMetadataSanitizer.GetIccProfile(data, document);
        }

        if (format == ImageOutputFormat.Png)
        {
            var document = PngMetadataSanitizer.Parse(data);
            var chunk = PngMetadataSanitizer.GetIccChunk(data, document);
            return chunk is null ? Array.Empty<byte>() : DecodePngIccChunk(chunk);
        }

        throw new NotSupportedException("仅支持 JPEG 和 PNG 的 ICC 元数据读取。");
    }

    /// <summary>
    /// 解压 PNG iCCP chunk 中的 ICC 配置文件。
    /// </summary>
    /// <param name="rawChunk">完整 iCCP chunk 数据。</param>
    /// <returns>解压后的 ICC 配置文件。</returns>
    private static byte[] DecodePngIccChunk(byte[] rawChunk)
    {
        if (rawChunk.Length < 12)
            throw new InvalidDataException("PNG iCCP chunk 不完整。");
        var payloadLength = ReadUInt32(rawChunk, 0);
        if (payloadLength > int.MaxValue || payloadLength + 12 != rawChunk.Length)
            throw new InvalidDataException("PNG iCCP chunk 长度无效。");

        var keywordEnd = -1;
        for (var i = 8; i < rawChunk.Length; i++)
        {
            if (rawChunk[i] == 0)
            {
                keywordEnd = i;
                break;
            }
            if (i - 8 >= 79)
                break;
        }
        if (keywordEnd <= 8 || keywordEnd + 2 >= rawChunk.Length)
            throw new InvalidDataException("PNG iCCP 关键字无效。");
        if (rawChunk[keywordEnd + 1] != 0)
            throw new InvalidDataException("PNG iCCP 压缩方法无效。");

        var compressedOffset = keywordEnd + 2;
        var compressedLength = rawChunk.Length - 4 - compressedOffset;
        if (compressedLength <= 0)
            throw new InvalidDataException("PNG iCCP 压缩数据为空。");
        return InflateZlib(rawChunk, compressedOffset, compressedLength);
    }

    /// <summary>
    /// 构造 PNG iCCP chunk 的数据部分。
    /// </summary>
    /// <param name="profile">ICC 配置文件数据。</param>
    /// <returns>压缩后的 iCCP 数据部分。</returns>
    private static byte[] BuildPngIccPayload(byte[] profile)
    {
        var compressed = DeflateZlib(profile);
        var keyword = Encoding.ASCII.GetBytes("ICC Profile");
        var payload = new byte[keyword.Length + 2 + compressed.Length];
        Array.Copy(keyword, 0, payload, 0, keyword.Length);
        payload[keyword.Length] = 0;
        payload[keyword.Length + 1] = 0;
        Array.Copy(compressed, 0, payload, keyword.Length + 2, compressed.Length);
        return payload;
    }

    /// <summary>
    /// 解压并校验 zlib 包装的 ICC 数据。
    /// </summary>
    /// <param name="data">压缩数据所在数组。</param>
    /// <param name="offset">压缩数据起始偏移量。</param>
    /// <param name="length">压缩数据长度。</param>
    /// <returns>解压后的数据。</returns>
    private static byte[] InflateZlib(byte[] data, int offset, int length)
    {
        if (offset < 0 || length < 6 || offset > data.Length - length)
            throw new InvalidDataException("PNG iCCP 压缩数据范围无效。");

        var cmf = data[offset];
        var flg = data[offset + 1];
        if ((cmf & 0x0F) != 8 || (cmf >> 4) > 7 || ((cmf << 8) | flg) % 31 != 0 ||
            (flg & 0x20) != 0)
            throw new InvalidDataException("PNG iCCP zlib 头部无效。");

        var expectedAdler = ReadUInt32(data, offset + length - 4);
        var rawOffset = offset + 2;
        var rawLength = length - 6;
        if (rawLength <= 0)
            throw new InvalidDataException("PNG iCCP 压缩数据为空。");

        // DeflateStream 在当前目标运行时按 raw-deflate 读取，不再根据数据前缀猜测包装格式。
        return InflateDeflate(data, rawOffset, rawLength, expectedAdler);
    }

    /// <summary>
    /// 解压 raw-deflate 数据并校验 Adler-32。
    /// </summary>
    /// <param name="data">压缩数据所在数组。</param>
    /// <param name="offset">raw-deflate 起始偏移量。</param>
    /// <param name="length">raw-deflate 长度。</param>
    /// <param name="expectedAdler">数据尾部声明的 Adler-32。</param>
    /// <returns>解压后的数据。</returns>
    private static byte[] InflateDeflate(byte[] data, int offset, int length, uint expectedAdler)
    {
        using var input = new MemoryStream(data, offset, length, false);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        var total = 0;
        uint adlerA = 1;
        uint adlerB = 0;
        int read;
        try
        {
            while ((read = deflate.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (read > MaxIccProfileBytes - total)
                    throw new InvalidDataException("PNG iCCP 解压结果超过 16 MiB 限制。");
                output.Write(buffer, 0, read);
                total += read;
                UpdateAdler(buffer, read, ref adlerA, ref adlerB);
            }
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (IOException ex)
        {
            throw new InvalidDataException("PNG iCCP 压缩数据无效。", ex);
        }

        if (input.Position != input.Length)
            throw new InvalidDataException("PNG iCCP raw-deflate 数据包含未消费尾部。");

        var actualAdler = (adlerB << 16) | adlerA;
        if (actualAdler != expectedAdler)
            throw new InvalidDataException("PNG iCCP Adler-32 校验失败。");
        return output.ToArray();
    }

    /// <summary>
    /// 使用 zlib 包装压缩 ICC 数据。
    /// </summary>
    /// <param name="data">待压缩数据。</param>
    /// <returns>zlib 格式的压缩数据。</returns>
    private static byte[] DeflateZlib(byte[] data)
    {
        using var compressedStream = new MemoryStream();
        using (var deflate = new DeflateStream(compressedStream, CompressionLevel.Optimal, true))
            deflate.Write(data, 0, data.Length);

        var compressed = compressedStream.ToArray();
        using var zlib = new MemoryStream();
        zlib.WriteByte(0x78);
        zlib.WriteByte(0x9C);
        zlib.Write(compressed, 0, compressed.Length);
        var adler = Adler32(data);
        zlib.WriteByte((byte)(adler >> 24));
        zlib.WriteByte((byte)(adler >> 16));
        zlib.WriteByte((byte)(adler >> 8));
        zlib.WriteByte((byte)adler);
        return zlib.ToArray();
    }

    /// <summary>
    /// 更新指定数据范围的 Adler-32 状态。
    /// </summary>
    /// <param name="data">待计算数据。</param>
    /// <param name="length">参与计算的长度。</param>
    /// <param name="a">Adler-32 的 A 分量。</param>
    /// <param name="b">Adler-32 的 B 分量。</param>
    private static void UpdateAdler(byte[] data, int length, ref uint a, ref uint b)
    {
        const uint modulo = 65521;
        for (var i = 0; i < length; i++)
        {
            a = (a + data[i]) % modulo;
            b = (b + a) % modulo;
        }
    }

    /// <summary>
    /// 计算数据的 Adler-32 校验值。
    /// </summary>
    /// <param name="data">待计算数据。</param>
    /// <returns>Adler-32 校验值。</returns>
    private static uint Adler32(byte[] data)
    {
        const uint modulo = 65521;
        uint a = 1;
        uint b = 0;
        foreach (var value in data)
        {
            a = (a + value) % modulo;
            b = (b + a) % modulo;
        }
        return (b << 16) | a;
    }

    /// <summary>
    /// 按大端字节序读取无符号 32 位整数。
    /// </summary>
    /// <param name="data">原始数据。</param>
    /// <param name="offset">读取偏移量。</param>
    /// <returns>读取到的整数。</returns>
    private static uint ReadUInt32(byte[] data, int offset)
    {
        if (offset < 0 || offset + 4 > data.Length)
            throw new InvalidDataException("PNG 整数字段不完整。");
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
            (data[offset + 2] << 8) | data[offset + 3]);
    }


    /// <summary>
    /// 判断数据是否以 PNG 签名开头。
    /// </summary>
    /// <param name="data">待判断数据。</param>
    /// <returns>数据为 PNG 签名时返回 true，否则返回 false。</returns>
    private static bool IsPng(byte[] data)
    {
        if (data.Length < PngSignature.Length)
            return false;
        for (var i = 0; i < PngSignature.Length; i++)
        {
            if (data[i] != PngSignature[i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// 判断数据是否以 JPEG 签名开头。
    /// </summary>
    /// <param name="data">待判断数据。</param>
    /// <returns>数据为 JPEG 签名时返回 true，否则返回 false。</returns>
    private static bool IsJpeg(byte[] data)
    {
        return data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8;
    }

    /// <summary>
    /// 判断数据是否具有 WebP 容器签名。
    /// </summary>
    /// <param name="data">待判断数据。</param>
    /// <returns>数据疑似 WebP 时返回 true，否则返回 false。</returns>
    private static bool LooksLikeWebp(byte[] data)
    {
        return data.Length >= 12 && data[0] == (byte)'R' && data[1] == (byte)'I' &&
            data[2] == (byte)'F' && data[3] == (byte)'F' &&
            data[8] == WebpSignature[0] && data[9] == WebpSignature[1] &&
            data[10] == WebpSignature[2] && data[11] == WebpSignature[3];
    }
}

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bing.Drawing.Internal;

/// <summary>
/// PNG chunk 解析、CRC 校验及元数据清理。
/// </summary>
internal static class PngMetadataSanitizer
{
    /// <summary>
    /// PNG 文件签名。
    /// </summary>
    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// 表示解析后的 PNG 文档。
    /// </summary>
    internal sealed class PngDocument
    {
        /// <summary>
        /// 获取或设置原始 PNG 数据。
        /// </summary>
        internal byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 获取 PNG chunk 列表。
        /// </summary>
        internal List<PngChunk> Chunks { get; } = new();

        /// <summary>
        /// 获取图像宽度。
        /// </summary>
        internal int Width { get; set; }

        /// <summary>
        /// 获取图像高度。
        /// </summary>
        internal int Height { get; set; }

        /// <summary>
        /// 获取图像帧数。
        /// </summary>
        internal int FrameCount { get; set; } = 1;

        /// <summary>
        /// 获取 EXIF 方向值。
        /// </summary>
        internal int Orientation { get; set; } = 1;

        /// <summary>
        /// 获取是否包含动画控制信息。
        /// </summary>
        internal bool HasAnimation { get; set; }
    }

    /// <summary>
    /// 表示 PNG 文件中的一个 chunk。
    /// </summary>
    internal sealed class PngChunk
    {
        /// <summary>
        /// 获取或设置 chunk 类型。
        /// </summary>
        internal string Type { get; set; } = string.Empty;

        /// <summary>
        /// 获取 chunk 在原始数据中的起始偏移量。
        /// </summary>
        internal int RawStart { get; set; }

        /// <summary>
        /// 获取 chunk 原始长度。
        /// </summary>
        internal int RawLength { get; set; }

        /// <summary>
        /// 获取 chunk 数据的起始偏移量。
        /// </summary>
        internal int DataOffset { get; set; }

        /// <summary>
        /// 获取 chunk 数据长度。
        /// </summary>
        internal int DataLength { get; set; }

        /// <summary>
        /// 从原始数据复制当前 chunk。
        /// </summary>
        /// <param name="data">原始 PNG 数据。</param>
        /// <returns>chunk 原始字节。</returns>
        internal byte[] GetRaw(byte[] data)
        {
            var result = new byte[RawLength];
            Array.Copy(data, RawStart, result, 0, RawLength);
            return result;
        }

        /// <summary>
        /// 从原始数据复制当前 chunk 的数据部分。
        /// </summary>
        /// <param name="data">原始 PNG 数据。</param>
        /// <returns>chunk 数据字节。</returns>
        internal byte[] GetData(byte[] data)
        {
            var result = new byte[DataLength];
            if (DataLength > 0)
                Array.Copy(data, DataOffset, result, 0, DataLength);
            return result;
        }
    }

    /// <summary>
    /// 严格解析 PNG chunk 并校验 CRC。
    /// </summary>
    /// <param name="data">完整 PNG 数据。</param>
    /// <returns>解析后的 PNG 文档。</returns>
    internal static PngDocument Parse(byte[] data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        if (data.Length < Signature.Length || !StartsWith(data, Signature))
            throw new InvalidDataException("PNG 签名无效。");

        var document = new PngDocument { Data = data };
        var position = Signature.Length;
        var seenHeader = false;
        var seenEnd = false;
        var seenImageData = false;
        var seenIcc = false;
        var animationFrames = 1;

        while (position < data.Length)
        {
            if (data.Length - position < 12)
                throw new InvalidDataException("PNG chunk 头部不完整。");

            var length = ReadUInt32(data, position);
            if (length > int.MaxValue)
                throw new InvalidDataException("PNG chunk 长度超出支持范围。");
            var dataLength = (int)length;
            var dataOffset = position + 8;
            var rawLength = CheckedAdd(12, dataLength);
            var end = CheckedAdd(position, rawLength);
            if (end > data.Length)
                throw new InvalidDataException("PNG chunk 超出输入范围。");

            var type = ReadType(data, position + 4);
            ValidateChunkType(type);
            ValidateCrc(data, position, dataLength);

            if (!seenHeader && !string.Equals(type, "IHDR", StringComparison.Ordinal))
                throw new InvalidDataException("PNG 第一个 chunk 必须是 IHDR。");

            var chunk = new PngChunk
            {
                Type = type,
                RawStart = position,
                RawLength = rawLength,
                DataOffset = dataOffset,
                DataLength = dataLength
            };
            document.Chunks.Add(chunk);

            if (type == "IHDR")
            {
                if (seenHeader || dataLength != 13)
                    throw new InvalidDataException("PNG IHDR chunk 无效。");
                document.Width = CheckedDimension(ReadUInt32(data, dataOffset));
                document.Height = CheckedDimension(ReadUInt32(data, dataOffset + 4));
                if (document.Width <= 0 || document.Height <= 0)
                    throw new InvalidDataException("PNG 图像尺寸无效。");
                seenHeader = true;
            }
            else if (type == "acTL")
            {
                if (dataLength != 8)
                    throw new InvalidDataException("PNG acTL chunk 无效。");
                animationFrames = CheckedFrameCount(ReadUInt32(data, dataOffset));
                document.HasAnimation = true;
            }
            else if (type == "fcTL")
            {
                if (dataLength != 26)
                    throw new InvalidDataException("PNG fcTL chunk 无效。");
                document.HasAnimation = true;
            }
            else if (type == "IDAT" || type == "fdAT")
            {
                if (type == "fdAT")
                {
                    if (dataLength < 4)
                        throw new InvalidDataException("PNG fdAT chunk 无效。");
                    document.HasAnimation = true;
                }
                seenImageData = true;
            }
            else if (type == "iCCP")
            {
                if (seenIcc)
                    throw new InvalidDataException("PNG 存在多个 iCCP chunk。");
                ValidateIccChunk(data, dataOffset, dataLength);
                seenIcc = true;
            }
            else if (type == "eXIf")
            {
                TiffMetadataSanitizer.Validate(data, dataOffset, dataLength);
                document.Orientation = TiffMetadataSanitizer.GetOrientation(data, dataOffset, dataLength);
            }
            else if (type == "IEND")
            {
                if (dataLength != 0)
                    throw new InvalidDataException("PNG IEND chunk 无效。");
                seenEnd = true;
                position = end;
                break;
            }

            position = end;
        }

        if (!seenHeader || !seenEnd || !seenImageData || position != data.Length)
            throw new InvalidDataException("PNG 缺少完整图像数据、IEND 或包含尾部数据。");
        document.FrameCount = document.HasAnimation ? Math.Max(2, animationFrames) : animationFrames;
        return document;
    }

    /// <summary>
    /// 按选项清理 PNG 元数据。
    /// </summary>
    /// <remarks>
    /// 输入数组不会被修改；未发生清理时返回原数组。
    /// </remarks>
    /// <param name="data">完整 PNG 数据。</param>
    /// <param name="options">元数据清理选项。</param>
    /// <returns>清理后的 PNG 数据。</returns>
    internal static byte[] Sanitize(byte[] data, ImageMetadataOptions options)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var document = Parse(data);
        var chunks = new List<byte[]>();
        var changed = false;

        foreach (var chunk in document.Chunks)
        {
            var raw = chunk.GetRaw(data);
            if (chunk.Type == "eXIf")
            {
                if (options.RemoveEntireExif)
                {
                    changed = true;
                    continue;
                }

                if (options.RemoveGps)
                {
                    var sanitized = TiffMetadataSanitizer.RemoveGps(data, chunk.DataOffset, chunk.DataLength, out var gpsChanged);
                    if (gpsChanged)
                    {
                        raw = BuildChunk(chunk.Type, sanitized);
                        changed = true;
                    }
                }
            }
            else if (chunk.Type == "iCCP" && !options.PreserveIccProfile)
            {
                changed = true;
                continue;
            }

            chunks.Add(raw);
        }

        if (!changed)
            return data;
        return Rewrite(data, chunks);
    }

    /// <summary>
    /// 使用新的 chunk 列表重建 PNG。
    /// </summary>
    /// <param name="encoded">原始 PNG 数据。</param>
    /// <param name="chunks">新的 chunk 列表。</param>
    /// <returns>重建后的 PNG 数据。</returns>
    internal static byte[] Rewrite(byte[] encoded, IList<byte[]> chunks)
    {
        using var output = new MemoryStream(encoded.Length);
        output.Write(Signature, 0, Signature.Length);
        foreach (var chunk in chunks)
            output.Write(chunk, 0, chunk.Length);
        return output.ToArray();
    }

    /// <summary>
    /// 构造带长度和 CRC 的 PNG chunk。
    /// </summary>
    /// <param name="type">四字符 ASCII chunk 类型。</param>
    /// <param name="payload">chunk 数据。</param>
    /// <returns>完整的 PNG chunk 数据。</returns>
    internal static byte[] BuildChunk(string type, byte[] payload)
    {
        if (type is null || type.Length != 4)
            throw new ArgumentException("PNG chunk 类型必须为四个 ASCII 字符。", nameof(type));
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));
        ValidateChunkType(type);

        var result = new byte[12 + payload.Length];
        WriteUInt32(result, 0, (uint)payload.Length);
        for (var i = 0; i < 4; i++)
            result[4 + i] = (byte)type[i];
        if (payload.Length > 0)
            Array.Copy(payload, 0, result, 8, payload.Length);
        var crc = ComputeCrc(result, 4, 4 + payload.Length);
        WriteUInt32(result, 8 + payload.Length, crc);
        return result;
    }

    /// <summary>
    /// 将 eXIf chunk 中的方向值规范化为默认方向。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
    /// <param name="chunk">待处理 chunk。</param>
    /// <returns>规范化后的 chunk；无需修改时返回原始 chunk。</returns>
    internal static byte[] NormalizeExifOrientation(byte[] data, PngChunk chunk)
    {
        if (chunk.Type != "eXIf")
            return chunk.GetRaw(data);
        var payload = chunk.GetData(data);
        var normalized = TiffMetadataSanitizer.SetOrientation(payload, 0, payload.Length, 1, out var changed);
        return changed ? BuildChunk(chunk.Type, normalized) : chunk.GetRaw(data);
    }

    /// <summary>
    /// 读取 eXIf chunk 中的方向值。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
    /// <param name="chunk">待读取 chunk。</param>
    /// <returns>EXIF 方向值。</returns>
    internal static int GetExifOrientation(byte[] data, PngChunk chunk)
    {
        if (chunk.Type != "eXIf")
            return 1;
        var payload = chunk.GetData(data);
        return TiffMetadataSanitizer.GetOrientation(payload, 0, payload.Length);
    }

    /// <summary>
    /// 获取 PNG 中的 ICC chunk。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
    /// <param name="document">已解析的 PNG 文档。</param>
    /// <returns>ICC chunk 原始数据；不存在时返回 null。</returns>
    internal static byte[]? GetIccChunk(byte[] data, PngDocument document)
    {
        byte[]? result = null;
        foreach (var chunk in document.Chunks)
        {
            if (chunk.Type != "iCCP")
                continue;
            if (result is not null)
                throw new InvalidDataException("PNG 存在多个 iCCP chunk。");
            result = chunk.GetRaw(data);
        }
        return result;
    }

    /// <summary>
    /// 获取 PNG 中的描述性元数据 chunk。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
    /// <param name="document">已解析的 PNG 文档。</param>
    /// <returns>描述性元数据 chunk 列表。</returns>
    internal static List<byte[]> GetDescriptionChunks(byte[] data, PngDocument document)
    {
        var result = new List<byte[]>();
        foreach (var chunk in document.Chunks)
        {
            if (chunk.Type == "tEXt" || chunk.Type == "zTXt" || chunk.Type == "iTXt")
                result.Add(chunk.GetRaw(data));
        }
        return result;
    }

    /// <summary>
    /// 判断 chunk 是否属于支持的元数据类型。
    /// </summary>
    /// <param name="chunk">待判断 chunk。</param>
    /// <returns>chunk 为元数据类型时返回 true，否则返回 false。</returns>
    internal static bool IsMetadataChunk(PngChunk chunk)
    {
        return chunk.Type == "eXIf" || chunk.Type == "iCCP" ||
            chunk.Type == "tEXt" || chunk.Type == "zTXt" || chunk.Type == "iTXt";
    }

    /// <summary>
    /// 验证 PNG chunk 的 CRC。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
    /// <param name="chunkStart">chunk 起始偏移量。</param>
    /// <param name="dataLength">chunk 数据长度。</param>
    private static void ValidateCrc(byte[] data, int chunkStart, int dataLength)
    {
        var expected = ReadUInt32(data, chunkStart + 8 + dataLength);
        var actual = ComputeCrc(data, chunkStart + 4, 4 + dataLength);
        if (expected != actual)
            throw new InvalidDataException("PNG chunk CRC 校验失败。");
    }

    /// <summary>
    /// 验证 PNG iCCP chunk 的关键字和压缩方法。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
    /// <param name="offset">chunk 数据起始偏移量。</param>
    /// <param name="length">chunk 数据长度。</param>
    private static void ValidateIccChunk(byte[] data, int offset, int length)
    {
        var keywordEnd = -1;
        for (var i = 0; i < length; i++)
        {
            if (data[offset + i] == 0)
            {
                keywordEnd = i;
                break;
            }
            if (i >= 79)
                break;
        }

        if (keywordEnd < 1 || keywordEnd > 79 || keywordEnd + 2 >= length)
            throw new InvalidDataException("PNG iCCP 关键字或数据无效。");
        if (data[offset + keywordEnd + 1] != 0)
            throw new InvalidDataException("PNG iCCP 压缩方法无效。");
    }

    /// <summary>
    /// 计算指定字节范围的 PNG CRC。
    /// </summary>
    /// <param name="data">待计算数据。</param>
    /// <param name="offset">起始偏移量。</param>
    /// <param name="length">计算长度。</param>
    /// <returns>CRC 值。</returns>
    private static uint ComputeCrc(byte[] data, int offset, int length)
    {
        uint crc = 0xFFFFFFFF;
        for (var i = 0; i < length; i++)
        {
            crc ^= data[offset + i];
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// 读取 PNG chunk 类型字符串。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
    /// <param name="offset">类型字段偏移量。</param>
    /// <returns>四字符 ASCII 类型。</returns>
    private static string ReadType(byte[] data, int offset)
    {
        return Encoding.ASCII.GetString(data, offset, 4);
    }

    /// <summary>
    /// 验证 PNG chunk 类型只包含 ASCII 字母。
    /// </summary>
    /// <param name="type">chunk 类型。</param>
    private static void ValidateChunkType(string type)
    {
        for (var i = 0; i < type.Length; i++)
        {
            var value = type[i];
            if (!((value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z')))
                throw new InvalidDataException("PNG chunk 类型无效。");
        }
    }

    /// <summary>
    /// 按大端字节序读取无符号 32 位整数。
    /// </summary>
    /// <param name="data">原始 PNG 数据。</param>
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
    /// 按大端字节序写入无符号 32 位整数。
    /// </summary>
    /// <param name="data">目标数据。</param>
    /// <param name="offset">写入偏移量。</param>
    /// <param name="value">待写入的整数。</param>
    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    /// <summary>
    /// 执行不会溢出的整数加法。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>相加结果。</returns>
    private static int CheckedAdd(int left, int right)
    {
        var result = (long)left + right;
        if (result > int.MaxValue)
            throw new InvalidDataException("PNG 长度超出支持范围。");
        return (int)result;
    }

    /// <summary>
    /// 验证并转换图像尺寸。
    /// </summary>
    /// <param name="value">待转换的尺寸值。</param>
    /// <returns>整数尺寸。</returns>
    private static int CheckedDimension(uint value)
    {
        if (value == 0 || value > int.MaxValue)
            throw new InvalidDataException("PNG 尺寸超出支持范围。");
        return (int)value;
    }

    /// <summary>
    /// 验证并转换图像帧数。
    /// </summary>
    /// <param name="value">待转换的帧数值。</param>
    /// <returns>整数帧数。</returns>
    private static int CheckedFrameCount(uint value)
    {
        if (value == 0 || value > int.MaxValue)
            throw new InvalidDataException("PNG 帧数无效。");
        return (int)value;
    }

    /// <summary>
    /// 判断字节数组是否以指定前缀开头。
    /// </summary>
    /// <param name="data">待判断数据。</param>
    /// <param name="prefix">待匹配前缀。</param>
    /// <returns>数据以此前缀开头时返回 true，否则返回 false。</returns>
    private static bool StartsWith(byte[] data, byte[] prefix)
    {
        if (data.Length < prefix.Length)
            return false;
        for (var i = 0; i < prefix.Length; i++)
        {
            if (data[i] != prefix[i])
                return false;
        }
        return true;
    }
}

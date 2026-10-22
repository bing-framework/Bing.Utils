using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bing.Drawing.Internal;

/// <summary>
/// JPEG 头部元数据清理与结构解析。
/// </summary>
internal static class JpegMetadataSanitizer
{
    /// <summary>
    /// JPEG 标记前缀。
    /// </summary>
    private const byte MarkerPrefix = 0xFF;

    /// <summary>
    /// JPEG 图像开始标记。
    /// </summary>
    private const byte MarkerSoi = 0xD8;

    /// <summary>
    /// JPEG 图像结束标记。
    /// </summary>
    private const byte MarkerEoi = 0xD9;

    /// <summary>
    /// JPEG 扫描开始标记。
    /// </summary>
    private const byte MarkerSos = 0xDA;

    /// <summary>
    /// JPEG APP1 应用段标记。
    /// </summary>
    private const byte MarkerApp1 = 0xE1;

    /// <summary>
    /// JPEG APP2 应用段标记。
    /// </summary>
    private const byte MarkerApp2 = 0xE2;

    /// <summary>
    /// EXIF 段前缀。
    /// </summary>
    private static readonly byte[] ExifPrefix = Encoding.ASCII.GetBytes("Exif\0\0");

    /// <summary>
    /// ICC 配置文件段前缀。
    /// </summary>
    private static readonly byte[] IccPrefix = Encoding.ASCII.GetBytes("ICC_PROFILE\0");

    /// <summary>
    /// MPF 段前缀。
    /// </summary>
    private static readonly byte[] MpfPrefix = Encoding.ASCII.GetBytes("MPF\0");

    /// <summary>
    /// XMP 段前缀。
    /// </summary>
    private static readonly byte[] XmpPrefix = Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0");

    /// <summary>
    /// 表示解析后的 JPEG 文档。
    /// </summary>
    internal sealed class JpegDocument
    {
        /// <summary>
        /// 获取或设置原始 JPEG 数据。
        /// </summary>
        internal byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 获取 JPEG 头部分段列表。
        /// </summary>
        internal List<JpegSegment> Segments { get; } = new();

        /// <summary>
        /// 获取扫描数据的起始偏移量。
        /// </summary>
        internal int TailStart { get; set; }

        /// <summary>
        /// 获取首个图像帧的宽度。
        /// </summary>
        internal int Width { get; set; }

        /// <summary>
        /// 获取首个图像帧的高度。
        /// </summary>
        internal int Height { get; set; }

        /// <summary>
        /// 获取 JPEG 帧数。
        /// </summary>
        internal int FrameCount { get; set; } = 1;

        /// <summary>
        /// 获取是否包含扫描数据。
        /// </summary>
        internal bool HasScan { get; set; }
    }

    /// <summary>
    /// 表示 JPEG 头部中的一个分段。
    /// </summary>
    internal sealed class JpegSegment
    {
        /// <summary>
        /// 获取或设置分段标记值。
        /// </summary>
        internal int Marker { get; set; }

        /// <summary>
        /// 获取分段在原始数据中的起始偏移量。
        /// </summary>
        internal int RawStart { get; set; }

        /// <summary>
        /// 获取分段原始长度。
        /// </summary>
        internal int RawLength { get; set; }

        /// <summary>
        /// 获取分段载荷的起始偏移量。
        /// </summary>
        internal int PayloadOffset { get; set; }

        /// <summary>
        /// 获取分段载荷长度。
        /// </summary>
        internal int PayloadLength { get; set; }

        /// <summary>
        /// 获取分段是否包含长度字段。
        /// </summary>
        internal bool HasLength { get; set; }

        /// <summary>
        /// 从原始数据复制当前分段。
        /// </summary>
        /// <param name="data">原始 JPEG 数据。</param>
        /// <returns>分段原始字节。</returns>
        internal byte[] GetRaw(byte[] data)
        {
            var result = new byte[RawLength];
            Array.Copy(data, RawStart, result, 0, RawLength);
            return result;
        }

        /// <summary>
        /// 从原始数据复制当前分段的载荷。
        /// </summary>
        /// <param name="data">原始 JPEG 数据。</param>
        /// <returns>分段载荷字节。</returns>
        internal byte[] GetPayload(byte[] data)
        {
            var result = new byte[PayloadLength];
            if (PayloadLength > 0)
                Array.Copy(data, PayloadOffset, result, 0, PayloadLength);
            return result;
        }
    }

    /// <summary>
    /// 严格解析 JPEG 的头部、扫描头和熵编码数据。
    /// </summary>
    /// <param name="data">完整 JPEG 数据。</param>
    /// <returns>解析后的 JPEG 文档。</returns>
    internal static JpegDocument Parse(byte[] data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        if (data.Length < 4 || data[0] != MarkerPrefix || data[1] != MarkerSoi)
            throw new InvalidDataException("JPEG SOI 头部无效。");

        var document = new JpegDocument { Data = data };
        var position = 2;
        var frameCount = 0;

        while (position < data.Length)
        {
            var rawStart = position;
            if (data[position++] != MarkerPrefix)
                throw new InvalidDataException("JPEG 标记前缀无效。");

            while (position < data.Length && data[position] == MarkerPrefix)
                position++;
            if (position >= data.Length)
                throw new InvalidDataException("JPEG 标记不完整。");

            var marker = data[position++];
            if (marker == 0)
                throw new InvalidDataException("JPEG 头部出现无效填充标记。");

            if (marker == MarkerSos)
            {
                var payloadOffset = position + 2;
                var payloadLength = ReadSegmentLength(data, position, out var end);
                if (payloadLength < 8 || payloadOffset > end)
                    throw new InvalidDataException("JPEG SOS 段长度无效。");
                var componentCount = data[payloadOffset];
                if (componentCount == 0 || payloadLength != 6 + componentCount * 2)
                    throw new InvalidDataException("JPEG SOS 扫描头长度与组件数不一致。");
                if (document.Width <= 0 || document.Height <= 0)
                    throw new InvalidDataException("JPEG 缺少有效的 SOF 尺寸。");

                document.TailStart = rawStart;
                document.HasScan = true;
                FindEndOfImage(data, end);
                document.FrameCount = Math.Max(1, frameCount);
                return document;
            }

            if (marker == MarkerEoi)
                throw new InvalidDataException("JPEG 在 SOS 前结束，缺少扫描数据。");

            if (IsStandaloneMarker(marker))
            {
                document.Segments.Add(new JpegSegment
                {
                    Marker = marker,
                    RawStart = rawStart,
                    RawLength = position - rawStart,
                    PayloadOffset = position,
                    PayloadLength = 0,
                    HasLength = false
                });
                continue;
            }

            var segmentPayloadOffset = position + 2;
            var segmentPayloadLength = ReadSegmentLength(data, position, out var segmentEnd) - 2;
            var segment = new JpegSegment
            {
                Marker = marker,
                RawStart = rawStart,
                RawLength = segmentEnd - rawStart,
                PayloadOffset = segmentPayloadOffset,
                PayloadLength = segmentPayloadLength,
                HasLength = true
            };
            document.Segments.Add(segment);

            if (IsStartOfFrame(marker))
            {
                if (segmentPayloadLength < 7)
                    throw new InvalidDataException("JPEG SOF 段长度无效。");
                var height = ReadUInt16(data, segmentPayloadOffset + 1);
                var width = ReadUInt16(data, segmentPayloadOffset + 3);
                if (width == 0 || height == 0)
                    throw new InvalidDataException("JPEG SOF 尺寸无效。");
                if (document.Width == 0)
                {
                    document.Width = width;
                    document.Height = height;
                }
                frameCount++;
            }

            position = segmentEnd;
        }

        throw new InvalidDataException("JPEG 缺少 SOS/EOI。");
    }

    /// <summary>
    /// 按选项清理 JPEG 元数据。
    /// </summary>
    /// <remarks>
    /// 输入数组不会被修改；未发生清理时返回原数组。
    /// </remarks>
    /// <param name="data">完整 JPEG 数据。</param>
    /// <param name="options">元数据清理选项。</param>
    /// <returns>清理后的 JPEG 数据。</returns>
    internal static byte[] Sanitize(byte[] data, ImageMetadataOptions options)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var document = Parse(data);
        var hasIcc = false;
        foreach (var segment in document.Segments)
        {
            if (segment.Marker == MarkerApp2 && IsIccPayload(segment.GetPayload(data)))
            {
                hasIcc = true;
                break;
            }
        }
        if (hasIcc)
            GetIccProfile(data, document);

        var header = new List<byte[]>();
        var changed = false;

        foreach (var segment in document.Segments)
        {
            var raw = segment.GetRaw(data);
            if (segment.Marker == MarkerApp1)
            {
                var payload = segment.GetPayload(data);
                if (IsExifPayload(payload))
                {
                    var tiffOffset = ExifPrefix.Length;
                    var tiffLength = payload.Length - tiffOffset;
                    TiffMetadataSanitizer.Validate(payload, tiffOffset, tiffLength);

                    if (options.RemoveEntireExif)
                    {
                        changed = true;
                        continue;
                    }

                    if (options.RemoveGps)
                    {
                        var sanitizedTiff = TiffMetadataSanitizer.RemoveGps(payload, tiffOffset, tiffLength, out var gpsChanged);
                        if (gpsChanged)
                        {
                            var sanitized = new byte[payload.Length];
                            Array.Copy(payload, 0, sanitized, 0, tiffOffset);
                            Array.Copy(sanitizedTiff, 0, sanitized, tiffOffset, sanitizedTiff.Length);
                            raw = BuildSegment(segment.Marker, sanitized);
                            changed = true;
                        }
                    }
                }
            }
            else if (segment.Marker == MarkerApp2 && IsIccPayload(segment.GetPayload(data)))
            {
                var payload = segment.GetPayload(data);
                ValidateIccPayload(payload);
                if (!options.PreserveIccProfile)
                {
                    changed = true;
                    continue;
                }
            }

            header.Add(raw);
        }

        if (!changed)
            return data;
        return RewriteWithHeader(data, document, header);
    }

    /// <summary>
    /// 使用新的头部分段重建 JPEG。
    /// </summary>
    /// <remarks>
    /// SOS 及其后的扫描数据按原样保留。
    /// </remarks>
    /// <param name="encoded">原始 JPEG 数据。</param>
    /// <param name="document">已解析的 JPEG 文档。</param>
    /// <param name="header">新的头部分段。</param>
    /// <returns>重建后的 JPEG 数据。</returns>
    internal static byte[] RewriteWithHeader(byte[] encoded, JpegDocument document, IList<byte[]> header)
    {
        if (!document.HasScan)
            throw new NotSupportedException("仅支持带 SOS 扫描数据的 JPEG 元数据重写。");

        using var output = new MemoryStream(encoded.Length);
        output.Write(encoded, 0, 2);
        foreach (var segment in header)
            output.Write(segment, 0, segment.Length);
        output.Write(encoded, document.TailStart, encoded.Length - document.TailStart);
        return output.ToArray();
    }

    /// <summary>
    /// 构造带长度字段的 JPEG 分段。
    /// </summary>
    /// <param name="marker">分段标记值。</param>
    /// <param name="payload">分段载荷。</param>
    /// <returns>完整的 JPEG 分段数据。</returns>
    internal static byte[] BuildSegment(int marker, byte[] payload)
    {
        if (marker < 0xC0 || marker > 0xFF || marker == 0xFF || marker == MarkerSoi || marker == MarkerEoi)
            throw new ArgumentOutOfRangeException(nameof(marker));
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));
        if (payload.Length > ushort.MaxValue - 2)
            throw new InvalidDataException("JPEG 元数据段过大。");

        var result = new byte[payload.Length + 4];
        result[0] = MarkerPrefix;
        result[1] = (byte)marker;
        var length = payload.Length + 2;
        result[2] = (byte)(length >> 8);
        result[3] = (byte)length;
        Array.Copy(payload, 0, result, 4, payload.Length);
        return result;
    }

    /// <summary>
    /// 判断 JPEG 分段是否为 EXIF 分段。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">待判断分段。</param>
    /// <returns>分段为 EXIF 分段时返回 true，否则返回 false。</returns>
    internal static bool IsExifSegment(byte[] data, JpegSegment segment)
    {
        return segment.Marker == MarkerApp1 && IsExifPayload(segment.GetPayload(data));
    }

    /// <summary>
    /// 判断 JPEG 分段是否为 ICC 分段。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">待判断分段。</param>
    /// <returns>分段为 ICC 分段时返回 true，否则返回 false。</returns>
    internal static bool IsIccSegment(byte[] data, JpegSegment segment)
    {
        return segment.Marker == MarkerApp2 && IsIccPayload(segment.GetPayload(data));
    }

    /// <summary>
    /// 判断 JPEG 分段是否为 XMP 分段。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">待判断分段。</param>
    /// <returns>分段为 XMP 分段时返回 true，否则返回 false。</returns>
    internal static bool IsXmpSegment(byte[] data, JpegSegment segment)
    {
        var payload = segment.GetPayload(data);
        return segment.Marker == MarkerApp1 && StartsWith(payload, XmpPrefix);
    }

    /// <summary>
    /// 判断 JPEG 分段是否为 MPF 分段。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">待判断分段。</param>
    /// <returns>分段为 MPF 分段时返回 true，否则返回 false。</returns>
    internal static bool IsMpfSegment(byte[] data, JpegSegment segment)
    {
        var payload = segment.GetPayload(data);
        return segment.Marker == MarkerApp2 && StartsWith(payload, MpfPrefix);
    }

    /// <summary>
    /// 将 EXIF 方向值规范化为默认方向。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">EXIF 分段。</param>
    /// <returns>规范化后的分段；无需修改时返回原始分段。</returns>
    internal static byte[] NormalizeExifOrientation(byte[] data, JpegSegment segment)
    {
        var payload = segment.GetPayload(data);
        if (!IsExifPayload(payload))
            return segment.GetRaw(data);
        var sanitizedTiff = TiffMetadataSanitizer.SetOrientation(payload, ExifPrefix.Length,
            payload.Length - ExifPrefix.Length, 1, out var changed);
        if (!changed)
            return segment.GetRaw(data);
        var sanitized = new byte[payload.Length];
        Array.Copy(payload, 0, sanitized, 0, ExifPrefix.Length);
        Array.Copy(sanitizedTiff, 0, sanitized, ExifPrefix.Length, sanitizedTiff.Length);
        return BuildSegment(segment.Marker, sanitized);
    }

    /// <summary>
    /// 合并 JPEG 中分片存储的 ICC 配置文件。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="document">已解析的 JPEG 文档。</param>
    /// <returns>合并后的 ICC 配置文件；未找到时返回空数组。</returns>
    internal static byte[] GetIccProfile(byte[] data, JpegDocument document)
    {
        var chunks = new Dictionary<int, byte[]>();
        var total = 0;
        foreach (var segment in document.Segments)
        {
            if (!IsIccSegment(data, segment))
                continue;
            var payload = segment.GetPayload(data);
            if (payload.Length < IccPrefix.Length + 2)
                throw new InvalidDataException("JPEG ICC_PROFILE 段不完整。");
            var sequence = payload[IccPrefix.Length];
            var count = payload[IccPrefix.Length + 1];
            if (sequence == 0 || count == 0 || sequence > count)
                throw new InvalidDataException("JPEG ICC_PROFILE 分片编号无效。");
            if (total == 0)
                total = count;
            else if (total != count)
                throw new InvalidDataException("JPEG ICC_PROFILE 分片总数不一致。");
            if (chunks.ContainsKey(sequence))
                throw new InvalidDataException("JPEG ICC_PROFILE 分片重复。");

            var profile = new byte[payload.Length - IccPrefix.Length - 2];
            Array.Copy(payload, IccPrefix.Length + 2, profile, 0, profile.Length);
            chunks.Add(sequence, profile);
        }

        if (total == 0)
            return Array.Empty<byte>();
        if (chunks.Count != total)
            throw new InvalidDataException("JPEG ICC_PROFILE 分片缺失。");

        using var output = new MemoryStream();
        for (var i = 1; i <= total; i++)
        {
            var profile = chunks[i];
            output.Write(profile, 0, profile.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// 将 ICC 配置文件拆分为 JPEG APP2 分段。
    /// </summary>
    /// <param name="profile">ICC 配置文件数据。</param>
    /// <returns>按顺序排列的 APP2 分段。</returns>
    internal static List<byte[]> BuildIccSegments(byte[] profile)
    {
        if (profile is null)
            throw new ArgumentNullException(nameof(profile));
        const int maxProfileBytes = ushort.MaxValue - 2 - 14;
        var count = Math.Max(1, (profile.Length + maxProfileBytes - 1) / maxProfileBytes);
        if (count > byte.MaxValue)
            throw new InvalidDataException("JPEG ICC 配置文件分片数量超出限制。");

        var result = new List<byte[]>();
        for (var i = 0; i < count; i++)
        {
            var offset = i * maxProfileBytes;
            var length = Math.Min(maxProfileBytes, profile.Length - offset);
            var payload = new byte[IccPrefix.Length + 2 + length];
            Array.Copy(IccPrefix, 0, payload, 0, IccPrefix.Length);
            payload[IccPrefix.Length] = (byte)(i + 1);
            payload[IccPrefix.Length + 1] = (byte)count;
            if (length > 0)
                Array.Copy(profile, offset, payload, IccPrefix.Length + 2, length);
            result.Add(BuildSegment(MarkerApp2, payload));
        }
        return result;
    }

    /// <summary>
    /// 判断载荷是否以 EXIF 前缀开头。
    /// </summary>
    /// <param name="payload">待判断载荷。</param>
    /// <returns>载荷为 EXIF 载荷时返回 true，否则返回 false。</returns>
    internal static bool IsExifPayload(byte[] payload) => payload.Length >= ExifPrefix.Length && StartsWith(payload, ExifPrefix);

    /// <summary>
    /// 判断载荷是否以 ICC 前缀开头。
    /// </summary>
    /// <param name="payload">待判断载荷。</param>
    /// <returns>载荷为 ICC 载荷时返回 true，否则返回 false。</returns>
    internal static bool IsIccPayload(byte[] payload) => payload.Length >= IccPrefix.Length && StartsWith(payload, IccPrefix);

    /// <summary>
    /// 判断载荷是否以 XMP 前缀开头。
    /// </summary>
    /// <param name="payload">待判断载荷。</param>
    /// <returns>载荷为 XMP 载荷时返回 true，否则返回 false。</returns>
    internal static bool IsXmpPayload(byte[] payload) => StartsWith(payload, XmpPrefix);

    /// <summary>
    /// 判断载荷是否以 MPF 前缀开头。
    /// </summary>
    /// <param name="payload">待判断载荷。</param>
    /// <returns>载荷为 MPF 载荷时返回 true，否则返回 false。</returns>
    internal static bool IsMpfPayload(byte[] payload) => payload.Length >= MpfPrefix.Length && StartsWith(payload, MpfPrefix);

    /// <summary>
    /// 读取 JPEG EXIF 方向值。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">EXIF 分段。</param>
    /// <returns>EXIF 方向值。</returns>
    internal static int GetExifOrientation(byte[] data, JpegSegment segment)
    {
        var payload = segment.GetPayload(data);
        if (!IsExifPayload(payload))
            return 1;
        return TiffMetadataSanitizer.GetOrientation(payload, ExifPrefix.Length, payload.Length - ExifPrefix.Length);
    }

    /// <summary>
    /// 读取 JPEG MPF 图像帧数。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="segment">MPF 分段。</param>
    /// <returns>MPF 图像帧数。</returns>
    internal static int GetMpfFrameCount(byte[] data, JpegSegment segment)
    {
        var payload = segment.GetPayload(data);
        if (!IsMpfPayload(payload))
            return 1;
        return TiffMetadataSanitizer.GetMpfFrameCount(payload, MpfPrefix.Length, payload.Length - MpfPrefix.Length);
    }

    /// <summary>
    /// 验证 JPEG ICC 分段载荷的分片信息。
    /// </summary>
    /// <param name="payload">ICC 分段载荷。</param>
    private static void ValidateIccPayload(byte[] payload)
    {
        if (payload.Length < IccPrefix.Length + 2)
            throw new InvalidDataException("JPEG ICC_PROFILE 段不完整。");

        var sequence = payload[IccPrefix.Length];
        var count = payload[IccPrefix.Length + 1];
        if (sequence == 0 || count == 0 || sequence > count)
            throw new InvalidDataException("JPEG ICC_PROFILE 分片编号无效。");
    }

    /// <summary>
    /// 读取 JPEG 分段长度并计算结束位置。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="lengthOffset">长度字段起始偏移量。</param>
    /// <param name="segmentEnd">输出分段结束偏移量。</param>
    /// <returns>长度字段声明的分段长度。</returns>
    private static int ReadSegmentLength(byte[] data, int lengthOffset, out int segmentEnd)
    {
        if (lengthOffset < 0 || lengthOffset + 2 > data.Length)
            throw new InvalidDataException("JPEG 段长度字段不完整。");
        var length = (data[lengthOffset] << 8) | data[lengthOffset + 1];
        if (length < 2)
            throw new InvalidDataException("JPEG 段长度小于最小值。");
        var end = (long)lengthOffset + length;
        if (end > data.Length || end > int.MaxValue)
            throw new InvalidDataException("JPEG 段长度超出输入范围。");
        segmentEnd = (int)end;
        return length;
    }

    /// <summary>
    /// 扫描熵编码数据并验证存在图像结束标记。
    /// </summary>
    /// <param name="data">原始 JPEG 数据。</param>
    /// <param name="scanStart">扫描数据起始偏移量。</param>
    private static void FindEndOfImage(byte[] data, int scanStart)
    {
        var position = scanStart;
        while (position < data.Length)
        {
            if (data[position++] != MarkerPrefix)
                continue;
            while (position < data.Length && data[position] == MarkerPrefix)
                position++;
            if (position >= data.Length)
                throw new InvalidDataException("JPEG 扫描数据缺少 EOI。");

            var marker = data[position++];
            if (marker == 0 || (marker >= 0xD0 && marker <= 0xD7))
                continue;
            if (marker == MarkerEoi)
                return;
            if (marker == MarkerSoi)
                throw new InvalidDataException("JPEG 扫描数据中出现重复 SOI。");

            // DNL、第二个 SOS 等合法扫描标记均带长度；严格校验长度后继续寻找最终 EOI。
            ReadSegmentLength(data, position, out var segmentEnd);
            position = segmentEnd;
        }

        throw new InvalidDataException("JPEG 扫描数据缺少 EOI。");
    }

    /// <summary>
    /// 判断 JPEG 标记是否为不带长度字段的独立标记。
    /// </summary>
    /// <param name="marker">标记值。</param>
    /// <returns>标记为独立标记时返回 true，否则返回 false。</returns>
    private static bool IsStandaloneMarker(int marker)
    {
        return marker == 0x01 || marker == MarkerSoi || (marker >= 0xD0 && marker <= 0xD7);
    }

    /// <summary>
    /// 判断 JPEG 标记是否为帧开始标记。
    /// </summary>
    /// <param name="marker">标记值。</param>
    /// <returns>标记为帧开始标记时返回 true，否则返回 false。</returns>
    private static bool IsStartOfFrame(int marker)
    {
        return (marker >= 0xC0 && marker <= 0xC3) ||
            (marker >= 0xC5 && marker <= 0xC7) ||
            (marker >= 0xC9 && marker <= 0xCB) ||
            (marker >= 0xCD && marker <= 0xCF);
    }

    /// <summary>
    /// 按大端字节序读取无符号 16 位整数。
    /// </summary>
    /// <param name="data">原始数据。</param>
    /// <param name="offset">读取偏移量。</param>
    /// <returns>读取到的整数。</returns>
    private static ushort ReadUInt16(byte[] data, int offset)
    {
        if (offset < 0 || offset + 2 > data.Length)
            throw new InvalidDataException("JPEG 尺寸字段不完整。");
        return (ushort)((data[offset] << 8) | data[offset + 1]);
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

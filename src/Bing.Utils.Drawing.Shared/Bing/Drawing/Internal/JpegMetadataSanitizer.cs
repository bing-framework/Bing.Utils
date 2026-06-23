namespace Bing.Drawing.Internal;

/// <summary>
/// JPEG 图像元数据清理器。
/// <para>
/// 支持移除 GPS IFD 指针标签（0x8825）或整个 APP1（EXIF）段。
/// </para>
/// </summary>
internal static class JpegMetadataSanitizer
{
    // JPEG 标记常量
    private const byte MarkerPrefix = 0xFF;
    private const byte MarkerSoi = 0xD8;    // Start of Image
    private const byte MarkerApp1 = 0xE1;    // APP1 (EXIF)
    private const byte MarkerApp2 = 0xE2;    // APP2 (ICC Profile)
    private const byte MarkerEoi = 0xD9;     // End of Image

    // EXIF/TIFF 常量
    private const ushort TiffBigEndian = 0x4D4D;     // "MM"
    private const ushort TiffLittleEndian = 0x4949;  // "II"
    private const ushort TiffMagic = 0x002A;
    private const ushort TagGpsIfdPointer = 0x8825;

    /// <summary>
    /// 对 JPEG 字节数据执行元数据清理
    /// </summary>
    /// <param name="data">完整 JPEG 文件数据</param>
    /// <param name="options">清理选项</param>
    /// <returns>清理后的数据</returns>
    internal static byte[] Sanitize(byte[] data, ImageMetadataOptions options)
    {
        if (data.Length < 4)
            return data;

        // 验证 JPEG SOI
        if (data[0] != MarkerPrefix || data[1] != MarkerSoi)
            return data;

        if (options.RemoveEntireExif)
            return RemoveApp1Segment(data, options.PreserveIccProfile);

        if (options.RemoveGps)
            return RemoveGpsFromExif(data);

        return data;
    }

    /// <summary>
    /// 移除整个 APP1 (EXIF) 段
    /// </summary>
    private static byte[] RemoveApp1Segment(byte[] data, bool preserveIcc)
    {
        using var output = new MemoryStream();
        var pos = 2; // 跳过 SOI

        while (pos < data.Length - 1)
        {
            if (data[pos] != MarkerPrefix)
                break;

            var marker = data[pos + 1];
            if (marker == MarkerEoi)
            {
                output.Write(data, pos, data.Length - pos);
                break;
            }

            // 段长度（大端）
            if (pos + 3 >= data.Length) break;
            var segLength = (data[pos + 2] << 8) | data[pos + 3];
            if (segLength < 2) break;

            var segStart = pos;
            var segEnd = pos + 2 + segLength;

            if (marker == MarkerApp1)
            {
                // 跳过 APP1
                pos = segEnd;
                continue;
            }

            output.Write(data, segStart, segEnd - segStart);
            pos = segEnd;
        }

        return output.ToArray();
    }

    /// <summary>
    /// 从 EXIF 数据中移除 GPS IFD 指针
    /// </summary>
    private static byte[] RemoveGpsFromExif(byte[] data)
    {
        var pos = 2; // 跳过 SOI

        while (pos < data.Length - 1)
        {
            if (data[pos] != MarkerPrefix)
                break;

            var marker = data[pos + 1];
            if (marker == MarkerEoi)
                break;

            if (pos + 3 >= data.Length) break;
            var segLength = (data[pos + 2] << 8) | data[pos + 3];
            if (segLength < 2) break;

            if (marker == MarkerApp1)
            {
                var segDataStart = pos + 4; // 跳过标记和长度
                var segDataLength = segLength - 2;

                // 验证 "Exif\0\0" 前缀
                if (segDataLength > 6 &&
                    data[segDataStart] == 0x45 && data[segDataStart + 1] == 0x78 &&
                    data[segDataStart + 2] == 0x69 && data[segDataStart + 3] == 0x66 &&
                    data[segDataStart + 4] == 0x00 && data[segDataStart + 5] == 0x00)
                {
                    var tiffStart = segDataStart + 6;
                    TryRemoveGpsTagInPlace(data, tiffStart, segDataStart + segDataLength);
                }

                break; // 只处理第一个 APP1
            }

            pos += 2 + segLength;
        }

        return data;
    }

    /// <summary>
    /// 尝试在 TIFF 数据中将 GPS IFD 指针标签（0x8825）标记为已移除
    /// </summary>
    private static void TryRemoveGpsTagInPlace(byte[] data, int tiffStart, int tiffEnd)
    {
        if (tiffStart + 8 > tiffEnd)
            return;

        // 读取字节序
        var byteOrder = (ushort)((data[tiffStart] << 8) | data[tiffStart + 1]);
        bool isBigEndian;
        if (byteOrder == TiffBigEndian)
            isBigEndian = true;
        else if (byteOrder == TiffLittleEndian)
            isBigEndian = false;
        else
            return; // 非 TIFF

        // 验证 TIFF magic
        var magic = ReadUInt16(data, tiffStart + 2, isBigEndian);
        if (magic != TiffMagic)
            return;

        // IFD0 偏移
        var ifdOffset = (int)ReadUInt32(data, tiffStart + 4, isBigEndian);
        var ifdStart = tiffStart + ifdOffset;

        if (ifdStart + 2 > tiffEnd)
            return;

        var entryCount = ReadUInt16(data, ifdStart, isBigEndian);
        var entriesStart = ifdStart + 2;

        // 扫描 IFD 条目，将 GPS IFD 指针标签（0x8825）清零
        for (var i = 0; i < entryCount; i++)
        {
            var entryOffset = entriesStart + i * 12;
            if (entryOffset + 12 > tiffEnd)
                break;

            var tag = ReadUInt16(data, entryOffset, isBigEndian);
            if (tag == TagGpsIfdPointer)
            {
                // 将 tag 改为 0x0000（使其被忽略）
                WriteUInt16(data, entryOffset, 0, isBigEndian);
                return;
            }
        }
    }

    private static ushort ReadUInt16(byte[] data, int offset, bool bigEndian)
    {
        if (bigEndian)
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static uint ReadUInt32(byte[] data, int offset, bool bigEndian)
    {
        if (bigEndian)
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value, bool bigEndian)
    {
        if (bigEndian)
        {
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = (byte)(value & 0xFF);
        }
        else
        {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)(value >> 8);
        }
    }
}

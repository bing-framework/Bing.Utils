namespace Bing.Drawing.Internal;

/// <summary>
/// PNG 图像元数据清理器。
/// <para>
/// PNG EXIF 数据存储在 eXIf chunk 中。支持移除整个 eXIf chunk 或修改其中的 GPS IFD 指针。
/// </para>
/// </summary>
internal static class PngMetadataSanitizer
{
    // PNG 常量
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] ExifChunkType = { 0x65, 0x58, 0x49, 0x66 }; // "eXIf"
    private static readonly byte[] IccpChunkType = { 0x69, 0x43, 0x43, 0x50 }; // "iCCP"
    private static readonly byte[] IendChunkType = { 0x49, 0x45, 0x4E, 0x44 }; // "IEND"

    // TIFF 常量（与 JpegMetadataSanitizer 一致）
    private const ushort TiffBigEndian = 0x4D4D;
    private const ushort TiffLittleEndian = 0x4949;
    private const ushort TiffMagic = 0x002A;
    private const ushort TagGpsIfdPointer = 0x8825;

    /// <summary>
    /// 对 PNG 字节数据执行元数据清理
    /// </summary>
    /// <param name="data">完整 PNG 文件数据</param>
    /// <param name="options">清理选项</param>
    /// <returns>清理后的数据</returns>
    internal static byte[] Sanitize(byte[] data, ImageMetadataOptions options)
    {
        if (data.Length < 8)
            return data;

        // 验证 PNG 签名
        for (var i = 0; i < 8; i++)
        {
            if (data[i] != PngSignature[i])
                return data;
        }

        if (options.RemoveEntireExif)
            return RemoveExifChunks(data, options.PreserveIccProfile);

        if (options.RemoveGps)
            return RemoveGpsFromExifChunk(data);

        return data;
    }

    /// <summary>
    /// 移除所有 eXIf chunks
    /// </summary>
    private static byte[] RemoveExifChunks(byte[] data, bool preserveIcc)
    {
        using var output = new MemoryStream();
        output.Write(data, 0, 8); // PNG 签名

        var pos = 8;
        while (pos + 12 <= data.Length)
        {
            var length = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
            var chunkType = new byte[4];
            Array.Copy(data, pos + 4, chunkType, 0, 4);
            var chunkEnd = pos + 12 + length;

            if (chunkEnd > data.Length)
                break;

            // 跳过 eXIf chunk
            if (ByteArrayEquals(chunkType, ExifChunkType))
            {
                pos = chunkEnd;
                continue;
            }

            output.Write(data, pos, chunkEnd - pos);
            pos = chunkEnd;
        }

        return output.ToArray();
    }

    /// <summary>
    /// 从 eXIf chunk 中移除 GPS IFD 指针
    /// </summary>
    private static byte[] RemoveGpsFromExifChunk(byte[] data)
    {
        // 找到 eXIf chunk 并修改
        var pos = 8;
        while (pos + 12 <= data.Length)
        {
            var length = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
            var chunkEnd = pos + 12 + length;

            if (chunkEnd > data.Length)
                break;

            if (pos + 4 + 4 <= data.Length &&
                data[pos + 4] == 0x65 && data[pos + 5] == 0x58 &&
                data[pos + 6] == 0x49 && data[pos + 7] == 0x66)
            {
                // 找到 eXIf chunk，尝试修改 TIFF 中的 GPS 标签
                var exifDataStart = pos + 8;
                TryRemoveGpsTagInTiff(data, exifDataStart, length);
                break; // 只处理第一个 eXIf
            }

            pos = chunkEnd;
        }

        return data;
    }

    /// <summary>
    /// 在 TIFF 数据中将 GPS IFD 指针标签清零
    /// </summary>
    private static void TryRemoveGpsTagInTiff(byte[] data, int tiffStart, int tiffLength)
    {
        if (tiffLength < 8)
            return;

        var tiffEnd = tiffStart + tiffLength;

        var byteOrder = (ushort)((data[tiffStart] << 8) | data[tiffStart + 1]);
        bool isBigEndian;
        if (byteOrder == TiffBigEndian)
            isBigEndian = true;
        else if (byteOrder == TiffLittleEndian)
            isBigEndian = false;
        else
            return;

        var magic = ReadUInt16(data, tiffStart + 2, isBigEndian);
        if (magic != TiffMagic)
            return;

        var ifdOffset = (int)ReadUInt32(data, tiffStart + 4, isBigEndian);
        var ifdStart = tiffStart + ifdOffset;

        if (ifdStart + 2 > tiffEnd)
            return;

        var entryCount = ReadUInt16(data, ifdStart, isBigEndian);
        var entriesStart = ifdStart + 2;

        for (var i = 0; i < entryCount; i++)
        {
            var entryOffset = entriesStart + i * 12;
            if (entryOffset + 12 > tiffEnd)
                break;

            var tag = ReadUInt16(data, entryOffset, isBigEndian);
            if (tag == TagGpsIfdPointer)
            {
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

    private static bool ByteArrayEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}

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
    /// <summary>
    /// PNG 文件头的 8 字节签名。
    /// </summary>
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    /// <summary>
    /// PNG EXIF 数据块的类型标识。
    /// </summary>
    private static readonly byte[] ExifChunkType = { 0x65, 0x58, 0x49, 0x66 }; // "eXIf"
    /// <summary>
    /// PNG ICC 配置文件数据块的类型标识。
    /// </summary>
    private static readonly byte[] IccpChunkType = { 0x69, 0x43, 0x43, 0x50 }; // "iCCP"
    /// <summary>
    /// PNG 图像结束数据块的类型标识。
    /// </summary>
    private static readonly byte[] IendChunkType = { 0x49, 0x45, 0x4E, 0x44 }; // "IEND"

    // TIFF 常量（与 JpegMetadataSanitizer 一致）
    /// <summary>
    /// TIFF 大端字节序标识。
    /// </summary>
    private const ushort TiffBigEndian = 0x4D4D;
    /// <summary>
    /// TIFF 小端字节序标识。
    /// </summary>
    private const ushort TiffLittleEndian = 0x4949;
    /// <summary>
    /// TIFF 文件头中的格式标识值。
    /// </summary>
    private const ushort TiffMagic = 0x002A;
    /// <summary>
    /// EXIF IFD0 中 GPS IFD 指针标签的编号。
    /// </summary>
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
    /// 移除 PNG 图像中的 eXIf 数据块。
    /// </summary>
    /// <param name="data">待处理的 PNG 数据。</param>
    /// <param name="preserveIcc">兼容保留参数；当前实现不会移除 iCCP 数据块。</param>
    /// <returns>移除 eXIf 数据块后的 PNG 数据。</returns>
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
    /// 从 PNG eXIf 数据块中移除 GPS IFD 指针。
    /// </summary>
    /// <param name="data">待处理的 PNG 数据。</param>
    /// <returns>处理后的 PNG 数据；未找到可处理的 EXIF 数据时返回原数组。</returns>
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
    /// 在 TIFF 数据中清除 GPS IFD 指针。
    /// </summary>
    /// <param name="data">包含 TIFF 数据的字节数组。</param>
    /// <param name="tiffStart">TIFF 数据在数组中的起始索引。</param>
    /// <param name="tiffLength">TIFF 数据的长度。</param>
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

    /// <summary>
    /// 按指定字节序读取 16 位无符号整数。
    /// </summary>
    /// <param name="data">待读取的字节数组。</param>
    /// <param name="offset">起始字节索引。</param>
    /// <param name="bigEndian">是否使用大端字节序。</param>
    /// <returns>读取的 16 位无符号整数。</returns>
    private static ushort ReadUInt16(byte[] data, int offset, bool bigEndian)
    {
        if (bigEndian)
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    /// <summary>
    /// 按指定字节序读取 32 位无符号整数。
    /// </summary>
    /// <param name="data">待读取的字节数组。</param>
    /// <param name="offset">起始字节索引。</param>
    /// <param name="bigEndian">是否使用大端字节序。</param>
    /// <returns>读取的 32 位无符号整数。</returns>
    private static uint ReadUInt32(byte[] data, int offset, bool bigEndian)
    {
        if (bigEndian)
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    /// <summary>
    /// 按指定字节序将 16 位无符号整数写入数组。
    /// </summary>
    /// <param name="data">接收写入数据的字节数组。</param>
    /// <param name="offset">起始写入索引。</param>
    /// <param name="value">要写入的无符号整数。</param>
    /// <param name="bigEndian">是否使用大端字节序。</param>
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

    /// <summary>
    /// 逐字节比较两个字节数组。
    /// </summary>
    /// <param name="a">第一个字节数组。</param>
    /// <param name="b">第二个字节数组。</param>
    /// <returns>两个数组长度相同且每个字节均相同时为 true；否则为 false。</returns>
    private static bool ByteArrayEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}

namespace Bing.Drawing.Internal;

/// <summary>
/// ICO 容器写入器。支持多尺寸 PNG 压缩 ICO。
/// </summary>
internal static class IcoContainerWriter
{
    /// <summary>
    /// 将多个 PNG 帧写入 ICO 格式流。
    /// </summary>
    /// <param name="frames">帧列表：每帧为 (pngBytes, width, height)。width/height 为 256 时写入 0。</param>
    /// <returns>ICO 格式的字节数组</returns>
    internal static byte[] WriteIco(IEnumerable<(byte[] PngBytes, int Width, int Height)> frames)
    {
        var frameList = new List<(byte[] PngBytes, int Width, int Height)>(frames);
        var count = frameList.Count;
        if (count == 0)
            throw new ArgumentException("至少需要一帧图像。");
        if (count > 65535)
            throw new ArgumentException("ICO 最多支持 65535 帧。");

        // ICO header: 6 bytes
        // Directory entry: 16 bytes each
        // PNG data follows
        var headerSize = 6;
        var directorySize = 16 * count;
        var dataOffset = headerSize + directorySize;

        // Calculate total size
        var totalSize = dataOffset;
        foreach (var frame in frameList)
            totalSize += frame.PngBytes.Length;

        var result = new byte[totalSize];
        var pos = 0;

        // ICO header
        result[pos++] = 0; // reserved
        result[pos++] = 0; // reserved
        result[pos++] = 1; // type: ICO
        result[pos++] = 0; // type high byte
        result[pos++] = (byte)(count & 0xFF);
        result[pos++] = (byte)((count >> 8) & 0xFF);

        // Directory entries
        var currentDataOffset = dataOffset;
        for (var i = 0; i < count; i++)
        {
            var frame = frameList[i];
            var width = frame.Width == 256 ? 0 : frame.Width;
            var height = frame.Height == 256 ? 0 : frame.Height;
            var size = frame.PngBytes.Length;

            result[pos++] = (byte)width;          // width
            result[pos++] = (byte)height;          // height
            result[pos++] = 0;                     // color count
            result[pos++] = 0;                     // reserved
            result[pos++] = 1;                     // color planes low
            result[pos++] = 0;                     // color planes high
            result[pos++] = 32;                    // bits per pixel low
            result[pos++] = 0;                     // bits per pixel high
            result[pos++] = (byte)(size & 0xFF);           // data size
            result[pos++] = (byte)((size >> 8) & 0xFF);
            result[pos++] = (byte)((size >> 16) & 0xFF);
            result[pos++] = (byte)((size >> 24) & 0xFF);
            result[pos++] = (byte)(currentDataOffset & 0xFF);           // data offset
            result[pos++] = (byte)((currentDataOffset >> 8) & 0xFF);
            result[pos++] = (byte)((currentDataOffset >> 16) & 0xFF);
            result[pos++] = (byte)((currentDataOffset >> 24) & 0xFF);

            currentDataOffset += size;
        }

        // PNG frame data
        for (var i = 0; i < count; i++)
        {
            var frame = frameList[i];
            Array.Copy(frame.PngBytes, 0, result, pos, frame.PngBytes.Length);
            pos += frame.PngBytes.Length;
        }

        return result;
    }

    /// <summary>
    /// 生成标准 ICO 尺寸列表（16, 32, 48, 64, 128, 256）
    /// </summary>
    internal static int[] GetStandardIcoSizes()
    {
        return new[] { 16, 32, 48, 64, 128, 256 };
    }
}

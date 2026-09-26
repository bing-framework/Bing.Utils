using System.Collections.Generic;
using System.IO;

namespace Bing.Drawing.Internal;

/// <summary>
/// 解析 EXIF/TIFF 目录并清理定位数据。
/// </summary>
internal static class TiffMetadataSanitizer
{
    /// <summary>
    /// 表示 TIFF 小端字节序标记。
    /// </summary>
    private const ushort LittleEndian = 0x4949;

    /// <summary>
    /// 表示 TIFF 大端字节序标记。
    /// </summary>
    private const ushort BigEndian = 0x4D4D;

    /// <summary>
    /// 表示 TIFF 文件格式标识值。
    /// </summary>
    private const ushort Magic = 42;

    /// <summary>
    /// 表示 EXIF 方向标签。
    /// </summary>
    private const ushort OrientationTag = 0x0112;

    /// <summary>
    /// 表示 MPF 图像数量标签。
    /// </summary>
    private const ushort MpfNumberOfImagesTag = 0xB001;

    /// <summary>
    /// 表示 EXIF 子目录指针标签。
    /// </summary>
    private const ushort ExifIfdPointerTag = 0x8769;

    /// <summary>
    /// 表示 GPS 子目录指针标签。
    /// </summary>
    private const ushort GpsIfdPointerTag = 0x8825;

    /// <summary>
    /// 表示互操作子目录指针标签。
    /// </summary>
    private const ushort InteropIfdPointerTag = 0xA005;

    /// <summary>
    /// 验证指定范围内的 TIFF 数据结构。
    /// </summary>
    /// <param name="data">包含 TIFF 数据的字节数组。</param>
    /// <param name="offset">TIFF 数据起始偏移量。</param>
    /// <param name="length">TIFF 数据长度。</param>
    /// <exception cref="InvalidDataException">TIFF 数据结构无效。</exception>
    internal static void Validate(byte[] data, int offset, int length)
    {
        var parser = new Parser(data, offset, length);
        parser.Parse();
    }

    /// <summary>
    /// 读取 TIFF 数据中的 EXIF 方向值。
    /// </summary>
    /// <param name="data">包含 TIFF 数据的字节数组。</param>
    /// <param name="offset">TIFF 数据起始偏移量。</param>
    /// <param name="length">TIFF 数据长度。</param>
    /// <returns>EXIF 方向值。</returns>
    internal static int GetOrientation(byte[] data, int offset, int length)
    {
        var parser = new Parser(data, offset, length);
        parser.Parse();
        return parser.Orientation;
    }

    /// <summary>
    /// 读取 TIFF 数据中的 MPF 帧数。
    /// </summary>
    /// <param name="data">包含 TIFF 数据的字节数组。</param>
    /// <param name="offset">TIFF 数据起始偏移量。</param>
    /// <param name="length">TIFF 数据长度。</param>
    /// <returns>MPF 声明的图像帧数。</returns>
    internal static int GetMpfFrameCount(byte[] data, int offset, int length)
    {
        var parser = new Parser(data, offset, length);
        parser.Parse();
        return parser.MpfFrameCount;
    }

    /// <summary>
    /// 从 TIFF 数据中清理 GPS 目录及其内容。
    /// </summary>
    /// <param name="data">包含 TIFF 数据的字节数组。</param>
    /// <param name="offset">TIFF 数据起始偏移量。</param>
    /// <param name="length">TIFF 数据长度。</param>
    /// <param name="changed">输出是否发现并清理了 GPS 数据。</param>
    /// <returns>清理后的 TIFF 数据；未发现 GPS 数据时返回原数组。</returns>
    internal static byte[] RemoveGps(byte[] data, int offset, int length, out bool changed)
    {
        var parser = new Parser(data, offset, length);
        parser.Parse();
        if (parser.GpsPointers.Count == 0)
        {
            changed = false;
            return data;
        }

        var result = new byte[length];
        Array.Copy(data, offset, result, 0, length);

        foreach (var pointer in parser.GpsPointers)
        {
            var relative = pointer - offset;
            // 保留字段类型和计数，清空标签及偏移，避免生成非法 TIFF 条目。
            result[relative] = 0;
            result[relative + 1] = 0;
            for (var i = 8; i < 12; i++)
                result[relative + i] = 0;
        }

        foreach (var range in parser.GpsRanges)
        {
            var relative = range.Offset - offset;
            for (var i = 0; i < range.Length; i++)
                result[relative + i] = 0;
        }

        changed = true;
        return result;
    }

    /// <summary>
    /// 设置 TIFF 数据中的 EXIF 方向值。
    /// </summary>
    /// <param name="data">包含 TIFF 数据的字节数组。</param>
    /// <param name="offset">TIFF 数据起始偏移量。</param>
    /// <param name="length">TIFF 数据长度。</param>
    /// <param name="orientation">新的方向值，范围为 1-8。</param>
    /// <param name="changed">输出方向值是否发生变化。</param>
    /// <returns>更新后的 TIFF 数据；无需更新时返回原数组。</returns>
    internal static byte[] SetOrientation(byte[] data, int offset, int length, int orientation, out bool changed)
    {
        if (orientation < 1 || orientation > 8)
            throw new ArgumentOutOfRangeException(nameof(orientation));

        var parser = new Parser(data, offset, length);
        parser.Parse();
        if (parser.OrientationEntryOffset < 0)
        {
            changed = false;
            return data;
        }

        var result = new byte[length];
        Array.Copy(data, offset, result, 0, length);
        var relative = parser.OrientationEntryOffset - offset;
        var oldValue = parser.Orientation;
        if (oldValue == orientation)
        {
            changed = false;
            return data;
        }

        if (parser.OrientationIsBigEndian)
        {
            result[relative + 8] = (byte)(orientation >> 8);
            result[relative + 9] = (byte)orientation;
        }
        else
        {
            result[relative + 8] = (byte)orientation;
            result[relative + 9] = (byte)(orientation >> 8);
        }

        changed = true;
        return result;
    }

    /// <summary>
    /// 表示 TIFF 数据中的连续字节范围。
    /// </summary>
    private readonly struct Range
    {
        /// <summary>
        /// 初始化 <see cref="Range" /> 结构的新实例。
        /// </summary>
        /// <param name="offset">范围起始偏移量。</param>
        /// <param name="length">范围长度。</param>
        internal Range(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// 获取范围起始偏移量。
        /// </summary>
        internal int Offset { get; }

        /// <summary>
        /// 获取范围长度。
        /// </summary>
        internal int Length { get; }
    }

    /// <summary>
    /// 解析 TIFF 目录并收集元数据位置。
    /// </summary>
    private sealed class Parser
    {
        /// <summary>
        /// 保存待解析的原始数据。
        /// </summary>
        private readonly byte[] _data;

        /// <summary>
        /// 保存 TIFF 数据在原数组中的起始偏移量。
        /// </summary>
        private readonly int _offset;

        /// <summary>
        /// 保存 TIFF 数据长度。
        /// </summary>
        private readonly int _length;

        /// <summary>
        /// 记录已访问的 IFD 相对偏移量。
        /// </summary>
        private readonly HashSet<int> _visitedIfds = new();

        /// <summary>
        /// 保存 GPS IFD 指针条目的绝对偏移量。
        /// </summary>
        private readonly List<int> _gpsPointers = new();

        /// <summary>
        /// 保存 GPS 数据范围。
        /// </summary>
        private readonly List<Range> _gpsRanges = new();

        /// <summary>
        /// 指示当前 TIFF 数据是否使用大端字节序。
        /// </summary>
        private bool _bigEndian;

        /// <summary>
        /// 保存方向条目的绝对偏移量；未找到时为 -1。
        /// </summary>
        private int _orientationEntryOffset = -1;

        /// <summary>
        /// 保存解析到的方向值。
        /// </summary>
        private int _orientation = 1;

        /// <summary>
        /// 保存解析到的 MPF 帧数。
        /// </summary>
        private int _mpfFrameCount = 1;

        /// <summary>
        /// 初始化 <see cref="Parser" /> 类的新实例。
        /// </summary>
        /// <param name="data">包含 TIFF 数据的字节数组。</param>
        /// <param name="offset">TIFF 数据起始偏移量。</param>
        /// <param name="length">TIFF 数据长度。</param>
        internal Parser(byte[] data, int offset, int length)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            if (offset < 0 || length < 0 || offset > data.Length - length)
                throw new InvalidDataException("TIFF 数据范围无效。");
            _offset = offset;
            _length = length;
        }

        /// <summary>
        /// 获取 GPS IFD 指针条目偏移量。
        /// </summary>
        internal List<int> GpsPointers => _gpsPointers;

        /// <summary>
        /// 获取 GPS 数据范围。
        /// </summary>
        internal List<Range> GpsRanges => _gpsRanges;

        /// <summary>
        /// 获取解析到的方向值。
        /// </summary>
        internal int Orientation => _orientation;

        /// <summary>
        /// 获取方向条目偏移量。
        /// </summary>
        internal int OrientationEntryOffset => _orientationEntryOffset;

        /// <summary>
        /// 获取方向条目使用的字节序。
        /// </summary>
        internal bool OrientationIsBigEndian => _bigEndian;

        /// <summary>
        /// 获取解析到的 MPF 帧数。
        /// </summary>
        internal int MpfFrameCount => _mpfFrameCount;

        /// <summary>
        /// 解析 TIFF 头部和目录结构。
        /// </summary>
        /// <exception cref="InvalidDataException">TIFF 结构无效。</exception>
        internal void Parse()
        {
            if (_length < 8)
                throw new InvalidDataException("TIFF 头部不完整。");

            var byteOrder = ReadUInt16(0);
            if (byteOrder == BigEndian)
                _bigEndian = true;
            else if (byteOrder == LittleEndian)
                _bigEndian = false;
            else
                throw new InvalidDataException("TIFF 字节序无效。");

            if (ReadUInt16(2) != Magic)
                throw new InvalidDataException("TIFF magic 无效。");

            var ifdOffset = ReadOffset(4);
            if (ifdOffset == 0)
                throw new InvalidDataException("TIFF 缺少 IFD0。");
            ParseIfd(ifdOffset, false, true);
        }

        /// <summary>
        /// 递归解析指定的 IFD 目录。
        /// </summary>
        /// <param name="relativeOffset">IFD 相对于 TIFF 起点的偏移量。</param>
        /// <param name="isGps">是否为 GPS 目录。</param>
        /// <param name="isRoot">是否为根目录。</param>
        private void ParseIfd(int relativeOffset, bool isGps, bool isRoot)
        {
            if (relativeOffset <= 0)
                throw new InvalidDataException("TIFF IFD 偏移无效。");
            EnsureRange(relativeOffset, 2);
            if (!_visitedIfds.Add(relativeOffset))
                throw new InvalidDataException("TIFF IFD 存在循环引用。");

            var count = ReadUInt16(relativeOffset);
            var tableLength = CheckedAdd(2, CheckedMultiply(count, 12));
            tableLength = CheckedAdd(tableLength, 4);
            EnsureRange(relativeOffset, tableLength);
            if (isGps)
                _gpsRanges.Add(new Range(ToAbsolute(relativeOffset), tableLength));

            var entriesOffset = CheckedAdd(relativeOffset, 2);
            for (var i = 0; i < count; i++)
            {
                var entryOffset = CheckedAdd(entriesOffset, CheckedMultiply(i, 12));
                var tag = ReadUInt16(entryOffset);
                var type = ReadUInt16(entryOffset + 2);
                var itemCount = ReadUInt32(entryOffset + 4);
                var itemSize = TypeSize(type);
                var valueLength = CheckedMultiply(itemCount, itemSize);

                if (valueLength > 4)
                {
                    var valueOffset = ReadOffset(entryOffset + 8);
                    EnsureRange(valueOffset, valueLength);
                    if (isGps)
                        _gpsRanges.Add(new Range(ToAbsolute(valueOffset), valueLength));
                }
                else if (isGps && valueLength > 0)
                {
                    _gpsRanges.Add(new Range(ToAbsolute(entryOffset + 8), valueLength));
                }

                if (isRoot && tag == OrientationTag)
                {
                    if (type != 3 || itemCount != 1)
                        throw new InvalidDataException("EXIF Orientation 字段格式无效。");
                    _orientationEntryOffset = ToAbsolute(entryOffset);
                    _orientation = ReadUInt16(entryOffset + 8);
                    if (_orientation < 1 || _orientation > 8)
                        throw new InvalidDataException("EXIF Orientation 值无效。");
                }

                if (isRoot && tag == MpfNumberOfImagesTag)
                {
                    if (type != 4 || itemCount != 1)
                        throw new InvalidDataException("MPF NumberOfImages 字段格式无效。");
                    var frameCount = ReadUInt32(entryOffset + 8);
                    if (frameCount == 0 || frameCount > int.MaxValue)
                        throw new InvalidDataException("MPF NumberOfImages 值无效。");
                    _mpfFrameCount = (int)frameCount;
                }

                if (tag == GpsIfdPointerTag)
                {
                    if (type != 4 || itemCount != 1)
                        throw new InvalidDataException("EXIF GPS IFD 指针格式无效。");
                    var gpsOffset = ReadOffset(entryOffset + 8);
                    if (gpsOffset == 0)
                        throw new InvalidDataException("EXIF GPS IFD 指针为空。");
                    _gpsPointers.Add(ToAbsolute(entryOffset));
                    ParseIfd(gpsOffset, true, false);
                }
                else if (tag == ExifIfdPointerTag || tag == InteropIfdPointerTag)
                {
                    if (type != 4 || itemCount != 1)
                        throw new InvalidDataException("EXIF IFD 指针格式无效。");
                    var childOffset = ReadOffset(entryOffset + 8);
                    if (childOffset == 0)
                        throw new InvalidDataException("EXIF IFD 指针为空。");
                    ParseIfd(childOffset, false, false);
                }
            }

            var nextOffset = ReadOffset(CheckedAdd(relativeOffset, tableLength - 4));
            if (nextOffset != 0)
                ParseIfd(nextOffset, isGps, false);
        }

        /// <summary>
        /// 按当前字节序读取无符号 16 位整数。
        /// </summary>
        /// <param name="relativeOffset">相对于 TIFF 起点的偏移量。</param>
        /// <returns>读取到的整数。</returns>
        private ushort ReadUInt16(int relativeOffset)
        {
            EnsureRange(relativeOffset, 2);
            var absolute = ToAbsolute(relativeOffset);
            if (_bigEndian)
                return (ushort)((_data[absolute] << 8) | _data[absolute + 1]);
            return (ushort)(_data[absolute] | (_data[absolute + 1] << 8));
        }

        /// <summary>
        /// 按当前字节序读取无符号 32 位整数。
        /// </summary>
        /// <param name="relativeOffset">相对于 TIFF 起点的偏移量。</param>
        /// <returns>读取到的整数。</returns>
        private uint ReadUInt32(int relativeOffset)
        {
            EnsureRange(relativeOffset, 4);
            var absolute = ToAbsolute(relativeOffset);
            if (_bigEndian)
            {
                return (uint)((_data[absolute] << 24) | (_data[absolute + 1] << 16) |
                    (_data[absolute + 2] << 8) | _data[absolute + 3]);
            }

            return (uint)(_data[absolute] | (_data[absolute + 1] << 8) |
                (_data[absolute + 2] << 16) | (_data[absolute + 3] << 24));
        }

        /// <summary>
        /// 读取并验证 TIFF 偏移量。
        /// </summary>
        /// <param name="relativeOffset">偏移字段的相对位置。</param>
        /// <returns>读取到的相对偏移量。</returns>
        private int ReadOffset(int relativeOffset)
        {
            var value = ReadUInt32(relativeOffset);
            if (value > int.MaxValue)
                throw new InvalidDataException("TIFF 偏移超出支持范围。");
            return (int)value;
        }

        /// <summary>
        /// 将 TIFF 相对偏移量转换为原数组绝对偏移量。
        /// </summary>
        /// <param name="relativeOffset">相对偏移量。</param>
        /// <returns>原数组中的绝对偏移量。</returns>
        private int ToAbsolute(int relativeOffset)
        {
            var absolute = (long)_offset + relativeOffset;
            if (absolute < 0 || absolute > int.MaxValue)
                throw new InvalidDataException("TIFF 偏移无效。");
            return (int)absolute;
        }

        /// <summary>
        /// 验证相对范围位于 TIFF 数据边界内。
        /// </summary>
        /// <param name="relativeOffset">范围起始偏移量。</param>
        /// <param name="length">范围长度。</param>
        private void EnsureRange(int relativeOffset, int length)
        {
            if (relativeOffset < 0 || length < 0 || relativeOffset > _length - length)
                throw new InvalidDataException("TIFF 数据范围超出边界。");
        }

        /// <summary>
        /// 计算并校验 TIFF 长度之和。
        /// </summary>
        /// <param name="left">左操作数。</param>
        /// <param name="right">右操作数。</param>
        /// <returns>相加结果。</returns>
        private static int CheckedAdd(int left, int right)
        {
            var result = (long)left + right;
            if (result > int.MaxValue)
                throw new InvalidDataException("TIFF 长度超出支持范围。");
            return (int)result;
        }

        /// <summary>
        /// 计算并校验 TIFF 长度乘积。
        /// </summary>
        /// <param name="left">左操作数。</param>
        /// <param name="right">右操作数。</param>
        /// <returns>相乘结果。</returns>
        private static int CheckedMultiply(int left, int right)
        {
            var result = (long)left * right;
            if (result > int.MaxValue)
                throw new InvalidDataException("TIFF 长度超出支持范围。");
            return (int)result;
        }

        /// <summary>
        /// 计算并校验 TIFF 长度乘积。
        /// </summary>
        /// <param name="left">左操作数。</param>
        /// <param name="right">右操作数。</param>
        /// <returns>相乘结果。</returns>
        private static int CheckedMultiply(uint left, int right)
        {
            var result = (ulong)left * (uint)right;
            if (result > int.MaxValue)
                throw new InvalidDataException("TIFF 长度超出支持范围。");
            return (int)result;
        }

        /// <summary>
        /// 获取 TIFF 字段类型的字节大小。
        /// </summary>
        /// <param name="type">TIFF 字段类型编号。</param>
        /// <returns>单个字段值占用的字节数。</returns>
        private static int TypeSize(ushort type)
        {
            switch (type)
            {
                case 1: // BYTE
                case 2: // ASCII
                case 7: // UNDEFINED
                    return 1;
                case 3: // SHORT
                    return 2;
                case 4: // LONG
                case 9: // SLONG
                case 11: // FLOAT
                    return 4;
                case 5: // RATIONAL
                case 10: // SRATIONAL
                case 12: // DOUBLE
                    return 8;
                default:
                    throw new InvalidDataException("TIFF 字段类型无效。");
            }
        }
    }
}

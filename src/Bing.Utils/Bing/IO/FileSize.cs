namespace Bing.IO;

/// <summary>
/// 文件大小
/// </summary>
public readonly struct FileSize : IEquatable<FileSize>, IComparable<FileSize>
{
    #region 常量

    /// <summary>
    /// 1KB 对应的字节数
    /// </summary>
    internal const long KiloByteSize = 1024L;

    /// <summary>
    /// 1MB 对应的字节数
    /// </summary>
    internal const long MegaByteSize = KiloByteSize * 1024L;

    /// <summary>
    /// 1GB 对应的字节数
    /// </summary>
    internal const long GigaByteSize = MegaByteSize * 1024L;

    /// <summary>
    /// 1TB 对应的字节数
    /// </summary>
    internal const long TeraByteSize = GigaByteSize * 1024L;

    /// <summary>
    /// 1PB 对应的字节数
    /// </summary>
    internal const long PetaByteSize = TeraByteSize * 1024L;

    #endregion

    /// <summary>
    /// 文件字节长度
    /// </summary>
    public long Size { get; }

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="FileSize"/>类型的实例
    /// </summary>
    /// <param name="size">文件大小</param>
    /// <param name="unit">文件大小单位</param>
    /// <exception cref="ArgumentOutOfRangeException">当文件大小为负数时抛出</exception>
    /// <exception cref="OverflowException">当计算结果超出长整型范围时抛出</exception>
    public FileSize(long size, FileSizeUnit unit = FileSizeUnit.Byte)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "文件大小不能为负数");
        Size = GetSizeInBytes(size, unit);
    }

    /// <summary>
    /// 根据指定单位计算字节大小
    /// </summary>
    /// <param name="size">原始大小值</param>
    /// <param name="unit">单位类型</param>
    /// <returns>字节大小</returns>
    /// <exception cref="OverflowException">当计算结果超出长整型范围时抛出</exception>
    private static long GetSizeInBytes(long size, FileSizeUnit unit)
    {
        return unit switch
        {
            FileSizeUnit.K => checked(size * KiloByteSize),
            FileSizeUnit.M => checked(size * MegaByteSize),
            FileSizeUnit.G => checked(size * GigaByteSize),
            FileSizeUnit.T => checked(size * TeraByteSize),
            FileSizeUnit.P => checked(size * PetaByteSize),
            _ => size
        };
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 从字节数创建文件大小实例
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>文件大小实例</returns>
    /// <exception cref="ArgumentOutOfRangeException">当字节数为负数时抛出</exception>
    public static FileSize FromBytes(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "字节数不能为负数");
        return new FileSize(bytes, FileSizeUnit.Byte);
    }

    /// <summary>
    /// 从文件大小字符串解析创建实例
    /// </summary>
    /// <param name="sizeString">文件大小字符串（如 "1.5 GB"）</param>
    /// <returns>文件大小实例</returns>
    /// <exception cref="ArgumentException">当字符串格式无效时抛出</exception>
    public static FileSize Parse(string sizeString)
    {
        if (!TryParse(sizeString, out var result))
            throw new ArgumentException($"无法解析文件大小字符串: {sizeString}", nameof(sizeString));
        return result;
    }

    /// <summary>
    /// 尝试从文件大小字符串解析创建实例
    /// </summary>
    /// <param name="sizeString">文件大小字符串（如 "1.5 GB"）</param>
    /// <param name="fileSize">解析出的文件大小实例</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParse(string sizeString, out FileSize fileSize)
    {
        fileSize = default;
        if (FileSizeHelper.TryParseSize(sizeString, out var bytes))
        {
            fileSize = FromBytes(bytes);
            return true;
        }
        return false;
    }

    #endregion

    #region 单位转换

    /// <summary>
    /// 获取文件大小，单位：字节
    /// </summary>
    public int GetSize() => Size > int.MaxValue ? int.MaxValue : (int)Size;

    /// <summary>
    /// 获取文件大小，单位：字节
    /// </summary>
    /// <returns>文件大小的字节数，若超出整型范围则返回整型最大值</returns>
    public long GetLongSize() => Size;

    /// <summary>
    /// 获取指定单位的文件大小
    /// </summary>
    /// <param name="unit">目标单位</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>指定单位的文件大小</returns>
    public double GetSize(FileSizeUnit unit, int precision = 2) => unit.ConvertFromBytes(Size, precision);

    /// <summary>
    /// 获取文件大小，单位：K
    /// </summary>
    /// <returns>以KB为单位的文件大小，保留2位小数</returns>
    public double GetSizeByK() => GetSize(FileSizeUnit.K);

    /// <summary>
    /// 获取文件大小，单位：M
    /// </summary>
    /// <returns>以MB为单位的文件大小，保留2位小数</returns>
    public double GetSizeByM() => GetSize(FileSizeUnit.M);

    /// <summary>
    /// 获取文件大小，单位：G
    /// </summary>
    /// <returns>以GB为单位的文件大小，保留2位小数</returns>
    public double GetSizeByG() => GetSize(FileSizeUnit.G);

    /// <summary>
    /// 获取文件大小，单位：T
    /// </summary>
    /// <returns>以TB为单位的文件大小，保留2位小数</returns>
    public double GetSizeByT() => GetSize(FileSizeUnit.T);

    /// <summary>
    /// 获取文件大小，单位：P
    /// </summary>
    /// <returns>以PB为单位的文件大小，保留2位小数</returns>
    public double GetSizeByP() => GetSize(FileSizeUnit.P);

    #endregion

    /// <summary>
    /// 获取最适合的单位及其对应的大小值
    /// </summary>
    /// <returns>包含大小值和单位的元组</returns>
    public (double Value, FileSizeUnit Unit) GetOptimalUnit()
    {
        var unit = FileSizeHelper.GetBestUnit(Size);
        var value = unit.ConvertFromBytes(Size);
        return (value, unit);
    }

    #region 字符串重载

    /// <summary>
    /// 格式化为指定单位的字符串
    /// </summary>
    /// <param name="unit">目标单位</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>格式化后的字符串</returns>
    public string ToString(FileSizeUnit unit, int precision = 2)
    {
        var value = unit.ConvertFromBytes(Size, precision);
        return unit.FormatSize(value, precision);
    }

    /// <summary>
    /// 自动选择最佳单位格式化字符串
    /// </summary>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>格式化后的字符串</returns>
    public string ToString(int precision) => FileSizeHelper.AutoFormat(Size, precision);

    /// <summary>
    /// 输出描述
    /// </summary>
    /// <returns>格式化的文件大小字符串</returns>
    public override string ToString() => ToString(2);

    #endregion

    #region 运算符重载

    /// <summary>
    /// 加法运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>相加后的文件大小</returns>
    public static FileSize operator +(FileSize left, FileSize right) => FromBytes(left.Size + right.Size);

    /// <summary>
    /// 减法运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>相减后的文件大小</returns>
    /// <exception cref="ArgumentOutOfRangeException">当结果为负数时抛出</exception>
    public static FileSize operator -(FileSize left, FileSize right)
    {
        var result = left.Size - right.Size;
        if (result < 0)
            throw new ArgumentOutOfRangeException(nameof(right), "减法运算结果不能为负数");
        return FromBytes(result);
    }

    /// <summary>
    /// 乘法运算符重载
    /// </summary>
    /// <param name="fileSize">文件大小</param>
    /// <param name="multiplier">倍数</param>
    /// <returns>相乘后的文件大小</returns>
    /// <exception cref="ArgumentOutOfRangeException">当倍数为负数时抛出</exception>
    public static FileSize operator *(FileSize fileSize, double multiplier)
    {
        if (multiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "倍数不能为负数");
        return FromBytes((long)(fileSize.Size * multiplier));
    }

    /// <summary>
    /// 除法运算符重载
    /// </summary>
    /// <param name="fileSize">文件大小</param>
    /// <param name="divisor">除数</param>
    /// <returns>相除后的文件大小</returns>
    /// <exception cref="ArgumentOutOfRangeException">当除数小于等于0时抛出</exception>
    public static FileSize operator /(FileSize fileSize, double divisor)
    {
        if (divisor <= 0)
            throw new ArgumentOutOfRangeException(nameof(divisor), "除数必须大于0");
        return FromBytes((long)(fileSize.Size / divisor));
    }

    #endregion

    #region 类型转换

    /// <summary>
    /// 隐式转换：从长整型转换为文件大小（按字节计算）
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>文件大小实例</returns>
    public static implicit operator FileSize(long bytes) => FromBytes(bytes);

    /// <summary>
    /// 显式转换：从文件大小转换为长整型（字节数）
    /// </summary>
    /// <param name="fileSize">文件大小</param>
    /// <returns>字节数</returns>
    public static explicit operator long(FileSize fileSize) => fileSize.Size;

    #endregion

    #region IEquatable<FileSize> 和 IComparable<FileSize> 实现

    /// <summary>
    /// 判断两个文件大小是否相等
    /// </summary>
    /// <param name="other">要比较的文件大小</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
    public bool Equals(FileSize other) => Size == other.Size;

    /// <summary>
    /// 判断与指定对象是否相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
    public override bool Equals(object? obj) => obj is FileSize other && Equals(other);

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode() => Size.GetHashCode();

    /// <summary>
    /// 比较两个文件大小
    /// </summary>
    /// <param name="other">要比较的文件大小</param>
    /// <returns>比较结果：小于返回负数，等于返回0，大于返回正数</returns>
    public int CompareTo(FileSize other) => Size.CompareTo(other.Size);

    /// <summary>
    /// 相等运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
    public static bool operator ==(FileSize left, FileSize right) => left.Equals(right);

    /// <summary>
    /// 不等运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>如果不相等则返回true，否则返回false</returns>
    public static bool operator !=(FileSize left, FileSize right) => !(left == right);

    /// <summary>
    /// 大于运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>如果左操作数大于右操作数则返回true，否则返回false</returns>
    public static bool operator >(FileSize left, FileSize right) => left.CompareTo(right) > 0;

    /// <summary>
    /// 小于运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>如果左操作数小于右操作数则返回true，否则返回false</returns>
    public static bool operator <(FileSize left, FileSize right) => left.CompareTo(right) < 0;

    /// <summary>
    /// 大于等于运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>如果左操作数大于等于右操作数则返回true，否则返回false</returns>
    public static bool operator >=(FileSize left, FileSize right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// 小于等于运算符重载
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>如果左操作数小于等于右操作数则返回true，否则返回false</returns>
    public static bool operator <=(FileSize left, FileSize right) => left.CompareTo(right) <= 0;

    #endregion

}
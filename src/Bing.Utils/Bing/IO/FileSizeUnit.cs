using System.ComponentModel;
using Bing.Extensions;

namespace Bing.IO;

/// <summary>
/// 文件大小单位
/// </summary>
public enum FileSizeUnit
{
    /// <summary>
    /// 字节
    /// </summary>
    [Description("B")]
    Byte = 0,

    /// <summary>
    /// K字节
    /// </summary>
    [Description("KB")]
    K = 1,

    /// <summary>
    /// M字节
    /// </summary>
    [Description("MB")]
    M = 2,

    /// <summary>
    /// G字节
    /// </summary>
    [Description("GB")]
    G = 3,

    /// <summary>
    /// T字节
    /// </summary>
    [Description("TB")]
    T = 4,

    /// <summary>
    /// P字节
    /// </summary>
    [Description("PB")]
    P = 5
}

/// <summary>
/// 文件大小单位枚举扩展
/// </summary>
public static class FileSizeUnitExtensions
{
    /// <summary>
    /// 基础字节数（1024）
    /// </summary>
    private const long ByteBase = 1024L;

    /// <summary>
    /// 获取描述
    /// </summary>
    /// <param name="unit">文件大小单位</param>
    public static string Description(this FileSizeUnit? unit) => unit == null ? string.Empty : unit.Value.Description();

    /// <summary>
    /// 获取值
    /// </summary>
    /// <param name="unit">文件大小单位</param>
    public static int? Value(this FileSizeUnit? unit) => unit?.Value();

    /// <summary>
    /// 获取单位对应的字节倍数
    /// </summary>
    /// <param name="unit">文件大小单位</param>
    /// <returns>字节倍数</returns>
    public static long GetByteMultiplier(this FileSizeUnit unit)
    {
        return unit switch
        {
            FileSizeUnit.Byte => 1L,
            FileSizeUnit.K => ByteBase,
            FileSizeUnit.M => ByteBase * ByteBase,
            FileSizeUnit.G => ByteBase * ByteBase * ByteBase,
            FileSizeUnit.T => ByteBase * ByteBase * ByteBase * ByteBase,
            FileSizeUnit.P => ByteBase * ByteBase * ByteBase * ByteBase * ByteBase,
            _ => 1L
        };
    }

    /// <summary>
    /// 将字节数转换为指定单位的数值
    /// </summary>
    /// <param name="unit">目标单位</param>
    /// <param name="bytes">字节数</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>转换后的数值</returns>
    public static double ConvertFromBytes(this FileSizeUnit unit, long bytes, int precision = 2)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "字节数不能为负数");
        if (precision < 0)
            throw new ArgumentOutOfRangeException(nameof(precision), "精度不能为负数");

        var multiplier = unit.GetByteMultiplier();
        var result = (double)bytes / multiplier;
        return Math.Round(result, precision);
    }

    /// <summary>
    /// 将指定单位的数值转换为字节数
    /// </summary>
    /// <param name="unit">源单位</param>
    /// <param name="value">数值</param>
    /// <returns>字节数</returns>
    public static long ConvertToBytes(this FileSizeUnit unit, double value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "数值不能为负数");

        var multiplier = unit.GetByteMultiplier();
        return (long)(value * multiplier);
    }

    /// <summary>
    /// 自动选择最合适的文件大小单位
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>最合适的单位</returns>
    public static FileSizeUnit GetBestUnit(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "字节数不能为负数");

        if (bytes < ByteBase) return FileSizeUnit.Byte;
        if (bytes < ByteBase * ByteBase) return FileSizeUnit.K;
        if (bytes < ByteBase * ByteBase * ByteBase) return FileSizeUnit.M;
        if (bytes < ByteBase * ByteBase * ByteBase * ByteBase) return FileSizeUnit.G;
        if (bytes < ByteBase * ByteBase * ByteBase * ByteBase * ByteBase) return FileSizeUnit.T;

        return FileSizeUnit.P;
    }

    /// <summary>
    /// 格式化文件大小为字符串
    /// </summary>
    /// <param name="unit">文件大小单位</param>
    /// <param name="value">数值</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>格式化后的字符串</returns>
    public static string FormatSize(this FileSizeUnit unit, double value, int precision = 2)
    {
        if (precision < 0)
            throw new ArgumentOutOfRangeException(nameof(precision), "精度不能为负数");

        var formattedValue = Math.Round(value, precision).ToString($"F{precision}");
        var unitDescription = unit.Description();
        return $"{formattedValue} {unitDescription}";
    }

    /// <summary>
    /// 自动格式化字节数为最合适的单位字符串
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>格式化后的字符串</returns>
    public static string AutoFormat(long bytes, int precision = 2)
    {
        var bestUnit = GetBestUnit(bytes);
        var value = bestUnit.ConvertFromBytes(bytes, precision);
        return bestUnit.FormatSize(value, precision);
    }

    /// <summary>
    /// 尝试解析文件大小字符串
    /// </summary>
    /// <param name="sizeString">文件大小字符串（如 "1.5 GB"）</param>
    /// <param name="bytes">解析出的字节数</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParseSize(string sizeString, out long bytes)
    {
        bytes = 0;

        if (string.IsNullOrWhiteSpace(sizeString))
            return false;

        var parts = sizeString.Trim().Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!double.TryParse(parts[0], out var value))
            return false;

        var unitString = parts[1].ToUpperInvariant();

        // 尝试匹配单位
        var enumValues = (FileSizeUnit[])Enum.GetValues(typeof(FileSizeUnit));
        foreach (var unit in enumValues)
        {
            if (unit.Description().ToUpperInvariant() == unitString)
            {
                try
                {
                    bytes = unit.ConvertToBytes(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        return false;
    }
}
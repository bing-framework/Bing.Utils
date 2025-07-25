using Bing.Extensions;
using System.ComponentModel;
using System.Globalization;

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
    /// 获取描述
    /// </summary>
    /// <param name="unit">文件大小单位</param>
    /// <returns>单位描述字符串</returns>
    public static string Description(this FileSizeUnit? unit) => unit?.Description() ?? string.Empty;

    /// <summary>
    /// 获取值
    /// </summary>
    /// <param name="unit">文件大小单位</param>
    /// <returns>枚举值</returns>
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
            FileSizeUnit.K => FileSize.KiloByteSize,
            FileSizeUnit.M => FileSize.MegaByteSize,
            FileSizeUnit.G => FileSize.GigaByteSize,
            FileSizeUnit.T => FileSize.TeraByteSize,
            FileSizeUnit.P => FileSize.PetaByteSize,
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
    /// <exception cref="ArgumentOutOfRangeException">当字节数或精度为负数时抛出</exception>
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
    /// <exception cref="ArgumentOutOfRangeException">当数值为负数时抛出</exception>
    /// <exception cref="OverflowException">当计算结果溢出时抛出</exception>
    public static long ConvertToBytes(this FileSizeUnit unit, double value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "数值不能为负数");

        var multiplier = unit.GetByteMultiplier();
        try
        {
            return checked((long)(value * multiplier));
        }
        catch (OverflowException)
        {
            throw new OverflowException($"转换结果超出长整型范围: {value} {unit.Description()}");
        }
    }

    /// <summary>
    /// 格式化文件大小为字符串
    /// </summary>
    /// <param name="unit">文件大小单位</param>
    /// <param name="value">数值</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>格式化后的字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当精度为负数时抛出</exception>
    public static string FormatSize(this FileSizeUnit unit, double value, int precision = 2)
    {
        if (precision < 0)
            throw new ArgumentOutOfRangeException(nameof(precision), "精度不能为负数");

        var roundedValue = Math.Round(value, precision);
        var formatString = precision == 0 ? "F0" : $"F{precision}";
        var formattedValue = roundedValue.ToString(formatString, CultureInfo.InvariantCulture);

        // 移除不必要的尾随零
        if (precision > 0 && formattedValue.Contains('.'))
            formattedValue = formattedValue.TrimEnd('0').TrimEnd('.');
        return $"{formattedValue} {unit.Description()}";
    }
}
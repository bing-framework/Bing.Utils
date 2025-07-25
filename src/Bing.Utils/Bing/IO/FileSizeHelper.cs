using System.Globalization;
using System.Text.RegularExpressions;

namespace Bing.IO;

/// <summary>
/// 文件大小辅助工具类
/// </summary>
public static class FileSizeHelper
{
    #region 私有字段

    /// <summary>
    /// 用于解析文件大小字符串的正则表达式
    /// </summary>
    private static readonly Regex SizeParseRegex = new(@"^\s*(?<value>[0-9]+(?:\.[0-9]+)?)\s*(?<unit>B|KB|MB|GB|TB|PB)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 单位描述映射表
    /// </summary>
    private static readonly Dictionary<string, FileSizeUnit> UnitMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "B", FileSizeUnit.Byte },
        { "KB", FileSizeUnit.K },
        { "MB", FileSizeUnit.M },
        { "GB", FileSizeUnit.G },
        { "TB", FileSizeUnit.T },
        { "PB", FileSizeUnit.P }
    };

    #endregion

    #region 快速创建方法

    /// <summary>
    /// 创建字节大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>文件大小实例</returns>
    public static FileSize Bytes(long bytes) => FileSize.FromBytes(bytes);

    /// <summary>
    /// 创建KB大小
    /// </summary>
    /// <param name="kilobytes">KB数</param>
    /// <returns>文件大小实例</returns>
    public static FileSize KB(double kilobytes) => new((long)kilobytes, FileSizeUnit.K);

    /// <summary>
    /// 创建MB大小
    /// </summary>
    /// <param name="megabytes">MB数</param>
    /// <returns>文件大小实例</returns>
    public static FileSize MB(double megabytes) => new((long)megabytes, FileSizeUnit.M);

    /// <summary>
    /// 创建GB大小
    /// </summary>
    /// <param name="gigabytes">GB数</param>
    /// <returns>文件大小实例</returns>
    public static FileSize GB(double gigabytes) => new((long)gigabytes, FileSizeUnit.G);

    /// <summary>
    /// 创建TB大小
    /// </summary>
    /// <param name="terabytes">TB数</param>
    /// <returns>文件大小实例</returns>
    public static FileSize TB(double terabytes) => new((long)terabytes, FileSizeUnit.T);

    /// <summary>
    /// 创建PB大小
    /// </summary>
    /// <param name="petabytes">PB数</param>
    /// <returns>文件大小实例</returns>
    public static FileSize PB(double petabytes) => new((long)petabytes, FileSizeUnit.P);

    #endregion

    #region Parse(解析)

    /// <summary>
    /// 解析文件大小字符串
    /// </summary>
    /// <param name="sizeString">文件大小字符串（如 "1.5 GB"）</param>
    /// <returns>文件大小实例</returns>
    /// <exception cref="ArgumentException">当字符串格式无效时抛出</exception>
    public static FileSize Parse(string sizeString) => FileSize.Parse(sizeString);

    /// <summary>
    /// 尝试解析文件大小字符串
    /// </summary>
    /// <param name="sizeString">文件大小字符串（如 "1.5 GB"）</param>
    /// <param name="fileSize">解析出的文件大小实例</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParse(string sizeString, out FileSize fileSize) => FileSize.TryParse(sizeString, out fileSize);

    /// <summary>
    /// 解析文件大小字符串为字节数
    /// </summary>
    /// <param name="sizeString">文件大小字符串（如 "1.5 GB"）</param>
    /// <returns>解析出的字节数</returns>
    /// <exception cref="ArgumentException">当字符串格式无效时抛出</exception>
    public static long ParseSize(string sizeString)
    {
        if (!TryParseSize(sizeString, out var bytes))
            throw new ArgumentException($"无法解析文件大小字符串: {sizeString}", nameof(sizeString));
        return bytes;
    }

    /// <summary>
    /// 尝试解析文件大小字符串为字节数
    /// </summary>
    /// <param name="sizeString">文件大小字符串（如 "1.5 GB"）</param>
    /// <param name="bytes">解析出的字节数</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParseSize(string sizeString, out long bytes)
    {
        bytes = 0;

        if (string.IsNullOrWhiteSpace(sizeString))
            return false;

        var match = SizeParseRegex.Match(sizeString.Trim());
        if (!match.Success)
            return false;

        if (!double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return false;

        var unitString = match.Groups["unit"].Value;
        if (!UnitMappings.TryGetValue(unitString, out var unit))
            return false;

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

    #endregion

    #region Format(格式化)

    /// <summary>
    /// 自动格式化字节数为最合适的单位字符串
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>格式化后的字符串</returns>
    public static string Format(long bytes, int precision = 2) => AutoFormat(bytes, precision);

    /// <summary>
    /// 格式化文件大小为指定单位的字符串
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="unit">目标单位</param>
    /// <param name="precision">小数位数，默认为2</param>
    /// <returns>格式化后的字符串</returns>
    public static string Format(long bytes, FileSizeUnit unit, int precision = 2)
    {
        var value = unit.ConvertFromBytes(bytes, precision);
        return unit.FormatSize(value, precision);
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

    #endregion

    #region 转换方法

    /// <summary>
    /// 转换字节数到指定单位
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="unit">目标单位</param>
    /// <param name="precision">精度</param>
    /// <returns>转换后的数值</returns>
    public static double Convert(long bytes, FileSizeUnit unit, int precision = 2) => unit.ConvertFromBytes(bytes, precision);

    /// <summary>
    /// 获取最适合的单位
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>最适合的单位</returns>
    /// <exception cref="ArgumentOutOfRangeException">当字节数为负数时抛出</exception>
    public static FileSizeUnit GetBestUnit(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "字节数不能为负数");

        return bytes switch
        {
            < FileSize.KiloByteSize => FileSizeUnit.Byte,
            < FileSize.MegaByteSize => FileSizeUnit.K,
            < FileSize.GigaByteSize => FileSizeUnit.M,
            < FileSize.TeraByteSize => FileSizeUnit.G,
            < FileSize.PetaByteSize => FileSizeUnit.T,
            _ => FileSizeUnit.P
        };
    }

    #endregion

    #region 单位工具方法

    /// <summary>
    /// 获取所有支持的单位描述
    /// </summary>
    /// <returns>单位描述数组</returns>
    public static string[] GetAllUnitDescriptions() => UnitMappings.Keys.ToArray();

    /// <summary>
    /// 验证单位字符串是否有效
    /// </summary>
    /// <param name="unitString">单位字符串</param>
    /// <returns>是否为有效单位</returns>
    public static bool IsValidUnitString(string unitString) =>
        !string.IsNullOrWhiteSpace(unitString) && UnitMappings.ContainsKey(unitString.Trim());

    /// <summary>
    /// 从单位字符串获取枚举值
    /// </summary>
    /// <param name="unitString">单位字符串</param>
    /// <param name="unit">输出的枚举值</param>
    /// <returns>是否转换成功</returns>
    public static bool TryGetUnitFromString(string unitString, out FileSizeUnit unit)
    {
        unit = default;
        return !string.IsNullOrWhiteSpace(unitString) && UnitMappings.TryGetValue(unitString.Trim(), out unit);
    }
    #endregion

    #region 比较方法

    /// <summary>
    /// 比较两个文件大小
    /// </summary>
    /// <param name="size1">第一个大小（字节）</param>
    /// <param name="size2">第二个大小（字节）</param>
    /// <returns>比较结果</returns>
    public static int Compare(long size1, long size2) => size1.CompareTo(size2);

    /// <summary>
    /// 检查是否大于指定大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="threshold">阈值</param>
    /// <returns>是否大于阈值</returns>
    public static bool IsLargerThan(long bytes, FileSize threshold) => bytes > threshold.Size;

    /// <summary>
    /// 检查是否小于指定大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <param name="threshold">阈值</param>
    /// <returns>是否小于阈值</returns>
    public static bool IsSmallerThan(long bytes, FileSize threshold) => bytes < threshold.Size;

    #endregion
}
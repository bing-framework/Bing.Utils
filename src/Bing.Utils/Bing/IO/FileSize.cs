using Bing.Extensions;

namespace Bing.IO;

/// <summary>
/// 文件大小
/// </summary>
public readonly struct FileSize
{
    /// <summary>
    /// 文件字节长度
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// 初始化一个<see cref="FileSize"/>类型的实例
    /// </summary>
    /// <param name="size">文件大小</param>
    /// <param name="unit">文件大小单位</param>
    public FileSize(long size, FileSizeUnit unit = FileSizeUnit.Byte) => Size = GetSize(size, unit);

    /// <summary>
    /// 获取文件大小
    /// </summary>
    /// <param name="size">文件大小</param>
    /// <param name="unit">文件大小单位</param>
    private static long GetSize(long size, FileSizeUnit unit)
    {
        return unit switch
        {
            FileSizeUnit.K => size * 1024L,
            FileSizeUnit.M => size * 1024L * 1024L,
            FileSizeUnit.G => size * 1024L * 1024L * 1024L,
            FileSizeUnit.T => size * 1024L * 1024L * 1024L * 1024L,
            FileSizeUnit.P => size * 1024L * 1024L * 1024L * 1024L * 1024L,
            _ => size
        };
    }

    /// <summary>
    /// 获取文件大小，单位：字节
    /// </summary>
    public int GetSize() => (int)Size;

    /// <summary>
    /// 获取文件大小，单位：K
    /// </summary>
    public double GetSizeByK() => Math.Round(Size / 1024.0, 2);

    /// <summary>
    /// 获取文件大小，单位：M
    /// </summary>
    public double GetSizeByM() => Math.Round(Size / 1024.0 / 1024.0, 2);

    /// <summary>
    /// 获取文件大小，单位：G
    /// </summary>
    public double GetSizeByG() => Math.Round(Size / 1024.0 / 1024.0 / 1024.0, 2);

    /// <summary>
    /// 获取文件大小，单位：T
    /// </summary>
    public double GetSizeByT() => Math.Round(Size / 1024.0 / 1024.0 / 1024.0 / 1024.0, 2);

    /// <summary>
    /// 获取文件大小，单位：P
    /// </summary>
    public double GetSizeByP() => Math.Round(Size / 1024.0 / 1024.0 / 1024.0 / 1024.0 / 1024.0, 2);

    /// <summary>
    /// 输出描述
    /// </summary>
    public override string ToString()
    {
        if (Size >= 1024L * 1024L * 1024L * 1024L * 1024L)
            return $"{GetSizeByP()} {FileSizeUnit.P.Description()}";
        if (Size >= 1024L * 1024L * 1024L * 1024L)
            return $"{GetSizeByT()} {FileSizeUnit.T.Description()}";
        if (Size >= 1024L * 1024L * 1024L)
            return $"{GetSizeByG()} {FileSizeUnit.G.Description()}";
        if (Size >= 1024L * 1024L)
            return $"{GetSizeByM()} {FileSizeUnit.M.Description()}";
        if (Size >= 1024L)
            return $"{GetSizeByK()} {FileSizeUnit.K.Description()}";
        return $"{GetSize()} {FileSizeUnit.Byte.Description()}";
    }
}
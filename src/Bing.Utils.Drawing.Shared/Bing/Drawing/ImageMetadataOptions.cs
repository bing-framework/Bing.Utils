namespace Bing.Drawing;

/// <summary>
/// 图像元数据清理选项
/// </summary>
public class ImageMetadataOptions
{
    /// <summary>
    /// 获取或设置是否移除 GPS 相关标签；默认值为 true。
    /// </summary>
    public bool RemoveGps { get; set; } = true;

    /// <summary>
    /// 获取或设置是否移除全部 EXIF 数据段（而非仅 GPS）；默认值为 false。
    /// </summary>
    public bool RemoveEntireExif { get; set; }

    /// <summary>
    /// 获取或设置是否保留 ICC 颜色配置文件；默认值为 true。
    /// </summary>
    public bool PreserveIccProfile { get; set; } = true;

    /// <summary>
    /// 获取或设置遇到不支持的格式时的行为；默认值为 <see cref="UnsupportedFormatBehavior.Throw"/>。
    /// </summary>
    public UnsupportedFormatBehavior UnsupportedFormatBehavior { get; set; } = UnsupportedFormatBehavior.Throw;
}

/// <summary>
/// 遇到不支持的图像格式时的行为
/// </summary>
public enum UnsupportedFormatBehavior
{
    /// <summary>
    /// 抛出 NotSupportedException
    /// </summary>
    Throw,

    /// <summary>
    /// 静默透传原始数据（不修改）
    /// </summary>
    PassThrough
}

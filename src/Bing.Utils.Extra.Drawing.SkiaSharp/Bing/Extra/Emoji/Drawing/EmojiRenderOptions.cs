using SkiaSharp;

namespace Bing.Extra.Emoji.Drawing;

/// <summary>
/// 定义 Emoji 图像的字体、尺寸和颜色选项。
/// </summary>
/// <remarks>
/// 未指定字体路径或字体名称时使用 SkiaSharp 默认字体。彩色 Emoji 的显示效果取决于所选字体和 SkiaSharp 后端。
/// </remarks>
public sealed class EmojiRenderOptions
{
    /// <summary>
    /// 获取或设置字体文件路径。
    /// </summary>
    /// <remarks>指定后会校验字体是否包含完整 Emoji 序列；与 <see cref="FontFamily" /> 不能同时指定。</remarks>
    public string? FontPath { get; set; }

    /// <summary>
    /// 获取或设置字体名称。
    /// </summary>
    /// <remarks>字体名称由 SkiaSharp 当前平台解析；与 <see cref="FontPath" /> 不能同时指定。</remarks>
    public string? FontFamily { get; set; }

    /// <summary>
    /// 获取或设置字号，单位为像素。
    /// </summary>
    public float FontSize { get; set; } = 64;

    /// <summary>
    /// 获取或设置图像四周的内边距，单位为像素。
    /// </summary>
    public int Padding { get; set; } = 8;

    /// <summary>
    /// 获取或设置图像背景色。
    /// </summary>
    public SKColor Background { get; set; } = SKColors.Transparent;

    /// <summary>
    /// 获取或设置非彩色字形的绘制颜色。
    /// </summary>
    /// <remarks>彩色字体可能忽略此颜色，由字体自身的颜色表决定。</remarks>
    public SKColor Color { get; set; } = SKColors.Black;
}

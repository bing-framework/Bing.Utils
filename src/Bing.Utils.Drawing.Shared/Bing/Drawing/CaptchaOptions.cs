namespace Bing.Drawing;

/// <summary>
/// 验证码配置选项
/// </summary>
public class CaptchaOptions
{
    /// <summary>
    /// 获取或设置背景色 R 分量。默认 240
    /// </summary>
    public byte BackgroundR { get; set; } = 240;

    /// <summary>
    /// 获取或设置背景色 G 分量。默认 240
    /// </summary>
    public byte BackgroundG { get; set; } = 240;

    /// <summary>
    /// 获取或设置背景色 B 分量。默认 240
    /// </summary>
    public byte BackgroundB { get; set; } = 240;

    /// <summary>
    /// 获取或设置字体大小。默认 20
    /// </summary>
    public int FontSize { get; set; } = 20;

    /// <summary>
    /// 获取或设置每个字符的宽度，单位为像素；默认值为 20。
    /// </summary>
    public int FontWidth { get; set; } = 20;

    /// <summary>
    /// 获取或设置干扰线数量。默认 3
    /// </summary>
    public int NoiseLineCount { get; set; } = 3;

    /// <summary>
    /// 获取或设置干扰点数量。默认自动计算
    /// </summary>
    public int NoisePointCount { get; set; } = -1;

    /// <summary>
    /// 获取或设置是否绘制边框。默认 true
    /// </summary>
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// 获取或设置是否随机文字位置。默认 false
    /// </summary>
    public bool RandomPosition { get; set; }

    /// <summary>
    /// 获取或设置是否随机文字颜色。默认 true
    /// </summary>
    public bool RandomColor { get; set; } = true;

    /// <summary>
    /// 获取或设置是否随机倾斜文字；默认值为 false。
    /// </summary>
    /// <remarks>
    /// 当前 ImageSharp 和 Skia 验证码生成路径均不应用此设置。
    /// </remarks>
    public bool RandomItalic { get; set; }

    /// <summary>
    /// 获取或设置背景色 A（Alpha）分量；默认值为 255。
    /// </summary>
    /// <remarks>
    /// 仅 Skia 验证码生成路径使用此值；ImageSharp 路径固定使用 255。
    /// </remarks>
    public byte BackgroundA { get; set; } = 255;

    /// <summary>
    /// 获取或设置图片宽度，单位为像素；默认值为 0。
    /// </summary>
    /// <remarks>
    /// 值为 0 时 Skia 路径按字符数和字体宽度计算；ImageSharp 路径始终按字符数和字体宽度计算。
    /// </remarks>
    public int Width { get; set; }

    /// <summary>
    /// 获取或设置图片高度，单位为像素；默认值为 0。
    /// </summary>
    /// <remarks>
    /// 值为 0 时 Skia 路径按字体大小计算；ImageSharp 路径始终按字体大小计算。
    /// </remarks>
    public int Height { get; set; }

    /// <summary>
    /// 获取或设置字符间距，单位为像素；默认值为 0。
    /// </summary>
    /// <remarks>
    /// 当前验证码生成路径未使用此设置。
    /// </remarks>
    public int CharacterSpacing { get; set; }

    /// <summary>
    /// 获取或设置是否随机旋转字符；默认值为 true。
    /// </summary>
    /// <remarks>
    /// 仅 Skia 验证码生成路径使用此设置。
    /// </remarks>
    public bool RandomRotation { get; set; } = true;

    /// <summary>
    /// 获取或设置字符最大旋转角度，单位为度；默认值为 10。
    /// </summary>
    /// <remarks>
    /// 仅 Skia 验证码生成路径使用此设置。
    /// </remarks>
    public int MaxRotationDegrees { get; set; } = 10;

    /// <summary>
    /// 获取或设置随机种子。
    /// </summary>
    /// <remarks>
    /// 仅 Skia 验证码生成路径使用此设置；为 null 时使用系统随机源，设置固定值时可复现随机效果。
    /// </remarks>
    public int? RandomSeed { get; set; }
}

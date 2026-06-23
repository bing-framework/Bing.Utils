namespace Bing.Drawing;

/// <summary>
/// 验证码配置选项
/// </summary>
public class CaptchaOptions
{
    /// <summary>
    /// 背景色 R 分量。默认 240
    /// </summary>
    public byte BackgroundR { get; set; } = 240;

    /// <summary>
    /// 背景色 G 分量。默认 240
    /// </summary>
    public byte BackgroundG { get; set; } = 240;

    /// <summary>
    /// 背景色 B 分量。默认 240
    /// </summary>
    public byte BackgroundB { get; set; } = 240;

    /// <summary>
    /// 字体大小。默认 20
    /// </summary>
    public int FontSize { get; set; } = 20;

    /// <summary>
    /// 每个字符的宽度（像素）。默认等于字体大小
    /// </summary>
    public int FontWidth { get; set; } = 20;

    /// <summary>
    /// 干扰线数量。默认 3
    /// </summary>
    public int NoiseLineCount { get; set; } = 3;

    /// <summary>
    /// 干扰点数量。默认自动计算
    /// </summary>
    public int NoisePointCount { get; set; } = -1;

    /// <summary>
    /// 是否绘制边框。默认 true
    /// </summary>
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// 是否随机文字位置。默认 false
    /// </summary>
    public bool RandomPosition { get; set; }

    /// <summary>
    /// 是否随机文字颜色。默认 true
    /// </summary>
    public bool RandomColor { get; set; } = true;

    /// <summary>
    /// 是否随机倾斜。默认 false
    /// </summary>
    public bool RandomItalic { get; set; }
}

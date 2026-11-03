using Bing.Drawing;

namespace Bing.Captcha.SkiaSharp;

/// <summary>SkiaSharp 验证码渲染配置。</summary>
public sealed class SkiaSharpCaptchaRendererOptions
{
    /// <summary>获取或设置生成器使用的字符类型。</summary>
    public CaptchaType Type { get; set; } = CaptchaType.NumberAndLetter;

    /// <summary>获取或设置字母比较是否忽略 ASCII 大小写。</summary>
    public bool IgnoreAsciiCase { get; set; } = true;

    /// <summary>获取或设置显式字体文件路径；中文字符集必须提供覆盖完整白名单的字体。</summary>
    public string? FontPath { get; set; }

    /// <summary>获取或设置绘图选项；构造渲染器时会复制并校验。</summary>
    public CaptchaOptions DrawingOptions { get; set; } = new()
    {
        Width = 160,
        Height = 60,
        FontSize = 30,
        FontWidth = 24,
        RandomItalic = false,
        RandomRotation = true,
        MaxRotationDegrees = 8
    };

    /// <summary>获取或设置编码后 PNG 的最大字节数。</summary>
    public int MaxImageBytes { get; set; } = 64 * 1024;

    internal SkiaSharpCaptchaRendererOptions Snapshot()
    {
        if (!Enum.IsDefined(typeof(CaptchaType), Type))
            throw new ArgumentOutOfRangeException(nameof(Type));
        if (MaxImageBytes < 1 || MaxImageBytes > 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaxImageBytes));
        if (FontPath != null && string.IsNullOrWhiteSpace(FontPath))
            throw new ArgumentException("字体路径不能为空白。", nameof(FontPath));
        if (Type == CaptchaType.ChineseChar && FontPath == null)
            throw new ArgumentException("中文验证码必须显式配置字体文件路径。", nameof(FontPath));
        if (DrawingOptions == null)
            throw new ArgumentNullException(nameof(DrawingOptions));

        var drawing = DrawingOptions;
        if (drawing.RandomItalic)
            throw new NotSupportedException("SkiaSharp 验证码渲染器不支持随机斜体。");
        if (drawing.CharacterSpacing != 0)
            throw new NotSupportedException("SkiaSharp 验证码渲染器不支持字符间距配置。");
        if (drawing.RandomSeed.HasValue)
            throw new ArgumentException("验证码渲染不允许使用固定随机种子。", nameof(DrawingOptions));
        if (drawing.FontSize < 8 || drawing.FontSize > 96)
            throw new ArgumentOutOfRangeException(nameof(DrawingOptions), "字体尺寸必须在 8 到 96 之间。");
        if (drawing.FontWidth < 8 || drawing.FontWidth > 128)
            throw new ArgumentOutOfRangeException(nameof(DrawingOptions), "字符宽度必须在 8 到 128 之间。");
        if (drawing.Width < 0 || drawing.Width > 512 || drawing.Height < 0 || drawing.Height > 256)
            throw new ArgumentOutOfRangeException(nameof(DrawingOptions), "图像宽高超出支持范围。");
        if ((drawing.Width == 0 && drawing.FontWidth * 9 > 512) || (drawing.Height == 0 && drawing.FontSize + drawing.FontSize / 2 > 256))
            throw new ArgumentOutOfRangeException(nameof(DrawingOptions), "自动计算的图像宽高超出支持范围。");
        if (drawing.NoiseLineCount < 0 || drawing.NoiseLineCount > 32)
            throw new ArgumentOutOfRangeException(nameof(DrawingOptions), "干扰线数量必须在 0 到 32 之间。");
        if (drawing.NoisePointCount < -1 || drawing.NoisePointCount > 4096)
            throw new ArgumentOutOfRangeException(nameof(DrawingOptions), "干扰点数量必须为 -1 到 4096。");
        if (drawing.MaxRotationDegrees < 0 || drawing.MaxRotationDegrees > 30)
            throw new ArgumentOutOfRangeException(nameof(DrawingOptions), "字符旋转角度必须在 0 到 30 度之间。");

        return new SkiaSharpCaptchaRendererOptions
        {
            Type = Type,
            IgnoreAsciiCase = IgnoreAsciiCase,
            FontPath = FontPath,
            MaxImageBytes = MaxImageBytes,
            DrawingOptions = new CaptchaOptions
            {
                BackgroundA = drawing.BackgroundA,
                BackgroundR = drawing.BackgroundR,
                BackgroundG = drawing.BackgroundG,
                BackgroundB = drawing.BackgroundB,
                FontSize = drawing.FontSize,
                FontWidth = drawing.FontWidth,
                Width = drawing.Width,
                Height = drawing.Height,
                CharacterSpacing = drawing.CharacterSpacing,
                HasBorder = drawing.HasBorder,
                NoiseLineCount = drawing.NoiseLineCount,
                NoisePointCount = drawing.NoisePointCount,
                RandomColor = drawing.RandomColor,
                RandomItalic = drawing.RandomItalic,
                RandomPosition = drawing.RandomPosition,
                RandomRotation = drawing.RandomRotation,
                MaxRotationDegrees = drawing.MaxRotationDegrees,
                RandomSeed = null
            }
        };
    }
}

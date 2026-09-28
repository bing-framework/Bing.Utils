namespace Bing.Extra.Emoji.Drawing;

/// <summary>
/// 验证 Emoji SkiaSharp 渲染适配的公共行为。
/// </summary>
public sealed class EmojiRendererTests
{
    /// <summary>
    /// 验证兼容选择符形式会规范化并生成可编码图像。
    /// </summary>
    [Fact]
    public void Render_CompatibleSequence_ReturnsSizedImage()
    {
        using var image = EmojiRenderer.Render("❤", new EmojiRenderOptions { FontSize = 48, Padding = 4 });

        image.Width.ShouldBeGreaterThan(0);
        image.Height.ShouldBeGreaterThan(0);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.ShouldNotBeNull();
        data!.Size.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 验证默认背景保持透明且显式背景颜色可见。
    /// </summary>
    [Fact]
    public void Render_BackgroundOptions_AffectCornerPixels()
    {
        using var transparent = EmojiRenderer.Render("😀", new EmojiRenderOptions { Padding = 8 });
        using var transparentBitmap = SKBitmap.FromImage(transparent);
        transparentBitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)0);

        var background = new SKColor(12, 34, 56, 255);
        using var colored = EmojiRenderer.Render("😀", new EmojiRenderOptions { Padding = 8, Background = background });
        using var coloredBitmap = SKBitmap.FromImage(colored);
        coloredBitmap.GetPixel(0, 0).ShouldBe(background);
    }

    /// <summary>
    /// 验证空值、未知序列和非法选项会抛出明确异常。
    /// </summary>
    [Fact]
    public void Render_InvalidArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => EmojiRenderer.Render(null!))
            .ParamName.ShouldBe("unicode");
        Should.Throw<ArgumentException>(() => EmojiRenderer.Render("A"))
            .ParamName.ShouldBe("unicode");
        Should.Throw<ArgumentOutOfRangeException>(() => EmojiRenderer.Render("😀", new EmojiRenderOptions { FontSize = 0 }))
            .ParamName.ShouldBe("FontSize");
        Should.Throw<ArgumentException>(() => EmojiRenderer.Render("😀", new EmojiRenderOptions
        {
            FontPath = "font.ttf",
            FontFamily = "Arial"
        })).ParamName.ShouldBe("options");
        Should.Throw<FileNotFoundException>(() => EmojiRenderer.Render("😀", new EmojiRenderOptions
        {
            FontPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ttf")
        })).FileName.ShouldNotBeNullOrWhiteSpace();
    }
}

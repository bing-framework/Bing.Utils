using Bing.Conversions;

namespace Bing.Utils.Drawing.Tests.Bing.Conversions;

/// <summary>
/// 验证共享颜色转换兼容层的 RGB 输入边界。
/// </summary>
public sealed class ColorConvBoundaryTest
{
    /// <summary>
    /// 验证共享颜色转换入口保留历史 HSL 语义及返回顺序。
    /// </summary>
    /// <param name="r">红色通道值，范围为 0 到 255。</param>
    /// <param name="g">绿色通道值，范围为 0 到 255。</param>
    /// <param name="b">蓝色通道值，范围为 0 到 255。</param>
    /// <param name="hue">期望的色相值。</param>
    /// <param name="saturation">期望的饱和度值。</param>
    /// <param name="lightness">期望的亮度值。</param>
    [Theory]
    [InlineData(0, 0, 0, 0f, 0f, 0f)]
    [InlineData(255, 255, 255, 0f, 0f, 1f)]
    [InlineData(0, 255, 0, 120f, 1f, 0.5f)]
    public void RgbToHsb_ValidChannels_PreservesHsl(int r, int g, int b, float hue, float saturation, float lightness)
    {
        ColorConv.RgbToHsb(r, g, b).ShouldBe(new[] { hue, saturation, lightness });
    }

    /// <summary>
    /// 验证有效 RGB 通道可转换为对应的十六进制颜色值。
    /// </summary>
    /// <param name="r">红色通道值，范围为 0 到 255。</param>
    /// <param name="g">绿色通道值，范围为 0 到 255。</param>
    /// <param name="b">蓝色通道值，范围为 0 到 255。</param>
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(1, 128, 254)]
    public void RgbToHex_ValidChannels_PreservesValues(int r, int g, int b)
    {
        ColorConv.RgbToHex(r, g, b).ShouldBe($"#{r:X2}{g:X2}{b:X2}");
    }

    /// <summary>
    /// 验证超出字节范围的 RGB 通道值会被拒绝。
    /// </summary>
    /// <param name="r">红色通道值。</param>
    /// <param name="g">绿色通道值。</param>
    /// <param name="b">蓝色通道值。</param>
    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(256, 0, 0)]
    [InlineData(0, 256, 0)]
    [InlineData(0, 0, 256)]
    public void RgbToHex_ChannelOutsideByteRange_Throws(int r, int g, int b)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConv.RgbToHex(r, g, b));
    }

    /// <summary>
    /// 验证超出字节范围的 RGB 通道值会被拒绝。
    /// </summary>
    /// <param name="r">红色通道值。</param>
    /// <param name="g">绿色通道值。</param>
    /// <param name="b">蓝色通道值。</param>
    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(256, 0, 0)]
    [InlineData(0, 256, 0)]
    [InlineData(0, 0, 256)]
    public void RgbToHsb_ChannelOutsideByteRange_Throws(int r, int g, int b)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorConv.RgbToHsb(r, g, b));
    }
}

using System.Drawing;
using Bing.Conversions;

namespace Bing.Conversions;

/// <summary>
/// 颜色转换测试
/// </summary>
public class ColorConvTest
{
    /// <summary>
    /// 测试目的：验证 RgbToHsb 转换的正确性 - 基本颜色
    /// </summary>
    [Theory]
    [InlineData(255, 0, 0)]     // 红色
    [InlineData(0, 255, 0)]     // 绿色
    [InlineData(0, 0, 255)]     // 蓝色
    [InlineData(255, 255, 255)] // 白色
    [InlineData(0, 0, 0)]       // 黑色
    [InlineData(128, 128, 128)] // 灰色
    public void RgbToHsb_PrimaryColors_ReturnsExpectedHsb(int r, int g, int b)
    {
        // Arrange
        var color = Color.FromArgb(r, g, b);

        // Act
        var hsb = ColorConv.RgbToHsb(color);

        // Assert
        Assert.NotNull(hsb);
        Assert.Equal(3, hsb.Length);
        Assert.InRange(hsb[0], 0, 360);     // 色相范围 0-360
        Assert.InRange(hsb[1], 0, 1);       // 饱和度范围 0-1
        Assert.InRange(hsb[2], 0, 1);       // 亮度范围 0-1
    }

    /// <summary>
    /// 测试目的：验证 RgbToHsb 对已知颜色的转换准确性
    /// </summary>
    [Fact]
    public void RgbToHsb_RedColor_ReturnsCorrectHsb()
    {
        // Arrange
        var red = Color.FromArgb(255, 0, 0);

        // Act
        var hsb = ColorConv.RgbToHsb(red);

        // Assert - 红色：色相=0, 饱和度=1, 亮度=0.5
        Assert.Equal(0, hsb[0]);
        Assert.Equal(1.0f, hsb[1], 2);
        Assert.Equal(0.5f, hsb[2], 2);
    }

    /// <summary>
    /// 测试目的：验证 RgbToHsb 对白色和黑色的特殊处理
    /// </summary>
    [Theory]
    [InlineData(255, 255, 255, 0, 0)] // 白色：饱和度=0
    [InlineData(0, 0, 0, 0, 0)]       // 黑色：饱和度=0
    public void RgbToHsb_WhiteAndBlack_ReturnsSaturationZero(int r, int g, int b, float expectedSaturation, float expectedHue)
    {
        // Arrange
        var color = Color.FromArgb(r, g, b);

        // Act
        var hsb = ColorConv.RgbToHsb(color);

        // Assert
        Assert.Equal(expectedHue, hsb[0]);
        Assert.Equal(expectedSaturation, hsb[1]);
    }

    /// <summary>
    /// 测试目的：验证 HsbToRgb 转换的正确性 - 零饱和度
    /// </summary>
    [Theory]
    [InlineData(0, 0, 0)]       // 黑色
    [InlineData(0, 0, 0.5)]     // 灰色
    [InlineData(0, 0, 1.0)]     // 白色
    public void HsbToRgb_ZeroSaturation_ReturnsGrayscale(double hue, double saturation, double value)
    {
        // Act
        var color = ColorConv.HsbToRgb(hue, saturation, value);

        // Assert - 饱和度为0时，RGB三个分量应该相等（灰度）
        Assert.Equal(color.R, color.G);
        Assert.Equal(color.G, color.B);
    }

    /// <summary>
    /// 测试目的：验证 HsbToRgb 转换到已知RGB颜色
    /// </summary>
    [Fact]
    public void HsbToRgb_RedColor_ReturnsRed()
    {
        // Arrange - 红色：H=0, S=1, B=0.5
        double hue = 0;
        double saturation = 1;
        double value = 0.5;

        // Act
        var color = ColorConv.HsbToRgb(hue, saturation, value);

        // Assert
        Assert.InRange(color.R, 250, 255);
        Assert.InRange(color.G, 0, 5);
        Assert.InRange(color.B, 0, 5);
    }

    /// <summary>
    /// 测试目的：验证 HsbToRgb 边界值处理
    /// </summary>
    [Theory]
    [InlineData(0, 0, 0)]       // 最小值
    [InlineData(360, 1, 1)]     // 最大值
    [InlineData(180, 0.5, 0.5)] // 中间值
    public void HsbToRgb_BoundaryValues_ReturnsValidColor(double hue, double saturation, double value)
    {
        // Act
        var color = ColorConv.HsbToRgb(hue, saturation, value);

        // Assert
        Assert.InRange(color.R, 0, 255);
        Assert.InRange(color.G, 0, 255);
        Assert.InRange(color.B, 0, 255);
        Assert.Equal(255, color.A); // Alpha应该是255
    }

    /// <summary>
    /// 测试目的：验证 RgbToHsb 和 HsbToRgb 的往返转换一致性
    /// </summary>
    [Theory]
    [InlineData(255, 0, 0)]     // 红色
    [InlineData(0, 255, 0)]     // 绿色
    [InlineData(0, 0, 255)]     // 蓝色
    [InlineData(255, 255, 0)]   // 黄色
    [InlineData(255, 0, 255)]   // 品红
    [InlineData(0, 255, 255)]   // 青色
    public void RoundTrip_RgbToHsbToRgb_ReturnsOriginalColor(int r, int g, int b)
    {
        // Arrange
        var originalColor = Color.FromArgb(r, g, b);

        // Act
        var hsb = ColorConv.RgbToHsb(originalColor);
        var convertedColor = ColorConv.HsbToRgb(hsb[0], hsb[1], hsb[2]);

        // Assert - 允许小的误差范围（±2），因为浮点运算精度
        Assert.InRange(convertedColor.R, r - 2, r + 2);
        Assert.InRange(convertedColor.G, g - 2, g + 2);
        Assert.InRange(convertedColor.B, b - 2, b + 2);
    }

    /// <summary>
    /// 测试目的：验证 HsbToRgb 对不同色相的处理
    /// </summary>
    [Theory]
    [InlineData(0)]     // 红色
    [InlineData(60)]    // 黄色
    [InlineData(120)]   // 绿色
    [InlineData(180)]   // 青色
    [InlineData(240)]   // 蓝色
    [InlineData(300)]   // 品红
    public void HsbToRgb_DifferentHues_ReturnsValidColors(double hue)
    {
        // Arrange
        double saturation = 1.0;
        double value = 0.5;

        // Act
        var color = ColorConv.HsbToRgb(hue, saturation, value);

        // Assert
        Assert.InRange(color.R, 0, 255);
        Assert.InRange(color.G, 0, 255);
        Assert.InRange(color.B, 0, 255);
        // 至少有一个通道的值应该接近最大或最小
        var maxChannel = Math.Max(Math.Max(color.R, color.G), color.B);
        var minChannel = Math.Min(Math.Min(color.R, color.G), color.B);
        Assert.True(maxChannel > 200 || minChannel < 55);
    }
}

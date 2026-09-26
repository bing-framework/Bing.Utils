using System.Drawing;
using Bing.Drawing;

namespace Bing.Utils.Drawing.Tests.Bing.Drawing;

/// <summary>
/// 验证原生 ICO 转换的尺寸边界。
/// </summary>
public sealed class ImageHelperConversionBoundaryTest
{
    /// <summary>
    /// 验证超出 ICO 尺寸范围的请求抛出范围异常。
    /// </summary>
    /// <param name="width">请求的 ICO 宽度。</param>
    /// <param name="height">请求的 ICO 高度。</param>
    [Theory]
    [InlineData(0, 16)]
    [InlineData(16, 0)]
    [InlineData(-1, 16)]
    [InlineData(16, -1)]
    [InlineData(257, 16)]
    [InlineData(16, 257)]
    public void ToIcoStream_SizeOutsideSupportedRange_Throws(int width, int height)
    {
        using var source = new Bitmap(1, 1);

        Should.Throw<ArgumentOutOfRangeException>(() => ImageHelper.ToIcoStream(source, new Size(width, height)));
    }
}

namespace Bing.Drawing.Internal;

/// <summary>
/// 验证灰度换算、阈值处理、内容裁剪及矩阵复制。
/// </summary>
[Trait("Drawing", "Shared.GrayImageBuffer")]
public class GrayImageBufferTest
{
    /// <summary>
    /// 验证白色 RGB 分量换算为灰度值 255。
    /// </summary>
    [Fact]
    public void ToGray_White_Returns255()
    {
        GrayImageBuffer.ToGray(255, 255, 255).ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证黑色 RGB 分量换算为灰度值零。
    /// </summary>
    [Fact]
    public void ToGray_Black_Returns0()
    {
        GrayImageBuffer.ToGray(0, 0, 0).ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证纯红色按加权系数换算为灰度值 76。
    /// </summary>
    [Fact]
    public void ToGray_PureRed_ReturnsApprox76()
    {
        // 0.299 * 255 ≈ 76
        GrayImageBuffer.ToGray(255, 0, 0).ShouldBe((byte)76);
    }

    /// <summary>
    /// 验证高于阈值的灰度值二值化为背景值 255。
    /// </summary>
    [Fact]
    public void Binarize_AboveThreshold_ReturnsBackground()
    {
        var gray = new byte[,] { { 128 }, { 200 } };
        var result = GrayImageBuffer.Binarize(gray, 100);
        result[0, 0].ShouldBe((byte)255);
        result[1, 0].ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证低于阈值的灰度值二值化为前景值零。
    /// </summary>
    [Fact]
    public void Binarize_BelowThreshold_ReturnsForeground()
    {
        var gray = new byte[,] { { 50 }, { 10 } };
        var result = GrayImageBuffer.Binarize(gray, 100);
        result[0, 0].ShouldBe((byte)0);
        result[1, 0].ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证前景加深将低于阈值的灰度置零且保留亮色。
    /// </summary>
    [Fact]
    public void DeepenForeground_BelowThreshold_BecomesZero()
    {
        var gray = new byte[,] { { 50 }, { 200 } };
        var result = GrayImageBuffer.DeepenForeground(gray, 100);
        result[0, 0].ShouldBe((byte)0);
        result[1, 0].ShouldBe((byte)200);
    }

    /// <summary>
    /// 验证仅将指定灰度范围内的像素置为背景。
    /// </summary>
    [Fact]
    public void ClearGrayRange_InRange_BecomesBackground()
    {
        var gray = new byte[,] { { 128 }, { 50 }, { 200 } };
        var result = GrayImageBuffer.ClearGrayRange(gray, 100, 150);
        result[0, 0].ShouldBe((byte)255); // 128 in [100,150]
        result[1, 0].ShouldBe((byte)50);  // 50 below range
        result[2, 0].ShouldBe((byte)200); // 200 above range
    }

    /// <summary>
    /// 验证内容裁剪去掉白色外圈并保留前景区域。
    /// </summary>
    [Fact]
    public void TrimToContent_FindsContentBounds()
    {
        var bin = new byte[,]
        {
            { 255, 255, 255, 255 },
            { 255, 0, 0, 255 },
            { 255, 0, 0, 255 },
            { 255, 255, 255, 255 }
        };
        var result = GrayImageBuffer.TrimToContent(bin, 128);
        result.GetLength(0).ShouldBe(2);
        result.GetLength(1).ShouldBe(2);
        result[0, 0].ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证纯背景矩阵裁剪为两个维度均为零的空矩阵。
    /// </summary>
    [Fact]
    public void TrimToContent_AllBackground_ReturnsEmpty()
    {
        var bin = new byte[,]
        {
            { 255, 255 },
            { 255, 255 }
        };
        var result = GrayImageBuffer.TrimToContent(bin, 128);
        result.GetLength(0).ShouldBe(0);
        result.GetLength(1).ShouldBe(0);
    }

    /// <summary>
    /// 验证清除边框将边缘置为背景且保留中心。
    /// </summary>
    [Fact]
    public void ClearBorder_SetsBorderToBackground()
    {
        var gray = new byte[,]
        {
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 0, 0, 0 }
        };
        var result = GrayImageBuffer.ClearBorder(gray, 1);
        result[0, 0].ShouldBe((byte)255);
        result[1, 1].ShouldBe((byte)0); // center preserved
        result[2, 2].ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证添加边框扩展矩阵且原始像素位于正确偏移处。
    /// </summary>
    [Fact]
    public void AddBorder_ExpandsCanvas()
    {
        var gray = new byte[,] { { 0 } };
        var result = GrayImageBuffer.AddBorder(gray, 2, 255);
        result.GetLength(0).ShouldBe(5);
        result.GetLength(1).ShouldBe(5);
        result[2, 2].ShouldBe((byte)0);  // center preserved
        result[0, 0].ShouldBe((byte)255); // border
    }

    /// <summary>
    /// 验证复制矩阵保留内容且修改副本不影响原矩阵。
    /// </summary>
    [Fact]
    public void Copy_CreatesIndependentCopy()
    {
        var source = new byte[,] { { 1, 2 }, { 3, 4 } };
        var copy = GrayImageBuffer.Copy(source);
        copy[0, 0].ShouldBe((byte)1);
        copy[1, 1].ShouldBe((byte)4);
        copy[0, 0] = 99;
        source[0, 0].ShouldBe((byte)1); // independent
    }

    /// <summary>
    /// 验证子矩阵提取保留指定区域的尺寸及像素值。
    /// </summary>
    [Fact]
    public void SubMatrix_ExtractsRegion()
    {
        var source = new byte[,]
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
            { 7, 8, 9 }
        };
        var result = GrayImageBuffer.SubMatrix(source, 1, 1, 2, 2);
        result.GetLength(0).ShouldBe(2);
        result.GetLength(1).ShouldBe(2);
        result[0, 0].ShouldBe((byte)5);
        result[1, 1].ShouldBe((byte)9);
    }
}

namespace Bing.Drawing.Internal;

/// <summary>
/// 测试类：覆盖 <see cref="GrayImageBuffer"/> 纯算法。
/// </summary>
[Trait("Drawing", "Shared.GrayImageBuffer")]
public class GrayImageBufferTest
{
    [Fact]
    public void ToGray_White_Returns255()
    {
        GrayImageBuffer.ToGray(255, 255, 255).ShouldBe((byte)255);
    }

    [Fact]
    public void ToGray_Black_Returns0()
    {
        GrayImageBuffer.ToGray(0, 0, 0).ShouldBe((byte)0);
    }

    [Fact]
    public void ToGray_PureRed_ReturnsApprox76()
    {
        // 0.299 * 255 ≈ 76
        GrayImageBuffer.ToGray(255, 0, 0).ShouldBe((byte)76);
    }

    [Fact]
    public void Binarize_AboveThreshold_ReturnsBackground()
    {
        var gray = new byte[,] { { 128 }, { 200 } };
        var result = GrayImageBuffer.Binarize(gray, 100);
        result[0, 0].ShouldBe((byte)255);
        result[1, 0].ShouldBe((byte)255);
    }

    [Fact]
    public void Binarize_BelowThreshold_ReturnsForeground()
    {
        var gray = new byte[,] { { 50 }, { 10 } };
        var result = GrayImageBuffer.Binarize(gray, 100);
        result[0, 0].ShouldBe((byte)0);
        result[1, 0].ShouldBe((byte)0);
    }

    [Fact]
    public void DeepenForeground_BelowThreshold_BecomesZero()
    {
        var gray = new byte[,] { { 50 }, { 200 } };
        var result = GrayImageBuffer.DeepenForeground(gray, 100);
        result[0, 0].ShouldBe((byte)0);
        result[1, 0].ShouldBe((byte)200);
    }

    [Fact]
    public void ClearGrayRange_InRange_BecomesBackground()
    {
        var gray = new byte[,] { { 128 }, { 50 }, { 200 } };
        var result = GrayImageBuffer.ClearGrayRange(gray, 100, 150);
        result[0, 0].ShouldBe((byte)255); // 128 in [100,150]
        result[1, 0].ShouldBe((byte)50);  // 50 below range
        result[2, 0].ShouldBe((byte)200); // 200 above range
    }

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

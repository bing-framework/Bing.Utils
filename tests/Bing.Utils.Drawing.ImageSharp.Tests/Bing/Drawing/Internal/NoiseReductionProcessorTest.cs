namespace Bing.Drawing.Internal;

/// <summary>
/// 验证按邻居数量和连通域面积去除二值图像噪点。
/// </summary>
[Trait("Drawing", "Shared.NoiseReductionProcessor")]
public class NoiseReductionProcessorTest
{
    /// <summary>
    /// 验证邻居数不足的孤立前景像素被清除。
    /// </summary>
    [Fact]
    public void ClearNoiseByNeighborCount_IsolatedPixel_Removed()
    {
        var bin = new byte[,]
        {
            { 255, 255, 255 },
            { 255, 0, 255 },
            { 255, 255, 255 }
        };
        var result = NoiseReductionProcessor.ClearNoiseByNeighborCount(bin, 128, 2);
        result[1, 1].ShouldBe((byte)255); // isolated → removed
    }

    /// <summary>
    /// 验证内部连通区域中具有足够邻居的像素被保留。
    /// </summary>
    [Fact]
    public void ClearNoiseByNeighborCount_ConnectedPixel_Preserved()
    {
        // 5x5 grid with connected group at interior (not border)
        var bin = new byte[,]
        {
            { 255, 255, 255, 255, 255 },
            { 255, 0, 0, 255, 255 },
            { 255, 0, 0, 255, 255 },
            { 255, 255, 255, 255, 255 },
            { 255, 255, 255, 255, 255 }
        };
        var result = NoiseReductionProcessor.ClearNoiseByNeighborCount(bin, 128, 2);
        result[1, 1].ShouldBe((byte)0); // interior, has enough neighbors → preserved
        result[1, 2].ShouldBe((byte)0);
    }

    /// <summary>
    /// 验证按邻居数量去噪时清除边界上的孤立像素。
    /// </summary>
    [Fact]
    public void ClearNoiseByNeighborCount_BorderPixel_Removed()
    {
        var bin = new byte[,]
        {
            { 0, 255, 255 },
            { 255, 255, 255 },
            { 255, 255, 255 }
        };
        var result = NoiseReductionProcessor.ClearNoiseByNeighborCount(bin, 128, 2);
        result[0, 0].ShouldBe((byte)255); // border → removed
    }

    /// <summary>
    /// 验证小于面积阈值的单像素连通域被清除。
    /// </summary>
    [Fact]
    public void ClearNoiseByArea_SmallComponent_Removed()
    {
        var bin = new byte[,]
        {
            { 0, 255, 0 },
            { 255, 255, 255 },
            { 0, 255, 255 }
        };
        var result = NoiseReductionProcessor.ClearNoiseByArea(bin, 128, 3);
        // All three foreground pixels are isolated (size 1 each, < 3) → all removed
        result[0, 0].ShouldBe((byte)255);
        result[0, 2].ShouldBe((byte)255);
        result[2, 0].ShouldBe((byte)255);
    }

    /// <summary>
    /// 验证达到面积阈值的连通域被保留。
    /// </summary>
    [Fact]
    public void ClearNoiseByArea_LargeComponent_Preserved()
    {
        var bin = new byte[,]
        {
            { 0, 0, 255 },
            { 0, 0, 255 },
            { 255, 255, 255 }
        };
        var result = NoiseReductionProcessor.ClearNoiseByArea(bin, 128, 3);
        result[0, 0].ShouldBe((byte)0); // component size=4 >= 3 → preserved
        result[1, 1].ShouldBe((byte)0);
    }
}

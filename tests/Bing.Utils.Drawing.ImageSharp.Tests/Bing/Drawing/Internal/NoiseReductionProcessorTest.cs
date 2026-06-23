namespace Bing.Drawing.Internal;

/// <summary>
/// 测试类：覆盖 <see cref="NoiseReductionProcessor"/> 纯算法。
/// </summary>
[Trait("Drawing", "Shared.NoiseReductionProcessor")]
public class NoiseReductionProcessorTest
{
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

namespace Bing.Drawing.Internal;

/// <summary>
/// 测试类：覆盖 <see cref="BinaryImageProcessor"/> 纯算法。
/// </summary>
[Trait("Drawing", "Shared.BinaryImageProcessor")]
public class BinaryImageProcessorTest
{
    [Fact]
    public void IsForeground_BelowThreshold_ReturnsTrue()
    {
        BinaryImageProcessor.IsForeground(0, 128).ShouldBeTrue();
        BinaryImageProcessor.IsForeground(127, 128).ShouldBeTrue();
    }

    [Fact]
    public void IsForeground_AtOrAboveThreshold_ReturnsFalse()
    {
        BinaryImageProcessor.IsForeground(128, 128).ShouldBeFalse();
        BinaryImageProcessor.IsForeground(255, 128).ShouldBeFalse();
    }

    [Fact]
    public void CountForegroundNeighbors_CenterWithAllForeground_Returns8()
    {
        var bin = new byte[,]
        {
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 0, 0, 0 }
        };
        BinaryImageProcessor.CountForegroundNeighbors(bin, 1, 1, 3, 3, 128).ShouldBe(8);
    }

    [Fact]
    public void CountForegroundNeighbors_CenterWithAllBackground_Returns0()
    {
        var bin = new byte[,]
        {
            { 255, 255, 255 },
            { 255, 0, 255 },
            { 255, 255, 255 }
        };
        BinaryImageProcessor.CountForegroundNeighbors(bin, 1, 1, 3, 3, 128).ShouldBe(0);
    }

    [Fact]
    public void CountForegroundNeighbors_Corner_ReturnsCorrectCount()
    {
        var bin = new byte[,]
        {
            { 0, 0 },
            { 0, 255 }
        };
        // At (0,0): neighbors are (1,0)=0, (0,1)=0, (1,1)=255 → 2 foreground
        BinaryImageProcessor.CountForegroundNeighbors(bin, 0, 0, 2, 2, 128).ShouldBe(2);
    }
}

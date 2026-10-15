namespace Bing.Drawing.Internal;

/// <summary>
/// 验证二值图像的前景阈值判定及邻域计数。
/// </summary>
[Trait("Drawing", "Shared.BinaryImageProcessor")]
public class BinaryImageProcessorTest
{
    /// <summary>
    /// 验证低于阈值的灰度值被识别为前景。
    /// </summary>
    [Fact]
    public void IsForeground_BelowThreshold_ReturnsTrue()
    {
        BinaryImageProcessor.IsForeground(0, 128).ShouldBeTrue();
        BinaryImageProcessor.IsForeground(127, 128).ShouldBeTrue();
    }

    /// <summary>
    /// 验证达到或超过阈值的灰度值被识别为背景。
    /// </summary>
    [Fact]
    public void IsForeground_AtOrAboveThreshold_ReturnsFalse()
    {
        BinaryImageProcessor.IsForeground(128, 128).ShouldBeFalse();
        BinaryImageProcessor.IsForeground(255, 128).ShouldBeFalse();
    }

    /// <summary>
    /// 验证中心像素周围八个前景邻居均被计入。
    /// </summary>
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

    /// <summary>
    /// 验证中心前景像素周围全为背景时邻域计数为零。
    /// </summary>
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

    /// <summary>
    /// 验证角点邻域计数跳过图像边界外的位置。
    /// </summary>
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

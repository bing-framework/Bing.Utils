using System.Drawing;

namespace Bing.Drawing.Internal;

/// <summary>
/// 测试类：覆盖 <see cref="ConnectedComponentProcessor"/> 纯算法。
/// </summary>
[Trait("Drawing", "Shared.ConnectedComponentProcessor")]
public class ConnectedComponentProcessorTest
{
    [Fact]
    public void LabelConnectedComponents_SingleForeground_ReturnsOneComponent()
    {
        var bin = new byte[,]
        {
            { 255, 255 },
            { 255, 0 }
        };
        var components = ConnectedComponentProcessor.LabelConnectedComponents(bin, 128);
        components.Count.ShouldBe(1);
        components[0].Count.ShouldBe(1);
        components[0][0].ShouldBe(new Point(1, 1));
    }

    [Fact]
    public void LabelConnectedComponents_TwoSeparateGroups_ReturnsTwoComponents()
    {
        // Two isolated foreground pixels separated by background: [3,1]
        var bin = new byte[,]
        {
            { 0, 255, 0 }
        };
        var components = ConnectedComponentProcessor.LabelConnectedComponents(bin, 128);
        components.Count.ShouldBe(2);
        components[0].Count.ShouldBe(1);
        components[1].Count.ShouldBe(1);
    }

    [Fact]
    public void LabelConnectedComponents_ConnectedRegion_ReturnsCorrectSize()
    {
        var bin = new byte[,]
        {
            { 0, 0, 255 },
            { 0, 0, 255 },
            { 255, 255, 255 }
        };
        var components = ConnectedComponentProcessor.LabelConnectedComponents(bin, 128);
        components.Count.ShouldBe(1);
        components[0].Count.ShouldBe(4);
    }

    [Fact]
    public void FloodFillReplace_FillsConnectedRegion()
    {
        var bin = new byte[,]
        {
            { 0, 0, 255 },
            { 0, 0, 255 },
            { 255, 255, 255 }
        };
        var points = ConnectedComponentProcessor.FloodFillReplace(bin, 0, 0, 128);
        points.Count.ShouldBe(4);
        bin[0, 0].ShouldBe((byte)128);
        bin[1, 1].ShouldBe((byte)128);
        bin[2, 0].ShouldBe((byte)255); // not touched
    }

    [Fact]
    public void FloodFillReplace_DifferentGray_DoesNotFill()
    {
        var bin = new byte[,]
        {
            { 0, 255 },
            { 255, 0 }
        };
        var points = ConnectedComponentProcessor.FloodFillReplace(bin, 0, 0, 128);
        points.Count.ShouldBe(1); // only the starting pixel
        bin[1, 1].ShouldBe((byte)0); // not connected, not touched
    }
}

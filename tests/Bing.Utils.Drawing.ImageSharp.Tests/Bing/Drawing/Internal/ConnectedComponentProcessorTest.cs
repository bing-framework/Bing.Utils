namespace Bing.Drawing.Internal;

/// <summary>
/// 验证前景连通域标记及同灰度区域填充。
/// </summary>
[Trait("Drawing", "Shared.ConnectedComponentProcessor")]
public class ConnectedComponentProcessorTest
{
    /// <summary>
    /// 验证单个前景像素形成一个包含正确坐标的连通域。
    /// </summary>
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
        components[0][0].X.ShouldBe(1);
        components[0][0].Y.ShouldBe(1);
    }

    /// <summary>
    /// 验证由背景隔开的前景像素分别形成连通域。
    /// </summary>
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

    /// <summary>
    /// 验证相连的四个前景像素形成同一连通域。
    /// </summary>
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

    /// <summary>
    /// 验证区域填充返回四个坐标并只替换相连前景。
    /// </summary>
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

    /// <summary>
    /// 验证仅对角相邻的同色像素不会被同一次区域填充替换。
    /// </summary>
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

namespace Bing.Drawing.Internal;

/// <summary>
/// 测试类：覆盖 <see cref="ProjectionProcessor"/> 纯算法。
/// </summary>
[Trait("Drawing", "Shared.ProjectionProcessor")]
public class ProjectionProcessorTest
{
    [Fact]
    public void VerticalProjection_CountsForegroundPerColumn()
    {
        // C# byte[,] initializer is [row, col], first index = row(y), second = col(x)
        // bin[0,*]={0,255,0}  bin[1,*]={0,255,0}  bin[2,*]={255,255,255}
        var bin = new byte[,]
        {
            { 0, 255, 0 },
            { 0, 255, 0 },
            { 255, 255, 255 }
        };
        var proj = ProjectionProcessor.VerticalProjection(bin, 128);
        proj.Length.ShouldBe(3);
        proj[0].ShouldBe(2); // first dim index 0: bin[0,0]=0, bin[0,1]=255, bin[0,2]=0 → 2 foreground
        proj[1].ShouldBe(2); // first dim index 1: bin[1,0]=0, bin[1,1]=255, bin[1,2]=0 → 2 foreground
        proj[2].ShouldBe(0); // first dim index 2: all 255 → 0 foreground
    }

    [Fact]
    public void HorizontalProjection_CountsForegroundPerRow()
    {
        // bin[0,*]={0,0,255}  bin[1,*]={255,255,255}  bin[2,*]={0,0,255}
        var bin = new byte[,]
        {
            { 0, 0, 255 },
            { 255, 255, 255 },
            { 0, 0, 255 }
        };
        var proj = ProjectionProcessor.HorizontalProjection(bin, 128);
        proj.Length.ShouldBe(3);
        proj[0].ShouldBe(2); // second dim index 0: bin[0,0]=0, bin[1,0]=255, bin[2,0]=0 → 2 foreground
        proj[1].ShouldBe(2); // second dim index 1: bin[0,1]=0, bin[1,1]=255, bin[2,1]=0 → 2 foreground
        proj[2].ShouldBe(0); // second dim index 2: all 255 → 0 foreground
    }

    [Fact]
    public void FindVerticalSegments_FindsCorrectSegments()
    {
        var projection = new[] { 0, 3, 3, 0, 0, 2, 2, 2, 0 };
        var segments = ProjectionProcessor.FindVerticalSegments(projection, 1);
        segments.Count.ShouldBe(2);
        segments[0].Start.ShouldBe(1);
        segments[0].End.ShouldBe(2);
        segments[1].Start.ShouldBe(5);
        segments[1].End.ShouldBe(7);
    }

    [Fact]
    public void FindVerticalSegments_NoForeground_ReturnsEmpty()
    {
        var projection = new[] { 0, 0, 0 };
        var segments = ProjectionProcessor.FindVerticalSegments(projection, 1);
        segments.Count.ShouldBe(0);
    }

    [Fact]
    public void FindVerticalSegments_AllForeground_ReturnsSingleSegment()
    {
        var projection = new[] { 5, 5, 5 };
        var segments = ProjectionProcessor.FindVerticalSegments(projection, 1);
        segments.Count.ShouldBe(1);
        segments[0].Start.ShouldBe(0);
        segments[0].End.ShouldBe(2);
    }

    [Fact]
    public void SplitByVerticalProjection_SplitsCharacters()
    {
        // [7,1] array: columns 0-2 foreground, column 3 background, columns 4-6 foreground
        var bin = new byte[,]
        {
            { 0 },
            { 0 },
            { 0 },
            { 255 },
            { 0 },
            { 0 },
            { 0 }
        };
        var splits = ProjectionProcessor.SplitByVerticalProjection(bin, 128, 0, 0);
        splits.Count.ShouldBe(2);
    }

    [Fact]
    public void ToCodeString_BreakLine_IncludesLineBreaks()
    {
        var bin = new byte[,]
        {
            { 0, 255 },
            { 255, 0 }
        };
        var code = ProjectionProcessor.ToCodeString(bin, 128, true);
        code.ShouldContain("\r\n");
        code.Length.ShouldBeGreaterThan(4);
    }

    [Fact]
    public void ToCodeString_NoBreakLine_SingleLine()
    {
        var bin = new byte[,]
        {
            { 0, 255 },
            { 255, 0 }
        };
        var code = ProjectionProcessor.ToCodeString(bin, 128, false);
        code.ShouldNotContain("\r\n");
        code.ShouldBe("1001");
    }
}

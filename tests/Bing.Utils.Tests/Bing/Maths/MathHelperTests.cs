using Bing.Utils.Maths;

namespace Bing.Utils.Tests.Bing.Maths;

/// <summary>
/// <see cref="MathHelper"/> 单元测试
/// </summary>
public class MathHelperTests
{
    [Fact]
    public void GetDistance_SamePoint_ReturnsZero() =>
        MathHelper.GetDistance(0, 0, 0, 0).ShouldBe(0.0);

    [Fact]
    public void GetDistance_ThreeFourFiveTriangle_ReturnsFive() =>
        MathHelper.GetDistance(0, 0, 3, 4).ShouldBe(5.0);

    [Fact]
    public void GetDistance_HorizontalLine_ReturnsCorrectDistance() =>
        MathHelper.GetDistance(0, 0, 10, 0).ShouldBe(10.0);

    [Fact]
    public void GetDistance_VerticalLine_ReturnsCorrectDistance() =>
        MathHelper.GetDistance(0, 0, 0, 7).ShouldBe(7.0);

    [Fact]
    public void GetDistance_IsSymmetric()
    {
        var d1 = MathHelper.GetDistance(1, 2, 5, 6);
        var d2 = MathHelper.GetDistance(5, 6, 1, 2);
        d1.ShouldBe(d2);
    }

    [Fact]
    public void GetDistance_NegativeCoordinates_ReturnsPositiveDistance() =>
        MathHelper.GetDistance(-3, -4, 0, 0).ShouldBe(5.0);
}

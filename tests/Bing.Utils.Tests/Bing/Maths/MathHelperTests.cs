using Bing.Utils.Maths;

namespace Bing.Utils.Tests.Bing.Maths;

/// <summary>
/// <see cref="MathHelper"/> 单元测试
/// </summary>
public class MathHelperTests
{
    /// <summary>
    /// 验证相同平面坐标之间的距离为零。
    /// </summary>
    [Fact]
    public void GetDistance_SamePoint_ReturnsZero() =>
        MathHelper.GetDistance(0, 0, 0, 0).ShouldBe(0.0);

    /// <summary>
    /// 验证三四五直角三角形的斜边距离为五。
    /// </summary>
    [Fact]
    public void GetDistance_ThreeFourFiveTriangle_ReturnsFive() =>
        MathHelper.GetDistance(0, 0, 3, 4).ShouldBe(5.0);

    /// <summary>
    /// 验证水平线两端的距离等于横坐标差。
    /// </summary>
    [Fact]
    public void GetDistance_HorizontalLine_ReturnsCorrectDistance() =>
        MathHelper.GetDistance(0, 0, 10, 0).ShouldBe(10.0);

    /// <summary>
    /// 验证竖直线两端的距离等于纵坐标差。
    /// </summary>
    [Fact]
    public void GetDistance_VerticalLine_ReturnsCorrectDistance() =>
        MathHelper.GetDistance(0, 0, 0, 7).ShouldBe(7.0);

    /// <summary>
    /// 验证交换平面坐标后距离不变。
    /// </summary>
    [Fact]
    public void GetDistance_IsSymmetric()
    {
        var d1 = MathHelper.GetDistance(1, 2, 5, 6);
        var d2 = MathHelper.GetDistance(5, 6, 1, 2);
        d1.ShouldBe(d2);
    }

    /// <summary>
    /// 验证负坐标参与距离计算时返回正距离。
    /// </summary>
    [Fact]
    public void GetDistance_NegativeCoordinates_ReturnsPositiveDistance() =>
        MathHelper.GetDistance(-3, -4, 0, 0).ShouldBe(5.0);
}

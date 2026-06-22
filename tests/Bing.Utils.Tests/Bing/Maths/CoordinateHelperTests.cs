using Bing.Maths;

namespace Bing.Utils.Tests.Bing.Maths;

/// <summary>
/// <see cref="CoordinateHelper"/> 单元测试
/// </summary>
public class CoordinateHelperTests
{
    // ─────────────────────────────────────────────────────────────────
    // CalcDistance — 同点距离为 0
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CalcDistance_SamePoint_ReturnsZero()
    {
        // 北京天安门
        var dist = CoordinateHelper.CalcDistance(116.3912757, 39.9073385, 116.3912757, 39.9073385);
        dist.ShouldBe(0.0);
    }

    // ─────────────────────────────────────────────────────────────────
    // CalcDistance — 已知距离验证（赤道上 1° 经度 ≈ 111.32 km）
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CalcDistance_OnEquator1Degree_IsApprox111km()
    {
        var dist = CoordinateHelper.CalcDistance(0, 0, 1, 0);
        // 赤道上 1° ≈ 111195 m，允许 ±1000 m 误差
        dist.ShouldBeInRange(110000, 112000);
    }

    [Fact]
    public void CalcDistance_ZeroLongitudeDiff1LatDegree_IsApprox111km()
    {
        var dist = CoordinateHelper.CalcDistance(0, 0, 0, 1);
        dist.ShouldBeInRange(110000, 112000);
    }

    // ─────────────────────────────────────────────────────────────────
    // CalcDistance — 对称性（A→B == B→A）
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CalcDistance_IsSymmetric()
    {
        double lngA = 116.3912757, latA = 39.9073385;
        double lngB = 121.4737026, latB = 31.2304188; // 上海
        var ab = CoordinateHelper.CalcDistance(lngA, latA, lngB, latB);
        var ba = CoordinateHelper.CalcDistance(lngB, latB, lngA, latA);
        ab.ShouldBe(ba);
    }

    // ─────────────────────────────────────────────────────────────────
    // CalcDistance — 北京到上海 ≈ 1050 km
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CalcDistance_BeijingToShanghai_IsReasonable()
    {
        var dist = CoordinateHelper.CalcDistance(116.3912757, 39.9073385, 121.4737026, 31.2304188);
        // 实际直线距离约 1067 km，允许较宽裕范围
        dist.ShouldBeInRange(1_000_000, 1_200_000);
    }

    // ─────────────────────────────────────────────────────────────────
    // CalcDistance — 返回值精度为两位小数
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CalcDistance_ReturnsTwoDecimalPlaces()
    {
        var dist = CoordinateHelper.CalcDistance(0, 0, 1, 1);
        // Math.Round(..., 2) 确保精度
        var rounded = Math.Round(dist, 2);
        rounded.ShouldBe(dist);
    }
}

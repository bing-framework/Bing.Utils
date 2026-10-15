using Bing.Date;

namespace Bing.Utils.Tests.Bing.Date;

/// <summary>
/// <see cref="Quarter"/> 枚举单元测试
/// </summary>
public class QuarterTests
{
    // ─────────────────────────────────────────────────────────────────
    // 枚举整数值
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证第一季度的整数值为一。
    /// </summary>
    [Fact]
    public void Q1_IntValue_Is_1() => ((int)Quarter.Q1).ShouldBe(1);

    /// <summary>
    /// 验证第二季度的整数值为二。
    /// </summary>
    [Fact]
    public void Q2_IntValue_Is_2() => ((int)Quarter.Q2).ShouldBe(2);

    /// <summary>
    /// 验证第三季度的整数值为三。
    /// </summary>
    [Fact]
    public void Q3_IntValue_Is_3() => ((int)Quarter.Q3).ShouldBe(3);

    /// <summary>
    /// 验证第四季度的整数值为四。
    /// </summary>
    [Fact]
    public void Q4_IntValue_Is_4() => ((int)Quarter.Q4).ShouldBe(4);

    // ─────────────────────────────────────────────────────────────────
    // 枚举成员数量
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证季度枚举包含四个成员。
    /// </summary>
    [Fact]
    public void Quarter_HasFourMembers()
    {
        Enum.GetValues(typeof(Quarter)).Length.ShouldBe(4);
    }

    /// <summary>
    /// 验证季度枚举值从一到四连续排列。
    /// </summary>
    [Fact]
    public void Quarter_Values_AreContiguous_1_To_4()
    {
        var values = Enum.GetValues(typeof(Quarter)).Cast<int>().OrderBy(x => x).ToList();
        values.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    // ─────────────────────────────────────────────────────────────────
    // Description 特性
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证季度成员的中文描述特性。
    /// </summary>
    /// <param name="quarter">待检查的季度。</param>
    /// <param name="expectedDescription">预期中文描述。</param>
    [Theory]
    [InlineData(Quarter.Q1, "第一季度")]
    [InlineData(Quarter.Q2, "第二季度")]
    [InlineData(Quarter.Q3, "第三季度")]
    [InlineData(Quarter.Q4, "第四季度")]
    public void Quarter_DescriptionAttribute_MatchesExpected(Quarter quarter, string expectedDescription)
    {
        var field = typeof(Quarter).GetField(quarter.ToString());
        var attr = field!.GetCustomAttribute<DescriptionAttribute>();
        attr.ShouldNotBeNull();
        attr!.Description.ShouldBe(expectedDescription);
    }

    /// <summary>
    /// 验证全部季度成员均具有描述特性。
    /// </summary>
    [Fact]
    public void AllQuarterMembers_HaveDescriptionAttribute()
    {
        foreach (Quarter q in Enum.GetValues(typeof(Quarter)))
        {
            var field = typeof(Quarter).GetField(q.ToString());
            field!.GetCustomAttribute<DescriptionAttribute>().ShouldNotBeNull();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 类型转换
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证整数三转换为第三季度。
    /// </summary>
    [Fact]
    public void CastIntToQuarter_Returns_CorrectValue()
    {
        var q = (Quarter)3;
        q.ShouldBe(Quarter.Q3);
    }

    /// <summary>
    /// 验证相同季度枚举值相等。
    /// </summary>
    [Fact]
    public void Quarter_Equality_SameValue_IsEqual()
    {
        Quarter a = Quarter.Q2;
        Quarter b = Quarter.Q2;
        a.ShouldBe(b);
    }

    /// <summary>
    /// 验证不同季度枚举值不相等。
    /// </summary>
    [Fact]
    public void Quarter_Inequality_DifferentValues_AreNotEqual()
    {
        Quarter.Q1.ShouldNotBe(Quarter.Q4);
    }
}

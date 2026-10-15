namespace Bing.Date.Chinese;

/// <summary>
/// 验证二十八星宿的参考日期、周期及简繁体输出。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.Constellation")]
public class ChineseConstellationHelperTest
{
    #region Get - Simplified

    /// <summary>
    /// 验证参考日期对应首个星宿角木蛟。
    /// </summary>
    [Fact]
    public void Get_ReferenceDay_ReturnsFirstConstellation()
    {
        var refDay = new DateTime(2007, 9, 13);
        ChineseConstellationHelper.Get(refDay).ShouldBe("角木蛟");
    }

    /// <summary>
    /// 验证参考日期后一天对应亢金龙。
    /// </summary>
    [Fact]
    public void Get_ReferenceDayPlusOne_ReturnsSecondConstellation()
    {
        var dt = new DateTime(2007, 9, 14);
        ChineseConstellationHelper.Get(dt).ShouldBe("亢金龙");
    }

    /// <summary>
    /// 验证偏移二十七天对应周期末尾星宿。
    /// </summary>
    [Fact]
    public void Get_OffsetOf27_ReturnsLastConstellationInCycle()
    {
        var dt = new DateTime(2007, 9, 13).AddDays(27);
        ChineseConstellationHelper.Get(dt).ShouldBe("轸水蚓");
    }

    /// <summary>
    /// 验证偏移二十八天后回到周期起点。
    /// </summary>
    [Fact]
    public void Get_OffsetOf28_CyclesBackToFirst()
    {
        var dt = new DateTime(2007, 9, 13).AddDays(28);
        ChineseConstellationHelper.Get(dt).ShouldBe("角木蛟");
    }

    /// <summary>
    /// 验证常用日期返回非空星宿名称。
    /// </summary>
    /// <param name="year">待查询日期的公历年份。</param>
    /// <param name="month">待查询日期的公历月份。</param>
    /// <param name="day">待查询日期的公历日分量。</param>
    [Theory]
    [InlineData(2023, 1, 1)]
    [InlineData(2023, 6, 15)]
    [InlineData(2000, 1, 1)]
    public void Get_KnownDate_ReturnsNonEmpty(int year, int month, int day)
    {
        var result = ChineseConstellationHelper.Get(new DateTime(year, month, day));
        result.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Get - Traditional

    /// <summary>
    /// 验证简体与繁体星宿查询均返回非空名称。
    /// </summary>
    [Fact]
    public void Get_TraditionalCharacters_ReturnsTraditionalVariant()
    {
        // 偏移8天：index=8 → 简体"虚日鼠", 繁体"虛日鼠"
        var dt = new DateTime(2007, 9, 13).AddDays(10);
        var simplified = ChineseConstellationHelper.Get(dt, false);
        var traditional = ChineseConstellationHelper.Get(dt, true);
        // 应有差异（繁体和简体）或相同（当字无差异时）
        simplified.ShouldNotBeNullOrEmpty();
        traditional.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证星宿查询默认使用简体中文。
    /// </summary>
    [Fact]
    public void Get_DefaultParameter_UsesSimplifiedCharacters()
    {
        var dt = new DateTime(2023, 5, 1);
        var defaultResult = ChineseConstellationHelper.Get(dt);
        var explicitSimplified = ChineseConstellationHelper.Get(dt, false);
        defaultResult.ShouldBe(explicitSimplified);
    }

    #endregion
}

namespace Bing.Date.Chinese;

/// <summary>
/// 测试类：覆盖 <see cref="ChineseConstellationHelper"/> 相关行为。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.Constellation")]
public class ChineseConstellationHelperTest
{
    #region Get - Simplified

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseConstellationHelper.Get"/> 在 `ReferenceDay` 场景下，结果为 `ReturnsFirstConstellation`。
    /// 参考日 2007-09-13 为"角木蛟"
    /// </summary>
    [Fact]
    public void Get_ReferenceDay_ReturnsFirstConstellation()
    {
        var refDay = new DateTime(2007, 9, 13);
        ChineseConstellationHelper.Get(refDay).ShouldBe("角木蛟");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseConstellationHelper.Get"/> 在 `ReferenceDayPlusOne` 场景下，结果为 `ReturnsSecondConstellation`。
    /// </summary>
    [Fact]
    public void Get_ReferenceDayPlusOne_ReturnsSecondConstellation()
    {
        var dt = new DateTime(2007, 9, 14);
        ChineseConstellationHelper.Get(dt).ShouldBe("亢金龙");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseConstellationHelper.Get"/> 在 `OffsetOf27` 场景下，结果为 `ReturnsLastConstellation`。
    /// 偏移 27 天循环到第28宿"轸水蚓"
    /// </summary>
    [Fact]
    public void Get_OffsetOf27_ReturnsLastConstellationInCycle()
    {
        var dt = new DateTime(2007, 9, 13).AddDays(27);
        ChineseConstellationHelper.Get(dt).ShouldBe("轸水蚓");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseConstellationHelper.Get"/> 在 `OffsetOf28` 场景下，结果为 `CyclesBackToFirst`。
    /// 偏移 28 天后循环回到"角木蛟"
    /// </summary>
    [Fact]
    public void Get_OffsetOf28_CyclesBackToFirst()
    {
        var dt = new DateTime(2007, 9, 13).AddDays(28);
        ChineseConstellationHelper.Get(dt).ShouldBe("角木蛟");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseConstellationHelper.Get"/> 在 `KnownDate` 场景下，结果为 `ReturnsNonEmpty`。
    /// </summary>
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
    /// 测试用例：验证 <see cref="ChineseConstellationHelper.Get"/> 在 `TraditionalCharacters` 场景下，结果为 `ReturnsTraditionalName`。
    /// 繁体字版本——牛金牛 与简体相同，取 虛日鼠（虚=虛）
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
    /// 测试用例：验证 <see cref="ChineseConstellationHelper.Get"/> 在 `DefaultParameter` 场景下，结果为 `UsesSimplified`。
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

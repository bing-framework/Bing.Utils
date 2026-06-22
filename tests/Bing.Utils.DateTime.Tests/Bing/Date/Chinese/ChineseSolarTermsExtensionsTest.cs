namespace Bing.Date.Chinese;

/// <summary>
/// 测试类：覆盖 <see cref="ChineseSolarTermsExtensions"/> 相关行为。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.SolarTermsExtensions")]
public class ChineseSolarTermsExtensionsTest
{
    #region GetName

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseSolarTermsExtensions.GetName"/> 在 `WithSimplifiedChinese` 场景下，结果为 `ReturnsSimplifiedName`。
    /// </summary>
    [Theory]
    [InlineData(ChineseSolarTerms.BeginningOfSpring, false, "立春")]
    [InlineData(ChineseSolarTerms.TheWakingOfInsects, false, "惊蛰")]
    [InlineData(ChineseSolarTerms.QingmingFestival, false, "清明")]
    [InlineData(ChineseSolarTerms.SummerSolstice, false, "夏至")]
    [InlineData(ChineseSolarTerms.BeginningOfAutumn, false, "立秋")]
    [InlineData(ChineseSolarTerms.WinterSolstice, false, "冬至")]
    public void GetName_WithSimplifiedChinese_ReturnsSimplifiedName(ChineseSolarTerms term, bool traditional, string expected)
    {
        term.GetName(traditional).ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseSolarTermsExtensions.GetName"/> 在 `WithTraditionalChinese` 场景下，结果为 `ReturnsTraditionalName`。
    /// </summary>
    [Theory]
    [InlineData(ChineseSolarTerms.TheWakingOfInsects, true, "驚蟄")]
    [InlineData(ChineseSolarTerms.QingmingFestival, true, "清明")]
    public void GetName_WithTraditionalChinese_ReturnsTraditionalName(ChineseSolarTerms term, bool traditional, string expected)
    {
        term.GetName(traditional).ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseSolarTermsExtensions.GetName"/> 在 `DefaultParameter` 场景下，结果为 `UsesSimplifiedChinese`。
    /// </summary>
    [Fact]
    public void GetName_DefaultParameter_UsesSimplifiedChinese()
    {
        var simplified = ChineseSolarTerms.VernalEquinox.GetName();
        var explicitSimplified = ChineseSolarTerms.VernalEquinox.GetName(false);
        simplified.ShouldBe(explicitSimplified);
        simplified.ShouldBe("春分");
    }

    #endregion

    #region GetEnglishName

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseSolarTermsExtensions.GetEnglishName"/> 在 `ValidTerms` 场景下，结果为 `ReturnsCorrectEnglishName`。
    /// </summary>
    [Theory]
    [InlineData(ChineseSolarTerms.BeginningOfSpring, "Beginning of Spring")]
    [InlineData(ChineseSolarTerms.VernalEquinox, "Vernal Equinox")]
    [InlineData(ChineseSolarTerms.QingmingFestival, "Qingming Festival")]
    [InlineData(ChineseSolarTerms.SummerSolstice, "Summer Solstice")]
    [InlineData(ChineseSolarTerms.WinterSolstice, "Winter Solstice")]
    public void GetEnglishName_ValidTerms_ReturnsCorrectEnglishName(ChineseSolarTerms term, string expected)
    {
        term.GetEnglishName().ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ChineseSolarTermsExtensions.GetEnglishName"/> 在 `DelegatesStaticHelper` 场景下，结果为 `ConsistentWithHelper`。
    /// </summary>
    [Fact]
    public void GetEnglishName_DelegatesStaticHelper_ConsistentWithHelper()
    {
        var term = ChineseSolarTerms.BeginningOfAutumn;
        var ext = term.GetEnglishName();
        var helper = ChineseSolarTermHelper.GetEnglishName(term);
        ext.ShouldBe(helper);
    }

    #endregion
}

namespace Bing.Date.Chinese;

/// <summary>
/// 验证节气枚举扩展的中文与英文名称输出。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.SolarTermsExtensions")]
public class ChineseSolarTermsExtensionsTest
{
    #region GetName

    /// <summary>
    /// 验证节气名称按简体中文输出。
    /// </summary>
    /// <param name="term">待查询的节气枚举值。</param>
    /// <param name="traditional">是否使用繁体中文。</param>
    /// <param name="expected">预期节气名称。</param>
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
    /// 验证节气名称按繁体中文输出。
    /// </summary>
    /// <param name="term">待查询的节气枚举值。</param>
    /// <param name="traditional">是否使用繁体中文。</param>
    /// <param name="expected">预期节气名称。</param>
    [Theory]
    [InlineData(ChineseSolarTerms.TheWakingOfInsects, true, "驚蟄")]
    [InlineData(ChineseSolarTerms.QingmingFestival, true, "清明")]
    public void GetName_WithTraditionalChinese_ReturnsTraditionalName(ChineseSolarTerms term, bool traditional, string expected)
    {
        term.GetName(traditional).ShouldBe(expected);
    }

    /// <summary>
    /// 验证节气名称默认使用简体中文。
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
    /// 验证节气枚举返回对应英文名称。
    /// </summary>
    /// <param name="term">待查询的节气枚举值。</param>
    /// <param name="expected">预期英文节气名称。</param>
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
    /// 验证英文名称扩展与静态帮助方法返回一致结果。
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

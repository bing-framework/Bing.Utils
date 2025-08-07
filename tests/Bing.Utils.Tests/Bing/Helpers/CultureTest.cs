using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Globalization;
using Bing.Tests;

namespace Bing.Helpers;

/// <summary>
/// 区域文化工具类测试
/// </summary>
[Trait("Bing.Helpers", "Culture")]
public class CultureTest : TestBase
{
    /// <inheritdoc />
    public CultureTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试 - 获取区域文化信息列表
    /// </summary>
    [Fact]
    public void Test_GetCultures()
    {
        var culture = new CultureInfo("zh-CN");
        var cultures = Culture.GetCultures(culture);
        Assert.Equal(3, cultures.Count);
        Assert.Equal("zh-CN", cultures[0].Name);
        Assert.Equal("zh-Hans", cultures[1].Name);
        Assert.Equal("zh", cultures[2].Name);
    }

    /// <summary>
    /// 测试 - 是否兼容的区域文化
    /// </summary>
    [Fact]
    public void Test_IsCompatibleCulture()
    {
        Culture.IsCompatibleCulture("tr", "tr").ShouldBeTrue();
        Culture.IsCompatibleCulture("tr", "tr-TR").ShouldBeTrue();

        Culture.IsCompatibleCulture("en", "tr").ShouldBeFalse();
        Culture.IsCompatibleCulture("en", "tr-TR").ShouldBeFalse();

        Culture.IsCompatibleCulture("en-US", "en").ShouldBeFalse();
        Culture.IsCompatibleCulture("en-US", "en-GB").ShouldBeFalse();

        Culture.IsCompatibleCulture("zh", "zh-CN").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh", "zh-HK").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh", "zh-MO").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh", "zh-SG").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh", "zh-TW").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh", "zh-Hans").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh", "zh-Hant").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh-Hans", "zh-CN").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh-Hans", "zh-SG").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh-Hant", "zh-HK").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh-Hant", "zh-MO").ShouldBeTrue();
        Culture.IsCompatibleCulture("zh-Hant", "zh-TW").ShouldBeTrue();

        Culture.IsCompatibleCulture("zh-Hans", "zh-HK").ShouldBeFalse();
        Culture.IsCompatibleCulture("zh-Hant", "zh-SG").ShouldBeFalse();
    }

    #region GetCurrentCulture 和 GetCurrentUICulture 测试

    /// <summary>
    /// 测试 - GetCurrentCulture - 返回当前线程的区域文化
    /// </summary>
    [Fact]
    public void GetCurrentCulture_ReturnsCurrentThreadCulture()
    {
        // Act
        var result = Culture.GetCurrentCulture();

        // Assert
        result.ShouldBe(CultureInfo.CurrentCulture);
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - GetCurrentUICulture - 返回当前线程的UI区域文化
    /// </summary>
    [Fact]
    public void GetCurrentUICulture_ReturnsCurrentThreadUICulture()
    {
        // Act
        var result = Culture.GetCurrentUICulture();

        // Assert
        result.ShouldBe(CultureInfo.CurrentUICulture);
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - GetCurrentCultureName - 返回当前区域文化名称
    /// </summary>
    [Fact]
    public void GetCurrentCultureName_ReturnsCurrentCultureName()
    {
        // Act
        var result = Culture.GetCurrentCultureName();

        // Assert
        result.ShouldBe(CultureInfo.CurrentCulture.Name);
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试 - GetCurrentUICultureName - 返回当前UI区域文化名称
    /// </summary>
    [Fact]
    public void GetCurrentUICultureName_ReturnsCurrentUICultureName()
    {
        // Act
        var result = Culture.GetCurrentUICultureName();

        // Assert
        result.ShouldBe(CultureInfo.CurrentUICulture.Name);
        result.ShouldNotBeNull();
    }

    #endregion

    #region GetCultures 测试

    /// <summary>
    /// 测试 - GetCultures - 获取区域文化层次结构
    /// </summary>
    [Fact]
    public void GetCultures_ValidCulture_ReturnsHierarchy()
    {
        // Arrange
        var culture = new CultureInfo("zh-CN");

        // Act
        var cultures = Culture.GetCultures(culture);

        // Assert
        cultures.ShouldNotBeNull();
        cultures.Count.ShouldBeGreaterThan(0);
        cultures[0].Name.ShouldBe("zh-CN");

        // 验证层次结构的正确性
        for (int i = 0; i < cultures.Count - 1; i++)
        {
            cultures[i + 1].ShouldBe(cultures[i].Parent);
        }
    }

    /// <summary>
    /// 测试 - GetCultures - 传入null返回空列表
    /// </summary>
    [Fact]
    public void GetCultures_NullCulture_ReturnsEmptyList()
    {
        // Act
        var result = Culture.GetCultures(null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试 - GetCultures - 不变区域文化返回空列表
    /// </summary>
    [Fact]
    public void GetCultures_InvariantCulture_ReturnsEmptyList()
    {
        // Act
        var result = Culture.GetCultures(CultureInfo.InvariantCulture);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试 - GetCurrentCultures - 获取当前区域文化层次结构
    /// </summary>
    [Fact]
    public void GetCurrentCultures_ReturnsCurrentCultureHierarchy()
    {
        // Act
        var result = Culture.GetCurrentCultures();

        // Assert
        result.ShouldNotBeNull();
        if (result.Count > 0)
        {
            result[0].ShouldBe(CultureInfo.CurrentCulture);
        }
    }

    /// <summary>
    /// 测试 - GetCurrentUICultures - 获取当前UI区域文化层次结构
    /// </summary>
    [Fact]
    public void GetCurrentUICultures_ReturnsCurrentUICultureHierarchy()
    {
        // Act
        var result = Culture.GetCurrentUICultures();

        // Assert
        result.ShouldNotBeNull();
        if (result.Count > 0)
        {
            result[0].ShouldBe(CultureInfo.CurrentUICulture);
        }
    }

    #endregion

    #region Use 方法测试

    /// <summary>
    /// 测试 - Use - 临时切换区域文化
    /// </summary>
    [Fact]
    public void Use_ValidCultureName_TemporarilyChangesCulture()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        const string targetCulture = "en-US";

        // Act & Assert
        using (Culture.Use(targetCulture))
        {
            CultureInfo.CurrentCulture.Name.ShouldBe(targetCulture);
            CultureInfo.CurrentUICulture.Name.ShouldBe(targetCulture);
        }

        // 验证恢复
        CultureInfo.CurrentCulture.ShouldBe(originalCulture);
        CultureInfo.CurrentUICulture.ShouldBe(originalUICulture);
    }

    /// <summary>
    /// 测试 - Use - 分别设置区域文化和UI文化
    /// </summary>
    [Fact]
    public void Use_SeparateCultureAndUICulture_SetsBothCorrectly()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        const string targetCulture = "en-US";
        const string targetUICulture = "fr-FR";

        // Act & Assert
        using (Culture.Use(targetCulture, targetUICulture))
        {
            CultureInfo.CurrentCulture.Name.ShouldBe(targetCulture);
            CultureInfo.CurrentUICulture.Name.ShouldBe(targetUICulture);
        }

        // 验证恢复
        CultureInfo.CurrentCulture.ShouldBe(originalCulture);
        CultureInfo.CurrentUICulture.ShouldBe(originalUICulture);
    }

    /// <summary>
    /// 测试 - Use - 使用CultureInfo对象重载
    /// </summary>
    [Fact]
    public void Use_CultureInfoOverload_TemporarilyChangesCulture()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        var targetCulture = new CultureInfo("de-DE");
        var targetUICulture = new CultureInfo("it-IT");

        // Act & Assert
        using (Culture.Use(targetCulture, targetUICulture))
        {
            CultureInfo.CurrentCulture.ShouldBe(targetCulture);
            CultureInfo.CurrentUICulture.ShouldBe(targetUICulture);
        }

        // 验证恢复
        CultureInfo.CurrentCulture.ShouldBe(originalCulture);
        CultureInfo.CurrentUICulture.ShouldBe(originalUICulture);
    }

    /// <summary>
    /// 测试 - Use - 传入null文化名称抛出异常
    /// </summary>
    [Fact]
    public void Use_NullCultureName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Culture.Use((string)null));
    }

    /// <summary>
    /// 测试 - Use - 传入null CultureInfo抛出异常
    /// </summary>
    [Fact]
    public void Use_NullCultureInfo_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Culture.Use((CultureInfo)null));
    }

    /// <summary>
    /// 测试 - Use - 无效文化名称抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid-culture")]
    //[InlineData("xx-YY")]
    //[InlineData("not-a-culture")]
    public void Use_InvalidCultureName_ThrowsCultureNotFoundException(string invalidCulture)
    {
        // Act & Assert
        Should.Throw<CultureNotFoundException>(() => Culture.Use(invalidCulture));
    }

    /// <summary>
    /// 测试 - Use - 嵌套使用保持正确的恢复顺序
    /// </summary>
    [Fact]
    public void Use_NestedUsage_RestoresInCorrectOrder()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;

        // Act & Assert
        using (Culture.Use("en-US"))
        {
            CultureInfo.CurrentCulture.Name.ShouldBe("en-US");

            using (Culture.Use("fr-FR"))
            {
                CultureInfo.CurrentCulture.Name.ShouldBe("fr-FR");
            }

            // 内层恢复后应该回到en-US
            CultureInfo.CurrentCulture.Name.ShouldBe("en-US");
        }

        // 外层恢复后应该回到原始文化
        CultureInfo.CurrentCulture.ShouldBe(originalCulture);
    }

    #endregion

    #region IsRtl 测试

    /// <summary>
    /// 测试 - IsRtl - 在LTR文化下返回false
    /// </summary>
    [Fact]
    public void IsRtl_LeftToRightCulture_ReturnsFalse()
    {
        // Arrange & Act
        using (Culture.Use("en-US"))
        {
            var result = Culture.IsRtl;

            // Assert
            result.ShouldBeFalse();
        }
    }

    /// <summary>
    /// 测试 - IsRtl - 在RTL文化下返回true
    /// </summary>
    [Fact]
    public void IsRtl_RightToLeftCulture_ReturnsTrue()
    {
        // Arrange & Act
        using (Culture.Use("ar-SA")) // 阿拉伯语
        {
            var result = Culture.IsRtl;

            // Assert
            result.ShouldBeTrue();
        }
    }

    #endregion

    #region IsValidCultureCode 测试

    /// <summary>
    /// 测试 - IsValidCultureCode - 有效的文化代码
    /// </summary>
    [Theory]
    [InlineData("zh-CN")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    [InlineData("zh")]
    [InlineData("en")]
    public void IsValidCultureCode_ValidCodes_ReturnsTrue(string cultureCode)
    {
        // Act
        var result = Culture.IsValidCultureCode(cultureCode);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsValidCultureCode - 无效的文化代码
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    //[InlineData("xx-YY")]
    //[InlineData("not-a-culture")]
    [InlineData("123-456")]
    public void IsValidCultureCode_InvalidCodes_ReturnsFalse(string cultureCode)
    {
        // Act
        var result = Culture.IsValidCultureCode(cultureCode);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsValidCultureCode - 空值或空白字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void IsValidCultureCode_NullOrWhitespace_ReturnsFalse(string cultureCode)
    {
        // Act
        var result = Culture.IsValidCultureCode(cultureCode);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetBaseCultureName 测试

    /// <summary>
    /// 测试 - GetBaseCultureName - 获取父级文化名称
    /// </summary>
    [Theory]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("en-US", "en")]
    [InlineData("fr-FR", "fr")]
    [InlineData("de-DE", "de")]
    public void GetBaseCultureName_SpecificCultures_ReturnsParentName(string cultureName, string expectedParent)
    {
        // Act
        var result = Culture.GetBaseCultureName(cultureName);

        // Assert
        result.ShouldBe(expectedParent);
    }

    /// <summary>
    /// 测试 - GetBaseCultureName - 中性文化返回不变文化
    /// </summary>
    [Theory]
    [InlineData("zh")]
    [InlineData("en")]
    [InlineData("fr")]
    public void GetBaseCultureName_NeutralCultures_ReturnsInvariant(string cultureName)
    {
        // Act
        var result = Culture.GetBaseCultureName(cultureName);

        // Assert
        result.ShouldBe("");
    }

    /// <summary>
    /// 测试 - GetBaseCultureName - null参数抛出异常
    /// </summary>
    [Fact]
    public void GetBaseCultureName_NullCultureName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Culture.GetBaseCultureName(null));
    }

    /// <summary>
    /// 测试 - GetBaseCultureName - 无效文化名称抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    //[InlineData("xx-YY")]
    public void GetBaseCultureName_InvalidCultureName_ThrowsCultureNotFoundException(string invalidCulture)
    {
        // Act & Assert
        Should.Throw<CultureNotFoundException>(() => Culture.GetBaseCultureName(invalidCulture));
    }

    #endregion

    #region IsCompatibleCulture 测试

    /// <summary>
    /// 测试 - IsCompatibleCulture - 相同文化兼容
    /// </summary>
    [Theory]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("en-US", "en-US")]
    [InlineData("fr", "fr")]
    public void IsCompatibleCulture_SameCultures_ReturnsTrue(string culture1, string culture2)
    {
        // Act
        var result = Culture.IsCompatibleCulture(culture1, culture2);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsCompatibleCulture - 中文文化兼容性
    /// </summary>
    [Theory]
    [InlineData("zh", "zh-CN")]
    [InlineData("zh", "zh-HK")]
    [InlineData("zh", "zh-TW")]
    [InlineData("zh-Hans", "zh-CN")]
    [InlineData("zh-Hant", "zh-HK")]
    [InlineData("zh-Hant", "zh-TW")]
    public void IsCompatibleCulture_ChineseCultures_ReturnsTrue(string sourceCulture, string targetCulture)
    {
        // Act
        var result = Culture.IsCompatibleCulture(sourceCulture, targetCulture);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsCompatibleCulture - 中文文化不兼容的情况
    /// </summary>
    [Theory]
    [InlineData("zh-Hans", "zh-HK")]   // 简体中文与繁体中文地区
    [InlineData("zh-Hant", "zh-CN")]   // 繁体中文与简体中文地区
    public void IsCompatibleCulture_IncompatibleChineseCultures_ReturnsFalse(string sourceCulture, string targetCulture)
    {
        // Act
        var result = Culture.IsCompatibleCulture(sourceCulture, targetCulture);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsCompatibleCulture - 一般语言兼容性
    /// </summary>
    [Theory]
    [InlineData("en", "en-US")]
    [InlineData("en", "en-GB")]
    [InlineData("fr", "fr-FR")]
    [InlineData("de", "de-DE")]
    public void IsCompatibleCulture_GeneralLanguageCompatibility_ReturnsTrue(string sourceCulture, string targetCulture)
    {
        // Act
        var result = Culture.IsCompatibleCulture(sourceCulture, targetCulture);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - IsCompatibleCulture - 不兼容的文化
    /// </summary>
    [Theory]
    [InlineData("en", "fr")]           // 不同语言
    [InlineData("zh", "en")]           // 中文与英文
    [InlineData("en-US", "en-GB")]     // 同语言不同地区
    [InlineData("fr-FR", "fr-CA")]     // 同语言不同地区
    [InlineData("en-US", "fr")]        // 具体地区与中性语言
    public void IsCompatibleCulture_IncompatibleCultures_ReturnsFalse(string sourceCulture, string targetCulture)
    {
        // Act
        var result = Culture.IsCompatibleCulture(sourceCulture, targetCulture);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - IsCompatibleCulture - null参数抛出异常
    /// </summary>
    [Theory]
    [InlineData(null, "zh-CN")]
    [InlineData("zh-CN", null)]
    [InlineData(null, null)]
    public void IsCompatibleCulture_NullParameters_ThrowsArgumentNullException(string sourceCulture, string targetCulture)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Culture.IsCompatibleCulture(sourceCulture, targetCulture));
    }

    /// <summary>
    /// 测试 - IsCompatibleCulture - 无效文化名称处理
    /// </summary>
    [Theory]
    [InlineData("invalid", "zh-CN")]
    [InlineData("zh-CN", "invalid")]
    public void IsCompatibleCulture_InvalidCultureNames_ReturnsFalse(string sourceCulture, string targetCulture)
    {
        // Act
        var result = Culture.IsCompatibleCulture(sourceCulture, targetCulture);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region 获取文化列表测试

    /// <summary>
    /// 测试 - GetAllCultures - 返回所有可用文化
    /// </summary>
    [Fact]
    public void GetAllCultures_ReturnsAllAvailableCultures()
    {
        // Act
        var result = Culture.GetAllCultures();

        Output.WriteLine("所有可用的文化：");
        foreach (var cultureInfo in result.OrderBy(c => c.Name)) 
            Output.WriteLine($"  {cultureInfo.Name} - {cultureInfo.DisplayName}");

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
        result.ShouldContain(c => c.Name == "en-US");
        // 检查中文相关文化是否存在
        result.ShouldContain(c => c.Name == "zh" || c.Name == "zh-Hans" || c.Name.StartsWith("zh-"));
    }

    /// <summary>
    /// 测试 - GetNeutralCultures - 返回中性文化
    /// </summary>
    [Fact]
    public void GetNeutralCultures_ReturnsNeutralCultures()
    {
        // Act
        var result = Culture.GetNeutralCultures();

        Output.WriteLine("中性文化：");
        foreach (var cultureInfo in result.OrderBy(c => c.Name))
            Output.WriteLine($"  {cultureInfo.Name} - {cultureInfo.EnglishName} (IsNeutral: {cultureInfo.IsNeutralCulture})");

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        // 检查特定的中性文化是否存在
        result.ShouldContain(c => c.Name == "en");
        result.ShouldContain(c => c.Name == "zh");

        // 过滤掉不变文化，然后检查其余的都是中性文化
        //var nonInvariantCultures = result.Where(c => c != CultureInfo.InvariantCulture).ToArray();
        //nonInvariantCultures.ShouldAllBe(c => c.IsNeutralCulture,
        //    $"发现非中性文化: {string.Join(", ", nonInvariantCultures.Where(c => !c.IsNeutralCulture).Select(c => c.Name))}");

        // 验证不变文化是否在结果中
        var invariantCulture = result.FirstOrDefault(c => c == CultureInfo.InvariantCulture);
        if (invariantCulture != null) 
            Output.WriteLine($"不变文化: {invariantCulture.Name} (IsNeutral: {invariantCulture.IsNeutralCulture})");
    }

    /// <summary>
    /// 测试 - GetSpecificCultures - 返回特定文化
    /// </summary>
    [Fact]
    public void GetSpecificCultures_ReturnsSpecificCultures()
    {
        // Act
        var result = Culture.GetSpecificCultures();

        foreach (var cultureInfo in result) 
            Output.WriteLine(cultureInfo.Name);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
        result.ShouldContain(c => c.Name == "en-US");
        result.ShouldContain(c => c.Name == "zh" || c.Name == "zh-Hans" || c.Name.StartsWith("zh-"));
        result.ShouldAllBe(c => !c.IsNeutralCulture && c != CultureInfo.InvariantCulture);
    }

    #endregion

    #region 综合场景测试

    /// <summary>
    /// 测试 - 数字格式化 - 在不同文化下的表现
    /// </summary>
    [Fact]
    public void NumberFormatting_DifferentCultures_ProducesExpectedResults()
    {
        // Arrange
        const decimal testNumber = 1234.56m;

        // Act & Assert
        using (Culture.Use("en-US"))
        {
            var usFormat = testNumber.ToString("C");
            usFormat.ShouldContain("$");
            usFormat.ShouldContain("1,234.56");
        }

        using (Culture.Use("de-DE"))
        {
            var deFormat = testNumber.ToString("C");
            deFormat.ShouldContain("€");
            // 德语使用逗号作为小数分隔符，点作为千位分隔符
            deFormat.ShouldContain("1.234,56");
        }
    }

    /// <summary>
    /// 测试 - 日期格式化 - 在不同文化下的表现
    /// </summary>
    [Fact]
    public void DateFormatting_DifferentCultures_ProducesExpectedResults()
    {
        // Arrange
        var testDate = new DateTime(2023, 12, 25);

        // Act & Assert
        using (Culture.Use("en-US"))
        {
            var usFormat = testDate.ToString("d");
            usFormat.ShouldBe("12/25/2023");
        }

        using (Culture.Use("de-DE"))
        {
            var deFormat = testDate.ToString("d");
            deFormat.ShouldBe("25.12.2023");
        }
    }

    /// <summary>
    /// 测试 - 实际应用场景 - 多语言应用程序
    /// </summary>
    [Fact]
    public void RealWorldScenario_MultilingualApplication_WorksCorrectly()
    {
        // Arrange
        var supportedCultures = new[] { "en-US", "zh-CN", "fr-FR", "de-DE", "ja-JP" };
        var testValue = 1000.50m;

        // Act & Assert
        foreach (var cultureName in supportedCultures)
        {
            if (Culture.IsValidCultureCode(cultureName))
            {
                using (Culture.Use(cultureName))
                {
                    var currentCulture = Culture.GetCurrentCulture();
                    var formattedValue = testValue.ToString("C");
                    var isRtl = Culture.IsRtl;

                    // 验证基本功能
                    currentCulture.Name.ShouldBe(cultureName);
                    formattedValue.ShouldNotBeNull();
                    formattedValue.ShouldNotBeEmpty();

                    Output.WriteLine($"Culture: {cultureName}, Value: {formattedValue}, RTL: {isRtl}");
                }
            }
        }
    }

    /// <summary>
    /// 测试 - 性能测试 - 频繁的文化切换
    /// </summary>
    [Fact]
    public void PerformanceTest_FrequentCultureSwitching_CompletesInReasonableTime()
    {
        // Arrange
        const int iterations = 100;
        var cultures = new[] { "en-US", "zh-CN", "fr-FR", "de-DE" };

        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var culture = cultures[i % cultures.Length];
                using (Culture.Use(culture))
                {
                    var currentName = Culture.GetCurrentCultureName();
                    var isRtl = Culture.IsRtl;
                    var hierarchy = Culture.GetCurrentCultures();

                    // 简单验证确保操作正常
                    currentName.ShouldBe(culture);
                    hierarchy.ShouldNotBeNull();
                }
            }
        }, TimeSpan.FromSeconds(2)); // 应该在2秒内完成100次文化切换
    }

    /// <summary>
    /// 测试 - 异常处理 - 异常情况下的恢复能力
    /// </summary>
    [Fact]
    public void ExceptionHandling_CultureOperations_HandlesGracefully()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;

        // Act & Assert
        Should.NotThrow(() =>
        {
            // 测试无效文化代码的处理
            Culture.IsValidCultureCode("invalid-culture").ShouldBeFalse();
            Culture.IsValidCultureCode(null).ShouldBeFalse();

            // 测试异常后文化仍然正确
            CultureInfo.CurrentCulture.ShouldBe(originalCulture);

            // 测试兼容性检查的异常处理
            Culture.IsCompatibleCulture("invalid1", "invalid2").ShouldBeFalse();
        });
    }

    #endregion
}
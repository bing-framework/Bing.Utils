using System.Globalization;
using Bing.Helpers;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// 区域文化测试
/// </summary>
public class CultureTest
{
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
}
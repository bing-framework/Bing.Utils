using System.ComponentModel;

namespace Bing.Helpers;

/// <summary>
/// 测试类：`Conv.ToDictionary` 回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class ConvToDictionaryRegressionTest
{
    /// <summary>
    /// 测试用例：切换 `useDisplayName` 时，应按约定选择字典键名（回归：显示名键选择错误）
    /// </summary>
    [Theory]
    [MemberData(nameof(GetUseDisplayNameCases))]
    [Trait("DefectPattern", "Helpers.Conv.ToDictionary.DisplayName")]
    public void ToDictionary_ObjectInput_UseDisplayNameFlag_ReturnsExpectedKeys(
        bool useDisplayName,
        string expectedDescriptionKey,
        string expectedDisplayKey)
    {
        var sample = new ToDictionarySample
        {
            DescriptionValue = "desc",
            DisplayValue = "display",
            PlainValue = 7
        };

        var dict = Conv.ToDictionary(sample, useDisplayName);

        dict.ContainsKey(expectedDescriptionKey).ShouldBeTrue();
        dict[expectedDescriptionKey].ShouldBe("desc");
        dict.ContainsKey(expectedDisplayKey).ShouldBeTrue();
        dict[expectedDisplayKey].ShouldBe("display");
        dict.ContainsKey(nameof(ToDictionarySample.PlainValue)).ShouldBeTrue();
        dict[nameof(ToDictionarySample.PlainValue)].ShouldBe(7);
    }

    /// <summary>
    /// 测试用例：输入为 `null` 时，应返回空字典而不是 `null`（回归：空引用契约）
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "Helpers.Conv.ToDictionary.NullContract")]
    public void ToDictionary_InputNull_ShouldReturnEmptyDictionary()
    {
        var dict = Conv.ToDictionary(null);

        dict.ShouldNotBeNull();
        dict.Count.ShouldBe(0);
    }

    /// <summary>
    /// 测试数据：`useDisplayName` 开关对应的预期键名
    /// </summary>
    /// <returns>供对应测试使用的参数数据行。</returns>
    public static IEnumerable<object[]> GetUseDisplayNameCases()
    {
        yield return new object[] { true, "描述名", "显示名" };
        yield return new object[] { false, nameof(ToDictionarySample.DescriptionValue), nameof(ToDictionarySample.DisplayValue) };
    }

    /// <summary>
    /// 用于验证字典键按名称特性选择规则生成的样例。
    /// </summary>
    private sealed class ToDictionarySample
    {
        /// <summary>
        /// 获取或初始化带有描述名的样例值。
        /// </summary>
        [Description("描述名")]
        public string DescriptionValue { get; init; }

        /// <summary>
        /// 获取或初始化带有显示名的样例值。
        /// </summary>
        [DisplayName("显示名")]
        public string DisplayValue { get; init; }

        /// <summary>
        /// 获取或初始化未配置名称特性的样例值。
        /// </summary>
        public int PlainValue { get; init; }
    }
}

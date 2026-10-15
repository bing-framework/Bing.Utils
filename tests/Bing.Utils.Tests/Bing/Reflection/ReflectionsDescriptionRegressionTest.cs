using System.ComponentModel;

namespace Bing.Reflection;

/// <summary>
/// 验证可空枚举成员描述的读取。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class ReflectionsDescriptionRegressionTest
{
    /// <summary>
    /// 验证可空枚举按底层枚举类型读取成员描述。
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "Reflection.Reflections.NullableEnumDescription")]
    public void GetDescription_NullableEnumMember_ShouldReturnMemberDescription()
    {
        var description = Reflections.GetDescription<RegressionState?>(nameof(RegressionState.Passed));

        description.ShouldBe("已通过");
    }

    /// <summary>
    /// 提供带描述特性的枚举测试值。
    /// </summary>
    private enum RegressionState
    {
        /// <summary>
        /// 用于验证描述读取的已通过测试值。
        /// </summary>
        [Description("已通过")]
        Passed = 1
    }
}

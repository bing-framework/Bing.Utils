using System.ComponentModel;

namespace Bing.Reflection;

/// <summary>
/// 测试类：Reflections 描述读取回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class ReflectionsDescriptionRegressionTest
{
    /// <summary>
    /// 测试用例：可空枚举类型读取成员描述时，应回退到底层枚举类型
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "Reflection.Reflections.NullableEnumDescription")]
    public void GetDescription_NullableEnumMember_ShouldReturnMemberDescription()
    {
        var description = Reflections.GetDescription<RegressionState?>(nameof(RegressionState.Passed));

        description.ShouldBe("已通过");
    }

    private enum RegressionState
    {
        [Description("已通过")]
        Passed = 1
    }
}

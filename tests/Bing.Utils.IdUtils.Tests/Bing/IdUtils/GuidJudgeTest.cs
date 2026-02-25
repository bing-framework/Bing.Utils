namespace Bing.IdUtils;
/// <summary>
/// 测试类：覆盖 `GuidJudge` 相关行为。
/// </summary>
[Trait("IdUtilsUT", "GuidJudge")]
public class GuidJudgeTest
{
    /// <summary>
    /// 测试用例：验证 `IsNullOrEmpty` 在 `Guid` 场景下，结果为 `ShouldReturnExpected`。
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_Guid_ShouldReturnExpected()
    {
        GuidJudge.IsNullOrEmpty(Guid.Empty).ShouldBeTrue();
        GuidJudge.IsNullOrEmpty(Guid.NewGuid()).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsNullOrEmpty` 在 `NullableGuid` 场景下，结果为 `ShouldReturnExpected`。
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_NullableGuid_ShouldReturnExpected()
    {
        Guid? value = null;
        GuidJudge.IsNullOrEmpty(value).ShouldBeTrue();
        GuidJudge.IsNullOrEmpty(Guid.Empty).ShouldBeTrue();
        GuidJudge.IsNullOrEmpty(Guid.NewGuid()).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsValid` 在 `ValidInputs` 场景下，结果为 `ShouldReturnTrue`。
    /// </summary>
    [Theory]
    [InlineData("6F9619FF-8B86-D011-B42D-00CF4FC964FF")]
    [InlineData("{6F9619FF-8B86-D011-B42D-00CF4FC964FF}")]
    [InlineData("6f9619ff8b86d011b42d00cf4fc964ff")]
    public void IsValid_ValidInputs_ShouldReturnTrue(string input)
    {
        GuidJudge.IsValid(input).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsValid` 在 `InvalidInputs` 场景下，结果为 `ShouldReturnFalse`。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-guid")]
    [InlineData("6F9619FF-8B86-D011-B42D-00CF4FC964F")]
    public void IsValid_InvalidInputs_ShouldReturnFalse(string input)
    {
        GuidJudge.IsValid(input).ShouldBeFalse();
    }
}


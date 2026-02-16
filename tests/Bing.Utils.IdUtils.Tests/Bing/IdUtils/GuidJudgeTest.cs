namespace Bing.IdUtils;

[Trait("IdUtilsUT", "GuidJudge")]
public class GuidJudgeTest
{
    [Fact]
    public void IsNullOrEmpty_Guid_ShouldReturnExpected()
    {
        GuidJudge.IsNullOrEmpty(Guid.Empty).ShouldBeTrue();
        GuidJudge.IsNullOrEmpty(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void IsNullOrEmpty_NullableGuid_ShouldReturnExpected()
    {
        Guid? value = null;
        GuidJudge.IsNullOrEmpty(value).ShouldBeTrue();
        GuidJudge.IsNullOrEmpty(Guid.Empty).ShouldBeTrue();
        GuidJudge.IsNullOrEmpty(Guid.NewGuid()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("6F9619FF-8B86-D011-B42D-00CF4FC964FF")]
    [InlineData("{6F9619FF-8B86-D011-B42D-00CF4FC964FF}")]
    [InlineData("6f9619ff8b86d011b42d00cf4fc964ff")]
    public void IsValid_ValidInputs_ShouldReturnTrue(string input)
    {
        GuidJudge.IsValid(input).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-guid")]
    [InlineData("6F9619FF-8B86-D011-B42D-00CF4FC964F")] // 少一位
    public void IsValid_InvalidInputs_ShouldReturnFalse(string input)
    {
        GuidJudge.IsValid(input).ShouldBeFalse();
    }
}

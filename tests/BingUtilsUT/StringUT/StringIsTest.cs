using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Is")]
public class StringIsTest
{
    /// <summary>
    /// 测试 - 是否为大写
    /// </summary>
    [Fact]
    public void Test_IsUpper()
    {
        Strings.IsUpper("").ShouldBeTrue();
        Strings.IsUpper("a").ShouldBeFalse();
        Strings.IsUpper("A").ShouldBeTrue();
        Strings.IsUpper("aa").ShouldBeFalse();
        Strings.IsUpper("AA").ShouldBeTrue();
        Strings.IsUpper("aA").ShouldBeFalse();
        Strings.IsUpper("Aa").ShouldBeFalse();
        Strings.IsUpper("a123").ShouldBeFalse();
        Strings.IsUpper("A123").ShouldBeTrue();
        Strings.IsUpper("aa123").ShouldBeFalse();
        Strings.IsUpper("AA123").ShouldBeTrue();
        Strings.IsUpper("aA123").ShouldBeFalse();
        Strings.IsUpper("Aa123").ShouldBeFalse();
        Strings.IsUpper("a°").ShouldBeFalse();
        Strings.IsUpper("A°").ShouldBeTrue();
        Strings.IsUpper("aa°").ShouldBeFalse();
        Strings.IsUpper("AA°").ShouldBeTrue();
        Strings.IsUpper("aA°").ShouldBeFalse();
        Strings.IsUpper("Aa°").ShouldBeFalse();
        Strings.IsUpper("a ").ShouldBeFalse();
        Strings.IsUpper("A ").ShouldBeTrue();
        Strings.IsUpper("a a").ShouldBeFalse();
        Strings.IsUpper("A A").ShouldBeTrue();
        Strings.IsUpper("a A").ShouldBeFalse();
        Strings.IsUpper("A a").ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 是否为小写
    /// </summary>
    [Fact]
    public void Test_IsLower()
    {
        Strings.IsLower("").ShouldBeTrue();
        Strings.IsLower("a").ShouldBeTrue();
        Strings.IsLower("A").ShouldBeFalse();
        Strings.IsLower("aa").ShouldBeTrue();
        Strings.IsLower("AA").ShouldBeFalse();
        Strings.IsLower("aA").ShouldBeFalse();
        Strings.IsLower("Aa").ShouldBeFalse();
        Strings.IsLower("a123").ShouldBeTrue();
        Strings.IsLower("A123").ShouldBeFalse();
        Strings.IsLower("aa123").ShouldBeTrue();
        Strings.IsLower("AA123").ShouldBeFalse();
        Strings.IsLower("aA123").ShouldBeFalse();
        Strings.IsLower("Aa123").ShouldBeFalse();
        Strings.IsLower("a°").ShouldBeTrue();
        Strings.IsLower("A°").ShouldBeFalse();
        Strings.IsLower("aa°").ShouldBeTrue();
        Strings.IsLower("AA°").ShouldBeFalse();
        Strings.IsLower("aA°").ShouldBeFalse();
        Strings.IsLower("Aa°").ShouldBeFalse();
        Strings.IsLower("a ").ShouldBeTrue();
        Strings.IsLower("A ").ShouldBeFalse();
        Strings.IsLower("a a").ShouldBeTrue();
        Strings.IsLower("A A").ShouldBeFalse();
        Strings.IsLower("a A").ShouldBeFalse();
        Strings.IsLower("A a").ShouldBeFalse();
    }
}
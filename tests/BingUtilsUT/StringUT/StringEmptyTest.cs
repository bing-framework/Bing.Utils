using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Empty")]
public class StringEmptyTest
{
    /// <summary>
    /// 测试 - 将 null 转换为 Empty
    /// </summary>
    [Fact]
    public void Test_AvoidNull()
    {
        string a = "";
        string b = null;
        string c = "a";
        string d = " ";

        Strings.AvoidNull(a).ShouldBe("");
        Strings.AvoidNull(b).ShouldBeEmpty();
        Strings.AvoidNull(c).ShouldBe("a");
        Strings.AvoidNull(d).ShouldBe(" ");
    }

    /// <summary>
    /// 测试 - 将 null 转换为 Empty
    /// </summary>
    [Fact]
    public void Test_NullToEmpty()
    {
        string a = "";
        string b = null;
        string c = "a";
        string d = " ";

        Strings.NullToEmpty(a).ShouldBe("");
        Strings.NullToEmpty(b).ShouldBeEmpty();
        Strings.NullToEmpty(c).ShouldBe("a");
        Strings.NullToEmpty(d).ShouldBe(" ");
    }

    /// <summary>
    /// 测试 - 将 Empty 转换为 null
    /// </summary>
    [Fact]
    public void Test_EmptyToNull()
    {
        string a = "";
        string b = null;
        string c = "a";
        string d = " ";
            
        Strings.EmptyToNull(a).ShouldBeNull();
        Strings.EmptyToNull(b).ShouldBeNull();
        Strings.EmptyToNull(c).ShouldBe("a");
        Strings.EmptyToNull(d).ShouldBe(" ");
    }
}
using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Hide")]
public class StringHideTest
{
    /// <summary>
    /// 测试 - 脱敏 - 检查null
    /// </summary>
    [Fact]
    public void Test_Hide_CheckNull()
    {
        Strings.Hide(null,0,1).ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 脱敏 - 检查空字符串
    /// </summary>
    [Fact]
    public void Test_Hide_CheckEmpty()
    {
        Strings.Hide(string.Empty,0,1).ShouldBeEmpty();
        Strings.Hide("",0,1).ShouldBeEmpty();
        Strings.Hide(" ",0,1).ShouldBeEmpty();
    }

    /// <summary>
    /// 测试 - 脱敏
    /// </summary>
    [Theory]
    [InlineData("jianxuanbing@126.com",-1,4,"****xuanbing@126.com")]
    [InlineData("jianxuanbing@126.com",2,3,"ji*nxuanbing@126.com")]
    [InlineData("jianxuanbing@126.com",3,2,"jianxuanbing@126.com")]
    [InlineData("jianxuanbing@126.com",20,20,"jianxuanbing@126.com")]
    [InlineData("jianxuanbing@126.com",20,21,"jianxuanbing@126.com")]
    public void Test_Hide(string input, int start, int end, string result)
    {
        Strings.Hide(input,start,end).ShouldBe(result);
    }
}
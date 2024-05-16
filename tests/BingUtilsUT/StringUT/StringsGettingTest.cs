using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Get")]
public class StringsGettingTest
{
    /// <summary>
    /// 测试 - 只获取字母和数字
    /// </summary>
    [Fact]
    public void Test_GetNumbersAndLetters()
    {
        Strings.GetNumbersAndLetters("abcd1234").ShouldBe("abcd1234");
    }

    /// <summary>
    /// 测试 - 只获取数字
    /// </summary>
    [Fact]
    public void Test_GetNumbers()
    {
        Strings.GetNumbers("abcd1234").ShouldBe("1234");
    }

    /// <summary>
    /// 测试 - 只获取字母
    /// </summary>
    [Fact]
    public void Test_GetLetters()
    {
        Strings.GetLetters("abcd1234").ShouldBe("abcd");
    }
}
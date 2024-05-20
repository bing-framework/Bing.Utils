using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Has")]
public class StringHasTest
{
    /// <summary>
    /// 测试 - 是否包含数字
    /// </summary>
    [Fact]
    public void Test_HasNumbers()
    {
        Strings.HasNumbers("").ShouldBeFalse();
        Strings.HasNumbers("abcdefg").ShouldBeFalse();
        Strings.HasNumbers("abcdefg0").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 是否包含指定数量的数字
    /// </summary>
    [Fact]
    public void Test_HasNumbersAtLeast()
    {
        Strings.HasNumbersAtLeast("abcdefg", 0).ShouldBeFalse();
        Strings.HasNumbersAtLeast("abcdefg", -1).ShouldBeFalse();
        Strings.HasNumbersAtLeast("abcdefg0", 0).ShouldBeTrue();
        Strings.HasNumbersAtLeast("abcdefg0", 1).ShouldBeTrue();
        Strings.HasNumbersAtLeast("abcdefg0", 2).ShouldBeFalse();
    }
}
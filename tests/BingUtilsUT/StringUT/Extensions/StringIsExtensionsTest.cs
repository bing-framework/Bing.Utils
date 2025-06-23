using Bing.Text;

namespace BingUtilsUT.StringUT.Extensions;

[Trait("StringUT.Extensions", "String.Is")]
public class StringIsExtensionsTest
{
    /// <summary>
    /// 测试 - 通配符比较
    /// </summary>
    [Fact]
    public void Test_IsLike()
    {
        // 基本匹配
        "hello".IsLike("hello").ShouldBeTrue();
        "hello".IsLike("h*").ShouldBeTrue();
        "hello".IsLike("*o").ShouldBeTrue();
        "hello".IsLike("h*o").ShouldBeTrue();
        "hello".IsLike("*ello").ShouldBeTrue();
        "hello".IsLike("*").ShouldBeTrue();

        // 不匹配
        "hello".IsLike("world").ShouldBeFalse();
        "hello".IsLike("h*a").ShouldBeFalse();
        "hello".IsLike("a*").ShouldBeFalse();
        "hello".IsLike("*a").ShouldBeFalse();

        // 边界情况
        "".IsLike("").ShouldBeTrue();
        "".IsLike("*").ShouldBeTrue();
        "hello".IsLike("hello*").ShouldBeTrue();
        "hello".IsLike("*hello").ShouldBeTrue();
        "hello".IsLike("*hello*").ShouldBeTrue();

        // 复杂情况
        "hello world".IsLike("h*world").ShouldBeTrue();
        "hello world".IsLike("*world").ShouldBeTrue();
        "hello world".IsLike("*o*").ShouldBeTrue();
        "hello world".IsLike("*o*d").ShouldBeTrue();
        "hello world".IsLike("h*o*d").ShouldBeTrue();

        // 参数验证
        Should.Throw<ArgumentNullException>(() => ((string)null).IsLike("*"));
        Should.Throw<ArgumentNullException>(() => "hello".IsLike(null));
    }

    /// <summary>
    /// 测试 - 任意模式通配符比较
    /// </summary>
    [Fact]
    public void Test_IsLikeAny()
    {
        // 基本匹配
        "hello".IsLikeAny("h*", "world").ShouldBeTrue();
        "hello".IsLikeAny("hello", "world").ShouldBeTrue();
        "hello".IsLikeAny("*o", "world").ShouldBeTrue();
        "hello".IsLikeAny("a*", "*o").ShouldBeTrue();
        "hello".IsLikeAny("*").ShouldBeTrue();

        // 不匹配
        "hello".IsLikeAny("a*", "b*").ShouldBeFalse();
        "hello".IsLikeAny().ShouldBeFalse(); // 空参数数组
        "hello".IsLikeAny(null).ShouldBeFalse(); // null参数数组

        // 边界情况
        "".IsLikeAny("*", "a").ShouldBeTrue();
        "".IsLikeAny("").ShouldBeTrue();
        "".IsLikeAny("a", "b").ShouldBeFalse();

        // 复杂情况
        "hello world".IsLikeAny("h*world", "test").ShouldBeTrue();
        "hello world".IsLikeAny("h*z", "*world").ShouldBeTrue();
        "hello world".IsLikeAny("a*", "b*", "h*").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 通配符比较性能（递归调用深度）
    /// </summary>
    [Fact]
    public void Test_IsLike_Performance()
    {
        // 长字符串与复杂模式的匹配
        string longString = new string('a', 1000);

        // 模式在开头有通配符，应该能快速处理
        longString.IsLike("*aaa").ShouldBeTrue();

        // 模式在末尾有通配符，应该能快速处理 
        longString.IsLike("aaa*").ShouldBeTrue();

        // 完全匹配模式
        longString.IsLike(longString).ShouldBeTrue();

        // 完全不匹配
        longString.IsLike("b*").ShouldBeFalse();
    }
}
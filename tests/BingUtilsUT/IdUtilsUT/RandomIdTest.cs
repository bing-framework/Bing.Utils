using Bing.IdUtils;

namespace BingUtilsUT.IdUtilsUT;

/// <summary>
/// 随机ID 测试
/// </summary>
[Trait("IdUtilsUT", "RandomIdTest")]
public class RandomIdTest
{
    /// <summary>
    /// 测试输出帮助类
    /// </summary>
    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// 测试初始化
    /// </summary>
    public RandomIdTest(ITestOutputHelper output)
    {
        Output = output;
    }

    /// <summary>
    /// 测试 - 随机ID生成
    /// </summary>
    [Fact]
    public void Test_RandomId()
    {
        var id1 = RandomIdGenerator.Create(16); 
        var id2 = RandomIdGenerator.Create(16);

        id1.ShouldNotBeNullOrEmpty();
        id2.ShouldNotBeNullOrEmpty();

        id1.Length.ShouldBe(16);
        id2.Length.ShouldBe(16);

        id1.ShouldNotBe(id2);

        Output.WriteLine(id1);
        Output.WriteLine(id2);
    }

    /// <summary>
    /// 测试 - 随机字符串生成
    /// </summary>
    [Fact]
    public void Test_RandomNonceStr()
    {
        var nonceStr1 = RandomNonceStrGenerator.Create();
        var nonceStr2 = RandomNonceStrGenerator.Create();
        var nonceStr3 = RandomNonceStrGenerator.Create(true);
        var nonceStr4 = RandomNonceStrGenerator.Create(true);

        nonceStr1.ShouldNotBeNullOrEmpty();
        nonceStr2.ShouldNotBeNullOrEmpty();
        nonceStr3.ShouldNotBeNullOrEmpty();
        nonceStr4.ShouldNotBeNullOrEmpty();

        nonceStr1.Length.ShouldBe(16);
        nonceStr2.Length.ShouldBe(16);
        nonceStr3.Length.ShouldBe(16);
        nonceStr4.Length.ShouldBe(16);

        nonceStr1.ShouldNotBe(nonceStr2);
        nonceStr3.ShouldNotBe(nonceStr4);

        Output.WriteLine(nonceStr1);
        Output.WriteLine(nonceStr2);
        Output.WriteLine(nonceStr3);
        Output.WriteLine(nonceStr4);
    }

    /// <summary>
    /// 测试 - 随机字符串生成 - 指定长度
    /// </summary>
    [Fact]
    public void Test_RandomNonceStr_With_Length()
    {
        var nonceStr1 = RandomNonceStrGenerator.Create(16);
        var nonceStr2 = RandomNonceStrGenerator.Create(16);
        var nonceStr3 = RandomNonceStrGenerator.Create(16,true);
        var nonceStr4 = RandomNonceStrGenerator.Create(16,true);

        nonceStr1.ShouldNotBeNullOrEmpty();
        nonceStr2.ShouldNotBeNullOrEmpty();
        nonceStr3.ShouldNotBeNullOrEmpty();
        nonceStr4.ShouldNotBeNullOrEmpty();

        nonceStr1.Length.ShouldBe(16);
        nonceStr2.Length.ShouldBe(16);
        nonceStr3.Length.ShouldBe(16);
        nonceStr4.Length.ShouldBe(16);

        nonceStr1.ShouldNotBe(nonceStr2);
        nonceStr3.ShouldNotBe(nonceStr4);

        Output.WriteLine(nonceStr1);
        Output.WriteLine(nonceStr2);
        Output.WriteLine(nonceStr3);
        Output.WriteLine(nonceStr4);
    }

    /// <summary>
    /// 测试 - 雪花算法ID生成
    /// </summary>
    [Fact]
    public void Test_SnowflakeGenerator()
    {
        var worker1 = SnowflakeGenerator.Create(1, 2, 17);
        var worker2 = SnowflakeGenerator.Create(2, 2, 17);

        var id11 = worker1.NextId();
        var id12 = worker1.NextId();

        var id21 = worker2.NextId();
        var id22 = worker2.NextId();

        id11.ShouldNotBe(id12);
        id11.ShouldNotBe(id22);
        id21.ShouldNotBe(id12);
        id21.ShouldNotBe(id22);

        Output.WriteLine(id11.ToString());
        Output.WriteLine(id12.ToString());
        Output.WriteLine(id21.ToString());
        Output.WriteLine(id22.ToString());
    }
}
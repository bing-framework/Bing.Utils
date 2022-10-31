using System.Collections.Generic;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Collections;

[Trait("ConvUT","Dict")]
public class DictConvTests
{
    /// <summary>
    /// 测试 - 将字典转换为其它
    /// </summary>
    [Fact]
    public void Test_DictConv_CastOneDictToAnother()
    {
        var source = new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } }.AsReadOnlyDictionary();
        var target = DictConv.Cast<int, int, int, dynamic>(source);

        target.ShouldNotBeNull();
        target.Count.ShouldBe(3);
    }

    /// <summary>
    /// 测试 - 将字典转换为其它【扩展方法】
    /// </summary>
    [Fact]
    public void Test_ExtensionMethods_For_DictConv_CastOneDictToAnother()
    {
        var source = new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } }.AsReadOnlyDictionary();
        var target = source.Cast<int, int, int, dynamic>();

        target.ShouldNotBeNull();
        target.Count.ShouldBe(3);
    }
}
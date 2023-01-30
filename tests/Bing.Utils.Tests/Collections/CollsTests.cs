using System.Collections.Generic;
using System.Linq;
using Bing.Collections;

namespace Bing.Utils.Tests.Collections;

[Trait("CollUT","Colls")]
public class CollsTests
{
    /// <summary>
    /// 测试 - 添加列表
    /// </summary>
    [Fact]
    public void Test_Colls_AddOneListIntoAnother()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var other = new List<int> { 6, 7, 8, 9, 10 };

        var target = Colls.AddRange(list, other).ToList();

        list.Count.ShouldBe(5);
        target.Count.ShouldBe(10);
        other.Count.ShouldBe(5);

        target[0].ShouldBe(1);
        target[1].ShouldBe(2);
        target[2].ShouldBe(3);
        target[3].ShouldBe(4);
        target[4].ShouldBe(5);
        target[5].ShouldBe(6);
        target[6].ShouldBe(7);
        target[7].ShouldBe(8);
        target[8].ShouldBe(9);
        target[9].ShouldBe(10);
    }

    /// <summary>
    /// 测试 - 添加列表【扩展方法】
    /// </summary>
    [Fact]
    public void Test_ExtensionMethods_For_Colls_AddOneListIntoAnother()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var other = new List<int> { 6, 7, 8, 9, 10 };

        var target = Colls.AddRange(list, other).ToList();
        list.Count.ShouldBe(5);
        target.Count.ShouldBe(10);
        other.Count.ShouldBe(5);

        target[0].ShouldBe(1);
        target[1].ShouldBe(2);
        target[2].ShouldBe(3);
        target[3].ShouldBe(4);
        target[4].ShouldBe(5);
        target[5].ShouldBe(6);
        target[6].ShouldBe(7);
        target[7].ShouldBe(8);
        target[8].ShouldBe(9);
        target[9].ShouldBe(10);
    }
}
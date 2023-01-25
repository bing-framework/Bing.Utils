using Bing.Collections;

namespace Bing.Utils.Tests.Collections;

/// <summary>
/// 数组捷径扩展
/// </summary>
[Trait("CollUT", "ArrayUT.Shortcut")]
public class ArrayShortcutTests
{
    /// <summary>
    /// 测试 - 二进制查询
    /// </summary>
    [Fact]
    public void Test_BinarySearch()
    {
        Array nice = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 10 };

        nice.BinarySearch(1).ShouldBe(0);
        nice.BinarySearch(2).ShouldBe(1);
        nice.BinarySearch(3).ShouldBe(2);
        nice.BinarySearch(4).ShouldBe(3);
        nice.BinarySearch(5).ShouldBe(4);
        nice.BinarySearch(6).ShouldBe(5);
        nice.BinarySearch(7).ShouldBe(6);
        nice.BinarySearch(8).ShouldBe(7);
        nice.BinarySearch(9).ShouldBe(8);
        nice.BinarySearch(0).ShouldBe(-1);
        nice.BinarySearch(10).ShouldBe(10);
        nice.BinarySearch(11).ShouldBe(-12);

        nice.BinarySearch(1, 2, 1).ShouldBe(-2);
        nice.BinarySearch(1, 2, 2).ShouldBe(1);
        nice.BinarySearch(1, 2, 3).ShouldBe(2);
        nice.BinarySearch(1, 2, 4).ShouldBe(-4);
        nice.BinarySearch(1, 2, 5).ShouldBe(-4);
    }

    /// <summary>
    /// 测试 - 清空
    /// </summary>
    [Fact]
    public void Test_Clear()
    {
        var nice = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 10 };

        nice.ShouldNotBeEmpty();
        nice.Clear();
        nice.Length.ShouldBe(11);
        nice[0].ShouldBe(0);
        nice[1].ShouldBe(0);
        nice[2].ShouldBe(0);
        nice[3].ShouldBe(0);
        nice[4].ShouldBe(0);
        nice[5].ShouldBe(0);
        nice[6].ShouldBe(0);
        nice[7].ShouldBe(0);
        nice[8].ShouldBe(0);
        nice[9].ShouldBe(0);
        nice[10].ShouldBe(0);

        nice = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 10 };

        nice.ShouldNotBeEmpty();
        nice.Clear(2, 4);
        nice.ShouldNotBeEmpty();
        nice.Length.ShouldBe(11);
        nice[0].ShouldBe(1);
        nice[1].ShouldBe(2);
        nice[2].ShouldBe(0);
        nice[3].ShouldBe(0);
        nice[4].ShouldBe(0);
        nice[5].ShouldBe(0);
        nice[6].ShouldBe(7);
        nice[7].ShouldBe(8);
        nice[8].ShouldBe(9);
        nice[9].ShouldBe(0);
        nice[10].ShouldBe(10);
    }

    /// <summary>
    /// 测试 - 查找
    /// </summary>
    [Fact]
    public void Test_FindAll()
    {
        var nice = new[] { 1, 2, 3, 4, 5, 6, 7 };
        var val = nice.FindAll(t => t >= 6);

        val.ShouldNotBeNull();
        val.ShouldNotBeEmpty();
        val.Length.ShouldBe(2);
        val[0].ShouldBe(6);
        val[1].ShouldBe(7);
    }

    /// <summary>
    /// 测试 - 获取指定对象的索引
    /// </summary>
    [Fact]
    public void Test_IndexOf()
    {
        Array nice = new[] { 1, 2, 3, 4, 5, 6, 7 };
        nice.IndexOf(1).ShouldBe(0);
        nice.IndexOf(8).ShouldBe(-1);
        nice.IndexOf(2).ShouldBe(1);
        nice.IndexOf(2, 1).ShouldBe(1);
        nice.IndexOf(8, 1).ShouldBe(-1);
        nice.IndexOf(1, 1, 1).ShouldBe(-1);
        nice.IndexOf(1, 1, 2).ShouldBe(-1);
        nice.IndexOf(1, 1, 1).ShouldBe(-1);
        nice.IndexOf(1, 1, 1).ShouldBe(-1);
    }

    /// <summary>
    /// 测试 - 获取指定对象的最后索引
    /// </summary>
    [Fact]
    public void Test_LastIndexOf()
    {
        Array nice = new[] { 1, 2, 3, 4, 5, 6, 7 };
        nice.LastIndexOf(1).ShouldBe(0);
        nice.LastIndexOf(8).ShouldBe(-1);
        nice.LastIndexOf(2).ShouldBe(1);
        nice.LastIndexOf(2, 1).ShouldBe(1);
        nice.LastIndexOf(8, 1).ShouldBe(-1);
        nice.LastIndexOf(1, 1, 1).ShouldBe(-1);
        nice.LastIndexOf(1, 1, 2).ShouldBe(0);
        nice.LastIndexOf(1, 1, 1).ShouldBe(-1);
        nice.LastIndexOf(1, 1, 1).ShouldBe(-1);
    }

    /// <summary>
    /// 测试 - 反转
    /// </summary>
    [Fact]
    public void Test_Reverse()
    {
        Array nice1 = new[] { 1, 2, 3, 4, 5, 6, 7 };
        Array nice2 = new[] { 7, 6, 5, 4, 3, 2, 1 };
        nice1.Reverse();

        Assert.Equal(nice2, nice1);
        Assert.NotStrictEqual(nice2, nice1);

        nice1 = new[] { 1, 2, 3, 4, 5, 6, 7 };
        nice2 = new[] { 1, 2, 3, 6, 5, 4, 7 };
        nice1.Reverse(3, 3);

        Assert.Equal(nice2, nice1);
        Assert.NotStrictEqual(nice2, nice1);
    }

    /// <summary>
    /// 测试 - 排序
    /// </summary>
    [Fact]
    public void Test_Sort()
    {
        Array nice1 = new[] { 4, 3, 5, 6, 1, 2, 7 };
        Array nice2 = new[] { 1, 2, 3, 4, 5, 6, 7 };
        nice1.Sort();

        Assert.Equal(nice2, nice1);
        Assert.NotStrictEqual(nice2, nice1);
    }

    /// <summary>
    /// 测试 - 字节操作
    /// </summary>
    [Fact]
    public void Test_ByteOps()
    {
        Array nice1 = new[] { (byte)4, (byte)3, (byte)5, (byte)6, (byte)1, (byte)2, (byte)7 };

        nice1.ByteLength().ShouldBe(7);
        nice1.GetByte(6).ShouldBe((byte)7);

        nice1.SetByte(6, (byte)8);
        nice1.GetByte(6).ShouldBe((byte)8);
    }
}
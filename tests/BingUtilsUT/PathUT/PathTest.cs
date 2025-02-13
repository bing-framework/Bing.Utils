using Bing.Helpers;

namespace BingUtilsUT.PathUT;

/// <summary>
/// 路径测试
/// </summary>
[Trait("PathUT", "Common.GetPhysicalPath")]
public class PathTest
{
    /// <summary>
    /// 测试 - 获取物理路径
    /// </summary>
    [Fact]
    public void Test_GetPhysicalPath_1()
    {
        var path = Common.GetPhysicalPath("a/b.txt");
        var result = $"{System.AppContext.BaseDirectory}a/b.txt";
        Assert.Equal(result, path);
    }

    /// <summary>
    /// 测试 - 获取物理路径 - 以 / 开头
    /// </summary>
    [Fact]
    public void Test_GetPhysicalPath_2()
    {
        var path = Common.GetPhysicalPath("/a/b.txt");
        var result = $"{System.AppContext.BaseDirectory}a/b.txt";
        Assert.Equal(result, path);
    }

    /// <summary>
    /// 测试 - 获取物理路径 - 以 \ 开头
    /// </summary>
    [Fact]
    public void Test_GetPhysicalPath_3()
    {
        var path = Common.GetPhysicalPath("\\a\\b.txt");
        var result = $"{System.AppContext.BaseDirectory}a\\b.txt";
        Assert.Equal(result, path);
    }

    /// <summary>
    /// 测试 - 获取物理路径
    /// </summary>
    [Fact]
    public void Test_GetPhysicalPath_4()
    {
        var path = Common.GetPhysicalPath("~/a/b.txt");
        var result = $"{System.AppContext.BaseDirectory}a/b.txt";
        Assert.Equal(result, path);
    }
}
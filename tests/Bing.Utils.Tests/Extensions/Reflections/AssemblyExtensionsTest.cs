using Bing.Reflection;

namespace Bing.Utils.Tests.Extensions.Reflections;

/// <summary>
/// 程序集 扩展测试
/// </summary>
public class AssemblyExtensionsTest
{
    /// <summary>
    /// 测试获取程序集文件版本
    /// </summary>
    [Fact]
    public void Test_GetFileVersion_1()
    {
        var assembly = typeof(AssemblyExtensionsTest).Assembly;
        var result = AssemblyVisit.GetFileVersion(assembly);
        Assert.Equal("1.0.0.0", result.ToString());
    }

    /// <summary>
    /// 测试获取程序集产品版本
    /// </summary>
    [Fact]
    public void Test_GetProductVersion_1()
    {
        var assembly = typeof(AssemblyExtensionsTest).Assembly;
        var result = AssemblyVisit.GetProductVersion(assembly);
        Assert.Equal("1.0.0.0", result);
    }

}
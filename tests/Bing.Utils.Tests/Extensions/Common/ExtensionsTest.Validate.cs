using Bing.Extensions;
using Bing.Tests.XUnitHelpers;

// ReSharper disable once CheckNamespace
namespace Bing.Utils.Tests.Extensions;

/// <summary>
/// 系统扩展测试 - 验证扩展
/// </summary>
public partial class ExtensionsTest : TestBase
{
    /// <summary>
    /// 测试 - 检查空值，不为空则正常执行
    /// </summary>
    [Fact]
    public void Test_CheckNull()
    {
        var test = new object();
        test.CheckNull(nameof(test));
    }

    /// <summary>
    /// 测试 - 检查空值，值为null则抛出异常
    /// </summary>
    [Fact]
    public void Test_CheckNull_Null_Throw()
    {
        AssertHelper.Throws<ArgumentNullException>(() =>
        {
            object test = null;
            test.CheckNull("test");
        }, "test");
    }

    /// <summary>
    /// 测试 - 是否默认值
    /// </summary>
    [Fact]
    public void Test_IsDefault()
    {
        Assert.True(Guid.Empty.IsDefault());
        Assert.False(Guid.NewGuid().IsDefault());
    }
}
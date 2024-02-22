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
    /// 测试 - 是否空值 - 字符串
    /// </summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("a", false)]
    public void Test_IsEmpty_String(string value, bool result)
    {
        Assert.Equal(result, value.IsEmpty());
    }

    /// <summary>
    /// 测试 - 是否空值 - Guid
    /// </summary>
    [Fact]
    public void Test_IsEmpty_Guid()
    {
        Assert.True(Guid.Empty.IsEmpty());
        Assert.False(Guid.NewGuid().IsEmpty());
    }

    /// <summary>
    /// 测试 - 是否空值 - 可空Guid
    /// </summary>
    [Fact]
    public void Test_IsEmpty_Guid_Nullable()
    {
        Guid? value = null;
        Assert.True(value.IsEmpty());
        value = Guid.Empty;
        Assert.True(value.IsEmpty());
        value = Guid.NewGuid();
        Assert.False(value.IsEmpty());
    }

    /// <summary>
    /// 测试 - 是否空值 - 集合
    /// </summary>
    [Fact]
    public void Test_IsEmpty_List()
    {
        List<int> list = null;
        Assert.True(list.IsEmpty());
        list = new List<int>();
        Assert.True(list.IsEmpty());
        list.Add(1);
        Assert.False(list.IsEmpty());
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
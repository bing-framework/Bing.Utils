using Bing.Collections;

namespace Bing.Collections.Legacy;

/// <summary>
/// 空数组创建的兼容测试。
/// </summary>
[Trait("CollUT", "ArrayUT.Empty")]
public class ArrayEmptyTests
{
    /// <summary>
    /// 验证创建的空数组非空引用且不含元素。
    /// </summary>
    [Fact]
    public void Test_EmptyArray()
    {
        var array = Arrays.Empty<string>();

        array.ShouldNotBeNull();
        array.ShouldBeEmpty();
    }
}

using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Collections;

/// <summary>
/// 空数组
/// </summary>
[Trait("CollUT", "ArrayUT.Empty")]
public class ArrayEmptyTests
{
    /// <summary>
    /// 测试 - 空数组
    /// </summary>
    [Fact]
    public void Test_EmptyArray()
    {
        var array = Arrays.Empty<string>();

        array.ShouldNotBeNull();
        array.ShouldBeEmpty();
    }
}
using Bing.IdUtils;

namespace BingUtilsUT.IdUtilsUT;

/// <summary>
/// 模型 ID 访问器 测试
/// </summary>
[Trait("IdUtilsUT", "ModelIdAccessorTest")]
public class ModelIdAccessorTest
{
    /// <summary>
    /// 测试 - 获取下一个索引
    /// </summary>
    [Fact]
    public void Test_GetNextIndex()
    {
        var accessor = new ModelIdAccessor();

        accessor.GetNextIndex().ShouldBe(0);
        accessor.GetNextIndex().ShouldBe(1);
        accessor.GetNextIndex().ShouldBe(2);
        accessor.GetNextIndex().ShouldBe(3);
        accessor.GetNextIndex().ShouldBe(4);
        accessor.GetNextIndex().ShouldBe(5);
    }
}
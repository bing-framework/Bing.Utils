namespace Bing.IdUtils.Legacy;

/// <summary>
/// 测试类：从 <c>Bing.Utils.Tests.UnitTest1</c> 拆分出的 IdUtils 混合测试方法。
/// </summary>
public class UnitTest1IdUtilsMixedSplitTest : TestBase
{
    public UnitTest1IdUtilsMixedSplitTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试用例：批量创建顺序字符串风格 Guid，并输出结果（保留原测试意图）。
    /// </summary>
    [Fact]
    public void Test_Id()
    {
        for (int i = 0; i < 1000; i++)
        {
            var id = GuidProvider.Create(GuidStyle.SequentialAsStringStyle);
            Output.WriteLine($"{id}");
        }
    }
}

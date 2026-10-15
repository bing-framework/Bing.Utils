namespace Bing.IdUtils.Legacy;
/// <summary>
/// 跟踪ID 测试
/// </summary>
[Trait("IdUtilsUT", "TraceIdAccessor")]
public class TraceIdTest
{
    /// <summary>
    /// 获取当前测试的输出助手。
    /// </summary>
    protected ITestOutputHelper Output { get; }
    /// <summary>
    /// 初始化一个 <see cref="TraceIdTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">当前测试的输出助手。</param>
    public TraceIdTest(ITestOutputHelper output)
    {
        Output = output;
    }
    /// <summary>
    /// 测试 - 跟踪ID生成
    /// </summary>
    [Fact]
    public void Test_TraceIdAccessor()
    {
        var accessor1 = new TraceIdAccessor(null);
        var accessor2 = new TraceIdAccessor(null);
        var id1 = accessor1.GetTraceId();
        var id2 = accessor2.GetTraceId();
        id1.ShouldNotBe(id2);
        Output.WriteLine(id1);
        Output.WriteLine(id2);
    }
}

namespace Bing.CommandLine;
/// <summary>
/// 命令行执行器单元测试
/// </summary>
public class CommandLineExecutorTest
{
    /// <summary>
    /// 验证单个参数追加到命令调试文本。
    /// </summary>
    [Fact]
    public void Test_Arguments_1()
    {
        var command = new CommandLineExecutor().Command("dapr").Arguments("run");
        command.GetDebugText().ShouldBe("dapr run");
    }
    /// <summary>
    /// 验证可变参数按输入顺序追加到命令调试文本。
    /// </summary>
    [Fact]
    public void Test_Arguments_2()
    {
        var command = new CommandLineExecutor().Command("dapr").Arguments("run", "--app-id", "80");
        command.GetDebugText().ShouldBe("dapr run --app-id 80");
    }
    /// <summary>
    /// 验证列表参数按输入顺序追加到命令调试文本。
    /// </summary>
    [Fact]
    public void Test_Arguments_3()
    {
        var list = new List<string>
        {
            "run",
            "--app-id",
            "80"
        };
        var command = new CommandLineExecutor().Command("dapr").Arguments(list);
        command.GetDebugText().ShouldBe("dapr run --app-id 80");
    }
    /// <summary>
    /// 验证多次配置参数会累积追加。
    /// </summary>
    [Fact]
    public void Test_Arguments_4()
    {
        var list = new List<string> {
            "--app-id",
            "80"
        };
        var command = new CommandLineExecutor().Command("dapr").Arguments("run").Arguments(list);
        command.GetDebugText().ShouldBe("dapr run --app-id 80");
    }
    /// <summary>
    /// 验证条件为假时不追加命令参数。
    /// </summary>
    [Fact]
    public void Test_ArgumentsIf()
    {
        var command = new CommandLineExecutor().Command("dapr").ArgumentsIf(false, "run");
        command.GetDebugText().ShouldBe("dapr ");
    }
}

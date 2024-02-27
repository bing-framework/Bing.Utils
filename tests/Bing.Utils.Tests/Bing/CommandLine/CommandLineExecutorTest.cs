namespace Bing.CommandLine;

/// <summary>
/// 命令行执行器单元测试
/// </summary>
public class CommandLineExecutorTest
{
    /// <summary>
    /// 测试 - 设置命令参数 - 1个参数
    /// </summary>
    [Fact]
    public void Test_Arguments_1()
    {
        var command = new CommandLineExecutor().Command("dapr").Arguments("run");
        command.GetDebugText().ShouldBe("dapr run");
    }

    /// <summary>
    /// 测试 - 设置命令参数 - 多个参数
    /// </summary>
    [Fact]
    public void Test_Arguments_2()
    {
        var command = new CommandLineExecutor().Command("dapr").Arguments("run", "--app-id", "80");
        command.GetDebugText().ShouldBe("dapr run --app-id 80");
    }

    /// <summary>
    /// 测试 - 设置命令参数 - 数组参数
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
    /// 测试 - 设置命令参数 - 多次调用Arguments
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
    /// 测试 - 根据条件设置命令参数
    /// </summary>
    [Fact]
    public void Test_ArgumentsIf()
    {
        var command = new CommandLineExecutor().Command("dapr").ArgumentsIf(false, "run");
        command.GetDebugText().ShouldBe("dapr ");
    }
}
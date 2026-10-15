namespace Bing.IO;
/// <summary>
/// 沙箱 测试
/// </summary>
public class SandboxTest : TestBase
{
    /// <summary>
    /// 初始化一个 <see cref="SandboxTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">当前测试的输出助手。</param>
    public SandboxTest(ITestOutputHelper output) : base(output)
    {
    }
    /// <summary>
    /// 验证默认沙箱名称采用预定义前缀。
    /// </summary>
    [Fact]
    public void Test_DefaultSandboxShouldUseDefaultPrefix()
    {
        using var sandbox = new Sandbox();
        Output.WriteLine(sandbox.FullPath);
        Output.WriteLine(sandbox.Name);
        Sandbox.DefaultPrefix.ShouldBe("Sandbox-");
        sandbox.Name.ShouldStartWith(Sandbox.DefaultPrefix);
    }
    /// <summary>
    /// 验证沙箱内路径组合到沙箱根目录。
    /// </summary>
    [Fact]
    public void Test_SandboxShouldResolvePaths()
    {
        using var sandbox = new Sandbox();
        Output.WriteLine(sandbox.FullPath);
        Output.WriteLine(sandbox.Name);
        var path = sandbox.ResolvePath("some/path");
        Output.WriteLine(path);
        path.ShouldBe(Path.Join(sandbox.FullPath, "some/path"));
    }
    /// <summary>
    /// 验证沙箱创建指定的多级目录。
    /// </summary>
    [Fact]
    public void Test_SandboxShouldCreateDirectories()
    {
        using var sandbox = new Sandbox();
        Output.WriteLine(sandbox.FullPath);
        Output.WriteLine(sandbox.Name);
        var path = sandbox.CreateDirectory("path/to/dir");
        Output.WriteLine(path);
        Directory.Exists(path).ShouldBeTrue();
    }
    /// <summary>
    /// 验证沙箱创建指定的文件。
    /// </summary>
    [Fact]
    public void Test_SandboxShouldCreateFiles()
    {
        using var sandbox = new Sandbox();
        Output.WriteLine(sandbox.FullPath);
        Output.WriteLine(sandbox.Name);
        var path = sandbox.CreateFile("path/to/file");
        Output.WriteLine(path);
        File.Exists(path).ShouldBeTrue();
    }
    /// <summary>
    /// 验证沙箱创建含指定文本的文件。
    /// </summary>
    [Fact]
    public void Test_SandboxShouldCreateFilesWithText()
    {
        using var sandbox = new Sandbox();
        Output.WriteLine(sandbox.FullPath);
        Output.WriteLine(sandbox.Name);
        var path = sandbox.CreateFile("path/to/file", "mytext");
        Output.WriteLine(path);
        File.ReadAllText(path).ShouldBe("mytext");
    }
}

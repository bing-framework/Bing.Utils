using System.IO;
using Bing.IO;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.IO;

/// <summary>
/// 沙箱 测试
/// </summary>
public class SandboxTest : TestBase
{
    /// <summary>
    /// 初始化一个<see cref="SandboxTest"/>类型的实例
    /// </summary>
    public SandboxTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Test_DefaultSandboxShouldUseDefaultPrefix()
    {
        using var sandbox = new Sandbox();
        Output.WriteLine(sandbox.FullPath);
        Output.WriteLine(sandbox.Name);

        Sandbox.DefaultPrefix.ShouldBe("Sandbox-");
        sandbox.Name.ShouldStartWith(Sandbox.DefaultPrefix);
    }

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
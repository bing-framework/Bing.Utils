using System.IO;
using Bing.IO;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.IO;

/// <summary>
/// 临时目录 测试
/// </summary>
public class TempDirectoryTest : TestBase
{
    /// <summary>
    /// 初始化一个<see cref="TempDirectoryTest"/>类型的实例
    /// </summary>
    public TempDirectoryTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Test_TempDirectoryRemovesItself()
    {
        var tempDir = new TempDirectory();
        Output.WriteLine(tempDir.FullPath);
        Output.WriteLine(tempDir.Name);

        Directory.Exists(tempDir.FullPath).ShouldBeTrue();

        tempDir.Dispose();

        Directory.Exists(tempDir.FullPath).ShouldBeFalse();
    }

    [Fact]
    public void Test_TempDirectoryCanSetPrefix()
    {
        const string prefix = "MyPrefix-";
        using var tempDir = new TempDirectory(prefix);
        Output.WriteLine(tempDir.FullPath);
        Output.WriteLine(tempDir.Name);

        tempDir.Name.ShouldStartWith(prefix);
    }
}
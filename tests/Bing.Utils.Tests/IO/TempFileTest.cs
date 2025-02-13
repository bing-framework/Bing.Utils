using Bing.IO;

namespace Bing.Utils.Tests.IO;

/// <summary>
/// 临时文件 测试
/// </summary>
public class TempFileTest : TestBase
{
    /// <summary>
    /// 初始化一个<see cref="TempFileTest"/>类型的实例
    /// </summary>
    public TempFileTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Test_TempFileRemoveItself()
    {
        var tempFile = new TempFile();
        Output.WriteLine(tempFile.FullPath);
        Output.WriteLine(tempFile.Name);

        File.Exists(tempFile.FullPath).ShouldBeTrue();

        tempFile.Dispose();

        File.Exists(tempFile.FullPath).ShouldBeFalse();
    }

    [Fact]
    public void Test_TempFileCanSetPrefix()
    {
        const string prefix = "MyPrefix-";
        using var tempFile = new TempFile(prefix);
        Output.WriteLine(tempFile.FullPath);
        Output.WriteLine(tempFile.Name);

        tempFile.Name.ShouldStartWith(prefix);
    }
}
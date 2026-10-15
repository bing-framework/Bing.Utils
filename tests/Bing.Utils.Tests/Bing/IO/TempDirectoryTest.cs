namespace Bing.IO;
/// <summary>
/// 临时目录 测试
/// </summary>
public class TempDirectoryTest : TestBase
{
    /// <summary>
    /// 初始化一个 <see cref="TempDirectoryTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">当前测试的输出助手。</param>
    public TempDirectoryTest(ITestOutputHelper output) : base(output)
    {
    }
    /// <summary>
    /// 验证临时目录在释放后自动删除。
    /// </summary>
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
    /// <summary>
    /// 验证临时目录名称采用指定前缀。
    /// </summary>
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

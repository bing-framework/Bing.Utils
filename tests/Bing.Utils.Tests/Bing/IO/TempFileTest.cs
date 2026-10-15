namespace Bing.IO;
/// <summary>
/// 临时文件 测试
/// </summary>
public class TempFileTest : TestBase
{
    /// <summary>
    /// 初始化一个 <see cref="TempFileTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">当前测试的输出助手。</param>
    public TempFileTest(ITestOutputHelper output) : base(output)
    {
    }
    /// <summary>
    /// 验证临时文件在释放后自动删除。
    /// </summary>
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
    /// <summary>
    /// 验证临时文件名称采用指定前缀。
    /// </summary>
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

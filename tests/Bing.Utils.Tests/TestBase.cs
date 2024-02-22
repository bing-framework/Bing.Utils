namespace Bing.Utils.Tests;

/// <summary>
/// 测试基类
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// 输出
    /// </summary>
    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    protected TestBase(ITestOutputHelper output) => Output = output;

    /// <summary>
    /// 获取根路径
    /// </summary>
    protected string GetTestRootPath() => Directory.GetCurrentDirectory();

    /// <summary>
    /// 获取测试文件根路径
    /// </summary>
    /// <param name="paths">路径</param>
    protected string GetTestFilePath(params string[] paths)
    {
        var rootPath = GetTestRootPath();
        var list = new List<string>
        {
            rootPath
        };
        list.AddRange(paths);
        var result = Path.Combine(list.ToArray());
        Output.WriteLine($"文件路径: {result}");
        return result;
    }
}
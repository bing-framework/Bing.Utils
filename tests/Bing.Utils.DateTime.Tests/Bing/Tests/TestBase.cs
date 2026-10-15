using System.Collections.Generic;
using System.IO;

namespace Bing.Tests;

/// <summary>
/// 提供测试输出与测试文件路径访问的基类。
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// 获取当前测试使用的输出助手。
    /// </summary>
    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// 初始化一个 <see cref="TestBase"/> 类型的实例。
    /// </summary>
    /// <param name="output">当前测试的输出助手。</param>
    protected TestBase(ITestOutputHelper output) => Output = output;

    /// <summary>
    /// 获取测试运行的当前工作目录。
    /// </summary>
    /// <returns>当前工作目录的路径。</returns>
    protected string GetTestRootPath() => Directory.GetCurrentDirectory();

    /// <summary>
    /// 将路径片段与测试工作目录组合为文件路径。
    /// </summary>
    /// <param name="paths">按顺序组合的路径片段。</param>
    /// <returns>组合后的测试文件路径。</returns>
    /// <remarks>将组合结果写入测试输出。</remarks>
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

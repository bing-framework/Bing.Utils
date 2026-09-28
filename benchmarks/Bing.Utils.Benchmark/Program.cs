using BenchmarkDotNet.Running;

namespace Bing.Utils.Benchmark;

using Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 启动基准测试程序。
/// </summary>
internal class Program
{
    /// <summary>
    /// 处理基准测试命令行参数。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--conv-baseline", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunBaseline();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-memory", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunMemoryProbe();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-collection-compare", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunCollectionComparison();
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
